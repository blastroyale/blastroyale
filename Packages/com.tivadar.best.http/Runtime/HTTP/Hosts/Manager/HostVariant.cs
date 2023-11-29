using System;
using System.Collections.Generic;
using System.Globalization;

using Best.HTTP.Hosts.Connections;
using Best.HTTP.Hosts.Connections.File;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.Logger;

namespace Best.HTTP.HostSetting
{
    /// <summary>
    /// An enumeration representing the protocol support for a host.
    /// </summary>
    public enum HostProtocolSupport : byte
    {
        /// <summary>
        /// Protocol support is unknown or undetermined.
        /// </summary>
        Unknown = 0x00,

        /// <summary>
        /// The host supports HTTP/1.
        /// </summary>
        HTTP1 = 0x01,

        /// <summary>
        /// The host supports HTTP/2.
        /// </summary>
        HTTP2 = 0x02
    }

    /// <summary>
    /// <para>The HostVariant class is a critical component in managing HTTP connections and handling HTTP requests for a specific host. It maintains a queue of requests and a list of active connections associated with the host, ensuring efficient utilization of available resources. Additionally, it supports protocol version detection (HTTP/1 or HTTP/2) for optimized communication with the host.</para>
    /// <list type="bullet">
    ///     <item><description>It maintains a queue of requests to ensure efficient and controlled use of available connections.</description></item>
    ///     <item><description>It supports HTTP/1 and HTTP/2 protocol versions, allowing requests to be sent using the appropriate protocol based on the host's protocol support.</description></item>
    ///     <item><description>Provides methods for sending requests, recycling connections, managing connection state, and handling the shutdown of connections and the host variant itself.</description></item>
    ///     <item><description>It includes logging for diagnostic purposes, helping to monitor and debug the behavior of connections and requests.</description></item>
    /// </list>
    /// <para>In summary, the HostVariant class plays a central role in managing HTTP connections and requests for a specific host, ensuring efficient and reliable communication with that host while supporting different protocol versions.</para>
    /// </summary>
    public sealed class HostVariant
    {
        public HostKey Host { get; private set; }

        public HostProtocolSupport ProtocolSupport { get; private set; }

        public DateTime LastProtocolSupportUpdate { get; private set; }
        
        public LoggingContext Context { get; private set; }

        private List<ConnectionBase> Connections = new List<ConnectionBase>();
        private List<HTTPRequest> Queue = new List<HTTPRequest>();

        internal HostVariant(HostKey host)
        {
            this.Host = host;
            
            this.Context = new LoggingContext(this);
            this.Context.Add("Host", this.Host.Host);
        }

        internal void AddProtocol(HostProtocolSupport protocolSupport)
        {
            this.LastProtocolSupportUpdate = DateTime.Now;

            var oldProtocol = this.ProtocolSupport;

            if (oldProtocol != protocolSupport)
            {
                this.ProtocolSupport = protocolSupport;

                HTTPManager.Logger.Information(typeof(HostVariant).Name, $"AddProtocol({oldProtocol} => {protocolSupport})", this.Context);
            }

            // Request might be sitting in the queue when the server supports only http/1.
            //if (protocolSupport == HostProtocolSupport.HTTP2)
                TryToSendQueuedRequests();
        }

        internal HostVariant Send(HTTPRequest request)
        {
            var conn = GetNextAvailable(request);

            if (conn != null)
            {
                RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(request, HTTPRequestStates.Processing, null));

                // then start process the request
                conn.Process(request);
            }
            else
            {
                // If no free connection found and creation prohibited, we will put back to the queue
                this.Queue.Add(request);
            }

            return this;
        }

        internal HostVariant TryToSendQueuedRequests()
        {
            while (this.Queue.Count > 0 && HasAnyAvailableOrRoomForNew())
            {
                var nextRequest = this.Queue[0];

                // If the queue is large, or timeouts are set low, a request might be in a queue while its state is set to > Finished.
                //  So we have to prevent sending it again.

                if (nextRequest.State <= HTTPRequestStates.Queued)
                    Send(nextRequest);

                this.Queue.RemoveAt(0);
            }

            return this;
        }

        internal bool HasAnyAvailableOrRoomForNew()
        {
            int activeConnections = 0;
            ConnectionBase conn = null;
            // Check the last created connection first. This way, if a higher level protocol is present that can handle more requests (== HTTP/2) that protocol will be chosen
            //  and others will be closed when their inactivity time is reached.
            for (int i = Connections.Count - 1; i >= 0; --i)
            {
                conn = Connections[i];

                if (conn.State == HTTPConnectionStates.Initial || conn.State == HTTPConnectionStates.Free || conn.CanProcessMultiple)
                    return true;

                activeConnections++;
            }

            var maxOpenConnections = HTTPManager.PerHostSettings.Get(this).HostVariantSettings.MaxConnectionPerVariant;
            if (activeConnections >= maxOpenConnections)
                return false;

#if !BESTHTTP_DISABLE_ALTERNATE_SSL
            // Hold back the creation of a new connection until we know more about the remote host's features.
            // If we send out multiple requests at once it will execute the first and delay the others. 
            // While it will decrease performance initially, it will prevent the creation of TCP connections
            //  that will be unused after their first request processing if the server supports HTTP/2.
            if (activeConnections >= 1 && (this.ProtocolSupport == HostProtocolSupport.Unknown || this.ProtocolSupport == HostProtocolSupport.HTTP2))
                return false;
#endif

            return true;
        }

        internal ConnectionBase GetNextAvailable(HTTPRequest request)
        {
            int activeConnections = 0;
            ConnectionBase conn = null;
            // Check the last created connection first. This way, if a higher level protocol is present that can handle more requests (== HTTP/2) that protocol will be chosen
            //  and others will be closed when their inactivity time is reached.
            for (int i = Connections.Count - 1; i >= 0; --i)
            {
                conn = Connections[i];

                if (conn.State == HTTPConnectionStates.Initial || conn.State == HTTPConnectionStates.Free || conn.CanProcessMultiple)
                {
                    HTTPManager.Logger.Verbose(nameof(HostVariant), $"GetNextAvailable - returning with connection. state: {conn.State}, CanProcessMultiple: {conn.CanProcessMultiple}", this.Context);
                    return conn;
                }

                activeConnections++;
            }

            var maxOpenConnections = HTTPManager.PerHostSettings.Get(this).HostVariantSettings.MaxConnectionPerVariant;
            if (activeConnections >= maxOpenConnections)
            {
                HTTPManager.Logger.Verbose(nameof(HostVariant), $"GetNextAvailable - activeConnections({activeConnections}) >= MaxOpenConnections({maxOpenConnections})", this.Context);
                return null;
            }

            HostKey hostKey = HostKey.From(request);

            conn = null;

            if (request.CurrentUri.IsFile)
                conn = new FileConnection(hostKey);
            else
            {
#if UNITY_WEBGL && !UNITY_EDITOR
            conn = new Best.HTTP.Hosts.Connections.WebGL.WebGLXHRConnection(hostKey);
#else
#if !BESTHTTP_DISABLE_ALTERNATE_SSL
                // Hold back the creation of a new connection until we know more about the remote host's features.
                // If we send out multiple requests at once it will execute the first and delay the others. 
                // While it will decrease performance initially, it will prevent the creation of TCP connections
                //  that will be unused after their first request processing if the server supports HTTP/2.
                if (activeConnections >= 1 && (this.ProtocolSupport == HostProtocolSupport.Unknown || this.ProtocolSupport == HostProtocolSupport.HTTP2))
                {
                    HTTPManager.Logger.Verbose(nameof(HostVariant), $"GetNextAvailable - waiting for protocol support message. activeConnections: {activeConnections}, ProtocolSupport: {ProtocolSupport}", this.Context);
                    return null;
                }
#endif

                conn = new HTTPOverTCPConnection(hostKey);
                HTTPManager.Logger.Verbose(nameof(HostVariant), $"GetNextAvailable - creating new connection, key: {hostKey}", this.Context);
#endif
            }
            Connections.Add(conn);

            return conn;
        }

        internal HostVariant RecycleConnection(ConnectionBase conn)
        {
            conn.State = HTTPConnectionStates.Free;

            Best.HTTP.Shared.Extensions.Timer.Add(new TimerData(TimeSpan.FromSeconds(1), conn, CloseConnectionAfterInactivity));

            return this;
        }

        private bool RemoveConnectionImpl(ConnectionBase conn, HTTPConnectionStates setState)
        {
            HTTPManager.Logger.Information(typeof(HostVariant).Name, $"RemoveConnectionImpl({conn}, {setState})", this.Context);

            conn.State = setState;
            conn.Dispose();

            bool found = this.Connections.Remove(conn);

            if (!found) // 
                HTTPManager.Logger.Information(typeof(HostVariant).Name, $"RemoveConnectionImpl - Couldn't find connection! key: {conn.HostKey}", this.Context);

            return found;
        }

        internal HostVariant RemoveConnection(ConnectionBase conn, HTTPConnectionStates setState)
        {
            RemoveConnectionImpl(conn, setState);

            return this;
        }

        public ConnectionBase Find(Predicate<ConnectionBase> match) => this.Connections.Find(match);

        private bool CloseConnectionAfterInactivity(DateTime now, object context)
        {
            var conn = context as ConnectionBase;

            bool closeConnection = conn.State == HTTPConnectionStates.Free && now - conn.LastProcessTime >= conn.KeepAliveTime;
            if (closeConnection)
            {
                HTTPManager.Logger.Information(typeof(HostVariant).Name, string.Format("CloseConnectionAfterInactivity - [{0}] Closing! State: {1}, Now: {2}, LastProcessTime: {3}, KeepAliveTime: {4}",
                    conn.ToString(), conn.State, now.ToString(System.Globalization.CultureInfo.InvariantCulture), conn.LastProcessTime.ToString(System.Globalization.CultureInfo.InvariantCulture), conn.KeepAliveTime), this.Context);

                RemoveConnection(conn, HTTPConnectionStates.Closed);
                return false;
            }

            // repeat until the connection's state is free
            return conn.State == HTTPConnectionStates.Free;
        }

        public void RemoveAllIdleConnections()
        {
            for (int i = 0; i < this.Connections.Count; i++)
                if (this.Connections[i].State == HTTPConnectionStates.Free)
                {
                    int countBefore = this.Connections.Count;
                    RemoveConnection(this.Connections[i], HTTPConnectionStates.Closed);

                    if (countBefore != this.Connections.Count)
                        i--;
                }
        }

        internal void Shutdown()
        {
            this.Queue.Clear();

            foreach (var conn in this.Connections)
            {
                // Swallow any exceptions, we are quitting anyway.
                try
                {
                    conn.Shutdown(ShutdownTypes.Immediate);
                }
                catch { }
            }
            //this.Connections.Clear();
        }

        internal void SaveTo(System.IO.BinaryWriter bw)
        {
            bw.Write(this.LastProtocolSupportUpdate.ToBinary());
            bw.Write((byte)this.ProtocolSupport);
        }

        internal void LoadFrom(int version, System.IO.BinaryReader br)
        {
            this.LastProtocolSupportUpdate = DateTime.FromBinary(br.ReadInt64());
            this.ProtocolSupport = (HostProtocolSupport)br.ReadByte();

            if (DateTime.Now - this.LastProtocolSupportUpdate >= TimeSpan.FromDays(1))
            {
                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Verbose(nameof(HostVariant), $"LoadFrom - Too Old! LastProtocolSupportUpdate: {this.LastProtocolSupportUpdate.ToString(CultureInfo.InvariantCulture)}, ProtocolSupport: {this.ProtocolSupport}", this.Context);
                this.ProtocolSupport = HostProtocolSupport.Unknown;                
            }
            else if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Verbose(nameof(HostVariant), $"LoadFrom - LastProtocolSupportUpdate: {this.LastProtocolSupportUpdate.ToString(CultureInfo.InvariantCulture)}, ProtocolSupport: {this.ProtocolSupport}", this.Context);
        }

        public override string ToString() => $"{this.Host}, {this.Queue.Count}/{this.Connections?.Count}, {this.ProtocolSupport}";
    }
}

using System;
using System.Collections.Concurrent;

using Best.HTTP.HostSetting;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.Logger;

namespace Best.HTTP.Hosts.Connections
{
    public enum ConnectionEvents
    {
        StateChange,
        ProtocolSupport
    }

    public readonly struct ConnectionEventInfo
    {
        public readonly ConnectionBase Source;

        public readonly ConnectionEvents Event;

        public readonly HTTPConnectionStates State;

        public readonly HostProtocolSupport ProtocolSupport;

        public readonly HTTPRequest Request;

        public readonly HTTPRequestStates RequestState;

        public ConnectionEventInfo(ConnectionBase sourceConn, ConnectionEvents @event)
        {
            this.Source = sourceConn;
            this.Event = @event;

            this.State = HTTPConnectionStates.Initial;

            this.ProtocolSupport = HostProtocolSupport.Unknown;

            this.Request = null;
            this.RequestState = HTTPRequestStates.Initial;
        }

        public ConnectionEventInfo(ConnectionBase sourceConn, HTTPConnectionStates newState)
        {
            this.Source = sourceConn;

            this.Event = ConnectionEvents.StateChange;

            this.State = newState;

            this.ProtocolSupport = HostProtocolSupport.Unknown;

            this.Request = null;
            this.RequestState = HTTPRequestStates.Initial;
        }

        public ConnectionEventInfo(ConnectionBase sourceConn, HostProtocolSupport protocolSupport)
        {
            this.Source = sourceConn;
            this.Event = ConnectionEvents.ProtocolSupport;

            this.State = HTTPConnectionStates.Initial;

            this.ProtocolSupport = protocolSupport;

            this.Request = null;
            this.RequestState = HTTPRequestStates.Initial;
        }

        public ConnectionEventInfo(ConnectionBase sourceConn, HTTPRequest request)
        {
            this.Source = sourceConn;

            this.Event = ConnectionEvents.StateChange;

            this.State = HTTPConnectionStates.ClosedResendRequest;

            this.ProtocolSupport = HostProtocolSupport.Unknown;

            this.Request = request;
            this.RequestState = HTTPRequestStates.Initial;
        }

        public override string ToString()
        {
            switch(this.Event)
            {
                case ConnectionEvents.StateChange: return $"[ConnectionEventInfo Source: {this.Source.ToString()} To State: {this.State}]";
                case ConnectionEvents.ProtocolSupport: return $"[ConnectionEventInfo Source: {this.Source.ToString()} ProtocolSupport: {this.ProtocolSupport}]";
                default: return string.Format("[ConnectionEventInfo SourceConnection: {0}, Event: {1}, State: {2}, ProtocolSupport: {3}]", this.Source.ToString(), this.Event, this.State, this.ProtocolSupport);
            }
        }
    }

    public static class ConnectionEventHelper
    {
        private static ConcurrentQueue<ConnectionEventInfo> connectionEventQueue = new ConcurrentQueue<ConnectionEventInfo>();

#pragma warning disable 0649
        public static Action<ConnectionEventInfo> OnEvent;
#pragma warning restore

        public static void EnqueueConnectionEvent(ConnectionEventInfo @event)
        {
            if (HTTPManager.Logger.Level == Loglevels.All)
                HTTPManager.Logger.Information("ConnectionEventHelper", "Enqueue " + @event.ToString(), @event.Source.Context);

            connectionEventQueue.Enqueue(@event);
        }

        internal static void Clear()
        {
            connectionEventQueue.Clear();
        }

        internal static void ProcessQueue()
        {
            ConnectionEventInfo connectionEvent;
            while (connectionEventQueue.TryDequeue(out connectionEvent))
            {
                //if (HTTPManager.Logger.Level == Loglevels.All)
                //    HTTPManager.Logger.Information("ConnectionEventHelper", "Processing connection event: " + connectionEvent.ToString(), connectionEvent.Source.Context);

                if (OnEvent != null)
                {
                    try
                    {
                        OnEvent(connectionEvent);
                    }
                    catch (Exception ex)
                    {
                        HTTPManager.Logger.Exception("ConnectionEventHelper", "ProcessQueue", ex, connectionEvent.Source.Context);
                    }
                }

                if (connectionEvent.Source.LastProcessedUri == null)
                {
                    HTTPManager.Logger.Information("ConnectionEventHelper", String.Format("Ignoring ConnectionEventInfo({0}) because its LastProcessedUri is null!", connectionEvent.ToString()), connectionEvent.Source.Context);
                    return;
                }

                switch (connectionEvent.Event)
                {
                    case ConnectionEvents.StateChange:
                        HandleConnectionStateChange(connectionEvent);
                        break;

                    case ConnectionEvents.ProtocolSupport:
                        HostManager.GetHostVariant(connectionEvent.Source)
                            .AddProtocol(connectionEvent.ProtocolSupport);
                        break;
                }
            }
        }

        private static void HandleConnectionStateChange(ConnectionEventInfo @event)
        {
            try
            {
                var connection = @event.Source;

                switch (@event.State)
                {
                    case HTTPConnectionStates.Recycle:
                        HostManager.GetHostVariant(connection)
                            .RecycleConnection(connection)
                            .TryToSendQueuedRequests();

                        break;

                    case HTTPConnectionStates.WaitForProtocolShutdown:
                        HostManager.GetHostVariant(connection)
                            .RemoveConnection(connection, @event.State);
                        break;

                    case HTTPConnectionStates.Closed:
                    case HTTPConnectionStates.ClosedResendRequest:
                        // in case of ClosedResendRequest
                        if (@event.Request != null)
                            RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(@event.Request, RequestEvents.Resend));

                        if (connection.LastProcessedUri == null)
                            UnityEngine.Debug.LogError($"{connection} - LastProcessedUri is null!");

                        HostManager.GetHostVariant(connection)
                            .RemoveConnection(connection, @event.State)
                            .TryToSendQueuedRequests();
                        break;
                }
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception("ConnectionEvents", $"HandleConnectionStateChange ({@event.State})", ex, @event.Source.Context);
            }
        }
    }
}

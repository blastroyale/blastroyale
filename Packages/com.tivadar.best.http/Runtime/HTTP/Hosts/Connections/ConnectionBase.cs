using System;

using Best.HTTP.HostSetting;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Threading;

namespace Best.HTTP.Hosts.Connections
{
    /// <summary>
    /// Abstract base class for concrete connection implementation (HTTP/1, HTTP/2, WebGL, File).
    /// </summary>
    public abstract class ConnectionBase : IDisposable
    {
        #region Public Properties

        /// <summary>
        /// The address of the server that this connection is bound to.
        /// </summary>
        public HostKey HostKey { get; protected set; }

        /// <summary>
        /// The state of this connection.
        /// </summary>
        public HTTPConnectionStates State { get; internal set; }

        /// <summary>
        /// If the State is HTTPConnectionStates.Processing, then it holds a HTTPRequest instance. Otherwise it's null.
        /// </summary>
        public HTTPRequest CurrentRequest { get; internal set; }

        /// <summary>
        /// How much the connection kept alive after its last request processing.
        /// </summary>
        public virtual TimeSpan KeepAliveTime { get; protected set; }

        public virtual bool CanProcessMultiple { get { return false; } }

        /// <summary>
        /// When we start to process the current request. It's set after the connection is established.
        /// </summary>
        public DateTime StartTime { get; protected set; }

        public Uri LastProcessedUri { get; protected set; }

        public DateTime LastProcessTime { get; protected set; }

        internal LoggingContext Context;

        #endregion

        #region Privates

        protected bool IsThreaded;

        #endregion

        public ConnectionBase(HostKey hostKey)
            :this(hostKey, true)
        {}

        public ConnectionBase(HostKey hostKey, bool threaded)
        {
            this.HostKey = hostKey;

            this.State = HTTPConnectionStates.Initial;
            this.LastProcessTime = HTTPManager.CurrentFrameDateTime; // DateTime.Now;

            // By default we assume an HTTP/1 connection, but in the HTTP-Over-TCP-Connection the request handlers will decide its value.
            this.KeepAliveTime = HTTPManager.PerHostSettings.Get(this.HostKey.Host).HTTP1ConnectionSettings.MaxConnectionIdleTime;

            this.IsThreaded = threaded;

            this.Context = new LoggingContext(this);
            this.Context.Add("HostKey", this.HostKey.ToString());
        }

        internal virtual void Process(HTTPRequest request)
        {
            if (State == HTTPConnectionStates.Processing)
                throw new Exception("Connection already processing a request! " + this.ToString());

            this.State = HTTPConnectionStates.Processing;

            this.CurrentRequest = request;
            this.LastProcessedUri = this.CurrentRequest.CurrentUri;

            if (IsThreaded)
            {
                ThreadedRunner.RunLongLiving(ThreadFunc);
            }
            else
                ThreadFunc();
        }

        protected virtual void ThreadFunc()
        {

        }

        public ShutdownTypes ShutdownType { get; protected set; }

        /// <summary>
        /// Called when the plugin shuts down immediately.
        /// </summary>
        public virtual void Shutdown(ShutdownTypes type)
        {
            this.ShutdownType = type;
        }
       
        #region Dispose Pattern

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        ~ConnectionBase()
        {
            Dispose(false);
        }

        #endregion

        public override string ToString()
        {
            return string.Format("[{0}:{1}]", this.Context.Hash, this.HostKey.ToString());
        }
    }
}

using Best.HTTP.Shared.Logger;

namespace Best.HTTP.Hosts.Connections
{
    /// <summary>
    /// Interface for signaling upload threads.
    /// </summary>
    public interface IThreadSignaler
    {
        /// <summary>
        /// A <see cref="LoggingContext"/> instance for debugging purposes.
        /// </summary>
        /// <remarks>
        /// To help <see cref="Best.HTTP.Request.Upload.UploadStreamBase"/> implementors log in the IThreadSignaler's context,
        /// the interface implementors must make their logging context accessible.
        /// </remarks>
        public LoggingContext Context { get; }

        /// <summary>
        /// Signals the associated thread to resume or wake up.
        /// </summary>
        void SignalThread();
    }
}

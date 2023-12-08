using Best.HTTP.Response;

namespace Best.HTTP.Hosts.Connections
{
    /// <summary>
    /// Defines an interface for notifying connections when space becomes available in a buffer for downloading data.
    /// Connections implementating of this interface are used to signal their internal logic that they can transfer data into the available buffer space.
    /// </summary>
    public interface IDownloadContentBufferAvailable
    {
        /// <summary>
        /// Notifies a connection that space has become available in the buffer for downloading data.
        /// When invoked, this method indicates to a connection that it can transfer additional data into the buffer for further processing.
        /// </summary>
        /// <param name="stream">The <see cref="DownloadContentStream"/> instance associated with the buffer.</param>
        void BufferAvailable(DownloadContentStream stream);
    }
}

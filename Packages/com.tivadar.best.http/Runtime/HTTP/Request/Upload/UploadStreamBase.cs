using Best.HTTP.Hosts.Connections;

namespace Best.HTTP.Request.Upload
{
    // [Request Creation] --> [Request Queued] --> [UploadStream.SetupRequestHeaders call] --> [Send Request Headers] --> [UploadStream.PrepareToSend call] --> [UploadStream.Read to Send Request Body] -> [Dispose UploadStream]

    public static class UploadReadConstants
    {
        public static int WaitForMore = -1;
        public static int Completed = 0;
    }

    /// <summary>
    /// Abstract class to serve as a base for non-conventional streams used in HTTP requests.
    /// </summary>
    /// <remarks>
    /// The return value of <see cref="System.IO.Stream.Read(byte[], int, int)"/> is treated specially in the plugin:
    /// <list type="bullet">
    ///     <item>
    ///         <term>Less than zero(<c>-1</c>)</term>
    ///         <description> indicates that no data is currently available but more is expected in the future. In this case, when new data becomes available the IThreadSignaler object must be signaled.</description>
    ///     </item>
    ///     <item>
    ///         <term>Zero (<c>0</c>)</term>
    ///         <description> means that the stream is closed, no more data can be expected.</description>
    ///     </item>
    ///     <item><description>Otherwise it must return with the number bytes copied to the buffer.</description></item>
    /// </list>
    /// A zero value to signal stream closure can follow a less than zero value.
    /// </remarks>
    public abstract class UploadStreamBase : System.IO.Stream
    {
        /// <summary>
        /// Gets the <see cref="IThreadSignaler"/> object for signaling when new data is available.
        /// </summary>
        public IThreadSignaler Signaler { get; private set; }

        /// <summary>
        /// Length in bytes that the stream will upload.
        /// </summary>
        /// <remarks>
        /// The return value of Length is treated specially in the plugin:
        /// <list type="bullet">
        ///     <item><term>-2</term><description>The stream's length is unknown and the plugin have to send data <c>with 'chunked' transfer-encoding</c>.</description></item>
        ///     <item><term>-1</term><description>The stream's length is unknown and the plugin have to send data <c>as-is, without any encoding</c>.</description></item>
        ///     <item><term>0</term><description>No content to send. The content-length header will contain zero (<c>0</c>).</description></item>
        ///     <item><term>>0</term><description>Length of the content is known, will be sent <c>as-is, without any encoding</c>. The content-length header will contain zero (<c>0</c>).</description></item>
        /// </list>
        /// Constants for the first three points can be found in <see cref="Best.HTTP.Request.Upload.BodyLengths"/>.
        /// </remarks>
        public override long Length => throw new System.NotImplementedException();

        /// <summary>
        /// Called before sending out the request's headers. Perform content processing to calculate the final length if possible.
        /// In this function the implementor can set headers and other parameters to the request.
        /// </summary>
        /// <remarks>Typically called on a thread.</remarks>
        /// <param name="request">The <see cref="HTTPRequest"/> associated with the stream.</param>
        public abstract void BeforeSendHeaders(HTTPRequest request);

        /// <summary>
        /// Called just before sending out the request's body, and saves the <see cref="IThreadSignaler"/> for signaling when new data is available.
        /// </summary>
        /// <param name="request">The HTTPRequest associated with the stream.</param>
        /// <param name="threadSignaler">The <see cref="IThreadSignaler"/> object to be used for signaling.</param>
        /// <remarks>Typically called on a separate thread.</remarks>

        /// <summary>
        /// Called just before sending out the request's body, saves the <see cref="IThreadSignaler"/> that can be used for signaling when new data is available.
        /// </summary>
        /// <param name="request">The HTTPRequest associated with the stream.</param>
        /// <param name="threadSignaler">The <see cref="IThreadSignaler"/> object to be used for signaling.</param>
        /// <remarks>Typically called on a separate thread.</remarks>
        public virtual void BeforeSendBody(HTTPRequest request, IThreadSignaler threadSignaler) => this.Signaler = threadSignaler;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.Signaler = null;
        }
    }

}

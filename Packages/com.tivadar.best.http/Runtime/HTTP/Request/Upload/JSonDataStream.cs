using System;
using System.IO;
using System.Text;

using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.HTTP.Shared.Streams;

namespace Best.HTTP.Request.Upload
{
    /// <summary>
    /// An <see cref="UploadStreamBase"/> implementation to convert and upload the object as JSON data. It sets the <c>"Content-Type"</c> header to <c>"application/json; charset=utf-8"</c>.
    /// </summary>
    /// <typeparam name="T">The type of the object to be converted to JSON.</typeparam>
    /// <remarks>
    /// <para>This stream keeps a reference to the object until the preparation in <see cref="BeforeSendHeaders"/>. This means, changes to the object after passing it to the constructor will be reflected in the sent data too.</para>
    /// <para>The return value of <see cref="System.IO.Stream.Read(byte[], int, int)"/> is treated specially in the plugin:
    /// <list type="bullet">
    ///     <item>
    ///         <term>Less than zero(<c>-1</c>) value </term>
    ///         <description> indicates that no data is currently available but more is expected in the future. In this case, when new data becomes available the IThreadSignaler object must be signaled.</description>
    ///     </item>
    ///     <item>
    ///         <term>Zero (<c>0</c>)</term>
    ///         <description> means that the stream is closed, no more data can be expected.</description>
    ///     </item>
    /// </list>
    /// A zero value to signal stream closure can follow a less than zero value.</para>
    /// </remarks>
    public sealed class JSonDataStream<T> : UploadStreamBase
    {
        public override long Length { get => this._innerStream.Length; }

        private BufferPoolMemoryStream _innerStream;
        private T _objToJson;

        /// <summary>
        /// Initializes a new instance of the <see cref="JSonDataStream{T}"/> class with the specified object.
        /// </summary>
        /// <param name="obj">The object to be converted to JSON and uploaded.</param>
        public JSonDataStream(T obj) => this._objToJson = obj;
                
        /// <summary>
        /// Called before sending out the request's headers. It sets the <c>"Content-Type"</c> header to <c>"application/json; charset=utf-8"</c>.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        public override void BeforeSendHeaders(HTTPRequest request)
        {
            request.SetHeader("Content-Type", "application/json; charset=utf-8");

            if (this._innerStream != null)
            {
                this._innerStream.Position = 0;
                return;
            }

            var json = Best.HTTP.JSON.LitJson.JsonMapper.ToJson(this._objToJson);
            this._objToJson = default;

            var byteLength = Encoding.UTF8.GetByteCount(json);
            var buffer = BufferPool.Get(byteLength, true);
            Encoding.UTF8.GetBytes(json, 0, json.Length, buffer, 0);

            this._innerStream = new BufferPoolMemoryStream(byteLength);
            this._innerStream.Write(buffer, 0, byteLength);
            this._innerStream.Position = 0;

            BufferPool.Release(buffer);
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset"/> and ( <paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        public override int Read(byte[] buffer, int offset, int count) => this._innerStream.Read(buffer, offset, count);

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="JSonDataStream{T}"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this._objToJson = default;
            this._innerStream?.Dispose();
            this._innerStream = null;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
        public override void SetLength(long value) { throw new NotImplementedException(); }
        public override void Write(byte[] buffer, int offset, int count) { throw new NotImplementedException(); }
    }
}

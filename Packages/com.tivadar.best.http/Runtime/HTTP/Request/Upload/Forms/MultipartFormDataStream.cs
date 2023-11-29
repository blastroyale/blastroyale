using System;
using System.Collections.Generic;
using System.IO;

using Best.HTTP.Hosts.Connections;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.HTTP.Shared.Streams;

using UnityEngine;

using static Best.HTTP.Hosts.Connections.HTTP1.Constants;

namespace Best.HTTP.Request.Upload.Forms
{
    /// <summary>
    /// An <see cref="UploadStreamBase"/> based implementation of the <c>multipart/form-data</c> Content-Type. It's very memory-effective, streams are read into memory in chunks.
    /// </summary>
    /// <remarks>
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
    public sealed class MultipartFormDataStream : UploadStreamBase
    {
        /// <summary>
        /// Gets the length of this multipart/form-data stream.
        /// </summary>
        public override long Length { get => this._length; }
        private long _length;

        /// <summary>
        /// A random boundary generated in the constructor.
        /// </summary>
        private string boundary;

        private Queue<StreamList> fields = new Queue<StreamList>(1);
        private StreamList currentField;

        /// <summary>
        /// Initializes a new instance of the MultipartFormDataStream class.
        /// </summary>
        public MultipartFormDataStream()
        {
            var hash = new Hash128();
            hash.Append(this.GetHashCode());
            
            this.boundary = $"com.Tivadar.Best.HTTP/boundary/{hash}";
        }

        public override void BeforeSendHeaders(HTTPRequest request)
        {
            request.SetHeader("Content-Type", "multipart/form-data; boundary=" + boundary);

            var boundaryStream = new BufferPoolMemoryStream();
            boundaryStream.WriteLine("--" + this.boundary + "--");
            boundaryStream.Position = 0;

            this.fields.Enqueue(new StreamList(boundaryStream));

            if (this._length >= 0)
                this._length += boundaryStream.Length;
        }

        /// <summary>
        /// Adds a textual field to the multipart/form-data stream.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="value">The textual value of the field.</param>
        /// <returns>The MultipartFormDataStream instance for method chaining.</returns>
        public MultipartFormDataStream AddField(string fieldName, string value)
            => AddField(fieldName, value, System.Text.Encoding.UTF8);

        /// <summary>
        /// Adds a textual field to the multipart/form-data stream.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="value">The textual value of the field.</param>
        /// <param name="encoding">The encoding to use for the value.</param>
        /// <returns>The MultipartFormDataStream instance for method chaining.</returns>
        public MultipartFormDataStream AddField(string fieldName, string value, System.Text.Encoding encoding)
        {
            var enc = encoding ?? System.Text.Encoding.UTF8;
            var byteCount = enc.GetByteCount(value);
            var buffer = BufferPool.Get(byteCount, true);
            var stream = new BufferPoolMemoryStream();

            enc.GetBytes(value, 0, value.Length, buffer, 0);

            stream.Write(buffer, 0, byteCount);

            stream.Position = 0;

            string mime = encoding != null ? "text/plain; charset=" + encoding.WebName : null;
            return AddStreamField(fieldName, stream, null, mime);
        }

        /// <summary>
        /// Adds a stream field to the multipart/form-data stream.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="data">The data containing the field data.</param>
        /// <returns>The MultipartFormDataStream instance for method chaining.</returns>
        public MultipartFormDataStream AddField(string fieldName, byte[] data)
            => AddStreamField(fieldName, new MemoryStream(data));

        /// <summary>
        /// Adds a stream field to the multipart/form-data stream.
        /// </summary>
        /// <param name="stream">The stream containing the field data.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <returns>The MultipartFormDataStream instance for method chaining.</returns>
        public MultipartFormDataStream AddStreamField(string fieldName, System.IO.Stream stream)
            => AddStreamField(fieldName, stream, null, null);

        /// <summary>
        /// Adds a stream field to the multipart/form-data stream.
        /// </summary>
        /// <param name="stream">The stream containing the field data.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="fileName">The name of the file, if applicable.</param>
        /// <returns>The MultipartFormDataStream instance for method chaining.</returns>
        public MultipartFormDataStream AddStreamField(string fieldName, System.IO.Stream stream, string fileName)
            => AddStreamField(fieldName, stream, fileName, null);

        /// <summary>
        /// Adds a stream field to the multipart/form-data stream.
        /// </summary>
        /// <param name="stream">The stream containing the field data.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="fileName">The name of the file, if applicable.</param>
        /// <param name="mimeType">The MIME type of the content.</param>
        /// <returns>The MultipartFormDataStream instance for method chaining.</returns>
        public MultipartFormDataStream AddStreamField(string fieldName, System.IO.Stream stream, string fileName, string mimeType)
        {
            var header = new BufferPoolMemoryStream();
            header.WriteLine("--" + this.boundary);
            header.WriteLine("Content-Disposition: form-data; name=\"" + fieldName + "\"" + (!string.IsNullOrEmpty(fileName) ? "; filename=\"" + fileName + "\"" : string.Empty));

            // Set up Content-Type head for the form.
            mimeType = mimeType ?? "application/octet-stream";
            if (!string.IsNullOrEmpty(mimeType))
                header.WriteLine("Content-Type: " + mimeType);
            
            header.WriteLine();
            header.Position = 0;

            var footer = new BufferPoolMemoryStream();
            footer.Write(EOL, 0, EOL.Length);
            footer.Position = 0;

            // all wrapped streams going to be disposed by the StreamList wrapper.
            var wrapper = new StreamList(header, stream, footer);

            try
            {
                if (this._length >= 0)
                    this._length += wrapper.Length;
            }
            catch
            {
                this._length = -1;
            }

            this.fields.Enqueue(wrapper);

            return this;
        }

        /// <summary>
        /// Adds the final boundary to the multipart/form-data stream before sending the request body.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <param name="threadSignaler">The thread signaler for handling asynchronous operations.</param>
        public override void BeforeSendBody(HTTPRequest request, IThreadSignaler threadSignaler)
        {
            base.BeforeSendBody(request, threadSignaler);
        }

        /// <summary>
        /// Reads data from the multipart/form-data stream into the provided buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read data into.</param>
        /// <param name="offset">The starting offset in the buffer.</param>
        /// <param name="length">The maximum number of bytes to read.</param>
        /// <returns>The number of bytes read into the buffer.</returns>
        public override int Read(byte[] buffer, int offset, int length)
        {
            if (this.currentField == null && this.fields.Count == 0)
                return -1;

            if (this.currentField == null && this.fields.Count > 0)
                this.currentField = this.fields.Dequeue();

            int readCount = 0;

            do
            {
                // read from the current stream
                int count = this.currentField.Read(buffer, offset + readCount, length - readCount);

                if (count > 0)
                    readCount += count;
                else
                {
                    // if the current field's stream is empty, go for the next one.

                    // dispose the current one first
                    try
                    {
                        this.currentField.Dispose();
                    }
                    catch
                    { }

                    // no more fields/streams? exit
                    if (this.fields.Count == 0)
                        break;

                    // grab the next one
                    this.currentField = this.fields.Dequeue();
                }

                // exit when we reach the length goal, or there's no more streams to read from
            } while (readCount < length && this.fields.Count > 0);

            return readCount;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (fields != null)
            {
                foreach (var field in fields)
                    field.Dispose();
                fields.Clear();
                fields = null;
            }

            currentField?.Dispose();
            currentField = null;
        }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return false; } }
        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
        public override void SetLength(long value) => throw new NotImplementedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        public override void Flush() { }
    }
}

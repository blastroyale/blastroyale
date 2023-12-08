using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Best.HTTP.Shared.PlatformSupport.Text;

namespace Best.HTTP.Request.Upload.Forms
{
    /// <summary>
    /// Readonly struct to hold key -> value pairs, where the value is either textual or binary.
    /// </summary>
    readonly struct FormField
    {
        public readonly string Key;
        public readonly string TextValue;
        public readonly byte[] BinaryValue;

        public FormField(string key, string textValue)
        {
            this.Key = key;
            this.TextValue = textValue;
            this.BinaryValue = null;
        }

        public FormField(string key, byte[] binaryValue)
        {
            this.Key = key;
            this.TextValue = null;
            this.BinaryValue = binaryValue;
        }
    }

    /// <summary>
    /// An <see cref="UploadStreamBase"/> implementation representing a stream that prepares and sends data as URL-encoded form data in an HTTP request.
    /// </summary>
    /// <remarks>
    /// <para>This stream is used to send data as URL-encoded form data in an HTTP request. It sets the <c>"Content-Type"</c> header to <c>"application/x-www-form-urlencoded"</c>.
    /// URL-encoded form data is typically used for submitting form data to a web server. It is commonly used in HTTP POST requests to send data to a server, such as submitting HTML form data.</para>
    /// 
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
    /// <para>While it's possible, it's not advised to send binary data url-encoded!</para>
    /// </remarks>
    public sealed class UrlEncodedStream : UploadStreamBase
    {
        private const int EscapeTreshold = 256;

        /// <summary>
        /// Gets the length of the stream.
        /// </summary>
        public override long Length { get => this._memoryStream.Length; }

        private MemoryStream _memoryStream;

        /// <summary>
        /// A list that holds the form's fields.
        /// </summary>
        private List<FormField> _fields = new List<FormField>();

        /// <summary>
        /// Sets up the HTTP request by adding the <c>"Content-Type"</c> header as <c>"application/x-www-form-urlencoded"</c>.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        public override void BeforeSendHeaders(HTTPRequest request)
        {
            request.SetHeader("Content-Type", "application/x-www-form-urlencoded");

            StringBuilder sb = StringBuilderPool.Get(_fields.Count * 4);

            // Create a "field1=value1&field2=value2" formatted string
            for (int i = 0; i < _fields.Count; ++i)
            {
                var field = _fields[i];

                if (i > 0)
                    sb.Append("&");

                sb.Append(EscapeString(field.Key));
                sb.Append("=");

                if (!string.IsNullOrEmpty(field.TextValue) || field.BinaryValue == null)
                    sb.Append(EscapeString(field.TextValue));
                else
                    // If forced to this form type with binary data, we will create a base64 encoded string from it.
                    sb.Append(Convert.ToBase64String(field.BinaryValue, 0, field.BinaryValue.Length));
            }

            this._memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(StringBuilderPool.ReleaseAndGrab(sb)));
        }

        /// <summary>
        /// Adds binary data to the form. It is not advised to send binary data with an URL-encoded form due to the conversion cost of binary to text conversion.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="content">The binary data content.</param>
        /// <returns>The UrlEncodedStream instance for method chaining.</returns>
        public UrlEncodedStream AddBinaryData(string fieldName, byte[] content)
        {
            _fields.Add(new FormField(fieldName, content));

            return this;
        }

        public UrlEncodedStream AddField(string fieldName, string value)
        {
            _fields.Add(new FormField(fieldName, value));

            return this;
        }

        public override int Read(byte[] buffer, int offset, int count) => this._memoryStream.Read(buffer, offset, count);

        private static string EscapeString(string originalString)
        {
            if (originalString.Length < EscapeTreshold)
                return Uri.EscapeDataString(originalString);
            else
            {
                int loops = originalString.Length / EscapeTreshold;
                StringBuilder sb = StringBuilderPool.Get(loops); //new StringBuilder(loops);

                for (int i = 0; i <= loops; i++)
                    sb.Append(i < loops ?
                                 Uri.EscapeDataString(originalString.Substring(EscapeTreshold * i, EscapeTreshold)) :
                                 Uri.EscapeDataString(originalString.Substring(EscapeTreshold * i)));
                return StringBuilderPool.ReleaseAndGrab(sb);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this._memoryStream?.Dispose();
            this._memoryStream = null;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
        public override void SetLength(long value) => throw new NotImplementedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        public override void Flush() => throw new NotImplementedException();
    }
}

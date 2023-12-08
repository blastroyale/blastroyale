using System;
using System.IO;

using Best.HTTP.Request.Upload;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;

namespace Best.HTTP.Request.Settings
{
    public delegate void OnHeadersSentDelegate(HTTPRequest req);

    /// <summary>
    /// Options for sending the request headers and content, including upload progress monitoring.
    /// </summary>
    /// <remarks><see cref="SetupRequest"/> might be called when redirected or retried!</remarks>
    public class UploadSettings : IDisposable
    {
        /// <summary>
        /// Size of the internal buffer, and upload progress will be fired when this size of data sent to the wire. Its default value is 4 KiB.
        /// </summary>
        public int UploadChunkSize = 4 * 1024;

        /// <summary>
        /// The stream that the plugin will use to send data to the server.
        /// </summary>
        /// <remarks>
        /// The stream can be any regular <see cref="System.IO.Stream"/> implementation or a specialized one inheriting from <see cref="UploadStreamBase"/>:
        /// <list type="bullet">
        ///     <item><term><see cref="DynamicUploadStream"/></term><description>A specialized <see cref="UploadStreamBase"/> for data generated on-the-fly or periodically. The request remains active until the <see cref="DynamicUploadStream.Complete"/> method is invoked, ensuring continuous data feed even during temporary empty states.</description></item>
        ///     <item><term><see cref="JSonDataStream{T}"/></term><description>An <see cref="UploadStreamBase"/> implementation to convert and upload the object as JSON data. It sets the <c>"Content-Type"</c> header to <c>"application/json; charset=utf-8"</c>.</description></item>
        ///     <item><term><see cref="Upload.Forms.UrlEncodedStream"/></term><description>An <see cref="UploadStreamBase"/> implementation representing a stream that prepares and sends data as URL-encoded form data in an HTTP request.</description></item>
        ///     <item><term><see cref="Upload.Forms.MultipartFormDataStream"/></term><description>An <see cref="UploadStreamBase"/> based implementation of the <c>multipart/form-data</c> Content-Type. It's very memory-effective, streams are read into memory in chunks.</description></item>
        /// </list>
        /// </remarks>
        public Stream UploadStream;

        /// <summary>
        /// Set to <c>false</c> if the plugin MUST NOT dispose <see cref="UploadStream"/> after the request is finished.
        /// </summary>
        public bool DisposeStream = true;

        /// <summary>
        /// Called periodically when data sent to the server.
        /// </summary>
        public OnProgressDelegate OnUploadProgress;

        /// <summary>
        /// This event is fired after the headers are sent to the server.
        /// </summary>
        public event OnHeadersSentDelegate OnHeadersSent
        {
            add { _onHeadersSent += value; }
            remove { _onHeadersSent -= value; }
        }
        private OnHeadersSentDelegate _onHeadersSent;

        // <summary>
        // Whether to send an "<c>Expect: 100-continue</c>" header and value when there's content to send (<see cref="UploadSettings.UploadStream"/> != <c>null</c>).
        // By using "<c>Expect: 100-continue</c>" the server is able to respond with an error (like <c>401-unauthorized</c>, <c>405-method not allowed</c>, etc.) or redirect before the client sends the whole payload.
        // </summary>
        // <remarks>
        // More details can be found here:
        // <list type="bullet">
        //     <item><description><see href="https://www.rfc-editor.org/rfc/rfc9110#name-expect">RFC-9110 - Expect header</see></description></item>
        //     <item><description><see href="https://daniel.haxx.se/blog/2020/02/27/expect-tweaks-in-curl/">EXPECT: TWEAKS IN CURL (by Daniel Stenberg)</see></description></item>
        // </list>
        // </remarks>
        //public bool SendExpect100Continue = true;

        // False by default, set to true only when "Expect: 100-continue" sent out.
        //internal bool Expect100Continue = false;

        //internal void ResetExpects() => this.SendExpect100Continue = this.Expect100Continue = false;

        private bool isDisposed;

        /// <summary>
        /// Called every time the request is sent out (redirected or retried).
        /// </summary>
        /// <param name="request">The <see cref="HTTPRequest"/> being prepared.</param>
        /// <param name="dispatchHeadersSentCallback"><c>true</c> if the <see cref="OnHeadersSent"/> can be fired.</param>
        public virtual void SetupRequest(HTTPRequest request, bool dispatchHeadersSentCallback)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(UploadSettings));

            if (this.UploadStream is UploadStreamBase upStream)
                upStream.BeforeSendHeaders(request);

            // Decide on whether append an "expect: 100-continue" or not.
            // https://www.rfc-editor.org/rfc/rfc9110#name-expect
            /*
            if (this.SendExpect100Continue && this.UploadStream != null)
            {
                request.AddHeader("expect", "100-continue");

                this.Expect100Continue = true;
            }
            else
                request.RemoveHeader("expect");
            */

            if (dispatchHeadersSentCallback)
            {
                // Call the callback on the unity main thread
                if (HTTPUpdateDelegator.Instance.IsMainThread())
                    call_onBeforeHeaderSend(request);
                else
                    new RunOnceOnMainThread(() => call_onBeforeHeaderSend(request), request.Context)
                        .Subscribe();
            }
        }

        protected void call_onBeforeHeaderSend(HTTPRequest request)
        {
            try
            {
                _onHeadersSent?.Invoke(request);
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception(nameof(UploadSettings), nameof(OnHeadersSent), ex, request.Context);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    if (this.DisposeStream)
                    {
                        var stream = this.UploadStream;

                        if (stream != null)
                        {
                            this.UploadStream?.Dispose();
                            this.UploadStream = null;

                            isDisposed = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Dispose of resources used by the UploadSettings instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

using System.Collections;
using System.Text;

using Best.HTTP.Examples.Helpers;
using Best.HTTP.Response;

using UnityEngine;

namespace Best.HTTP.Examples
{
    /// <summary>
    /// Example showing the usage of download streaming using the DownloadContentStream class.
    /// In this example content processing is done on the Unity main thread wrapped in a coroutine.
    /// </summary>
    class DownStreamSample : SampleBase
    {
#pragma warning disable 0649, 0169
        [Header("Sample Fields")]

        /// <summary>
        /// GameObject that will be used as a root for new UI objects.
        /// </summary>
        [SerializeField]
        private RectTransform _contentRoot;

        /// <summary>
        /// Prefab of a UI object with a Text field.
        /// </summary>
        [SerializeField]
        private TextListItem _listItemPrefab;

#pragma warning restore

        /// <summary>
        /// Address of the used end point.
        /// </summary>
        private string _baseAddress = "https://besthttpwebgldemo.azurewebsites.net/sse";

        /// <summary>
        /// Cached reference to the HTTPRequest instance.
        /// </summary>
        private HTTPRequest _request;

        protected override void Start()
        {
            base.Start();

            // Create a regular get request with a regular callback too. We still need a callback,
            //  because it might encounter an error before able to start a download. 
            _request = HTTPRequest.CreateGet(this._baseAddress, OnRequestFinishedCallack);

            // Request a notification when download starts. This callback will be fired when
            //  the status code suggests that we can expect actual content (2xx status codes).
            _request.DownloadSettings.OnDownloadStarted += OnDownloadStarted;

            // Don't want to retry when there's a failure
            _request.RetrySettings.MaxRetries = 0;

            // Start processing the request
            _request.Send();

            AddUIText("Connecting...");
        }

        private void OnDownloadStarted(HTTPRequest req, HTTPResponse resp, DownloadContentStream stream)
        {
            AddUIText("Download started!");

            // We can expect content from the server, start our logic.
            StartCoroutine(ParseContent(stream));
        }

        IEnumerator ParseContent(DownloadContentStream stream)
        {
            try
            {
                while (!stream.IsCompleted)
                {
                    // Try to take out a download segment from the Download Stream.
                    if (stream.TryTake(out var buffer))
                    {
                        // Make sure that the buffer is released back to the BufferPool.
                        using var _ = buffer.AsAutoRelease();

                        try
                        {
                            // Try to create a string from the downloaded content
                            var str = Encoding.UTF8.GetString(buffer.Data, buffer.Offset, buffer.Count).TrimEnd();

                            // And display it in the UI
                            AddUIText(str);
                        }
                        catch { }
                    }

                    yield return null;
                }
            }
            finally
            {
                // Don't forget to Dispose the stream!
                stream.Dispose();
            }
        }

        private void OnRequestFinishedCallack(HTTPRequest req, HTTPResponse resp)
        {
            if (_request == null)
                return;

            string log = null;

            if (req.State == HTTPRequestStates.Finished)
            {
                if (!resp.IsSuccess)
                    log = resp.StatusCode.ToString();
            }
            else
                log = req.State.ToString();

            AddUIText(log);

            _request = null;
        }

        private void OnDestroy()
        {
            _request?.Abort();
            _request = null;
        }

        void AddUIText(string text)
            => Instantiate<TextListItem>(this._listItemPrefab, this._contentRoot)
                .SetText(text);
    }
}

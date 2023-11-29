using System.Threading.Tasks;

using Best.HTTP.Caching;
using Best.HTTP.Examples.Helpers;
using Best.HTTP.Response;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Compression.Crc;
using Best.HTTP.Shared.PlatformSupport.Memory;

using UnityEngine;

namespace Best.HTTP.Examples
{
    /// <summary>
    /// Example showing the usage of download streaming using the DownloadContentStream class.
    /// In this example content processing is done on a Thread to make it as fast as possible
    /// without causing CPU spikes on the Unity main thread.
    /// </summary>
    class DownStreamWithThreadSample : SampleBase
    {
        /// <summary>
        /// Precomputed and expected value of the content _baseAddress points to.
        /// </summary>
        const int EXPECTED_CRC = 0x4B282398;

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
        private string _baseAddress = "https://besthttpwebgldemo.azurewebsites.net/test100mb.dat";

        /// <summary>
        /// Cached reference to the HTTPRequest instance.
        /// </summary>
        private HTTPRequest _request;

        protected override void Start()
        {
            base.Start();

            // Set a custom size to make sure the local cache will accept the downloaded file into its cache.
            HTTPManager.LocalCache?.Dispose();
            HTTPManager.LocalCache = new HTTPCacheBuilder()
                                            .WithOptions(new HTTPCacheOptionsBuilder()
                                                                .WithMaxCacheSize(128 * 1024 * 1024)
                                                                .Build())
                                            .Build();


            // Create a regular get request with a regular callback too. We still need a callback,
            //  because it might encounter an error before able to start a download. 
            _request = HTTPRequest.CreateGet(this._baseAddress, OnRequestFinishedCallack);

            // Request a notification when download starts
            _request.DownloadSettings.OnDownloadStarted += OnDownloadStarted;

            // When needed, create a BlockingDownloadContentStream instead of the regular DownloadContentStream.
            // BlockingDownloadContentStream's Take function will block until new data is available.
            _request.DownloadSettings.DownloadStreamFactory = (req, resp, bufferAvailableHandler)
                => new BlockingDownloadContentStream(resp, req.DownloadSettings.ContentStreamMaxBuffered, bufferAvailableHandler);

            _request.DownloadSettings.OnDownloadProgress += OnDownloadProgress;

            // Don't want to retry when there's a failure
            _request.RetrySettings.MaxRetries = 0;

            // Start processing the request
            _request.Send();

            CreateUIText("Connecting...");
        }

        private void OnDownloadProgress(HTTPRequest req, long progress, long length)
        {
            var progressUIEntry = req.Tag as TextListItem;
            if (progressUIEntry == null)
                req.Tag = progressUIEntry = CreateUIText(string.Empty);

            progressUIEntry.SetText($"{progress:N0}/{length:N0}");
        }

        private async void OnDownloadStarted(HTTPRequest req, HTTPResponse resp, DownloadContentStream stream)
        {
            CreateUIText("Download started!");

            // Task.Run will execute the ConsumeDownloadStream on a background thread.
            // By using a thread we can offload the CPU intensive work and
            // it's also desirable because it can block the executing thread if the stream is empty!
            // Task returns with the calculated checksum.
            var crc = await Task.Run<int>(() => ConsumeDownloadStream(stream as BlockingDownloadContentStream));

            CreateUIText($"CRC checksum calculation finished. Result: 0x{crc:X}, Expected: 0x{EXPECTED_CRC:X}");
        }

        int ConsumeDownloadStream(BlockingDownloadContentStream blockingStream)
        {
            var crc = new CRC32();

            try
            {
                while (!blockingStream.IsCompleted)
                {
                    // Take out a segment from the downloaded
                    if (blockingStream.TryTake(out var buffer))
                    {
                        try
                        {
                            // In this case content processing is just calculating the CRC checksum of the data.
                            crc.SlurpBlock(buffer.Data, buffer.Offset, buffer.Count);
                        }
                        finally
                        {
                            BufferPool.Release(buffer);
                        }
                    }
                }
            }
            finally
            {
                blockingStream.Dispose();
            }

            return crc.Crc32Result;
        }

        private void OnRequestFinishedCallack(HTTPRequest req, HTTPResponse resp)
        {
            // If we leaved the sample, the _request is nulled out and we can ignore this callback.
            if (_request == null)
                return;
            _request = null;

            string log = null;

            if (req.State == HTTPRequestStates.Finished)
            {
                if (resp.IsSuccess)
                {
                    log = "Done! ";

                    // If IsFromCache is true, the response is read from the local cache.
                    if (resp.IsFromCache)
                        log += "From Local Cache!";
                    else
                        log += "Fresh From The Server";
                }
                else
                    log = resp.StatusCode.ToString();
            }
            else
                log = req.State.ToString();

            CreateUIText(log);
        }

        private void OnDestroy()
        {
            _request?.Abort();
            _request = null;
        }

        TextListItem CreateUIText(string text)
            => Instantiate<TextListItem>(this._listItemPrefab, this._contentRoot)
                .SetText(text);
    }
}

using System.Collections;

using Best.HTTP.Examples.Helpers;
using Best.HTTP.Request.Upload;

using UnityEngine;

namespace Best.HTTP.Examples
{
    /// <summary>
    /// On-demand upload
    /// </summary>
    class UploadOnDemand : SampleBase
    {
#pragma warning disable 0649, 0169
        [Header("Sample Fields")]

        /// <summary>
        /// GameObject that will be used as a root for new UI objects.
        /// </summary>
        [SerializeField]
        private RectTransform _contentRoot;

        /// <summary>
        /// Prefab of a UI object with two Text fields.
        /// </summary>
        [SerializeField]
        private MultiTextListItem _listItemPrefab;

#pragma warning restore

        /// <summary>
        /// Address of the used end point.
        /// </summary>
        private string _baseAddress = "https://httpbin.org/post";

        protected override void Start()
        {
            base.Start();

            // create request
            var request = HTTPRequest.CreatePost(_baseAddress, OnUploadFinished);

            // setup
            request.UploadSettings.UploadStream = new DynamicUploadStream();
            request.UploadSettings.OnHeadersSent += UploadSettings_OnBeforeHeaderSend;

            // send
            request.Send();

            CreateUIText("Connecting...");
        }

        /// <summary>
        /// This callback fired before the request's headers are sent. It's a good opportunity to start generating our (fake) data.
        /// </summary>
        private void UploadSettings_OnBeforeHeaderSend(HTTPRequest req)
            => StartCoroutine(GenerateData(req));

        /// <summary>
        /// This is our main logic to generate fake data and feed it to the upload stream to send it to the server.
        /// </summary>
        private IEnumerator GenerateData(HTTPRequest req)
        {
            const int MAX_CHUNKS = 10;
            const int CHUNK_SIZE = 16 * 1024;

            CreateUIText("Connected, upload can start");

            // get the upload stream
            DynamicUploadStream uploadStream = req.UploadSettings.UploadStream as DynamicUploadStream;

            try
            {
                int uploadedChunks = 0;
                do
                {
                    yield return new WaitForSeconds(2f);

                    // check whether the request is finished, it can be in an errored state
                    if (req.State >= HTTPRequestStates.Finished)
                        break;

                    // write the data to the stream and let the connection know about it
                    uploadStream.Write(new byte[CHUNK_SIZE], 0, CHUNK_SIZE);

                    CreateUIText($"Added new chunk to upload ({uploadedChunks + 1} / {MAX_CHUNKS})");

                } while (++uploadedChunks < MAX_CHUNKS);
            }
            finally
            {
                // complete upload
                uploadStream.Complete();
            }
        }

        /// <summary>
        /// Callback of the request when finished.
        /// </summary>
        private void OnUploadFinished(HTTPRequest req, HTTPResponse resp)
        {
            if (resp != null)
            {
                if (resp.IsSuccess)
                    CreateUIText("Response recceived, finished succesfully!");
                else
                    CreateUIText($"Finished with error: {resp.StatusCode} - {resp.Message}!");
            }
            else
                CreateUIText(req.State.ToString());
        }

        /// <summary>
        /// UI helper function to create a list item from a prefab and set its text to display.
        /// </summary>
        TextListItem CreateUIText(string text)
            => Instantiate<MultiTextListItem>(this._listItemPrefab, this._contentRoot)
                .SetText(text);
    }
}

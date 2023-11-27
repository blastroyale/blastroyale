using System;
using System.Threading.Tasks;

using Best.HTTP.Examples.Helpers;
using Best.HTTP.Hosts.Settings;
using Best.HTTP.Shared;

using UnityEngine;

namespace Best.HTTP.Examples
{
    /// <summary>
    /// Demonstrates the usage of HTTPManager.PerHostSettings to set rules and configurations on a per-host basis.
    /// </summary>
    class PerHostManagerSample : SampleBase
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

        /// <summary>
        /// Prefab of a UI object with Text and (Raw)Image fields.
        /// </summary>
        [SerializeField]
        private TextWithImageListItem _listItemWithImagePrefab;

#pragma warning restore


        readonly Uri host_1 = new Uri("https://besthttpwebgldemo.azurewebsites.net");
        readonly string[] host_1_image_paths = new string[] { "/images/Demo/Two.png", "/images/Demo/Three.png" };

        readonly Uri host_2 = new Uri("https://httpbingo.org");
        readonly string[] host_2_image_paths = new string[] { "/image/jpeg", "/image/png" };

        protected override async void Start()
        {
            base.Start();

#if !UNITY_WEBGL || UNITY_EDITOR
            CreateUIItem("Disabling HTTP/2 globally, all following request should done over HTTP/1.");

            // Get the global/fallback settings.
            var globalSettings = HTTPManager.PerHostSettings.Get("*");

            // Turn off connection pooling
            globalSettings.HTTP1ConnectionSettings.TryToReuseConnections = false;

            // Turn off HTTP/2 connections.
            globalSettings.HTTP2ConnectionSettings.EnableHTTP2Connections = false;

            await DownloadImages();            

            CreateUIItem("Enabling HTTP/2 for only a couple hosts");

            // Add new settings. New connections to these hosts will use these settings instead of the global one.
            // By default both connection pooling and HTTP/2 are enabled, downloading the images again will be done over HTTP/2.
            HTTPManager.PerHostSettings.Add(host_1, new HostSettings());
            HTTPManager.PerHostSettings.Add(host_2, new HostSettings());
#else
            CreateUIItem("Please note that this sample can't work under WebGL!");
#endif

            await DownloadImages();
        }

        async Task DownloadImages()
        {
            // Images from the first host
            foreach (var path in host_1_image_paths)
            {
                var request = HTTPRequest.CreateGet(new Uri(host_1, path));
                request.DownloadSettings.DisableCache = true;
                var resp = await request.GetHTTPResponseAsync();

                CreateUIItemWithImage(host_1.Host)
                    .SetStatusText($"http/{resp.HTTPVersion}")
                    .SetImage(resp.DataAsTexture2D);
            }

            // Images from the second host
            foreach (var path in host_2_image_paths)
            {
                var request = HTTPRequest.CreateGet(new Uri(host_2, path));
                request.DownloadSettings.DisableCache = true;
                var resp = await request.GetHTTPResponseAsync();

                CreateUIItemWithImage(host_2.Host)
                    .SetStatusText($"http/{resp.HTTPVersion}")
                    .SetImage(resp.DataAsTexture2D);
            }
        }

        private void OnDisable()
            => HTTPManager.PerHostSettings.Clear();

        MultiTextListItem CreateUIItem(string str)
            => Instantiate<MultiTextListItem>(this._listItemPrefab, this._contentRoot)
                .SetText(str) as MultiTextListItem;

        TextWithImageListItem CreateUIItemWithImage(string str)
                    => Instantiate<TextWithImageListItem>(this._listItemWithImagePrefab, this._contentRoot)
                        .SetText(str) as TextWithImageListItem;
    }
}

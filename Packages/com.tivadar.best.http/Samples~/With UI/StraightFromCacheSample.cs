using System;
using System.Threading.Tasks;

using Best.HTTP.Caching;
using Best.HTTP.Examples.Helpers;
using Best.HTTP.Shared;

using UnityEngine;

namespace Best.HTTP.Examples
{
    class StraightFromCacheSample : SampleBase
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

        /// <summary>
        /// Address of the used end point.
        /// </summary>
#if UNITY_ANDROID && !UNITY_EDITOR
        private string _baseAddress = "https://besthttpwebgldemo.azurewebsites.net/AssetBundles/Android/demobundle.assetbundle";
#else
        private string _baseAddress = "https://besthttpwebgldemo.azurewebsites.net/AssetBundles/WebGL/demobundle.assetbundle";
#endif

        protected override async void Start()
        {
            base.Start();

            var uri = new Uri(this._baseAddress);

            CreateUIItem("Calling await LoadImageFromLocalCacheAsync");

            // Try load the bundle and texture from local cache
            var texture = await LoadImageFromLocalCacheAsync(uri);

            // If it fails, download
            if (texture == null)
            {
                CreateUIItem("Content isn't cached yet! Downloading....");

                await DownloadIntoLocalCache(uri);

                CreateUIItem("Done! Retrying LoadImageFromLocalCacheAsync");

                texture = await LoadImageFromLocalCacheAsync(uri);
            }

            if (texture == null)
                CreateUIItem("Couldn't load image!");
            else
                CreateUIItemWithImage("Image loaded!")
                    .SetImage(texture);
        }

        async Task DownloadIntoLocalCache(Uri uri)
        {
            var request = HTTPRequest.CreateGet(uri);

            // No content will be stored in memory.
            request.DownloadSettings.CacheOnly = true;

            try
            {
                await request.GetHTTPResponseAsync();
            }
            catch (AsyncHTTPException ex)
            {
                Debug.LogException(ex);
            }
        }

#if UNITY_2023_1_OR_NEWER
        async 
#endif
            Task<Texture2D> LoadImageFromLocalCacheAsync(Uri uri)
        {
#if UNITY_2023_1_OR_NEWER
            // If Setup isn't called yet, HTTPManager.LocalCache isn't created yet either and would be null.
            HTTPManager.Setup();

            var hash = HTTPCache.CalculateHash(HTTPMethods.Get, uri);

            // Call BeginReadContent to try to acquire a Stream to the cached content
            var stream = HTTPManager.LocalCache.BeginReadContent(hash, null);

            if (stream == null)
                return null;

            try
            {
                var bundleLoadAsyncOp = AssetBundle.LoadFromStreamAsync(stream);

                // Unity's GetAwaiter extension is typeless, we can await it but can't await and return with the asset bundle.
                //  See "Loading resources asynchronously" at https://docs.unity3d.com/2023.2/Documentation/Manual/AwaitSupport.html
                await Awaitable.FromAsyncOperation(bundleLoadAsyncOp);

                var assetBundle = bundleLoadAsyncOp.assetBundle;

                var resourceLoadAsyncOp = assetBundle.LoadAssetAsync<Texture2D>("9443182_orig");
                await Awaitable.FromAsyncOperation(resourceLoadAsyncOp);

                return resourceLoadAsyncOp.asset as Texture2D;
            }
            finally
            {
                // If we called BeginReadContent and it's returned with a non-null value, we have to call EndReadContent too!
                HTTPManager.LocalCache.EndReadContent(hash, null);

                // We are responsible disposing the stream!
                stream?.Dispose();
            }
#else
            return null;
#endif
        }

        private void OnDestroy()
            => AssetBundle.UnloadAllAssetBundles(true);

        MultiTextListItem CreateUIItem(string str)
            => Instantiate<MultiTextListItem>(this._listItemPrefab, this._contentRoot)
                .SetText(str) as MultiTextListItem;

        TextWithImageListItem CreateUIItemWithImage(string str)
            => Instantiate<TextWithImageListItem>(this._listItemWithImagePrefab, this._contentRoot)
                .SetText(str) as TextWithImageListItem;
    }
}

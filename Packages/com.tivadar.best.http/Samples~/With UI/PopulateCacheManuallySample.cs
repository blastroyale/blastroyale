using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Best.HTTP.Examples.Helpers;
using Best.HTTP.Response;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.PlatformSupport.Memory;

using UnityEngine;
using UnityEngine.Scripting;

namespace Best.HTTP.Examples
{
    /// <summary>
    /// Example demonstrating usage of BeginCache, EndCache functions and HTTPCacheContentWriter class to manually store content for an url.
    /// </summary>
    class PopulateCacheManuallySample : SampleBase
    {
        // Constants passed to the API endpoint stored in _baseAddress
        const int FromUserId = 1;
        const int ToUserId = 10;

        /// <summary>
        /// String template used for generating unique uris for every user stored individually in the local cache.
        /// </summary>
        const string LocalCacheUserURLTemplate = "httpcache://userapi/user/{0}";

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
        private string _baseAddress = "https://besthttpwebgldemo.azurewebsites.net/users/{0}/{1}";

        protected override async void Start()
        {
            base.Start();

            CreateUIItem("Loading users...");

            await PopulateLocalCache();
            await LoadUsersFromLocalCache();
        }

        async Task PopulateLocalCache()
        {
            try
            {
                // Load a list of users from the server
                var users = await HTTPRequest.CreateGet(string.Format(_baseAddress, FromUserId, ToUserId))
                    .GetFromJsonResultAsync<List<User>>();

                CreateUIItem($"Received {users.Count} users from /users/{FromUserId}/{ToUserId}");

                StoreUsersInLocalCache(users);
            }
            catch (AsyncHTTPException ex)
            {
                CreateUIItem($"/Users request failed: {ex.Message}");
            }
        }

        void StoreUsersInLocalCache(List<User> users)
        {
            // Go over all the users and save them to the local cache with a custom uri
            foreach (var user in users)
            {
                var userUri = new Uri(string.Format(LocalCacheUserURLTemplate, user.Id));

                // convert the user object to json string and get the string's bytes
                var content = UserToByteArray(user);

                // BeginCache expects at least one caching header ("cache-control" with "max-age" directive, "etag", "expires" or "last-modified").
                // A cache-control with a max-age directive is a good choice for fabricated urls as the plugin will not try to valide the content's freshness.
                var headers = new Dictionary<string, List<string>> { { "cache-control", new List<string> { $"max-age={TimeSpan.FromDays(360).TotalSeconds}" } } };

                // Start the caching procedure by calling BeginCache and pass all the required parameters.
                var cacheWriter = HTTPManager.LocalCache.BeginCache(HTTPMethods.Get, userUri, HTTPStatusCodes.OK, headers, null);

                // If the writer is null, something prevents caching, these can be one of the following:
                //  - caching itself isn't supported
                //  - userUri doesn't start with 'http'
                //  - can't acquire a write lock on the resource (a request currently reading or writing the same uri)
                //  - IO error
                //  - If the headers passed to BeginCache has a "content-length" header too, BeginCache checks whether it's in the limits of the cache. If it can make enough room to fit a null value will be returned.
                if (cacheWriter == null)
                {
                    CreateUIItem($"Can't cache user('{user.Id}')!");
                    continue;
                }

                try
                {
                    // Write content to the cache.
                    // Under the hood this writes to a file, if presumably it will take a long time (large content and/or slow media) it's advised to use a Thread.
                    cacheWriter.Write(content);

                    // Finish the caching process by calling EndCache.
                    // This call will do all housekeeping, like disposing internal streams and releasing the write lock on the uri.
                    // After this call, requests to this very same uri will load and serve from the local cache.
                    HTTPManager.LocalCache.EndCache(cacheWriter, true, null);

                    CreateUIItem($"User cached as '{userUri}'");
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);

                    // We must call EndCache even if there's an error, otherwise not all resources are disposed and
                    //  new attempts to write to this cache entry will fail!
                    HTTPManager.LocalCache.EndCache(cacheWriter, false, null);

                    CreateUIItem($"Writing to cache failed with user '{user.Id}'");
                }
                finally
                {
                    // content's byte[] is borrowed from the BufferPool, it's a nice thing to return it!
                    BufferPool.Release(content);
                }
            }
        }

        /// <summary>
        /// This function will test/emulate requests loading cached individual users
        /// </summary>
        async Task LoadUsersFromLocalCache()
        {
            for (int id = FromUserId; id < ToUserId; id++)
            {
                // Create and execute a HTTP request to get one individual user's data from the local cache
                var user = await HTTPRequest.CreateGet(string.Format(LocalCacheUserURLTemplate, id))
                    .GetFromJsonResultAsync<User>();

                CreateUIItem($"User '{user.Id}' loaded from local cache (uri: '{string.Format(LocalCacheUserURLTemplate, id)}')!");
            }
        }

        /// <summary>
        /// Converts a User object to JSon string and returns with the byte representation of the string
        /// </summary>
        BufferSegment UserToByteArray(User user)
        {
            string json = JSON.LitJson.JsonMapper.ToJson(user);

            int length = Encoding.UTF8.GetByteCount(json);

            byte[] result = BufferPool.Get(length, true);

            Encoding.UTF8.GetBytes(json, 0, json.Length, result, 0);

            return result.AsBuffer(length);
        }

        MultiTextListItem CreateUIItem(string str)
            => Instantiate<MultiTextListItem>(this._listItemPrefab, this._contentRoot)
                .SetText(str) as MultiTextListItem;
    }

    [Preserve]
    class User
    {
        [Preserve] public string Id { get; set; }
        [Preserve] public string Name { get; set; }
        [Preserve] public DateTime Joined { get; set; }
    }
}

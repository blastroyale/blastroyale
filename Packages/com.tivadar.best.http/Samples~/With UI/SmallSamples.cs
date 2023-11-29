using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Best.HTTP.Cookies;
using Best.HTTP.Examples.Helpers;
using Best.HTTP.Request.Authentication;
using Best.HTTP.Request.Authenticators;
using Best.HTTP.Shared.PlatformSupport.IL2CPP;

using UnityEngine;

namespace Best.HTTP.Examples
{
    /// <summary>
    /// Collection of small samples that can be fit in one method and an additional callback. Result of the requests are logged/displayed on the UI.
    /// </summary>
    sealed class SmallSamples : SampleBase
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
        private string _baseAddress = "https://httpbingo.org";

        /// <summary>
        /// Queue of the samples executed sequentially.
        /// </summary>
        private Queue<Action> examples = new Queue<Action>();

        private AssetBundle _assetBundleToUnload;

        protected override void Start()
        {
            base.Start();

            this.examples.Enqueue(SimpleGet);
            this.examples.Enqueue(GetImageFromAssetBundle);

            this.examples.Enqueue(PostRaw);
            this.examples.Enqueue(PostUrlEncoded);
            this.examples.Enqueue(PostMultiPartEncoded);
            this.examples.Enqueue(PostJSon);

            this.examples.Enqueue(Redirect);
            this.examples.Enqueue(AbsoluteRedirect);
            this.examples.Enqueue(RelativeRedirect);
            this.examples.Enqueue(RedirectTo);
            
            this.examples.Enqueue(DecodeBase64);
            this.examples.Enqueue(EncodeBase64);
            
            this.examples.Enqueue(ExpectedToSucceedBasicAuth);
            this.examples.Enqueue(ExpectedToFailBasicAuth);
            this.examples.Enqueue(ExpectedToSucceedDigestAuth);
            this.examples.Enqueue(ExpectedToFailDigestAuth);
            this.examples.Enqueue(BearerTokenAuth);
            
            this.examples.Enqueue(CachedForNSeconds);
            this.examples.Enqueue(CachedForNSeconds);
            this.examples.Enqueue(SendWithETag);
            
            this.examples.Enqueue(CheckCookies);
            this.examples.Enqueue(SetCustomCookies);
            this.examples.Enqueue(RequestCookies);
            this.examples.Enqueue(DeleteCookies);
            
            this.examples.Enqueue(GetGZip);
            this.examples.Enqueue(GetDeflate);
            this.examples.Enqueue(GetBrotli);

            this.examples.Enqueue(GetJPEGResponse);
            this.examples.Enqueue(GetPNGResponse);
            this.examples.Enqueue(GetJSonResponseAsync);

            ExecuteNext();
        }

        private void OnDisable()
        {
            Resources.UnloadUnusedAssets();
#if UNITY_2023_1_OR_NEWER
            _assetBundleToUnload?.UnloadAsync(true);
#else
            _assetBundleToUnload?.Unload(true);
#endif
        }

        void SimpleGet()
        {
            var request = HTTPRequest.CreateGet($"{_baseAddress}/get", SimpleCallback);
            request.Tag = CreateUIItem("Simple GET");
            request.Send();
        }

        async void GetImageFromAssetBundle()
        {
            var uiEntry = CreateUIItemWithImage($"Image from AssetBundle (using GetAssetBundleAsync with async-await)");

            string url = null;

#if UNITY_ANDROID && !UNITY_EDITOR
            url = "https://besthttpwebgldemo.azurewebsites.net/AssetBundles/Android/demobundle.assetbundle";
#else
            url = "https://besthttpwebgldemo.azurewebsites.net/AssetBundles/WebGL/demobundle.assetbundle";
#endif

            var request = HTTPRequest.CreateGet(url);
            try
            {
                _assetBundleToUnload = await request.GetAssetBundleAsync();

                var texture = _assetBundleToUnload.LoadAsset<Texture2D>("9443182_orig");

                uiEntry.SetImage(texture);
            }
            catch (AsyncHTTPException ex)
            {
                uiEntry.SetStatusText(ex.Message);
            }
            catch (Exception ex)
            {
                uiEntry.SetStatusText(ex.Message);
            }
            finally
            {
                ExecuteNext();
            }
        }

        void PostRaw()
        {
            var request = HTTPRequest.CreatePost($"{_baseAddress}/post", SimpleCallback);

            var data = new byte[] { 1, 2, 3, 4 };
            request.UploadSettings.UploadStream = new MemoryStream(data);

            request.Tag = CreateUIItem("POST - Raw");
            request.Send();
        }

        void PostUrlEncoded()
        {
            var request = HTTPRequest.CreatePost($"{_baseAddress}/post", SimpleCallback);

            var formStream = new Best.HTTP.Request.Upload.Forms.UrlEncodedStream();
            formStream.AddField("field 1", "value 1");
            formStream.AddField("field 2", "value 2");

            request.UploadSettings.UploadStream = formStream;

            request.Tag = CreateUIItem("POST - Url Encoded");
            request.Send();
        }

        void PostMultiPartEncoded()
        {
            var request = HTTPRequest.CreatePost($"{_baseAddress}/post", SimpleCallback);

            var formStream = new Best.HTTP.Request.Upload.Forms.MultipartFormDataStream();
            formStream.AddField("field 1", "value 1");
            formStream.AddField("field 2", "value 2");

            var data = new byte[] { 1, 2, 3, 4 };
            formStream.AddStreamField("binary field", new MemoryStream(data));

            request.UploadSettings.UploadStream = formStream;

            request.Tag = CreateUIItem("POST - Multipart/Form-Data Encoded");
            request.Send();
        }

        class SendAsJson { public string key1, key2; }
        void PostJSon()
        {
            var obj = new SendAsJson
            {
                key1 = "value 1",
                key2 = "value 2"
            };

            var request = HTTPRequest.CreatePost($"{_baseAddress}/post", SimpleCallback);

            request.UploadSettings.UploadStream = new Best.HTTP.Request.Upload.JSonDataStream<SendAsJson>(obj);

            request.Tag = CreateUIItem("POST - JSon");
            request.Send();
        }

        void Redirect()
        {
            var request = HTTPRequest.CreateGet($"{_baseAddress}/redirect/6", SimpleCallback);
            request.Tag = CreateUIItem("Redirect");
            request.Send();
        }

        void AbsoluteRedirect()
        {
            var request = HTTPRequest.CreateGet($"{_baseAddress}/absolute-redirect/6", SimpleCallback);
            request.Tag = CreateUIItem("Absolute Redirect");
            request.Send();
        }

        void RelativeRedirect()
        {
            var request = HTTPRequest.CreateGet($"{_baseAddress}/relative-redirect/6", SimpleCallback);
            request.Tag = CreateUIItem("Relative Redirect");
            request.Send();
        }

        void RedirectTo()
        {
            var request = HTTPRequest.CreateGet($"{_baseAddress}/redirect-to?status_code=307&url={Uri.EscapeUriString(_baseAddress)}", SimpleCallback);
            request.Tag = CreateUIItem("Redirect To");
            request.Send();
        }

        void DecodeBase64()
        {
            string base64 = "aHR0cGJpbmdvLm9yZw==";

            var request = HTTPRequest.CreateGet($"{_baseAddress}/base64/decode/{base64}", TextCallback);
            request.Tag = CreateUIItem("Decode Base64");
            request.Send();
        }

        void EncodeBase64()
        {
            string strToEncode = "httpbingo.org";

            var request = HTTPRequest.CreateGet($"{_baseAddress}/base64/encode/{Uri.EscapeUriString(strToEncode)}", TextCallback);
            request.Tag = CreateUIItem("Encode Base64");
            request.Send();
        }

        void ExpectedToSucceedBasicAuth()
        {
            DigestStore.Clear();

            var request = HTTPRequest.CreateGet($"{_baseAddress}/basic-auth/user/passwd", SimpleCallback);

            request.Authenticator = new CredentialAuthenticator(new Credentials(AuthenticationTypes.Basic, "user", "passwd"));

            request.Tag = CreateUIItem("Basic Auth (Expected to Succeed)");
            request.Send();
        }

        void ExpectedToFailBasicAuth()
        {
            DigestStore.Clear();

            var request = HTTPRequest.CreateGet($"{_baseAddress}/basic-auth/user/passwd", SimpleCallback);

            // Here we set a wrong password
            request.Authenticator = new CredentialAuthenticator(new Credentials(AuthenticationTypes.Basic, "user", "wrong passwd"));

            request.Tag = CreateUIItem("Basic Auth (Expected to Fail)");
            request.Send();
        }

        void ExpectedToSucceedDigestAuth()
        {
            DigestStore.Clear();

            var request = HTTPRequest.CreateGet($"{_baseAddress}/digest-auth/auth/user/passwd/MD5", SimpleCallback);

            request.Authenticator = new CredentialAuthenticator(new Credentials(AuthenticationTypes.Digest, "user", "passwd"));

            request.Tag = CreateUIItem("Digest Auth (Expected to Succeed)");
            request.Send();
        }

        void ExpectedToFailDigestAuth()
        {
            DigestStore.Clear();

            var request = HTTPRequest.CreateGet($"{_baseAddress}/digest-auth/auth/user/passwd/MD5", SimpleCallback);

            // Here we set a wrong password
            request.Authenticator = new CredentialAuthenticator(new Credentials(AuthenticationTypes.Digest, "user", "wrong passwd"));

            request.Tag = CreateUIItem("Digest Auth (Expected to Succeed)");
            request.Send();
        }

        void BearerTokenAuth()
        {
            var request = HTTPRequest.CreateGet($"{_baseAddress}/bearer", SimpleCallback);

            // here we create a fake token by base64 encoding some bytes. httpbingo just checks for the Authorization header and whether it's bearer or not.
            string bearerToken = Convert.ToBase64String(Encoding.UTF8.GetBytes("<bearer token>"));
            request.Authenticator = new BearerTokenAuthenticator(bearerToken);

            request.Tag = CreateUIItem("Bearer Token");
            request.Send();
        }

        void CachedForNSeconds()
        {
            const int CacheTime = 30;

            // By calling /cache/30 the server will send caching headers that informs the client that the content is cachable and valid for 30 seconds.
            // For 30 seconds, request to the same uri should return the content from the local cache.
            var request = HTTPRequest.CreateGet($"{_baseAddress}/cache/{CacheTime}", CheckCachedCallback);
            request.Tag = CreateUIItem($"Cached content for {CacheTime} seconds");
            request.Send();
        }

        void SendWithETag()
        {
            const string etag = "<my etag>";

            var request = HTTPRequest.CreateGet($"{_baseAddress}/etag/{etag}", CheckCachedCallback);
            request.Tag = CreateUIItem($"Request response with ETag");
            request.Send();
        }

        void CheckCookies()
        {
            var request = HTTPRequest.CreateGet($"{_baseAddress}/cookies", CookiesCallback);
            request.Tag = CreateUIItem($"Check # of Cookies");
            request.Send();
        }

        void SetCustomCookies()
        {
            var uri = new Uri($"{_baseAddress}/cookies");
            CookieJar.Set(uri, new Cookie("custom1", "value1"));
            CookieJar.Set(uri, new Cookie("custom2", "value2"));

            var request = HTTPRequest.CreateGet(uri, CookiesCallback);
            request.Tag = CreateUIItem($"Set Custom Cookies");
            request.Send();
        }

        void RequestCookies()
        {
            // Ask the server to set a few cookies for us.

            var request = HTTPRequest.CreateGet($"{_baseAddress}/cookies/set?k1=v1&k2=v2", CookiesCallback);
            request.Tag = CreateUIItem($"Request Cookies from Server");
            request.Send();
        }

        void DeleteCookies()
        {
            var request = HTTPRequest.CreateGet($"{_baseAddress}/cookies/delete?k1=", CookiesCallback);
            request.Tag = CreateUIItem($"Delete One Cookie");
            request.Send();
        }

        void GetGZip()
        {
            var request = HTTPRequest.CreateGet($"{_baseAddress}/gzip", SimpleCallback);
            request.Tag = CreateUIItem($"gzip");
            request.Send();
        }

        void GetDeflate()
        {
            var request = HTTPRequest.CreateGet($"{_baseAddress}/deflate", SimpleCallback);
            request.Tag = CreateUIItem($"Deflate");
            request.Send();
        }

        void GetBrotli()
        {
            var request = HTTPRequest.CreateGet($"{_baseAddress}/brotli", SimpleCallback);
            request.Tag = CreateUIItem($"Brotli");
            request.Send();
        }

        void GetJPEGResponse()
        {
            var request = HTTPRequest.CreateGet($"{_baseAddress}/image/jpeg", ImageCallback);
            request.Tag = CreateUIItemWithImage($"Image (JPEG) response");
            request.Send();
        }

        void GetPNGResponse()
        {
            var request = HTTPRequest.CreateGet($"{_baseAddress}/image/png", ImageCallback);
            request.Tag = CreateUIItemWithImage($"Image (PNG) response");
            request.Send();
        }

#pragma warning disable 0649
        [Preserve]
        class JSonResult
        {
            [Preserve] public JSonSlideshow slideshow;
        }

        [Preserve]
        class JSonSlideshow
        {
            [Preserve] public string author;
            [Preserve] public string date;
            [Preserve] public string title;
        }
#pragma warning restore

        async void GetJSonResponseAsync()
        {
            var request = HTTPRequest.CreateGet($"{_baseAddress}/json");

            var uiEntry = CreateUIItem($"JSon response (GetFromJsonResultAsync<> with async-await)");

            try
            {
                var jsonResult = await request.GetFromJsonResultAsync<JSonResult>();

                var slideshow = jsonResult.slideshow;

                uiEntry.SetStatusText($"{slideshow.author}: {slideshow.title} ({slideshow.date})");
            }
            catch (AsyncHTTPException ex)
            {
                uiEntry.SetStatusText(ex.Message);
            }
            catch (Exception ex)
            {
                uiEntry.SetStatusText(ex.Message);
            }
            finally
            {
                ExecuteNext();
            }
        }

        /// <summary>
        /// A simple callback for HTTPRequest, called when the request finished either succesfully or with an error.
        /// </summary>
        /// <param name="req">The original HTTPRequest instance.</param>
        /// <param name="resp">Reference to a HTTPResponse object or null in case of an error.</param>
        private void SimpleCallback(HTTPRequest req, HTTPResponse resp)
        {
            var uiEntry = req.Tag as MultiTextListItem;
            
            if (resp != null)
                uiEntry.SetStatusText(resp.StatusCode.ToString());
            else
                uiEntry.SetStatusText(req.State.ToString());

            ExecuteNext();
        }

        /// <summary>
        /// Callback to display the received content as an utf-8 text.
        /// </summary>
        /// <param name="req">The original HTTPRequest instance.</param>
        /// <param name="resp">Reference to a HTTPResponse object or null in case of an error.</param>
        private void TextCallback(HTTPRequest req, HTTPResponse resp)
        {
            var uiEntry = req.Tag as MultiTextListItem;

            if (resp != null)
            {
                if (resp.IsSuccess)
                    uiEntry.SetStatusText(resp.DataAsText);
                else
                    uiEntry.SetStatusText(resp.StatusCode.ToString());
            }
            else
                uiEntry.SetStatusText(req.State.ToString());

            ExecuteNext();
        }

        /// <summary>
        /// Callback to display whether the content is loaded from the local cache or freshly downloaded from the server.
        /// </summary>
        /// <param name="req">The original HTTPRequest instance.</param>
        /// <param name="resp">Reference to a HTTPResponse object or null in case of an error.</param>
        private void CheckCachedCallback(HTTPRequest req, HTTPResponse resp)
        {
            var uiEntry = req.Tag as MultiTextListItem;

            if (resp != null)
            {
                if (resp.IsSuccess)
                    uiEntry.SetStatusText(resp.IsFromCache ? "from cache" : "fresh from the server");
                else
                    uiEntry.SetStatusText(resp.StatusCode.ToString());
            }
            else
                uiEntry.SetStatusText(req.State.ToString());

            ExecuteNext();
        }

        /// <summary>
        /// Callback to display the number of cookies stored for the request's uri.
        /// </summary>
        /// <param name="req">The original HTTPRequest instance.</param>
        /// <param name="resp">Reference to a HTTPResponse object or null in case of an error.</param>
        private void CookiesCallback(HTTPRequest req, HTTPResponse resp)
        {
            var uiEntry = req.Tag as MultiTextListItem;

            if (resp != null)
            {
                if (resp.IsSuccess)
                {
                    var cookies = CookieJar.Get(req.CurrentUri);
                    int cookieCount = 0;
                    if (cookies != null)
                        cookieCount = cookies.Count;

                    uiEntry.SetStatusText($"Cookies: {cookieCount}");
                }
                else
                    uiEntry.SetStatusText(resp.StatusCode.ToString());
            }
            else
                uiEntry.SetStatusText(req.State.ToString());

            ExecuteNext();
        }

        /// <summary>
        /// Callback to display the content as image/texture.
        /// </summary>
        /// <param name="req">The original HTTPRequest instance.</param>
        /// <param name="resp">Reference to a HTTPResponse object or null in case of an error.</param>
        private void ImageCallback(HTTPRequest req, HTTPResponse resp)
        {
            var uiEntry = req.Tag as TextWithImageListItem;

            if (resp != null)
            {
                if (resp.IsSuccess)
                {
                    uiEntry.SetImage(resp.DataAsTexture2D);
                }
                else
                    uiEntry.SetStatusText(resp.StatusCode.ToString());
            }
            else
                uiEntry.SetStatusText(req.State.ToString());

            ExecuteNext();
        }

        void ExecuteNext()
        {
            if (this.examples.Count > 0)
                this.examples.Dequeue()?.Invoke();
        }

        MultiTextListItem CreateUIItem(string str)
            => Instantiate<MultiTextListItem>(this._listItemPrefab, this._contentRoot)
                .SetText(str) as MultiTextListItem;

        TextWithImageListItem CreateUIItemWithImage(string str)
            => Instantiate<TextWithImageListItem>(this._listItemWithImagePrefab, this._contentRoot)
                .SetText(str) as TextWithImageListItem;
    }
}

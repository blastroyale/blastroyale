using System;
using System.Collections.Generic;
using System.Threading;

using Best.HTTP.Hosts.Connections;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.PlatformSupport.FileSystem;

namespace Best.HTTP.Cookies
{
    /// <summary>
    /// The Cookie Jar implementation based on RFC 6265(http://tools.ietf.org/html/rfc6265).
    /// </summary>
    public static class CookieJar
    {
        /// <summary>
        /// Maximum size of the Cookie Jar in bytes. It's default value is 10485760 (10 MB).
        /// </summary>
        public static uint MaximumSize { get; set; } = 10 * 1024 * 1024;

        // Version of the cookie store. It may be used in a future version for maintaining compatibility.
        private const int Version = 1;

        /// <summary>
        /// Returns true if File apis are supported.
        /// </summary>
        public static bool IsSavingSupported
        {
            get
            {
                if (IsSupportCheckDone)
                    return _isSavingSupported;

                try
                {
#if UNITY_WEBGL && !UNITY_EDITOR
                    _isSavingSupported = false;
#else
                    HTTPManager.IOService.DirectoryExists(HTTPManager.GetRootSaveFolder());
                    _isSavingSupported = true;
#endif
                }
                catch
                {
                    _isSavingSupported = false;

                    HTTPManager.Logger.Warning("CookieJar", "Cookie saving and loading disabled!");
                }
                finally
                {
                    IsSupportCheckDone = true;
                }

                return _isSavingSupported;
            }
        }

        /// <summary>
        /// The plugin will delete cookies that are accessed this threshold ago. Its default value is 7 days.
        /// </summary>
        public static TimeSpan AccessThreshold = TimeSpan.FromDays(7);

        /// <summary>
        /// If this property is set to <c>true</c>, then new cookies treated as session cookies and these cookies are not saved to disk. Its default value is <c>false</c>.
        /// </summary>
        public static bool IsSessionOverride = false;

#region Privates

        /// <summary>
        /// List of the Cookies
        /// </summary>
        private static List<Cookie> Cookies = new List<Cookie>();
        private static string CookieFolder { get; set; }
        private static string LibraryPath { get; set; }

        /// <summary>
        /// Synchronization object for thread safety.
        /// </summary>
        private static ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private static bool _isSavingSupported;
        private static bool IsSupportCheckDone;

        private static bool Loaded;
        private static RunOnceOnMainThread _saveLibraryRunner = new RunOnceOnMainThread(Persist, null);

#endregion

#region Internal Functions

        internal static void SetupFolder()
        {
            if (!CookieJar.IsSavingSupported)
                return;

            try
            {
                if (string.IsNullOrEmpty(CookieFolder) || string.IsNullOrEmpty(LibraryPath))
                {
                    CookieFolder = System.IO.Path.Combine(HTTPManager.GetRootSaveFolder(), "Cookies");
                    LibraryPath = System.IO.Path.Combine(CookieFolder, "Library");
                }
            }
            catch
            { }
        }

        /// <summary>
        /// Will set or update all cookies from the response object.
        /// </summary>
        internal static bool SetFromRequest(HTTPResponse response)
        {
            if (response == null)
                return false;

            List<Cookie> newCookies = new List<Cookie>();
            var setCookieHeaders = response.GetHeaderValues("set-cookie");

            // No cookies. :'(
            if (setCookieHeaders == null)
                return false;

            foreach (var cookieHeader in setCookieHeaders)
            {
                Cookie cookie = Cookie.Parse(cookieHeader, response.Request.CurrentUri, response.Request.Context);
                if (cookie != null)
                {
                    rwLock.EnterWriteLock();
                    try
                    {
                        int idx;
                        var old = Find(cookie, out idx);

                        // if no value for the cookie or already expired then the server asked us to delete the cookie
                        bool expired = string.IsNullOrEmpty(cookie.Value) || !cookie.WillExpireInTheFuture();

                        if (!expired)
                        {
                            // no old cookie, add it straight to the list
                            if (old == null)
                            {
                                Cookies.Add(cookie);

                                newCookies.Add(cookie);
                            }
                            else
                            {
                                // Update the creation-time of the newly created cookie to match the creation-time of the old-cookie.
                                cookie.Date = old.Date;
                                Cookies[idx] = cookie;

                                newCookies.Add(cookie);
                            }
                        }
                        else if (idx != -1) // delete the cookie
                            Cookies.RemoveAt(idx);
                    }
                    catch
                    {
                        // Ignore cookie on error
                    }
                    finally
                    {
                        rwLock.ExitWriteLock();
                    }
                }
            }

            _saveLibraryRunner.Subscribe();

            return true;
        }

        internal static void SetupRequest(HTTPRequest request)
        {
            // Cookies
            // User added cookies are sent even when IsCookiesEnabled is set to false
            List<Cookie> cookies = CookieJar.Get(request.CurrentUri);

            // http://tools.ietf.org/html/rfc6265#section-5.4
            //  -When the user agent generates an HTTP request, the user agent MUST NOT attach more than one Cookie header field.
            if (cookies != null && cookies.Count > 0)
            {
                // Room for improvement:
                //   2. The user agent SHOULD sort the cookie-list in the following order:
                //      *  Cookies with longer paths are listed before cookies with shorter paths.
                //      *  Among cookies that have equal-length path fields, cookies with earlier creation-times are listed before cookies with later creation-times.

                bool first = true;
                string cookieStr = string.Empty;

                bool isSecureProtocolInUse = HTTPProtocolFactory.IsSecureProtocol(request.CurrentUri);

                foreach (var cookie in cookies)
                    if (!cookie.IsSecure || (cookie.IsSecure && isSecureProtocolInUse))
                    {
                        if (!first)
                            cookieStr += "; ";
                        else
                            first = false;

                        cookieStr += cookie.ToString();

                        // 3. Update the last-access-time of each cookie in the cookie-list to the current date and time.
                        cookie.LastAccess = DateTime.Now;
                    }

                if (!string.IsNullOrEmpty(cookieStr))
                    request.SetHeader("Cookie", cookieStr);
            }
        }

        /// <summary>
        /// Deletes all expired or 'old' cookies, and will keep the sum size of cookies under the given size.
        /// </summary>
        internal static void Maintain(bool sendEvent)
        {
            // It's not the same as in the rfc:
            //  http://tools.ietf.org/html/rfc6265#section-5.3

            rwLock.EnterWriteLock();
            try
            {
                uint size = 0;

                for (int i = 0; i < Cookies.Count; )
                {
                    var cookie = Cookies[i];

                    // Remove expired or not used cookies
                    if (!cookie.WillExpireInTheFuture() || (cookie.LastAccess + AccessThreshold) < DateTime.Now)
                    {
                        Cookies.RemoveAt(i);
                    }
                    else
                    {
                        if (!cookie.IsSession)
                            size += cookie.GuessSize();
                        i++;
                    }
                }

                if (size > MaximumSize)
                {
                    Cookies.Sort();

                    while (size > MaximumSize && Cookies.Count > 0)
                    {
                        var cookie = Cookies[0];
                        Cookies.RemoveAt(0);

                        size -= cookie.GuessSize();
                    }
                }
            }
            catch
            { }
            finally
            {
                rwLock.ExitWriteLock();
            }

            if (sendEvent)
                _saveLibraryRunner.Subscribe();
        }

        /// <summary>
        /// Saves the Cookie Jar to a file.
        /// </summary>
        /// <remarks>Not implemented under Unity WebPlayer</remarks>
        internal static void Persist()
        {
            if (!IsSavingSupported)
                return;

            if (!Loaded)
                return;

            // Delete any expired cookie
            Maintain(false);

            rwLock.EnterWriteLock();
            try
            {
                if (!HTTPManager.IOService.DirectoryExists(CookieFolder))
                    HTTPManager.IOService.DirectoryCreate(CookieFolder);

                using (var fs = HTTPManager.IOService.CreateFileStream(LibraryPath, FileStreamModes.Create))
                using (var bw = new System.IO.BinaryWriter(fs))
                {
                    bw.Write(Version);

                    // Count how many non-session cookies we have
                    int count = 0;
                    foreach (var cookie in Cookies)
                        if (!cookie.IsSession)
                            count++;

                    bw.Write(count);

                    // Save only the persistable cookies
                    foreach (var cookie in Cookies)
                        if (!cookie.IsSession)
                            cookie.SaveTo(bw);
                }
            }
            catch
            { }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Load previously persisted cookie library from the file.
        /// </summary>
        internal static void Load()
        {
            if (!IsSavingSupported)
                return;

            if (Loaded)
                return;

            SetupFolder();

            rwLock.EnterWriteLock();
            try
            {
                Cookies.Clear();

                if (!HTTPManager.IOService.DirectoryExists(CookieFolder))
                    HTTPManager.IOService.DirectoryCreate(CookieFolder);

                if (!HTTPManager.IOService.FileExists(LibraryPath))
                    return;

                using (var fs = HTTPManager.IOService.CreateFileStream(LibraryPath, FileStreamModes.OpenRead))
                using (var br = new System.IO.BinaryReader(fs))
                {
                    /*int version = */br.ReadInt32();
                    int cookieCount = br.ReadInt32();

                    for (int i = 0; i < cookieCount; ++i)
                    {
                        Cookie cookie = new Cookie();
                        cookie.LoadFrom(br);

                        if (cookie.WillExpireInTheFuture())
                            Cookies.Add(cookie);
                    }
                }
            }
            catch
            {
                Cookies.Clear();
            }
            finally
            {
                Loaded = true;
                rwLock.ExitWriteLock();
            }
        }
                
        #endregion

        #region Public Functions

        /// <summary>
        /// Returns all Cookies that corresponds to the given Uri.
        /// </summary>
        public static List<Cookie> Get(Uri uri)
        {
            Load();

            rwLock.EnterReadLock();
            try
            {
                List<Cookie> result = null;

                for (int i = 0; i < Cookies.Count; ++i)
                {
                    Cookie cookie = Cookies[i];

                    if (cookie == null)
                        continue;

                    bool willExpireInTheFuture = cookie.WillExpireInTheFuture();
                    bool domainMatch = uri.Host.IndexOf(cookie.Domain) != -1 || string.Format("{0}:{1}", uri.Host, uri.Port).IndexOf(cookie.Domain) != -1;

                    if (willExpireInTheFuture && domainMatch)
                    {
                        string requestPath = uri.AbsolutePath;

                        // https://datatracker.ietf.org/doc/html/rfc6265#section-5.1.4
                        // A request-path path-matches a given cookie-path if at least one of
                        //    the following conditions holds:
                        //    o  The cookie-path and the request-path are identical.
                        bool exactMatch = cookie.Path.Equals(requestPath, StringComparison.Ordinal);

                        //    o  The cookie-path is a prefix of the request-path, and the last
                        //       character of the cookie-path is %x2F ("/").
                        bool prefixMatch = cookie.Path[cookie.Path.Length - 1] == '/' && requestPath.StartsWith(cookie.Path, StringComparison.Ordinal);

                        //    o  The cookie-path is a prefix of the request-path, and the first
                        //       character of the request-path that is not included in the cookie-
                        //       path is a %x2F ("/") character.
                        bool prefixMatch2 = requestPath.Length > cookie.Path.Length &&
                                            requestPath.StartsWith(cookie.Path, StringComparison.Ordinal) &&
                                            requestPath[cookie.Path.Length] == '/';

                        if (exactMatch || prefixMatch || prefixMatch2)
                        {
                            if (result == null)
                                result = new List<Cookie>();

                            result.Add(cookie);
                        }
                    }
                }

                return result;
            }
            finally
            {
                rwLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Will add a new, or overwrite an old cookie if already exists.
        /// </summary>
        public static void Set(Uri uri, Cookie cookie)
        {
            cookie.Domain = uri.Host;
            cookie.Path = uri.AbsolutePath;

            Set(cookie);
        }

        /// <summary>
        /// Will add a new, or overwrite an old cookie if already exists.
        /// </summary>
        public static void Set(Cookie cookie)
        {
            Load();

            rwLock.EnterWriteLock();
            try
            {
                int idx;
                Find(cookie, out idx);

                if (idx >= 0)
                    Cookies[idx] = cookie;
                else
                    Cookies.Add(cookie);
            }
            finally
            {
                rwLock.ExitWriteLock();
            }

            _saveLibraryRunner.Subscribe();
        }

        public static List<Cookie> GetAll()
        {
            Load();

            return Cookies;
        }

        /// <summary>
        /// Deletes all cookies from the Jar.
        /// </summary>
        public static void Clear()
        {
            Load();

            rwLock.EnterWriteLock();
            try
            {
                Cookies.Clear();
            }
            finally
            {
                rwLock.ExitWriteLock();
            }

            Persist();
        }

        /// <summary>
        /// Removes cookies that older than the given parameter.
        /// </summary>
        public static void Clear(TimeSpan olderThan)
        {
            Load();

            rwLock.EnterWriteLock();
            try
            {
                for (int i = 0; i < Cookies.Count; )
                {
                    var cookie = Cookies[i];

                    // Remove expired or not used cookies
                    if (!cookie.WillExpireInTheFuture() || (cookie.Date + olderThan) < DateTime.Now)
                        Cookies.RemoveAt(i);
                    else
                        i++;
                }
            }
            finally
            {
                rwLock.ExitWriteLock();
            }

            Persist();
        }

        /// <summary>
        /// Removes cookies that matches to the given domain.
        /// </summary>
        public static void Clear(string domain)
        {
            Load();

            rwLock.EnterWriteLock();
            try
            {
                for (int i = 0; i < Cookies.Count; )
                {
                    var cookie = Cookies[i];

                    // Remove expired or not used cookies
                    if (!cookie.WillExpireInTheFuture() || cookie.Domain.IndexOf(domain) != -1)
                        Cookies.RemoveAt(i);
                    else
                        i++;
                }
            }
            finally
            {
                rwLock.ExitWriteLock();
            }

            Persist();
        }

        public static void Remove(Uri uri, string name)
        {
            Load();

            rwLock.EnterWriteLock();
            try
            {
                for (int i = 0; i < Cookies.Count; )
                {
                    var cookie = Cookies[i];

                    if (cookie.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && uri.Host.IndexOf(cookie.Domain) != -1)
                        Cookies.RemoveAt(i);
                    else
                        i++;
                }
            }
            finally
            {
                rwLock.ExitWriteLock();
            }

            _saveLibraryRunner.Subscribe();
        }

#endregion

#region Private Helper Functions

        /// <summary>
        /// Find and return a Cookie and his index in the list.
        /// </summary>
        private static Cookie Find(Cookie cookie, out int idx)
        {
            for (int i = 0; i < Cookies.Count; ++i)
            {
                Cookie c = Cookies[i];

                if (c.Equals(cookie))
                {
                    idx = i;
                    return c;
                }
            }

            idx = -1;
            return null;
        }

#endregion
    }
}

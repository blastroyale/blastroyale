using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.Logger;

namespace Best.HTTP.Cookies
{
    /// <summary>
    /// The Cookie implementation based on <see href="http://tools.ietf.org/html/rfc6265">RFC-6265</see>.
    /// </summary>
    public sealed class Cookie : IComparable<Cookie>, IEquatable<Cookie>
    {
        private const int Version = 1;

        #region Public Properties

        /// <summary>
        /// The name of the cookie.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The value of the cookie.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// The Date when the Cookie is registered.
        /// </summary>
        public DateTime Date { get; internal set; }

        /// <summary>
        /// When this Cookie last used in a request.
        /// </summary>
        public DateTime LastAccess { get; set; }

        /// <summary>
        /// The Expires attribute indicates the maximum lifetime of the cookie, represented as the date and time at which the cookie expires.
        /// The user agent is not required to retain the cookie until the specified date has passed.
        /// In fact, user agents often evict cookies due to memory pressure or privacy concerns.
        /// </summary>
        public DateTime Expires { get; private set; }

        /// <summary>
        /// The Max-Age attribute indicates the maximum lifetime of the cookie, represented as the number of seconds until the cookie expires.
        /// The user agent is not required to retain the cookie for the specified duration.
        /// In fact, user agents often evict cookies due to memory pressure or privacy concerns.
        /// </summary>
        public long MaxAge { get; private set; }

        /// <summary>
        /// If a cookie has neither the Max-Age nor the Expires attribute, the user agent will retain the cookie until "the current session is over".
        /// </summary>
        public bool IsSession { get; private set; }

        /// <summary>
        /// The Domain attribute specifies those hosts to which the cookie will be sent.
        /// For example, if the value of the Domain attribute is "example.com", the user agent will include the cookie
        /// in the Cookie header when making HTTP requests to example.com, www.example.com, and www.corp.example.com.
        /// If the server omits the Domain attribute, the user agent will return the cookie only to the origin server.
        /// </summary>
        public string Domain { get; internal set; }

        /// <summary>
        /// The scope of each cookie is limited to a set of paths, controlled by the Path attribute.
        /// If the server omits the Path attribute, the user agent will use the "directory" of the request-uri's path component as the default value.
        /// </summary>
        public string Path { get; internal set; }

        /// <summary>
        /// The Secure attribute limits the scope of the cookie to "secure" channels (where "secure" is defined by the user agent).
        /// When a cookie has the Secure attribute, the user agent will include the cookie in an HTTP request only if the request is
        /// transmitted over a secure channel (typically HTTP over Transport Layer Security (TLS)).
        /// </summary>
        public bool IsSecure { get; private set; }

        /// <summary>
        /// The HttpOnly attribute limits the scope of the cookie to HTTP requests.
        /// In particular, the attribute instructs the user agent to omit the cookie when providing access to
        /// cookies via "non-HTTP" APIs (such as a web browser API that exposes cookies to scripts).
        /// </summary>
        public bool IsHttpOnly { get; private set; }

        /// <summary>
        /// SameSite prevents the browser from sending this cookie along with cross-site requests.
        /// The main goal is mitigate the risk of cross-origin information leakage.
        /// It also provides some protection against cross-site request forgery attacks.
        /// Possible values for the flag are lax or strict.
        /// </summary>
        /// <remarks>
        /// More details can be found here:
        /// <list type="bullet">
        ///     <item><description><see href="https://web.dev/samesite-cookies-explained/">SameSite cookies explained</see></description></item>
        /// </list>
        /// </remarks>
        public string SameSite { get; private set; }

        #endregion

        #region Public Constructors

        public Cookie(string name, string value)
            :this(name, value, "/", string.Empty)
        {}

        public Cookie(string name, string value, string path)
            : this(name, value, path, string.Empty)
        {}

        public Cookie(string name, string value, string path, string domain)
            :this() // call the parameter-less constructor to set default values
        {
            this.Name = name;
            this.Value = value;
            this.Path = path;
            this.Domain = domain;
        }

        public Cookie(Uri uri, string name, string value, DateTime expires, bool isSession = true)
            :this(name, value, uri.AbsolutePath, uri.Host)
        {
            this.Expires = expires;
            this.IsSession = isSession;
            this.Date = DateTime.Now;
        }

        public Cookie(Uri uri, string name, string value, long maxAge = -1, bool isSession = true)
            :this(name, value, uri.AbsolutePath, uri.Host)
        {
            this.MaxAge = maxAge;
            this.IsSession = isSession;
            this.Date = DateTime.Now;
            this.SameSite = "none";
        }

        #endregion

        internal Cookie()
        {
            // If a cookie has neither the Max-Age nor the Expires attribute, the user agent will retain the cookie
            //  until "the current session is over" (as defined by the user agent).
            IsSession = true;
            MaxAge = -1;
            LastAccess = DateTime.Now;
        }

        public bool WillExpireInTheFuture()
        {
            // No Expires or Max-Age value sent from the server, we will fake the return value so we will not delete a freshly received cookie
            if (IsSession)
                return true;

            // If a cookie has both the Max-Age and the Expires attribute, the Max-Age attribute has precedence and controls the expiration date of the cookie.
            return MaxAge != -1 ?
                    Math.Max(0, (long)(DateTime.Now - Date).TotalSeconds) < MaxAge :
                    Expires > DateTime.Now;
        }

        /// <summary>
        /// Guess the storage size of the cookie.
        /// </summary>
        /// <returns></returns>
        public uint GuessSize()
        {
            return (uint)((this.Name != null ? this.Name.Length * sizeof(char) : 0) +
                          (this.Value != null ? this.Value.Length * sizeof(char) : 0) +
                          (this.Domain != null ? this.Domain.Length * sizeof(char) : 0) +
                          (this.Path != null ? this.Path.Length * sizeof(char) : 0) +
                          (this.SameSite != null ? this.SameSite.Length * sizeof(char) : 0) +
                          (sizeof(long) * 4) +
                          (sizeof(bool) * 3));
        }

        public static Cookie Parse(string header, Uri defaultDomain, LoggingContext context)
        {
            Cookie cookie = new Cookie();
            try
            {
                var kvps = ParseCookieHeader(header);

                foreach (var kvp in kvps)
                {
                    switch (kvp.Key.ToLowerInvariant())
                    {
                        case "path":
                            // https://datatracker.ietf.org/doc/html/rfc6265#section-5.1.4
                            // If the attribute-value is empty or if the first character of the attribute-value is not %x2F ("/"):
                            //  Let cookie-path be the default-path.
                            cookie.Path = string.IsNullOrEmpty(kvp.Value) || !kvp.Value.StartsWith("/") ? "/" : cookie.Path = kvp.Value;
                            break;

                        case "domain":
                            // If the attribute-value is empty, the behavior is undefined. However, the user agent SHOULD ignore the cookie-av entirely.
                            if (string.IsNullOrEmpty(kvp.Value))
                                return null;

                            // If the first character of the attribute-value string is %x2E ("."):
                            //  Let cookie-domain be the attribute-value without the leading %x2E (".") character.
                            cookie.Domain = kvp.Value.StartsWith(".") ? kvp.Value.Substring(1) : kvp.Value;
                            break;

                        case "expires":
                            cookie.Expires = kvp.Value.ToDateTime(DateTime.FromBinary(0));
                            cookie.IsSession = false;
                            break;

                        case "max-age":
                            cookie.MaxAge = kvp.Value.ToInt64(-1);
                            cookie.IsSession = false;
                            break;

                        case "secure":
                            cookie.IsSecure = true;
                            break;

                        case "httponly":
                            cookie.IsHttpOnly = true;
                            break;

                        case "samesite":
                            cookie.SameSite = kvp.Value;
                            break;

                        default:
                            // check whether name is already set to avoid overwriting it with a non-listed setting
                            if (string.IsNullOrEmpty(cookie.Name))
                            {
                                cookie.Name = kvp.Key;
                                cookie.Value = kvp.Value;
                            }
                            break;
                    }
                }

                // Some user agents provide users the option of preventing persistent storage of cookies across sessions.
                // When configured thusly, user agents MUST treat all received cookies as if the persistent-flag were set to false.
                if (CookieJar.IsSessionOverride)
                    cookie.IsSession = true;

                // http://tools.ietf.org/html/rfc6265#section-4.1.2.3
                // WARNING: Some existing user agents treat an absent Domain attribute as if the Domain attribute were present and contained the current host name.
                // For example, if example.com returns a Set-Cookie header without a Domain attribute, these user agents will erroneously send the cookie to www.example.com as well.
                if (string.IsNullOrEmpty(cookie.Domain))
                    cookie.Domain = defaultDomain.Host;

                // http://tools.ietf.org/html/rfc6265#section-5.3 section 7:
                // If the cookie-attribute-list contains an attribute with an attribute-name of "Path",
                // set the cookie's path to attribute-value of the last attribute in the cookie-attribute-list with an attribute-name of "Path".
                // __Otherwise, set the cookie's path to the default-path of the request-uri.__
                if (string.IsNullOrEmpty(cookie.Path))
                {
                    // https://datatracker.ietf.org/doc/html/rfc6265#section-5.1.4
                    // 1. Let uri-path be the path portion of the request-uri if such a portion exists
                    cookie.Path = defaultDomain.AbsolutePath;

                    // 2. If the uri-path is empty or if the first character of the uri-path is not a % x2F("/") character,
                    // output % x2F("/") and skip the remaining steps.
                    // Uri's AbsolutePath always emits at least a '/' character.

                    // 3. If the uri-path contains no more than one %x2F ("/") character,
                    // output % x2F("/") and skip the remaining step.
                    
                    int slashCount = 1;
                    int lastSlashIdx = 0;
                    for (int i = 1; i < cookie.Path.Length; i++)
                    {
                        if (cookie.Path[i] == '/')
                        {
                            slashCount++;
                            lastSlashIdx = i;
                        }
                    }

                    if (slashCount == 1)
                    {
                        cookie.Path = "/";
                    }
                    else
                    {
                        // 4. Output the characters of the uri-path from the first character up to,
                        // but not including, the right-most % x2F("/").

                        cookie.Path = cookie.Path.Substring(0, lastSlashIdx);
                    }
                }

                cookie.Date = cookie.LastAccess = DateTime.Now;
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Warning("Cookie", "Parse - Couldn't parse header: " + header + " exception: " + ex.ToString() + " " + ex.StackTrace, context);
            }
            return cookie;
        }

        #region Save & Load

        internal void SaveTo(BinaryWriter stream)
        {
            stream.Write(Version);
            stream.Write(Name ?? string.Empty);
            stream.Write(Value ?? string.Empty);
            stream.Write(Date.ToBinary());
            stream.Write(LastAccess.ToBinary());
            stream.Write(Expires.ToBinary());
            stream.Write(MaxAge);
            stream.Write(IsSession);
            stream.Write(Domain ?? string.Empty);
            stream.Write(Path ?? string.Empty);
            stream.Write(IsSecure);
            stream.Write(IsHttpOnly);
        }

        internal void LoadFrom(BinaryReader stream)
        {
            /*int version = */stream.ReadInt32();
            this.Name = stream.ReadString();
            this.Value = stream.ReadString();
            this.Date = DateTime.FromBinary(stream.ReadInt64());
            this.LastAccess = DateTime.FromBinary(stream.ReadInt64());
            this.Expires = DateTime.FromBinary(stream.ReadInt64());
            this.MaxAge = stream.ReadInt64();
            this.IsSession = stream.ReadBoolean();
            this.Domain = stream.ReadString();
            this.Path = stream.ReadString();
            this.IsSecure = stream.ReadBoolean();
            this.IsHttpOnly = stream.ReadBoolean();
        }

        #endregion

        #region Overrides and new Equals function

        public override string ToString()
        {
            return string.Concat(this.Name, "=", this.Value);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            return this.Equals(obj as Cookie);
        }

        public bool Equals(Cookie cookie)
        {
            if (cookie == null)
                return false;

            if (Object.ReferenceEquals(this, cookie))
                return true;

            return this.Name.Equals(cookie.Name, StringComparison.Ordinal) &&
                ((this.Domain == null && cookie.Domain == null) || this.Domain.Equals(cookie.Domain, StringComparison.Ordinal)) &&
                ((this.Path == null && cookie.Path == null) || this.Path.Equals(cookie.Path, StringComparison.Ordinal));
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        #endregion

        #region Private Helper Functions

        private static string ReadValue(string str, ref int pos)
        {
            string result = string.Empty;
            if (str == null)
                return result;

            return str.Read(ref pos, ';');
        }

        private static List<HeaderValue> ParseCookieHeader(string str)
        {
            List<HeaderValue> result = new List<HeaderValue>();

            if (str == null)
                return result;

            int idx = 0;

            // process the rest of the text
            while (idx < str.Length)
            {
                // Read key
                string key = str.Read(ref idx, (ch) => ch != '=' && ch != ';').Trim();
                HeaderValue qp = new HeaderValue(key);

                if (idx < str.Length && str[idx - 1] == '=')
                    qp.Value = ReadValue(str, ref idx);

                result.Add(qp);
            }

            return result;
        }

        #endregion

        #region IComparable<Cookie> implementation

        public int CompareTo(Cookie other)
        {
            return this.LastAccess.CompareTo(other.LastAccess);
        }

        #endregion
    }
}

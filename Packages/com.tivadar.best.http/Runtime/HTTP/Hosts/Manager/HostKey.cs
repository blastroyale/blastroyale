using System;

using Best.HTTP.Request.Settings;

using UnityEngine;

namespace Best.HTTP.HostSetting
{
    /// <summary>
    /// The <see cref="HostKey"/> struct represents a unique key for identifying hosts based on their <see cref="System.Uri"/> and <see cref="ProxySettings"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="HostKey"/> struct is designed to uniquely identify a host based on its URI (Uniform Resource Identifier) and optional proxy settings.
    /// It provides a way to create, compare, and hash host keys, enabling efficient host variant management in the <see cref="HostManager"/>.
    /// </para>
    /// <para>
    /// Key features of the <see cref="HostKey"/> struct include:
    /// </para>
    /// <list type="bullet">
    ///     <item>
    ///         <term>Uniqueness</term>
    ///         <description>
    ///         Each <see cref="HostKey"/> is guaranteed to be unique for a specific host, considering both the URI and proxy settings.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>Hashing</term>
    ///         <description>
    ///         The struct provides a method to calculate a hash code for a <see cref="HostKey"/>, making it suitable for use as a dictionary key.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>Creation</term>
    ///         <description>
    ///         You can create a <see cref="HostKey"/> instance from a <see cref="System.Uri"/> and optional <see cref="ProxySettings"/>.
    ///         </description>
    ///     </item>
    /// </list>
    /// <para>
    /// Usage of the <see cref="HostKey"/> struct is typically handled internally by the BestHTTP library to manage unique hosts and optimize resource usage.
    /// Developers can use it when dealing with host-specific operations or customization of the library's behavior.
    /// </para>
    /// </remarks>
    public readonly struct HostKey
    {
        /// <summary>
        /// Gets the URI (Uniform Resource Identifier) associated with the host.
        /// </summary>
        public readonly Uri Uri;

        /// <summary>
        /// Gets the proxy settings associated with the host.
        /// </summary>
        public readonly ProxySettings Proxy;

        /// <summary>
        /// Gets the unique hash key for the host.
        /// </summary>
        public readonly Hash128 Key;

        /// <summary>
        /// Gets the host name from the URI or "file" if the URI is a file URI.
        /// </summary>
        public string Host { get => !this.Uri.IsFile ? this.Uri.Host : "file"; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HostKey"/> struct with the specified URI and proxy settings.
        /// </summary>
        /// <param name="uri">The URI of the host.</param>
        /// <param name="proxy">The proxy settings associated with the host, or <c>null</c> if no proxy is used.</param>
        public HostKey(Uri uri, ProxySettings proxy)
        {
            this.Uri = uri;
            this.Proxy = proxy;

            this.Key = CalculateHash(uri, proxy);
        }

        public override bool Equals(object obj) => obj switch
        {
            HostKey hostKey => hostKey.Equals(this),
            _ => false
        };

        public bool Equals(HostKey hostKey) => this.Key.Equals(hostKey.Key);

        public override int GetHashCode() => this.Key.GetHashCode();

        public override string ToString() => $"{{\"Uri\":\"{this.Uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped)}\", \"Proxy\": {this.Proxy.ToString()}, \"Key\": {this.Key.ToString()}}}";

        private static Hash128 CalculateHash(Uri uri, ProxySettings proxy)
        {
            Hash128 hash = new Hash128();

            Append(uri, ref hash);
            proxy?.AddToHash(uri, ref hash);

            return hash;
        }

        internal static void Append(Uri uri, ref Hash128 hash)
        {
            if (uri != null)
            {
                hash.Append(uri.Scheme);
                hash.Append(uri.Host);
                hash.Append(uri.Port);
            }
        }

        /// <summary>
        /// Creates a <see cref="HostKey"/> instance from an HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request from which to extract the current URI and proxy settings.</param>
        /// <returns>A <see cref="HostKey"/> representing the host of the HTTP request.</returns>
        public static HostKey From(HTTPRequest request) => new HostKey(request.CurrentUri, request.ProxySettings);

        /// <summary>
        /// Creates a <see cref="HostKey"/> instance from a URI and proxy settings.
        /// </summary>
        /// <param name="uri">The URI of the host.</param>
        /// <param name="proxy">The proxy settings associated with the host, or <c>null</c> if no proxy is used.</param>
        /// <returns>A <see cref="HostKey"/> representing the host with the given URI and proxy settings.</returns>
        public static HostKey From(Uri uri, ProxySettings proxy) => new HostKey(uri, proxy);
    }
}

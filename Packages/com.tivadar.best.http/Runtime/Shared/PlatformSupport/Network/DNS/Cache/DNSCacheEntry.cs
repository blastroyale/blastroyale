#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading;

using Best.HTTP.Shared.PlatformSupport.Text;
using Best.HTTP.Shared.Logger;

using UnityEngine;

namespace Best.HTTP.Shared.PlatformSupport.Network.DNS.Cache
{
    /// <summary>
    /// Represents a cached entry for DNS query results, including resolved IP addresses and metadata.
    /// </summary>
    /// <remarks>
    /// Almost immutable, all changes are done in-class in a thread-safe manner.
    /// </remarks>
    internal class DNSCacheEntry
    {
        /// <summary>
        /// Gets the 128-bit hash derived from the host name.
        /// </summary>
        public readonly Hash128 Key;

        /// <summary>
        /// Gets the host name this entry stores the IP addresses for.
        /// </summary>
        public readonly string Host;

        /// <summary>
        /// Gets the timestamp when the entry was last resolved.
        /// </summary>
        public readonly DateTime ResolvedAt;

        /// <summary>
        /// Gets the timestamp when the entry was last used by calling <see cref="GetAddresses()"/>.
        /// </summary>
        public DateTime LastUsed { get => new DateTime(this._lastUsedTicks); }
        private long _lastUsedTicks;

        /// <summary>
        /// Resolved IP addresses. It's private, accesible through the <see cref="GetAddresses()"/> call only.
        /// </summary>
        private readonly List<DNSIPAddress> _resolvedAddresses;

        /// <summary>
        /// Flag that is set to <c>true</c> when the cache is refreshing this host.
        /// </summary>
        /// <remarks>
        /// When set to <c>true</c>, <see cref="IsStalled(DateTime)"/> will always return as non-stalled.
        /// </remarks>
        private int _isRefreshing;

        public DNSCacheEntry(Hash128 key, string host, List<DNSIPAddress> resolvedAddresses)
            : this(key, host, DateTime.Now.Ticks, resolvedAddresses) { }

        /// <summary>
        /// Initializes a new instance of the DNSCacheEntry class.
        /// </summary>
        /// <param name="key">The 128-bit hash key derived from the host name.</param>
        /// <param name="host">The host name associated with this entry.</param>
        /// <param name="resolvedAddresses">The list of <see cref="DNSIPAddress"/> containing the resolved IP addresses.</param>
        private DNSCacheEntry(Hash128 key, string host, long lastUsedTicks, List<DNSIPAddress> resolvedAddresses)
        {
            this.Key = key;
            this.Host = host;
            this.ResolvedAt = DateTime.Now;
            this._lastUsedTicks = lastUsedTicks;

            this._resolvedAddresses = resolvedAddresses;
        }

        /// <summary>
        /// Called to clone the entry. The new entry will inherit the last used timestamp.
        /// </summary>
        /// <param name="resolvedAddresses">The list of <see cref="DNSIPAddress"/> containing the resolved IP addresses.</param>
        /// <returns>A new DNSCacheEntry instance with updated resolved addresses.</returns>
        public DNSCacheEntry DeriveWith(List<DNSIPAddress> resolvedAddresses) => new DNSCacheEntry(this.Key, this.Host, this._lastUsedTicks, resolvedAddresses);

        /// <summary>
        /// Checks if the entry is stalled and needs to be refreshed.
        /// </summary>
        /// <param name="now">The current timestamp.</param>
        /// <returns><c>true</c> if the entry is stalled; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// The entry is considered stalled if it is not currently being refreshed (i.e., <see cref="_isRefreshing"/> is false)
        /// and the time since the last resolution exceeds the refresh interval specified in <see cref="DNSCacheOptions.RefreshAfter"/>.
        /// </remarks>
        public bool IsStalled(DateTime now) => Volatile.Read(ref this._isRefreshing) == 0 && this.ResolvedAt + DNSCache.Options.RefreshAfter < now;

        /// <summary>
        /// Checks if the entry is ready to be removed from the cache.
        /// </summary>
        /// <param name="now">The current timestamp.</param>
        /// <returns><c>true</c> if the entry is ready for removal; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// The entry is considered ready for removal if the time since it was last used exceeds the removal interval specified in <see cref="DNSCacheOptions.RemoveAfter"/>.
        /// </remarks>
        public bool IsReadyToRemove(DateTime now) => this.LastUsed + DNSCache.Options.RemoveAfter < now;

        /// <summary>
        /// Refreshes the entry by initiating a DNS prefetch (by calling <see cref="DNSCache.Prefetch(string)"/>) for the associated host name.
        /// </summary>
        /// <remarks>
        /// This method initiates a DNS prefetch operation for the host name associated with this entry.
        /// DNS prefetching is used to resolve and cache DNS records for host names in advance, reducing latency for future network requests.
        /// </remarks>
        public void Refresh()
        {
            if (Volatile.Read(ref this._isRefreshing) != 0)
                return;

            // isRefreshing - we set it only to true, when the fresh records are in a new CacheEntry is created with isRefreshing beeing false.
            if (Interlocked.CompareExchange(ref this._isRefreshing, 1, 0) == 0)
                DNSCache.Prefetch(this.Host);
        }

        /// <summary>
        /// Gets the resolved IP addresses associated with this entry.
        /// </summary>
        /// <returns>An array of <see cref="DNSIPAddress"/> representing resolved IP addresses.</returns>
        /// <remarks>
        /// This method returns the resolved IP addresses associated with this entry and updates the last used timestamp.
        /// </remarks>
        public DNSIPAddress[] GetAddresses()
        {
            Interlocked.Exchange(ref this._lastUsedTicks, DateTime.Now.Ticks);

            return this._resolvedAddresses.ToArray();
        }

        /// <summary>
        /// Reports an IP address as non-working for the specified host name. In cases where a previously resolved IP address
        /// is determined to be non-functional, this method updates the cache to mark the IP address as non-working.
        /// </summary>
        /// <param name="nonWorking">The non-working IP address to report.</param>
        /// <param name="context">Optional logging context for debugging purposes.</param>
        /// <remarks>
        /// This method is used to report an IP address associated with a host name as non-working.
        /// When a previously resolved IP address is determined to be non-functional, this method updates the cache to mark the IP address as non-working.
        /// It can be useful in situations where network errors or issues with specific IP addresses need to be recorded and managed.
        /// </remarks>
        public void ReportNonWorking(System.Net.IPAddress nonWorking, LoggingContext context)
        {
            var address = this._resolvedAddresses.Find(adr => adr.IPAddress == nonWorking);
            if (address != null)
            {
                // Because the TCP ringmaster not just probes an address, but a port of on that address, setting IsWorkedLastTime to false here
                //  means, that the address will be tried last even for a different port too.
                address.IsWorkedLastTime = false;
            }
            else
            {
                // It could happen if a refresh query is started while a tcp race probing the previous addresses and when the refresh finished with different addresses the
                //  tcp race will try report non-working and now non-existing addresses.

                //HTTPManager.Logger.Warning(nameof(DNSCacheEntry), $"{nameof(ReportNonWorking)}({nonWorking}) - couldn't find IP address in resolved addresses!", context);
            }
        }

        public override string ToString()
        {
            var sb = StringBuilderPool.Get(1);

            sb.Append('[');
            sb.Append(nameof(DNSCacheEntry));
            sb.Append(" Key: ");
            sb.Append(this.Key.ToString());
            sb.Append(" ResolvedAt: ");
            sb.Append(this.ResolvedAt.ToString());
            sb.Append(" LastUsed: ");
            sb.Append(this.LastUsed.ToString());
            sb.Append(" IsRefreshing: ");
            sb.Append(this._isRefreshing.ToString());
            sb.Append(" IsStalled: ");
            sb.Append(this.IsStalled(DateTime.Now));
            sb.Append(" IsReadyToRemove: ");
            sb.Append(this.IsReadyToRemove(DateTime.Now));
            sb.Append(" Resolved: [");
            for (int i = 0; i < this._resolvedAddresses?.Count; ++i)
            {
                sb.Append(this._resolvedAddresses[i].ToString());
                if (i < this._resolvedAddresses.Count - 1)
                    sb.Append(", ");
            }
            sb.Append(']');
            return StringBuilderPool.ReleaseAndGrab(sb);
        }
    }
}
#endif

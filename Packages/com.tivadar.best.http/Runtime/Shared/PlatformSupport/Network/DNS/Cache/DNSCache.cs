#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.Logger;

using UnityEngine;

using Best.HTTP.Shared.PlatformSupport.Text;
using System.Net;
using System.Net.Sockets;

namespace Best.HTTP.Shared.PlatformSupport.Network.DNS.Cache
{
    /// <summary>
    /// Represents the result of a DNS query, including the original host name, resolved IP addresses, and any error.
    /// </summary>
    public readonly struct DNSQueryResult
    {
        /// <summary>
        /// The host name used in the DNS query.
        /// </summary>
        public readonly string HostName;

        /// <summary>
        /// The resolved IP addresses associated with the host name.
        /// </summary>
        public readonly DNSIPAddress[] Addresses;

        /// <summary>
        /// Any error that occurred during the DNS query.
        /// </summary>
        public readonly Exception Error;

        internal DNSQueryResult(string hostName, DNSIPAddress[] addresses, Exception error)
        {
            this.HostName = hostName;
            this.Addresses = addresses;
            this.Error = error;
        }

        public override string ToString()
        {
            if (this.Error != null)
                return $"[{nameof(DNSQueryResult)}(\"{HostName}\", {this.Addresses?.Length}, Error: \"{this.Error?.Message}\")]";
            else
                return $"[{nameof(DNSQueryResult)}(\"{HostName}\", {this.Addresses?.Length})]";
        }
    }

    /// <summary>
    /// Represents an IP address obtained from DNS resolution.
    /// </summary>
    public sealed class DNSIPAddress
    {
        /// <summary>
        /// The resolved IP address.
        /// </summary>
        public IPAddress IPAddress { get; private set; }

        /// <summary>
        /// Indicates whether this IP address worked during the last connection attempt.
        /// </summary>
        public bool IsWorkedLastTime { get; internal set; }

        internal DNSIPAddress(IPAddress iPAddress)
        {
            this.IPAddress = iPAddress;

            // By default, assumme it's a working IP address
            this.IsWorkedLastTime = true;
        }

        public override string ToString() => $"[{nameof(DNSIPAddress)}({this.IPAddress}, Working: {this.IsWorkedLastTime})]";
    }

    /// <summary>
    /// Represents options for configuring the DNS cache behavior.
    /// </summary>
    public sealed class DNSCacheOptions
    {
        /// <summary>
        /// The time interval after which DNS cache entries should be refreshed.
        /// </summary>
        public TimeSpan RefreshAfter = TimeSpan.FromSeconds(30);

        /// <summary>
        /// The time interval after which DNS cache entries should be removed if not used.
        /// </summary>
        public TimeSpan RemoveAfter = TimeSpan.FromSeconds(70);

        /// <summary>
        /// The granularity of cancellation checks for DNS queries.
        /// </summary>
        public TimeSpan CancellationCheckGranularity = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// The frequency of cache maintenance.
        /// </summary>
        public TimeSpan MaintenanceFrequency = TimeSpan.FromSeconds(1);
    }

    /// <summary>
    /// Represents parameters for a DNS query, including the host name, address, cancellation token, logging context, callback, and tag.
    /// </summary>
    public sealed class DNSQueryParameters
    {
        /// <summary>
        /// The hash key associated with the DNS query.
        /// </summary>
        public Hash128 Key { get; private set; }

        /// <summary>
        /// The host name used in the DNS query.
        /// </summary>
        public string Hostname { get => this.Address.Host; }

        /// <summary>
        /// The URI address used in the DNS query.
        /// </summary>
        public Uri Address { get; private set; }

        /// <summary>
        /// The cancellation token used to cancel the DNS query.
        /// </summary>
        public CancellationToken Token;

        /// <summary>
        /// Optional logging context.
        /// </summary>
        public LoggingContext Context;

        /// <summary>
        /// The callback to be invoked upon completion of the DNS query.
        /// </summary>
        public Action<DNSQueryParameters, DNSQueryResult> Callback;

        /// <summary>
        /// An optional object reference associated with the DNS query.
        /// </summary>
        public object Tag;

        /// <summary>
        /// Indicates whether the DNS query is a prefetch query.
        /// </summary>
        public bool IsPrefetch { get => this.Context == null; }

        public DNSQueryParameters(Uri address)
        {
            this.Address = address;
            this.Key = Hash128.Compute(this.Hostname);
        }

        public override string ToString()
        {
            return $"{Key} => {Address}";
        }
    }

    /// <summary>
    /// The DNSCache class is a static utility that manages DNS caching and queries within the Best HTTP library.
    /// It helps improve network efficiency by caching DNS query results, reducing the need for redundant DNS resolutions.
    /// </summary>
    /// <remarks>
    /// <para>By utilizing the DNSCache class and its associated features, you can optimize DNS resolution in your network communication, leading to improved performance and reduced latency in your applications.</para>
    /// <para>
    /// Its key features include:
    /// <list type="bullet">
    ///     <item>
    ///         <term>Improving Network Efficiency</term>
    ///         <description>The DNSCache class is designed to enhance network efficiency by caching DNS query results.
    ///         When your application needs to resolve hostnames to IP addresses for making network requests, the DNSCache stores previously resolved results.
    ///         This reduces the need for redundant DNS resolutions, making network communication faster and more efficient.
    ///         </description>
    ///     </item>
    ///     
    ///     <item>
    ///         <term>DNS Prefetching</term>
    ///         <description>You can use the DNSCache to initiate DNS prefetch operations.
    ///         Prefetching allows you to resolve and cache DNS records for hostnames in advance, reducing latency for future network requests.
    ///         This is particularly useful when you expect to make multiple network requests to the same hostnames, as it helps to avoid DNS resolution delays.
    ///         </description>
    ///     </item>
    ///
    ///     <item>
    ///         <term>Marking IP Addresses as Non-Working</term>
    ///         <description>In cases where a previously resolved IP address is determined to be non-functional (e.g., due to network issues), you can use the DNSCache to report IP addresses as non-working.
    ///         This information helps the cache make better decisions about which IP addresses to use for future network connections. <see cref="Best.HTTP.Shared.PlatformSupport.Network.Tcp.TCPRingmaster"/> gives higher priority for adresses not marked as non-working.
    ///         </description>
    ///     </item>
    ///
    ///     <item>
    ///         <term>Clearing the DNS Cache</term>
    ///         <description>If you need to reset the DNS cache and remove all stored DNS resolutions, you can use the Clear method provided by the DNSCache class.
    ///         This operation can be useful in scenarios where you want to start with a fresh cache.
    ///         </description>
    ///     </item>
    ///
    ///     <item>
    ///         <term>Performing DNS Queries</term>
    ///         <description>The primary function of the DNSCache class is to perform DNS queries with specified parameters.
    ///         It resolves DNS records for a given hostname and caches the results. This can be called directly or used internally by the Best HTTP library for resolving hostnames.
    ///         </description>
    ///     </item>
    ///
    ///     <item>
    ///         <term>Configuring Cache Behavior</term>
    ///         <description>You can configure the behavior of the DNS cache using the DNSCacheOptions class.
    ///         This includes setting refresh intervals for cache entries, defining the granularity of cancellation checks for DNS queries, and specifying the frequency of cache maintenance.
    ///         </description>
    ///     </item>
    /// </list>
    /// </para>
    /// </remarks>
    [Best.HTTP.Shared.PlatformSupport.IL2CPP.Il2CppEagerStaticClassConstruction]
    public static class DNSCache
    {
        /// <summary>
        /// Options for configuring the DNS cache behavior, including refresh intervals and maintenance frequency.
        /// </summary>
        public static DNSCacheOptions Options = new DNSCacheOptions();
        private static ConcurrentDictionary<Hash128, DNSCacheEntry> _cache = new ConcurrentDictionary<Hash128, DNSCacheEntry>();
        private static int _isMaintenanceScheduled = 0;

        /// <summary>
        /// Initiates a DNS prefetch operation for the specified host name. DNS prefetching is used to resolve and cache
        /// DNS records for host names in advance, reducing latency for future network requests.
        /// </summary>
        /// <param name="hostName">The host name to prefetch.</param>
        public static void Prefetch(string hostName)
        {
            HTTPManager.Logger.Verbose(nameof(DNSCache), $"{nameof(Prefetch)}(\"{hostName}\")");

            Query(new DNSQueryParameters(new Uri($"prefetch://{hostName}")));
        }

        /// <summary>
        /// Reports an IP address as non-working for the specified host name. In cases where a previously resolved IP address
        /// is determined to be non-functional, this method updates the cache to mark the IP address as non-working.
        /// </summary>
        /// <param name="hostName">The host name associated with the IP address.</param>
        /// <param name="address">The <see cref="IPAddress"/> to report as non-working.</param>
        /// <param name="context">Optional logging context for debugging purposes.</param>
        public static void ReportAsNonWorking(string hostName, IPAddress address, LoggingContext context)
        {
            var key = Hash128.Compute(hostName);

            if (_cache.TryGetValue(key, out var entry))
            {
                HTTPManager.Logger.Verbose(nameof(DNSCache), $"{nameof(ReportAsNonWorking)}(\"{hostName}\", {address}) - CacheEntry found", context);
                entry.ReportNonWorking(address, context);
            }
            else
                HTTPManager.Logger.Verbose(nameof(DNSCache), $"{nameof(ReportAsNonWorking)}(\"{hostName}\", {address}) - CacheEntry not found!", context);
        }

        /// <summary>
        /// Clears the DNS cache, removing all cached DNS records. This operation can be used to reset the cache
        /// and remove all stored DNS resolutions.
        /// </summary>
        public static void Clear()
        {
            HTTPManager.Logger.Verbose(nameof(DNSCache), $"{nameof(Clear)}()");

            _cache.Clear();

#if BESTHTTP_PROFILE && UNITY_2021_2_OR_NEWER
            Profiler.Network.NetworkStats.TotalDNSCacheMissCounter.Value = 0;
            Profiler.Network.NetworkStats.TotalDNSCacheHitsCounter.Value = 0;
#endif
        }

        /// <summary>
        /// Performs a DNS query with the specified parameters. It resolves DNS records for a given host name,
        /// caching the results to reduce the need for redundant DNS resolutions.
        /// </summary>
        /// <param name="parameters">The parameters for the DNS query.</param>
        public static void Query(DNSQueryParameters parameters)
        {
            // First check whether it's already an IP address. If so, call the callback without touching any DNS query.
            if (IPAddress.TryParse(parameters.Hostname, out var ip) &&
                (ip.AddressFamily == AddressFamily.InterNetwork || ip.AddressFamily == AddressFamily.InterNetworkV6))
            {
                try
                {
                    if (HTTPManager.Logger.IsDiagnostic)
                        HTTPManager.Logger.Verbose(nameof(DNSCache), $"{nameof(Query)}(\"{parameters.Hostname}\") - It's already an IP address, skipping DNS query...", parameters.Context);

                    parameters.Callback?.Invoke(parameters, new DNSQueryResult(parameters.Hostname, new DNSIPAddress[] { new DNSIPAddress(ip) }, null));
                }
                catch (Exception ex)
                {
                    HTTPManager.Logger.Exception(nameof(DNSCache), $"{nameof(Query)} - Callback", ex, parameters.Context);
                }
                return;
            }

            // When context is null, it's a call to refresh cached entry, so it must skip the cache check and have to go straight for the DNS query
            if (!parameters.IsPrefetch && _cache.TryGetValue(parameters.Key, out var entry))
            {
                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Verbose(nameof(DNSCache), $"{nameof(Query)}(\"{parameters.Hostname}\", \"{parameters.Key}\") - Cache hit: {entry}", parameters.Context);

                var addresses = entry.GetAddresses();
                if (addresses != null && addresses.Length > 0)
                {
#if BESTHTTP_PROFILE && UNITY_2021_2_OR_NEWER
                    Profiler.Network.NetworkStats.TotalDNSCacheHitsCounter.Value++;
#endif
                    try
                    {
                        var result = new DNSQueryResult(parameters.Hostname, addresses, null);
                        parameters.Callback?.Invoke(parameters, result);
                    }
                    catch (Exception ex)
                    {
                        HTTPManager.Logger.Exception(nameof(DNSCache), $"{nameof(Query)}(\"{parameters.Hostname}\", \"{parameters.Key}\") - Cache hit - QueryImpl", ex, parameters.Context);
                    }

                    // Return now, if it's a stalled entry, the regular maintenance call will do its job.
                    return;
                }
                else if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Verbose(nameof(DNSCache), $"{nameof(Query)}(\"{parameters.Hostname}\", \"{parameters.Key}\") - CacheEntry found, but no addresses returned!", parameters.Context);
            }

            var ar = Dns.BeginGetHostAddresses(parameters.Hostname, OnGetHostAddresses, /*query*/ parameters);

            // Apply a timer only when there's a context == it's in the context of a request where it can be cancelled.
            // If it's a refresh/prefech request, it would be a waste of resources.
            if (!parameters.IsPrefetch)
            {
#if BESTHTTP_PROFILE && UNITY_2021_2_OR_NEWER
                Profiler.Network.NetworkStats.TotalDNSCacheMissCounter.Value++;
#endif

                if (!ar.CompletedSynchronously && parameters.Token != CancellationToken.None)
                    Extensions.Timer.Add(new TimerData(Options.CancellationCheckGranularity, /*query*/ parameters, CheckForCanceled));
            }

            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Verbose(nameof(DNSCache), $"{nameof(Query)}(\"{parameters.Hostname}\", \"{parameters.Key}\") - Cache miss/Prefetch! {nameof(Dns.BeginGetHostAddresses)} called!", parameters.Context);
        }

        private static void OnGetHostAddresses(IAsyncResult ar)
        {
            var parameters = ar.AsyncState as /*DNSQuery*/DNSQueryParameters;

            var callback = parameters.Callback;
            callback = Interlocked.CompareExchange<Action<DNSQueryParameters, DNSQueryResult>>(ref parameters.Callback, null, callback);

            try
            {
                // If something went wrong, this will throw an exception.
                var addresses = Dns.EndGetHostAddresses(ar);

                if (HTTPManager.Logger.IsDiagnostic)
                {
                    var sb = StringBuilderPool.Get(1);
                    sb.Append('[');
                    for (int i = 0; i < addresses.Length; ++i)
                    {
                        sb.Append(addresses[i]);

                        if (i < addresses.Length - 1)
                            sb.Append(", ");
                    }
                    sb.Append(']');
                    var ips = StringBuilderPool.ReleaseAndGrab(sb);
                    HTTPManager.Logger.Verbose(nameof(DNSCache), $"{nameof(OnGetHostAddresses)}({parameters}) - {nameof(Dns.EndGetHostAddresses)} returned with {addresses?.Length} IPs: {ips}", parameters.Context);
                }

                DNSCacheEntry AddCacheEntry(Hash128 key)
                {
                    var resolved = new List<DNSIPAddress>(addresses.Length);
                    foreach (var address in addresses)
                        if (address.AddressFamily == AddressFamily.InterNetwork || address.AddressFamily == AddressFamily.InterNetworkV6)
                            resolved.Add(new DNSIPAddress(address));

                    var entry = new DNSCacheEntry(key, parameters.Hostname, resolved);

                    return entry;
                }

                DNSCacheEntry UpdateCacheEntry(Hash128 key, DNSCacheEntry oldEntry)
                {
                    var resolved = new List<DNSIPAddress>(addresses.Length);
                    foreach (var address in addresses)
                        if (address.AddressFamily == AddressFamily.InterNetwork || address.AddressFamily == AddressFamily.InterNetworkV6)
                            resolved.Add(new DNSIPAddress(address));

                    var entry = oldEntry.DeriveWith(resolved);

                    return entry;
                }

                // Store/update cach entry
                var entry = _cache.AddOrUpdate(parameters.Key, AddCacheEntry, UpdateCacheEntry);

                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Verbose(nameof(DNSCache), $"{nameof(OnGetHostAddresses)}({parameters}) - entry added to/updated in the cache: {entry}. Cache size: {_cache.Count}", parameters.Context);

                if (entry != null && Interlocked.CompareExchange(ref _isMaintenanceScheduled, 1, 0) == 0)
                {
                    Extensions.Timer.Add(new TimerData(Options.MaintenanceFrequency, null, Maintenance));

                    if (HTTPManager.Logger.IsDiagnostic)
                        HTTPManager.Logger.Verbose(nameof(DNSCache), $"{nameof(OnGetHostAddresses)}({parameters}) - Scheduled maintenance rutin", parameters.Context);
                }

                try
                {
                    callback?.Invoke(parameters, new DNSQueryResult(parameters.Hostname, entry.GetAddresses(), null));
                }
                catch (Exception ex)
                {
                    HTTPManager.Logger.Exception(nameof(DNSCache), $"{nameof(OnGetHostAddresses)}({parameters}) - callback", ex, parameters.Context);
                }
            }
            catch (Exception ex)
            {
                // If there's an error (like DNS couldn't resolve the host) old entries are will remain in the cache.
                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Verbose(nameof(DNSCache), $"{nameof(OnGetHostAddresses)}({parameters}) calling callback with no addresses!", parameters.Context);

                try
                {
                    callback?.Invoke(parameters, new DNSQueryResult(parameters.Hostname, null, ex));
                }
                catch (Exception e)
                {
                    HTTPManager.Logger.Exception(nameof(DNSCache), $"{nameof(OnGetHostAddresses)}({parameters}) - callback", e, parameters.Context);
                }
            }
        }

        /// <summary>
        /// It's plan-b for the case where BeginGetHostAddresses take too long and no reply in time. If the query's Token is canceled it will call the callback if it's still available.
        /// </summary>
        private static bool CheckForCanceled(DateTime now, object context)
        {
            var query = context as /*DNSQuery*/ DNSQueryParameters;

            if (query.Token.IsCancellationRequested)
            {
                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Verbose(nameof(DNSCache), $"{nameof(CheckForCanceled)}({query}) - Token.IsCancellationRequested!", query.Context);

                var callback = query.Callback;

                callback = Interlocked.CompareExchange<Action<DNSQueryParameters, DNSQueryResult>>(ref query.Callback, null, callback);

                try
                {
                    callback?.Invoke(query, new DNSQueryResult(query.Hostname, null, new TimeoutException("DNS Query Timed Out")));
                }
                catch (Exception ex)
                {
                    HTTPManager.Logger.Exception(nameof(DNSCache), $"{nameof(CheckForCanceled)}({query}) - callback", ex, query.Context);
                }

                return false;
            }

            return query.Callback != null;
        }

        private static bool Maintenance(DateTime now, object context)
        {
            using var __ = new Unity.Profiling.ProfilerMarker(nameof(DNSCache)).Auto();

            foreach (var kvp in _cache)
            {
                Hash128 key = kvp.Key;
                DNSCacheEntry entry = kvp.Value;

                if (entry.IsReadyToRemove(now))
                {
                    if (_cache.TryRemove(key, out _))
                    {
                        if (HTTPManager.Logger.IsDiagnostic)
                            HTTPManager.Logger.Verbose(nameof(DNSCache), $"{nameof(Maintenance)}.{nameof(DNSCacheEntry.IsReadyToRemove)}: Removed entry from cache: {entry}");
                    }
                    else if (HTTPManager.Logger.IsDiagnostic)
                        HTTPManager.Logger.Verbose(nameof(DNSCache), $"{nameof(Maintenance)}.{nameof(DNSCacheEntry.IsReadyToRemove)}: Couldn't remove entry from cache: {entry}");
                }
                else if (entry.IsStalled(now))
                {
                    if (HTTPManager.Logger.IsDiagnostic)
                        HTTPManager.Logger.Verbose(nameof(DNSCache), $"{nameof(Maintenance)}.{nameof(DNSCacheEntry.IsStalled)}: Refreshing entry: {entry}");

                    entry.Refresh();
                }
            }

            // return true to repeat. So return false if there's no more entries in the cache and we could change _isMaintenanceScheduled.
            bool removeShedule = _cache.Count == 0 && Interlocked.CompareExchange(ref _isMaintenanceScheduled, 0, 1) == 1;

            if (removeShedule && HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Verbose(nameof(DNSCache), $"{nameof(Maintenance)} - Remove scheduled rutin");

            return !removeShedule; // Timer expects false to remove the timer, so here we actually have to negate removeSchedule to remove.
        }
    }
}
#endif

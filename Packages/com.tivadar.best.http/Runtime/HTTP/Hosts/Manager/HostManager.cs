using System.Collections.Generic;

using Best.HTTP.Hosts.Connections;
using Best.HTTP.Shared;

namespace Best.HTTP.HostSetting
{
    /*
                                  ┌───────────────┐
                           ┌──────┤  HostManager  ├──────────────────────┐
                           │      └───────────────┘                      │
                           │                                             │
                ┌──────────▼───────┐                          ┌──────────▼────────┐
                │  HostVariant     │                          │  HostVariant      │
                │(http://host:port)│                          │(https://host:port)│
           ┌────┴──────┬──────────┬┘                     ┌────┴──────┬───────────┬┘
           │           │          │                      │           │           │
    ┌──────▼──────┐ ┌──▼──┐       ▼               ┌──────▼──────┐ ┌──▼──┐        ▼
    │ Connections │ │Queue│  ProtocolSupport      │ Connections │ │Queue│  ProtocolSupport
    ├─────────────┤ ├─────┤   (http/1.1)          ├─────────────┤ ├─────┤   (http/1.1, h2)
    │    ...      │ │ ... │                       │    ...      │ │ ... │
    │    ...      │ │ ... │                       │    ...      │ │ ... │
    │    ...      │ │ ... │                       │    ...      │ │ ... │
    │    ...      │ │ ... │                       │    ...      │ │ ... │
    └─────────────┘ └─────┘                       └─────────────┘ └─────┘
     */
    /// <summary>
    /// The <see cref="HostManager"/> class provides centralized management for <see cref="HostVariant"/> objects associated with HTTP requests and connections.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="HostManager"/> class acts as a central registry for managing <see cref="HostVariant"/> objects, each associated with a unique <see cref="HostKey"/>.
    /// It facilitates the creation, retrieval, and management of <see cref="HostVariant"/> instances based on HTTP requests and connections.
    /// </para>
    /// <para>
    /// A <see cref="HostVariant"/> represents a specific host and port combination (e.g., "http://example.com:80" or "https://example.com:443") and
    /// manages the connections and request queues for that host. The class ensures that a single <see cref="HostVariant"/> instance is used for
    /// each unique host, helping optimize resource usage and connection pooling.
    /// </para>
    /// <para>
    /// Key features of the <see cref="HostManager"/> class include:
    /// </para>
    /// <list type="bullet">
    ///     <item>
    ///         <term>Creation and Retrieval</term>
    ///         <description>
    ///         The class allows you to create and retrieve <see cref="HostVariant"/> instances based on HTTP requests, connections, or <see cref="HostKey"/>.
    ///         It ensures that a single <see cref="HostVariant"/> is used for each unique host.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>Queue Management</term>
    ///         <description>
    ///         The <see cref="HostManager"/> manages the queue of pending requests for each <see cref="HostVariant"/>, ensuring efficient request processing.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>Connection Management</term>
    ///         <description>
    ///         The class handles the management of connections associated with <see cref="HostVariant"/> objects, including recycling idle connections,
    ///         removing idle connections, and shutting down connections when needed.
    ///         </description>
    ///     </item>
    /// </list>
    /// <para>
    /// Usage of the <see cref="HostManager"/> class is typically transparent to developers and is handled internally by the Best HTTP library. However,
    /// it provides a convenient and efficient way to manage connections and requests when needed.
    /// </para>
    /// </remarks>
    public static class HostManager
    {
        /// <summary>
        /// Dictionary to store <see cref="HostKey"/>-<see cref="HostVariant"/> mappings.
        /// </summary>
        private static Dictionary<HostKey, HostVariant> hosts = new Dictionary<HostKey, HostVariant>();

        /// <summary>
        /// Gets the <see cref="HostVariant"/> associated with an HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <returns>The <see cref="HostVariant"/> for the request's host.</returns>
        public static HostVariant GetHostVariant(HTTPRequest request) => GetHostVariant(request.CurrentHostKey);

        /// <summary>
        /// Gets the <see cref="HostVariant"/> associated with a connection.
        /// </summary>
        /// <param name="connection">The HTTP connection.</param>
        /// <returns>The <see cref="HostVariant"/> for the connection's host.</returns>
        public static HostVariant GetHostVariant(ConnectionBase connection) => GetHostVariant(connection.HostKey);

        /// <summary>
        /// Gets the <see cref="HostVariant"/> associated with a HostKey.
        /// </summary>
        /// <param name="key">The HostKey for which to get the HostVariant.</param>
        /// <returns>The <see cref="HostVariant"/> for the specified HostKey.</returns>
        public static HostVariant GetHostVariant(HostKey key)
        {
            if (!hosts.TryGetValue(key, out var variant))
            {
                hosts.Add(key, variant = new HostVariant(key));

                HTTPManager.Logger.Information("HostManager", $"Variant added with key: {key}");
            }

            return variant;
        }

        /// <summary>
        /// Removes all idle connections for all hosts.
        /// </summary>
        public static void RemoveAllIdleConnections()
        {
            HTTPManager.Logger.Information("HostManager", "RemoveAllIdleConnections");
            foreach (var host in hosts)
                    host.Value.RemoveAllIdleConnections();
        }

        /// <summary>
        /// Tries to send queued requests for all hosts.
        /// </summary>
        public static void TryToSendQueuedRequests()
        {
            foreach (var kvp in hosts)
                kvp.Value.TryToSendQueuedRequests();
        }

        /// <summary>
        /// Shuts down all connections for all hosts.
        /// </summary>
        public static void Shutdown()
        {
            HTTPManager.Logger.Information("HostManager", "Shutdown()");
            foreach (var kvp in hosts)
                kvp.Value.Shutdown();
        }

        /// <summary>
        /// Clears all hosts and their associated variants.
        /// </summary>
        public static void Clear()
        {
            HTTPManager.Logger.Information("HostManager", "Clearing()");
            hosts.Clear();
        }
    }
}

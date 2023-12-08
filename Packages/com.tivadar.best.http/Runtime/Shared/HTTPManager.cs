using Best.HTTP.Caching;
using Best.HTTP.Cookies;
using Best.HTTP.Hosts.Connections;
using Best.HTTP.Hosts.Settings;
using Best.HTTP.HostSetting;
using Best.HTTP.Request.Authentication;
using Best.HTTP.Request.Timings;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.HTTP.Shared.PlatformSupport.Text;

using System;
using System.IO;

namespace Best.HTTP.Shared
{
    public enum ShutdownTypes
    {
        Running,
        Gentle,
        Immediate
    }

    public delegate void OnSetupFinishedDelegate();

    /// <summary>
    /// Global entry point to access and manage main services of the plugin.
    /// </summary>
    [Best.HTTP.Shared.PlatformSupport.IL2CPP.Il2CppEagerStaticClassConstructionAttribute]
    public static partial class HTTPManager
    {
        /// <summary>
        /// Static constructor. Setup default values.
        /// </summary>
        static HTTPManager()
        {
            PerHostSettings.Add("*", new HostSettings());

            // Set the default logger mechanism
            logger = new Best.HTTP.Shared.Logger.ThreadedLogger();

            IOService = new Best.HTTP.Shared.PlatformSupport.FileSystem.DefaultIOService();

#if !UNITY_WEBGL || UNITY_EDITOR
            ProxyDetector = new HTTP.Proxies.Autodetect.ProxyDetector();
#endif

            UserAgent = $"com.Tivadar.Best.HTTP v{typeof(HTTPManager)?.Assembly?.GetName()?.Version}/Unity {UnityEngine.Application.unityVersion}";
        }

        /// <summary>
        /// Delegate for the setup finished event.
        /// </summary>
        public static OnSetupFinishedDelegate OnSetupFinished;

        /// <summary>
        /// Instance of the per-host settings manager.
        /// </summary>
        public static HostSettingsManager PerHostSettings { get; private set; } = new HostSettingsManager();

        /// <summary>
        /// Cached DateTime value for cases where high resolution isn't needed.
        /// </summary>
        /// <remarks>Warning!! It must be used only on the main update thread!</remarks>
        public static DateTime CurrentFrameDateTime { get; private set; } = DateTime.Now;

        /// <summary>
        /// By default the plugin will save all cache and cookie data under the path returned by Application.persistentDataPath.
        /// You can assign a function to this delegate to return a custom root path to define a new path.
        /// <remarks>This delegate will be called on a non Unity thread!</remarks>
        /// </summary>
        public static Func<string> RootSaveFolderProvider { get; set; }

#if !UNITY_WEBGL || UNITY_EDITOR

        public static HTTP.Proxies.Autodetect.ProxyDetector ProxyDetector {
            get => _proxyDetector;
            set {
                _proxyDetector?.Detach();
                _proxyDetector = value;
            }
        }
        private static HTTP.Proxies.Autodetect.ProxyDetector _proxyDetector;
#endif

        /// <summary>
        /// The global, default proxy for all HTTPRequests. The HTTPRequest's Proxy still can be changed per-request. Default value is null.
        /// </summary>
        public static HTTP.Proxies.Proxy Proxy { get; set; }

        /// <summary>
        /// Heartbeat manager to use less threads in the plugin. The heartbeat updates are called from the OnUpdate function.
        /// </summary>
        public static HeartbeatManager Heartbeats
        {
            get
            {
                if (heartbeats == null)
                    heartbeats = new HeartbeatManager();
                return heartbeats;
            }
        }
        private static HeartbeatManager heartbeats;

        /// <summary>
        /// A basic Best.HTTP.Logger.ILogger implementation to be able to log intelligently additional informations about the plugin's internal mechanism.
        /// </summary>
        public static Best.HTTP.Shared.Logger.ILogger Logger
        {
            get
            {
                // Make sure that it has a valid logger instance.
                if (logger == null)
                {
                    logger = new ThreadedLogger();
                    logger.Level = Loglevels.None;
                }

                return logger;
            }

            set { logger = value; }
        }
        private static Best.HTTP.Shared.Logger.ILogger logger;

        /// <summary>
        /// An IIOService implementation to handle filesystem operations.
        /// </summary>
        public static Best.HTTP.Shared.PlatformSupport.FileSystem.IIOService IOService;

        /// <summary>
        /// User-agent string that will be sent with each requests.
        /// </summary>
        public static string UserAgent;

        /// <summary>
        /// It's true if the application is quitting and the plugin is shutting down itself.
        /// </summary>
        public static bool IsQuitting { get { return _isQuitting; } private set { _isQuitting = value; } }
        private static volatile bool _isQuitting;

        public static string RootFolderName = "com.Tivadar.Best.HTTP.v3";

        /// <summary>
        /// The local content cache, maintained by the plugin. When set to a non-null value, Maintain called immediately on the cache.
        /// </summary>
        public static HTTPCache LocalCache
        {
            get => _httpCache;
            set
            {
                _httpCache?.Dispose();
                (_httpCache = value)?.Maintain(contentLength: 0, deleteLockedEntries: true, context: null);
            }
        }
        private static HTTPCache _httpCache;

        private static bool IsSetupCalled;

        /// <summary>
        /// Initializes the HTTPManager with default settings. This method should be called on Unity's main thread before using the HTTP plugin. By default it gets called by <see cref="HTTPUpdateDelegator"/>.
        /// </summary>
        public static void Setup()
        {
            if (IsSetupCalled)
                return;
            IsSetupCalled = true;
            IsQuitting = false;

            HTTPManager.Logger.Information("HTTPManager", "Setup called! UserAgent: " + UserAgent);

            HTTPUpdateDelegator.CheckInstance();

            if (LocalCache == null)
            {
                // this will trigger a maintain call too.
                LocalCache = new HTTPCache(new HTTPCacheOptions());
            }

            CookieJar.SetupFolder();
            CookieJar.Load();

            try
            {
                OnSetupFinished?.Invoke();
            }
            catch(Exception ex)
            {
                HTTPManager.logger.Exception(nameof(HTTPManager), "OnSetupFinished", ex, null);
            }
        }

        internal static HTTPRequest SendRequest(HTTPRequest request)
        {
            if (!IsSetupCalled)
                Setup();

            if (request.IsCancellationRequested || IsQuitting)
                return request;

            if (!request.DownloadSettings.DisableCache)
            {
                var hash = HTTPCache.CalculateHash(request.MethodType, request.CurrentUri);
                if (LocalCache.CanServeWithoutValidation(hash, ErrorTypeForValidation.None, request.Context))
                    LocalCache.Redirect(request, hash);
            }

            RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(request, HTTPRequestStates.Queued, null));
            RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(request, RequestEvents.Resend));

            return request;
        }

        /// <summary>
        /// Will return where the various caches should be saved.
        /// </summary>
        public static string GetRootSaveFolder()
        {
            try
            {
                if (RootSaveFolderProvider != null)
                    return Path.Combine(RootSaveFolderProvider(), RootFolderName);
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception(nameof(HTTPManager), nameof(GetRootSaveFolder), ex);
            }

            return Path.Combine(UnityEngine.Application.persistentDataPath, RootFolderName);
        }

#if UNITY_EDITOR
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetSetup()
        {
            IsSetupCalled = false;
            Profiler.Network.NetworkStatsCollector.ResetNetworkStats();
#if !UNITY_WEBGL || UNITY_EDITOR
            PlatformSupport.Network.DNS.Cache.DNSCache.Clear();
#endif
            DigestStore.Clear();
            PerHostSettings.Clear();
            PerHostSettings.Add("*", new HostSettings());

            LocalCache = null;

            HTTPManager.Logger.Information("HTTPManager", "Reset called!");
        }
#endif

        /// <summary>
        /// Updates the HTTPManager. This method should be called regularly from a Unity event (e.g., Update, LateUpdate).
        /// It processes various events and callbacks and manages internal tasks.
        /// </summary>
        public static void OnUpdate()
        {
            try
            {
                CurrentFrameDateTime = DateTime.Now;

                using (new Unity.Profiling.ProfilerMarker(nameof(TimingEventHelper)).Auto())
                    TimingEventHelper.ProcessQueue();

                using (new Unity.Profiling.ProfilerMarker(nameof(RequestEventHelper)).Auto())
                    RequestEventHelper.ProcessQueue();

                using (new Unity.Profiling.ProfilerMarker(nameof(ConnectionEventHelper)).Auto())
                    ConnectionEventHelper.ProcessQueue();

                if (heartbeats != null)
                {
                    using (new Unity.Profiling.ProfilerMarker(nameof(HeartbeatManager)).Auto())
                        heartbeats.Update();
                }

                using (new Unity.Profiling.ProfilerMarker(nameof(BufferPool)).Auto())
                    BufferPool.Maintain();

                using (new Unity.Profiling.ProfilerMarker(nameof(StringBuilderPool)).Auto())
                    StringBuilderPool.Maintain();

#if BESTHTTP_PROFILE && UNITY_2021_2_OR_NEWER
                using (new Unity.Profiling.ProfilerMarker("Profile").Auto())
                {
                    // Sent
                    {
                        long newNetworkBytesSent = Profiler.Network.NetworkStatsCollector.TotalNetworkBytesSent;
                        var diff = newNetworkBytesSent - _lastNetworkBytesSent;
                        _lastNetworkBytesSent = newNetworkBytesSent;

                        Profiler.Network.NetworkStats.SentSinceLastFrame.Value = diff;
                        Profiler.Network.NetworkStats.SentTotal.Value = newNetworkBytesSent;

                        Profiler.Network.NetworkStats.BufferedToSend.Value = Profiler.Network.NetworkStatsCollector.BufferedToSend;
                    }

                    // Received
                    {
                        long newNetworkBytesReceived = Profiler.Network.NetworkStatsCollector.TotalNetworkBytesReceived;
                        var diff = newNetworkBytesReceived - _lastNetworkBytesReceived;
                        _lastNetworkBytesReceived = newNetworkBytesReceived;

                        Profiler.Network.NetworkStats.ReceivedSinceLastFrame.Value = diff;
                        Profiler.Network.NetworkStats.ReceivedTotal.Value = newNetworkBytesReceived;

                        Profiler.Network.NetworkStats.ReceivedAndUnprocessed.Value = Profiler.Network.NetworkStatsCollector.ReceivedAndUnprocessed;
                    }

                    // Open/Total connections
                    Profiler.Network.NetworkStats.OpenConnectionsCounter.Value = Profiler.Network.NetworkStatsCollector.OpenConnections;
                    Profiler.Network.NetworkStats.TotalConnectionsCounter.Value = Profiler.Network.NetworkStatsCollector.TotalConnections;

                    // Memory stats
                    BufferPool.GetStatistics(ref bufferPoolStats);
                    Profiler.Memory.MemoryStats.Borrowed.Value = bufferPoolStats.Borrowed;
                    Profiler.Memory.MemoryStats.Pooled.Value = bufferPoolStats.PoolSize;
                    Profiler.Memory.MemoryStats.CacheHits.Value = bufferPoolStats.GetBuffers;
                    Profiler.Memory.MemoryStats.ArrayAllocations.Value = bufferPoolStats.ArrayAllocations;
                }
#endif
            }
            catch (Exception ex)
            {
                HTTPManager.logger.Exception(nameof(HTTPManager), nameof(OnUpdate), ex);
            }
        }

#if BESTHTTP_PROFILE && UNITY_2021_2_OR_NEWER
        private static long _lastNetworkBytesSent = 0;
        private static long _lastNetworkBytesReceived = 0;

        private static BufferPoolStats bufferPoolStats = default;
#endif

        /// <summary>
        /// Shuts down the HTTPManager and performs cleanup operations. This method should be called when the application is quitting.
        /// </summary>
        public static void OnQuit()
        {
            HTTPManager.Logger.Information("HTTPManager", "OnQuit called!");

            IsQuitting = true;

            AbortAll();

            CookieJar.Persist();

            OnUpdate();

            HostManager.Clear();

            Heartbeats.Clear();

            DigestStore.Clear();
        }

        /// <summary>
        /// Aborts all ongoing HTTP requests and performs an immediate shutdown of the HTTPManager.
        /// </summary>
        public static void AbortAll()
        {
            HTTPManager.Logger.Information("HTTPManager", "AbortAll called!");

            // This is an immediate shutdown request!

            RequestEventHelper.Clear();
            ConnectionEventHelper.Clear();
            
            HostManager.Shutdown();
        }
    }
}

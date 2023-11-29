#if BESTHTTP_PROFILE && UNITY_2021_2_OR_NEWER
using Unity.Profiling.Editor;

using Best.HTTP.Profiler.Network;

namespace Best.HTTP.Editor.Profiler.Network
{
    [System.Serializable]
    [ProfilerModuleMetadata(NetworkStats.CategoryName)]
    public sealed class NetworkStatsProfilerModule : ProfilerModule
    {
        static readonly ProfilerCounterDescriptor[] k_Counters =
        {
            new ProfilerCounterDescriptor(NetworkStats.BufferedToSendName, NetworkStats.Category),
            new ProfilerCounterDescriptor(NetworkStats.SentSinceLastFrameName, NetworkStats.Category),
            new ProfilerCounterDescriptor(NetworkStats.SentTotalName, NetworkStats.Category),

            new ProfilerCounterDescriptor(NetworkStats.ReceivedAndUnprocessedName, NetworkStats.Category),
            new ProfilerCounterDescriptor(NetworkStats.ReceivedSinceLastFrameName, NetworkStats.Category),
            new ProfilerCounterDescriptor(NetworkStats.ReceivedTotalName, NetworkStats.Category),

            new ProfilerCounterDescriptor(NetworkStats.OpenConnectionsName, NetworkStats.Category),
            new ProfilerCounterDescriptor(NetworkStats.TotalConnectionsName, NetworkStats.Category),

            new ProfilerCounterDescriptor(NetworkStats.TotalDNSCacheHits, NetworkStats.Category),
            new ProfilerCounterDescriptor(NetworkStats.TotalDNSCacheMiss, NetworkStats.Category),
        };

        public NetworkStatsProfilerModule() : base(k_Counters)
        {
        }
    }
}
#endif

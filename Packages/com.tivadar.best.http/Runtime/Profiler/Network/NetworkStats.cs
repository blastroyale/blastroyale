#if BESTHTTP_PROFILE && UNITY_2021_2_OR_NEWER
using Unity.Profiling;

namespace Best.HTTP.Profiler.Network
{
    public sealed class NetworkStats
    {
        public const string CategoryName = "Best - Network";

        public static readonly ProfilerCategory Category = new ProfilerCategory(CategoryName, ProfilerCategoryColor.Scripts);

        // Sent

        public const string BufferedToSendName = "Buffered to Send";
        public static readonly ProfilerCounterValue<long> BufferedToSend = new ProfilerCounterValue<long>(Category, BufferedToSendName, ProfilerMarkerDataUnit.Bytes, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        public const string SentSinceLastFrameName = "Sent (Since Last Frame)";
        public static readonly ProfilerCounterValue<long> SentSinceLastFrame = new ProfilerCounterValue<long>(Category, SentSinceLastFrameName, ProfilerMarkerDataUnit.Bytes, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        public const string SentTotalName = "Sent Total";
        public static readonly ProfilerCounterValue<long> SentTotal = new ProfilerCounterValue<long>(Category, SentTotalName, ProfilerMarkerDataUnit.Bytes, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        // Received

        public const string ReceivedSinceLastFrameName = "Received (Since Last Frame)";
        public static readonly ProfilerCounterValue<long> ReceivedSinceLastFrame = new ProfilerCounterValue<long>(Category, ReceivedSinceLastFrameName, ProfilerMarkerDataUnit.Bytes, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        public const string ReceivedAndUnprocessedName = "Received and Unprocessed";
        public static readonly ProfilerCounterValue<long> ReceivedAndUnprocessed = new ProfilerCounterValue<long>(Category, ReceivedAndUnprocessedName, ProfilerMarkerDataUnit.Bytes, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        public const string ReceivedTotalName = "Received Total";
        public static readonly ProfilerCounterValue<long> ReceivedTotal = new ProfilerCounterValue<long>(Category, ReceivedTotalName, ProfilerMarkerDataUnit.Bytes, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        // Connections

        public const string OpenConnectionsName = "Open Connections";
        public static readonly ProfilerCounterValue<int> OpenConnectionsCounter = new ProfilerCounterValue<int>(Category, OpenConnectionsName, ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public const string TotalConnectionsName = "Total Connections";
        public static readonly ProfilerCounterValue<int> TotalConnectionsCounter = new ProfilerCounterValue<int>(Category, TotalConnectionsName, ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        // DNS

        public const string TotalDNSCacheHits = "DNS Cache Hits";
        public static readonly ProfilerCounterValue<int> TotalDNSCacheHitsCounter = new ProfilerCounterValue<int>(Category, TotalDNSCacheHits, ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public const string TotalDNSCacheMiss = "DNS Cache Miss";
        public static readonly ProfilerCounterValue<int> TotalDNSCacheMissCounter = new ProfilerCounterValue<int>(Category, TotalDNSCacheMiss, ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);
    }
}
#endif

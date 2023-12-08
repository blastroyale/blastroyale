#if BESTHTTP_PROFILE && UNITY_2021_2_OR_NEWER
using Unity.Profiling;

namespace Best.HTTP.Profiler.Memory
{
    public sealed class MemoryStats
    {
        public const string CategoryName = "Best - Memory";

        public static readonly ProfilerCategory Category = new ProfilerCategory(CategoryName, ProfilerCategoryColor.Scripts);

        public const string BorrowedName = "Borrowed";
        public static readonly ProfilerCounterValue<long> Borrowed = new ProfilerCounterValue<long>(Category, BorrowedName, ProfilerMarkerDataUnit.Bytes, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        public const string PooledName = "Pooled";
        public static readonly ProfilerCounterValue<long> Pooled = new ProfilerCounterValue<long>(Category, PooledName, ProfilerMarkerDataUnit.Bytes, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        public const string CacheHitsName = "Cache Hits";
        public static readonly ProfilerCounterValue<long> CacheHits = new ProfilerCounterValue<long>(Category, CacheHitsName, ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        public const string ArrayAllocationsName = "Array Allocations (Cache Misses)";
        public static readonly ProfilerCounterValue<long> ArrayAllocations = new ProfilerCounterValue<long>(Category, ArrayAllocationsName, ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
    }
}
#endif

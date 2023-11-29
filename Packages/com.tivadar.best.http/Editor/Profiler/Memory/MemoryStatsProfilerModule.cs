#if BESTHTTP_PROFILE && UNITY_2021_2_OR_NEWER
using Best.HTTP.Profiler.Memory;

using Unity.Profiling.Editor;

namespace Best.HTTP.Editor.Profiler.Memory
{
    [System.Serializable]
    [ProfilerModuleMetadata(MemoryStats.CategoryName)]
    public sealed class MemoryStatsProfilerModule : ProfilerModule
    {
        static readonly ProfilerCounterDescriptor[] k_Counters =
        {
            new ProfilerCounterDescriptor(MemoryStats.BorrowedName, MemoryStats.Category),
            new ProfilerCounterDescriptor(MemoryStats.PooledName, MemoryStats.Category),
            new ProfilerCounterDescriptor(MemoryStats.CacheHitsName, MemoryStats.Category),
            new ProfilerCounterDescriptor(MemoryStats.ArrayAllocationsName, MemoryStats.Category)
        };

        public MemoryStatsProfilerModule() : base(k_Counters)
        {
        }
    }
}
#endif

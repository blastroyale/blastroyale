using System;
using System.Collections.Generic;
using System.Threading;

using System.Runtime.CompilerServices;
using Best.HTTP.Shared.Logger;
using System.Collections.Concurrent;

#if BESTHTTP_ENABLE_BUFFERPOOL_BORROWED_BUFFERS_COLLECTION
using System.Linq;
#endif

namespace Best.HTTP.Shared.PlatformSupport.Memory
{
    /// <summary>
    /// Light-weight user-mode lock for code blocks that has rare contentions and doesn't take a long time to finish.
    /// </summary>
    internal sealed class UserModeLock
    {
        private int _locked = 0;

        public void Acquire()
        {
            SpinWait spinWait = new SpinWait();
            while (Interlocked.CompareExchange(ref _locked, 1, 0) != 0)
                spinWait.SpinOnce();
        }

        public bool TryAcquire()
        {
            SpinWait spinWait = new SpinWait();
            while (Interlocked.CompareExchange(ref _locked, 1, 0) != 0)
            {
                if (spinWait.NextSpinWillYield)
                    return false;
                spinWait.SpinOnce();
            }

            return true;
        }

        public void Release()
        {
            Interlocked.Exchange(ref _locked, 0);
        }
    }

#if BESTHTTP_PROFILE
    public struct BufferStats
    {
        public long Size;
        public int Count;
    }

    public struct BufferPoolStats
    {
        public long GetBuffers;
        public long ReleaseBuffers;
        public long PoolSize;
        public long MaxPoolSize;
        public long MinBufferSize;
        public long MaxBufferSize;

        public long Borrowed;
        public long ArrayAllocations;

        public int FreeBufferCount;
        public List<BufferStats> FreeBufferStats;

        public TimeSpan NextMaintenance;
    }

#if BESTHTTP_ENABLE_BUFFERPOOL_BORROWED_BUFFERS_COLLECTION
    public readonly struct BorrowedBuffer
    {
        public readonly string StackTrace;
        public readonly LoggingContext Context;

        public BorrowedBuffer(string stackTrace, LoggingContext context)
        {
            this.StackTrace = stackTrace;
            this.Context = context;
        }
    }
#endif
#endif

    /// <summary>
    /// The BufferPool is a foundational element of the Best HTTP package, aiming to reduce dynamic memory allocation overheads by reusing byte arrays. The concept is elegantly simple: rather than allocating and deallocating memory for every requirement, byte arrays can be "borrowed" and "returned" within this pool. Once returned, these arrays are retained for subsequent use, minimizing repetitive memory operations.
    /// <para>While the BufferPool is housed within the Best HTTP package, its benefits are not limited to just HTTP operations. All protocols and packages integrated with or built upon the Best HTTP package utilize and benefit from the BufferPool. This ensures that memory is used efficiently and performance remains optimal across all integrated components.</para>
    /// </summary>
    [Best.HTTP.Shared.PlatformSupport.IL2CPP.Il2CppEagerStaticClassConstructionAttribute]
    public static class BufferPool
    {
        /// <summary>
        /// Represents an empty byte array that can be returned for zero-length requests.
        /// </summary>
        public static readonly byte[] NoData = new byte[0];

        /// <summary>
        /// Gets or sets a value indicating whether the buffer pooling mechanism is enabled or disabled.
        /// Disabling will also clear all stored entries.
        /// </summary>
        public static bool IsEnabled {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;

                // When set to non-enabled remove all stored entries
                if (!_isEnabled)
                    Clear();
            }
        }
        private static volatile bool _isEnabled = true;

        /// <summary>
        /// Specifies the duration after which buffer entries, once released back to the pool, are deemed old and will be
        /// considered for removal in the next maintenance cycle.
        /// </summary>
        public static TimeSpan RemoveOlderThan = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Specifies how frequently the maintenance cycle should run to manage old buffers.
        /// </summary>
        public static TimeSpan RunMaintenanceEvery = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Specifies the minimum buffer size that will be allocated. If a request is made for a size smaller than this and canBeLarger is <c>true</c>, 
        /// this size will be used.
        /// </summary>
        public static long MinBufferSize = 32;

        /// <summary>
        /// Specifies the maximum size of a buffer that the system will consider storing back into the pool.
        /// </summary>
        public static long MaxBufferSize = long.MaxValue;

        /// <summary>
        /// Specifies the maximum total size of all stored buffers. When the buffer reach this threshold, new releases will be declined.
        /// </summary>
        public static long MaxPoolSize = 30 * 1024 * 1024;

        /// <summary>
        /// Indicates whether to remove buffer stores that don't hold any buffers from the free list.
        /// </summary>
        public static bool RemoveEmptyLists = false;

        /// <summary>
        /// If set to <c>true</c>, and a byte array is released back to the pool more than once, an error will be logged.
        /// </summary>
        /// <remarks>Error checking is expensive and has a very large overhead! Turn it on with caution!</remarks>
        public static bool IsDoubleReleaseCheckEnabled = false;

        // It must be sorted by buffer size!
        private readonly static List<BufferStore> FreeBuffers = new List<BufferStore>();
        private static DateTime lastMaintenance = DateTime.MinValue;

        // Statistics
        private static long PoolSize = 0;
        private static long GetBuffers = 0;
        private static long ReleaseBuffers = 0;
        private static long Borrowed = 0;
        private static long ArrayAllocations = 0;

#if BESTHTTP_PROFILE && BESTHTTP_ENABLE_BUFFERPOOL_BORROWED_BUFFERS_COLLECTION
        private static Dictionary<byte[], BorrowedBuffer> BorrowedBuffers = new Dictionary<byte[], BorrowedBuffer>();
#endif

        //private readonly static ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private readonly static UserModeLock _lock = new UserModeLock();

        static BufferPool()
        {
#if UNITY_EDITOR
            IsDoubleReleaseCheckEnabled = true;
#else
            IsDoubleReleaseCheckEnabled = false;
#endif

#if UNITY_ANDROID || UNITY_IOS
            UnityEngine.Application.lowMemory -= OnLowMemory;
            UnityEngine.Application.lowMemory += OnLowMemory;
#endif
        }

#if UNITY_EDITOR
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetSetup()
        {
            HTTPManager.Logger.Information("BufferPool", "Reset called!");
            PoolSize = 0;
            GetBuffers = 0;
            ReleaseBuffers = 0;
            Borrowed = 0;
            ArrayAllocations = 0;

            FreeBuffers.Clear();
            lastMaintenance = DateTime.MinValue;

#if UNITY_ANDROID || UNITY_IOS
            UnityEngine.Application.lowMemory -= OnLowMemory;
            UnityEngine.Application.lowMemory += OnLowMemory;
#endif

#if BESTHTTP_ENABLE_BUFFERPOOL_BORROWED_BUFFERS_COLLECTION
            BorrowedBuffers.Clear();
#endif
        }
#endif

#if UNITY_ANDROID || UNITY_IOS
        private static void OnLowMemory()
        {
            HTTPManager.Logger.Warning(nameof(BufferPool), nameof(OnLowMemory));

            Clear();
        }
#endif

        /// <summary>
        /// Fetches a byte array from the pool.
        /// </summary>
        /// <remarks>Depending on the `canBeLarger` parameter, the returned buffer may be larger than the requested size!</remarks>
        /// <param name="size">Requested size of the buffer.</param>
        /// <param name="canBeLarger">If <c>true</c>, the returned buffer can be larger than the requested size.</param>
        /// <param name="context">Optional context for logging purposes.</param>
        /// <returns>A byte array from the pool or a newly allocated one if suitable size is not available.</returns>
        public static byte[] Get(long size, bool canBeLarger, LoggingContext context = null)
        {
            if (!_isEnabled)
                return new byte[size];

            // Return a fix reference for 0 length requests. Any resize call (even Array.Resize) creates a new reference
            //  so we are safe to expose it to multiple callers.
            if (size == 0)
                return BufferPool.NoData;

            if (canBeLarger)
            {
                if (size < MinBufferSize)
                    size = MinBufferSize;
                else if (!IsPowerOfTwo(size))
                    size = NextPowerOf2(size);
            }
            else
            {
                if (size < MinBufferSize)
                    return new byte[size];
            }

            if (FreeBuffers.Count == 0)
            {
                Interlocked.Add(ref Borrowed, size);
                Interlocked.Increment(ref ArrayAllocations);

                var result = new byte[size];

#if BESTHTTP_ENABLE_BUFFERPOOL_BORROWED_BUFFERS_COLLECTION
                lock (FreeBuffers)
                    BorrowedBuffers.Add(result, new BorrowedBuffer(ProcessStackTrace(Environment.StackTrace), context));
#endif

                return result;
            }

            BufferDesc bufferDesc = FindFreeBuffer(size, canBeLarger);

            if (bufferDesc.buffer == null)
            {
                Interlocked.Add(ref Borrowed, size);
                Interlocked.Increment(ref ArrayAllocations);

                var result = new byte[size];

#if BESTHTTP_ENABLE_BUFFERPOOL_BORROWED_BUFFERS_COLLECTION
                lock (FreeBuffers)
                    BorrowedBuffers.Add(result, new BorrowedBuffer(ProcessStackTrace(Environment.StackTrace), context));
#endif

                return result;
            }
            else
            {
#if BESTHTTP_ENABLE_BUFFERPOOL_BORROWED_BUFFERS_COLLECTION
                lock (FreeBuffers)
                    BorrowedBuffers.Add(bufferDesc.buffer, new BorrowedBuffer(ProcessStackTrace(Environment.StackTrace), context));
#endif

                Interlocked.Increment(ref GetBuffers);
            }

            Interlocked.Add(ref Borrowed, bufferDesc.buffer.Length);
            Interlocked.Add(ref PoolSize, -bufferDesc.buffer.Length);

            return bufferDesc.buffer;
        }

        /// <summary>
        /// Releases a list of buffer segments back to the pool in a bulk operation.
        /// </summary>
        /// <param name="segments">List of buffer segments to release.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReleaseBulk(ConcurrentQueue<BufferSegment> segments)
        {
            if (!_isEnabled || segments == null)
                return;

            //using var _ = new WriteLock(rwLock);
            _lock.Acquire();
            try
            {
                while (segments.TryDequeue(out var segment))
                    Release(segment, false);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Releases a list of buffer segments back to the pool in a bulk operation.
        /// </summary>
        /// <param name="segments">List of buffer segments to release.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReleaseBulk(List<BufferSegment> segments)
        {
            if (!_isEnabled || segments == null)
                return;

            //using var _ = new WriteLock(rwLock);
            _lock.Acquire();
            try
            {
                while (segments.Count > 0)
                {
                    var segment = segments[0];

                    Release(segment, false);

                    segments.RemoveAt(0);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Releases a byte array back to the pool.
        /// </summary>
        /// <param name="buffer">Buffer to be released back to the pool.</param>
        public static void Release(byte[] buffer) => Release(buffer, true);
        
        private static void Release(byte[] buffer, bool acquireLock)
        {
            if (!_isEnabled || buffer == null)
                return;

            int size = buffer.Length;

#if BESTHTTP_ENABLE_BUFFERPOOL_BORROWED_BUFFERS_COLLECTION
            lock (FreeBuffers)
                BorrowedBuffers.Remove(buffer);
#endif

            Interlocked.Add(ref Borrowed, -size);

            if (size == 0 || size < MinBufferSize || size > MaxBufferSize)
                return;

            //using (new WriteLock(rwLock))
            if (acquireLock)
                _lock.Acquire();
            try
            {

                var ps = Interlocked.Read(ref PoolSize);
                if (ps + size > MaxPoolSize)
                    return;

                Interlocked.Add(ref PoolSize, size);

                ReleaseBuffers++;

                AddFreeBuffer(buffer);
            }
            finally
            {
                if (acquireLock)
                    _lock.Release();
            }
        }

        /// <summary>
        /// Resizes a byte array by returning the old one to the pool and fetching (or creating) a new one of the specified size.
        /// </summary>
        /// <param name="buffer">Buffer to resize.</param>
        /// <param name="newSize">New size for the buffer.</param>
        /// <param name="canBeLarger">If <c>true</c>, the new buffer can be larger than the specified size.</param>
        /// <param name="clear">If <c>true</c>, the new buffer will be cleared (set to all zeros).</param>
        /// <param name="context">Optional context for logging purposes.</param>
        /// <returns>A resized buffer.</returns>
        public static byte[] Resize(ref byte[] buffer, int newSize, bool canBeLarger, bool clear, LoggingContext context = null)
        {
            if (!_isEnabled)
            {
                Array.Resize<byte>(ref buffer, newSize);
                return buffer;
            }

            byte[] newBuf = BufferPool.Get(newSize, canBeLarger, context);
            if (buffer != null)
            {
                if (!clear)
                    Array.Copy(buffer, 0, newBuf, 0, Math.Min(newBuf.Length, buffer.Length));
                BufferPool.Release(buffer);
            }

            if (clear)
                Array.Clear(newBuf, 0, newSize);

            return buffer = newBuf;
        }

#if BESTHTTP_ENABLE_BUFFERPOOL_BORROWED_BUFFERS_COLLECTION
        public static KeyValuePair<byte[], BorrowedBuffer>[] GetBorrowedBuffers()
        {
            lock (FreeBuffers)
                return BorrowedBuffers.ToArray();
        }
#endif

#if BESTHTTP_PROFILE
        public static void GetStatistics(ref BufferPoolStats stats)
        {
            //using (new ReadLock(rwLock))
            if (!_lock.TryAcquire())
                return;
            try
            {
                stats.GetBuffers = GetBuffers;
                stats.ReleaseBuffers = ReleaseBuffers;
                stats.PoolSize = PoolSize;
                stats.MinBufferSize = MinBufferSize;
                stats.MaxBufferSize = MaxBufferSize;
                stats.MaxPoolSize = MaxPoolSize;

                stats.Borrowed = Borrowed;
                stats.ArrayAllocations = ArrayAllocations;

                stats.FreeBufferCount = FreeBuffers.Count;
                if (stats.FreeBufferStats == null)
                    stats.FreeBufferStats = new List<BufferStats>(FreeBuffers.Count);
                else
                    stats.FreeBufferStats.Clear();

                for (int i = 0; i < FreeBuffers.Count; ++i)
                {
                    BufferStore store = FreeBuffers[i];
                    List<BufferDesc> buffers = store.buffers;

                    BufferStats bufferStats = new BufferStats();
                    bufferStats.Size = store.Size;
                    bufferStats.Count = buffers.Count;

                    stats.FreeBufferStats.Add(bufferStats);
                }

                stats.NextMaintenance = (lastMaintenance + RunMaintenanceEvery) - DateTime.Now;
            }
            finally
            {
                _lock.Release();
            }
        }
#endif

        /// <summary>
        /// Clears all stored entries in the buffer pool instantly, releasing memory.
        /// </summary>
        public static void Clear()
        {
            //using (new WriteLock(rwLock))
            _lock.Acquire();
            try
            {
                FreeBuffers.Clear();
                Interlocked.Exchange(ref PoolSize, 0);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Internal method called by the plugin to remove old, non-used buffers.
        /// </summary>
        internal static void Maintain()
        {
            DateTime now = DateTime.Now;
            if (!_isEnabled || lastMaintenance + RunMaintenanceEvery > now)
                return;
            
            DateTime olderThan = now - RemoveOlderThan;
            //using (new WriteLock(rwLock))
            if (!_lock.TryAcquire())
                return;

            lastMaintenance = now;
            try
            {
                for (int i = 0; i < FreeBuffers.Count; ++i)
                {
                    BufferStore store = FreeBuffers[i];
                    List<BufferDesc> buffers = store.buffers;

                    for (int cv = buffers.Count - 1; cv >= 0; cv--)
                    {
                        BufferDesc desc = buffers[cv];

                        if (desc.released < olderThan)
                        {
                            // buffers stores available buffers ascending by age. So, when we find an old enough, we can
                            //  delete all entries in the [0..cv] range.

                            int removeCount = cv + 1;
                            buffers.RemoveRange(0, removeCount);
                            PoolSize -= (int)(removeCount * store.Size);
                            break;
                        }
                    }

                    if (RemoveEmptyLists && buffers.Count == 0)
                        FreeBuffers.RemoveAt(i--);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

#region Private helper functions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(long x)
        {
            return (x & (x - 1)) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long NextPowerOf2(long x)
        {
            long pow = 1;
            while (pow <= x)
                pow *= 2;
            return pow;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static BufferDesc FindFreeBuffer(long size, bool canBeLarger)
        {
            // Previously it was an upgradable read lock, and later a write lock around store.buffers.RemoveAt.
            // However, checking store.buffers.Count in the if statement, and then get the last buffer and finally write lock the RemoveAt call
            //  has plenty of time for race conditions.
            //  Another thread could change store.buffers after checking count and getting the last element and before the write lock,
            //  so in theory we could return with an element and remove another one from the buffers list.
            //  A new FindFreeBuffer call could return it again causing malformed data and/or releasing it could duplicate it in the store.
            // I tried to reproduce both issues (malformed data, duble entries) with a test where creating growin number of threads getting buffers writing to them, check the buffers and finally release them
            //  would fail _only_ if i used a plain Enter/Exit ReadLock pair, or no locking at all.
            // But, because there's quite a few different platforms and unity's implementation can be different too, switching from an upgradable lock to a more stricter write lock seems safer.
            //
            // An interesting read can be found here: https://stackoverflow.com/questions/21411018/readerwriterlockslim-enterupgradeablereadlock-always-a-deadlock

            //using (new WriteLock(rwLock))
            _lock.Acquire();
            try
            {
                for (int i = 0; i < FreeBuffers.Count; ++i)
                {
                    BufferStore store = FreeBuffers[i];

                    if (store.buffers.Count > 0 && (store.Size == size || (canBeLarger && store.Size > size)))
                    {
                        // Getting the last one has two desired effect:
                        //  1.) RemoveAt should be quicker as it don't have to move all the remaining entries
                        //  2.) Old, non-used buffers will age. Getting a buffer and putting it back will not keep buffers fresh.

                        BufferDesc lastFree = store.buffers[store.buffers.Count - 1];
                        store.buffers.RemoveAt(store.buffers.Count - 1);
                        
                        return lastFree;
                    }
                }
            }
            finally
            {
                _lock.Release();
            }

            return BufferDesc.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddFreeBuffer(byte[] buffer)
        {
            int bufferLength = buffer.Length;

            for (int i = 0; i < FreeBuffers.Count; ++i)
            {
                BufferStore store = FreeBuffers[i];

                if (store.Size == bufferLength)
                {
                    // We highly assume here that every buffer will be released only once.
                    //  Checking for double-release would mean that we have to do another O(n) operation, where n is the
                    //  count of the store's elements.

                    if (IsDoubleReleaseCheckEnabled)
                        for (int cv = 0; cv < store.buffers.Count; ++cv)
                        {
                            var entry = store.buffers[cv];
                            if (System.Object.ReferenceEquals(entry.buffer, buffer))
                            {
#if BESTHTTP_ENABLE_BUFFERPOOL_BORROWED_BUFFERS_COLLECTION
                                if (BorrowedBuffers.TryGetValue(buffer, out var bb))
                                {
                                    HTTPManager.Logger.Error("BufferPool", $"Buffer ({entry}) already added to the pool! BorrowedBuffer: {bb.StackTrace}", bb.Context);
                                }
                                else
#endif
                                    HTTPManager.Logger.Error("BufferPool", $"Buffer ({entry}) already added to the pool!");
                                //throw new Exception($"Buffer ({entry}) already added to the pool!");
                                return;
                            }
                        }

                    store.buffers.Add(new BufferDesc(buffer));
                    return;
                }

                if (store.Size > bufferLength)
                {
                    FreeBuffers.Insert(i, new BufferStore(bufferLength, buffer));
                    return;
                }
            }

            // When we reach this point, there's no same sized or larger BufferStore present, so we have to add a new one
            //  to the end of our list.
            FreeBuffers.Add(new BufferStore(bufferLength, buffer));
        }

#if BESTHTTP_ENABLE_BUFFERPOOL_BORROWED_BUFFERS_COLLECTION
        private static System.Text.StringBuilder stacktraceBuilder;
        private static string ProcessStackTrace(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return string.Empty;

            var lines = stackTrace.Split('\n');

            if (stacktraceBuilder == null)
                stacktraceBuilder = new System.Text.StringBuilder(lines.Length);
            else
                stacktraceBuilder.Length = 0;

            // skip top 4 lines that would show the logger.

            for (int i = 0; i < lines.Length; ++i)
                if (!lines[i].Contains(".Memory.BufferPool") &&
                    !lines[i].Contains("Environment") &&
                    !lines[i].Contains("System.Threading"))
                    stacktraceBuilder.Append(lines[i].Replace("Best.HTTP.", ""));

            return stacktraceBuilder.ToString();
        }
#endif

#endregion
    }
}

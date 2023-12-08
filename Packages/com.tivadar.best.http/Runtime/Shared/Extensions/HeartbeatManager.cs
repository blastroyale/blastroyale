using System;
using System.Collections.Generic;
using System.Threading;

using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Threading;

namespace Best.HTTP.Shared.Extensions
{
    sealed class RunOnceOnMainThread : IHeartbeat
    {
        private Action _action;
        private int _subscribed;
        private LoggingContext _context;

        public RunOnceOnMainThread(Action action, LoggingContext context)
        {
            this._action = action;
            this._context = context;
        }

        public void Subscribe()
        {
            if (Interlocked.CompareExchange(ref this._subscribed, 1, 0) == 0)
                HTTPManager.Heartbeats.Subscribe(this);
        }

        public void OnHeartbeatUpdate(DateTime now, TimeSpan dif)
        {
            try
            {
                this._action?.Invoke();
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception(nameof(RunOnceOnMainThread.OnHeartbeatUpdate), $"{nameof(_action)}", ex, this._context);
            }
            finally
            {
                HTTPManager.Heartbeats.Unsubscribe(this);
            }
        }
    }

    public interface IHeartbeat
    {
        void OnHeartbeatUpdate(DateTime now, TimeSpan dif);
    }

    /// <summary>
    /// A manager class that can handle subscribing and unsubscribeing in the same update.
    /// </summary>
    public sealed class HeartbeatManager
    {
        private ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private List<IHeartbeat> Heartbeats = new List<IHeartbeat>();
        private IHeartbeat[] UpdateArray;
        private DateTime LastUpdate = DateTime.MinValue;

        public void Subscribe(IHeartbeat heartbeat)
        {
            using var _ = new WriteLock(rwLock);

            if (!Heartbeats.Contains(heartbeat))
                Heartbeats.Add(heartbeat);
        }

        public void Unsubscribe(IHeartbeat heartbeat)
        {
            using var _ = new WriteLock(rwLock);

            Heartbeats.Remove(heartbeat);
        }

        public void Update()
        {
            var now = HTTPManager.CurrentFrameDateTime;

            if (LastUpdate == DateTime.MinValue)
                LastUpdate = now;
            else
            {
                TimeSpan dif = now - LastUpdate;
                LastUpdate = now;
                
                int count = 0;

                using (var _ = new ReadLock(rwLock))
                {
                    if (UpdateArray == null || UpdateArray.Length < Heartbeats.Count)
                        Array.Resize(ref UpdateArray, Heartbeats.Count);

                    Heartbeats.CopyTo(0, UpdateArray, 0, Heartbeats.Count);
                    Array.Clear(UpdateArray, Heartbeats.Count, UpdateArray.Length - Heartbeats.Count);

                    count = Heartbeats.Count;
                }

                for (int i = 0; i < count; ++i)
                {
                    try
                    {
                        UpdateArray[i].OnHeartbeatUpdate(now, dif);
                    }
                    catch
                    { }
                }
            }
        }

        public void Clear()
        {
            using var _ = new WriteLock(rwLock);

            Heartbeats.Clear();
        }
    }
}

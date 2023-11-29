using System;
using System.Collections.Generic;
using System.Threading;

using Best.HTTP.Shared;
using Best.HTTP.Shared.PlatformSupport.Threading;

namespace Best.HTTP.Shared.Extensions
{
    public readonly struct TimerData
    {
        public readonly DateTime Created;
        public readonly TimeSpan Interval;
        public readonly object Context;

        public readonly Func<DateTime, object, bool> OnTimer;

        public bool IsOnTime(DateTime now)
        {
            return now >= this.Created + this.Interval;
        }

        public TimerData(TimeSpan interval, object context, Func<DateTime, object, bool> onTimer)
        {
            this.Created = DateTime.Now;
            this.Interval = interval;
            this.Context = context;
            this.OnTimer = onTimer;
        }

        /// <summary>
        /// Create a new TimerData but the Created field will be set to the current time.
        /// </summary>
        public TimerData CreateNew()
        {
            return new TimerData(this.Interval, this.Context, this.OnTimer);
        }

        public override string ToString()
        {
            return string.Format("[TimerData Created: {0}, Interval: {1}, IsOnTime: {2}]", this.Created.ToString(System.Globalization.CultureInfo.InvariantCulture), this.Interval, this.IsOnTime(DateTime.Now));
        }
    }

    public static class Timer
    {
        private static List<TimerData> _timers = new List<TimerData>(1);
        private static ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        private static int _isSubscribed;

#if UNITY_EDITOR
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetSetup()
        {
            HTTPManager.Logger.Information(nameof(Timer), "Reset called!");
            Interlocked.Exchange(ref _isSubscribed, 0);
        }
#endif

        public static void Add(TimerData timer)
        {
            using (var _ = new WriteLock(_lock))
                _timers.Add(timer);

            if (Interlocked.CompareExchange(ref _isSubscribed, 1, 0) == 0)
            {
                HTTPManager.Logger.Information(nameof(Timer), "Subscribing timer to heartbeats!");
                HTTPManager.Heartbeats.Subscribe(new TimerImplementation());
            }
        }

        private sealed class TimerImplementation : IHeartbeat
        {
            public void OnHeartbeatUpdate(DateTime now, TimeSpan dif)
            {
                using var __ = new Unity.Profiling.ProfilerMarker(nameof(Timer)).Auto();

                using (var _ = new WriteLock(_lock))
                {
                    if (_timers.Count == 0)
                    {
                        HTTPManager.Heartbeats.Unsubscribe(this);

                        Interlocked.Exchange(ref _isSubscribed, 0);

                        HTTPManager.Logger.Information(nameof(Timer), "Unsubscribing timer from heartbeats!");
                        return;
                    }

                    for (int i = 0; i < _timers.Count; ++i)
                    {
                        TimerData timer = _timers[i];

                        if (timer.IsOnTime(now))
                        {
                            try
                            {
                                bool repeat = timer.OnTimer(now, timer.Context);

                                if (repeat)
                                    _timers[i] = timer.CreateNew();
                                else
                                    _timers.RemoveAt(i--);
                            }
                            catch (Exception ex)
                            {
                                HTTPManager.Logger.Exception(nameof(Timer), "OnTimer", ex);
                            }
                        }
                    }
                }
            }
        }
    }
}

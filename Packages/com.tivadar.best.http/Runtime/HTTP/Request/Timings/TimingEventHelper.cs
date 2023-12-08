using System.Collections.Concurrent;

using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;

namespace Best.HTTP.Request.Timings
{
    public static class TimingEventHelper
    {
        private static ConcurrentQueue<TimingEventInfo> eventQueue = new ConcurrentQueue<TimingEventInfo>();

        public static void Enqueue(TimingEventInfo timingEvent)
        {
            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Information(nameof(TimingEventHelper), $"{nameof(Enqueue)} {timingEvent}", timingEvent.SourceRequest.Context);

            /*if (HTTPUpdateDelegator.Instance.IsMainThread())
                timingEvent.SourceRequest.Timing.AddEvent(timingEvent);
            else*/
            eventQueue.Enqueue(timingEvent);
        }

        internal static void Clear()
        {
            eventQueue.Clear();
        }

        internal static void ProcessQueue()
        {
            TimingEventInfo timingEvent;
            while (eventQueue.TryDequeue(out timingEvent))
                timingEvent.SourceRequest.Timing.AddEvent(timingEvent);
        }
    }
}

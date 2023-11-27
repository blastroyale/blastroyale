using System;

namespace Best.HTTP.Request.Timings
{
    public enum TimingEvents
    {
        StartNext,
        Finish,
        Abort
    }

    public readonly struct TimingEventInfo
    {
        public readonly HTTPRequest SourceRequest;
        public readonly TimingEvents Event;

        public readonly string Name;
        public readonly DateTime Time;

        public TimingEventInfo(HTTPRequest parentRequest, TimingEvents timingEvent)
        {
            this.SourceRequest = parentRequest;
            this.Event = timingEvent;

            this.Name = null;
            this.Time = DateTime.Now;
        }

        public TimingEventInfo(HTTPRequest parentRequest, TimingEvents timingEvent, string eventName)
        {
            this.SourceRequest = parentRequest;
            this.Event = timingEvent;

            this.Name = eventName;
            this.Time = DateTime.Now;
        }

        public override string ToString() => $"[{Event} \"{Name}\", Time: \"{Time.ToString("hh:mm:ss.fffffff")}\", Source: {SourceRequest.Context.Hash}]";
    }
}

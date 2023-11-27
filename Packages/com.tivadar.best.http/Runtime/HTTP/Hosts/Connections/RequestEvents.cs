using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Best.HTTP.Caching;
using Best.HTTP.HostSetting;
using Best.HTTP.Request.Settings;
using Best.HTTP.Request.Timings;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;

namespace Best.HTTP.Hosts.Connections
{
    public enum RequestEvents
    {
        Upgraded,
        DownloadProgress,
        UploadProgress,
        StreamingData,
        DownloadStarted,
        StateChange,
        SetState,
        Resend,
        Headers
    }

    public readonly struct RequestEventInfo
    {
        public readonly HTTPRequest SourceRequest;
        public readonly RequestEvents Event;

        public readonly HTTPRequestStates State;

        public readonly Exception Error;

        public readonly long Progress;
        public readonly long ProgressLength;

        public readonly byte[] Data;
        public readonly int DataLength;

        // Headers
        public readonly Dictionary<string, List<string>> Headers;

        public RequestEventInfo(HTTPRequest request, RequestEvents @event)
        {
            this.SourceRequest = request;
            this.Event = @event;

            this.State = HTTPRequestStates.Initial;

            this.Error = null;

            this.Progress = this.ProgressLength = 0;

            this.Data = null;
            this.DataLength = 0;

            // Headers
            this.Headers = null;
        }

        public RequestEventInfo(HTTPRequest request, RequestEvents @event, HTTPRequestStates newState)
        {
            this.SourceRequest = request;
            this.Event = @event;

            this.State = newState;

            this.Error = null;

            this.Progress = this.ProgressLength = 0;

            this.Data = null;
            this.DataLength = 0;

            // Headers
            this.Headers = null;
        }

        public RequestEventInfo(HTTPRequest request, HTTPRequestStates newState)
        {
            this.SourceRequest = request;
            this.Event = RequestEvents.StateChange;
            this.State = newState;

            this.Error = null;

            this.Progress = this.ProgressLength = 0;
            this.Data = null;
            this.DataLength = 0;

            // Headers
            this.Headers = null;
        }

        public RequestEventInfo(HTTPRequest request, HTTPRequestStates newState, Exception error)
        {
            this.SourceRequest = request;
            this.Event = RequestEvents.SetState;
            this.State = newState;

            this.Error = error;

            this.Progress = this.ProgressLength = 0;
            this.Data = null;
            this.DataLength = 0;

            // Headers
            this.Headers = null;
        }

        public RequestEventInfo(HTTPRequest request, RequestEvents @event, long progress, long progressLength)
        {
            this.SourceRequest = request;
            this.Event = @event;
            this.State = HTTPRequestStates.Initial;

            this.Error = null;

            this.Progress = progress;
            this.ProgressLength = progressLength;
            this.Data = null;
            this.DataLength = 0;

            // Headers
            this.Headers = null;
        }

        public RequestEventInfo(HTTPRequest request, byte[] data, int dataLength)
        {
            this.SourceRequest = request;
            this.Event = RequestEvents.StreamingData;
            this.State = HTTPRequestStates.Initial;

            this.Error = null;

            this.Progress = this.ProgressLength = 0;
            this.Data = data;
            this.DataLength = dataLength;

            // Headers
            this.Headers = null;
        }

        public RequestEventInfo(HTTPRequest request, Dictionary<string, List<string>> headers)
        {
            this.SourceRequest = request;
            this.Event = RequestEvents.Headers;
            this.State = HTTPRequestStates.Initial;

            this.Error = null;

            this.Progress = this.ProgressLength = 0;
            this.Data = null;
            this.DataLength = 0;

            // Headers
            this.Headers = headers;
        }

        public override string ToString()
        {
            switch (this.Event)
            {
                case RequestEvents.Upgraded:
                    return string.Format("[RequestEventInfo Event: Upgraded, Source: {0}]", this.SourceRequest.Context.Hash);
                case RequestEvents.DownloadProgress:
                    return string.Format("[RequestEventInfo Event: DownloadProgress, Progress: {1}, ProgressLength: {2}, Source: {0}]", this.SourceRequest.Context.Hash, this.Progress, this.ProgressLength);
                case RequestEvents.UploadProgress:
                    return string.Format("[RequestEventInfo Event: UploadProgress, Progress: {1}, ProgressLength: {2}, Source: {0}]", this.SourceRequest.Context.Hash, this.Progress, this.ProgressLength);
                case RequestEvents.StreamingData:
                    return string.Format("[RequestEventInfo Event: StreamingData, DataLength: {1}, Source: {0}]", this.SourceRequest.Context.Hash, this.DataLength);
                case RequestEvents.DownloadStarted:
                    return $"[RequestEventInfo Event: DownloadStarted, Source: {this.SourceRequest.Context.Hash}]";
                case RequestEvents.StateChange:
                    return string.Format("[RequestEventInfo Event: StateChange, State: {1}, Source: {0}]", this.SourceRequest.Context.Hash, this.State);
                case RequestEvents.SetState:
                    return string.Format("[RequestEventInfo Event: SetState, State: {1}, Source: {0}]", this.SourceRequest.Context.Hash, this.State);
                case RequestEvents.Resend:
                    return string.Format("[RequestEventInfo Event: Resend, Source: {0}]", this.SourceRequest.Context.Hash);
                case RequestEvents.Headers:
                    return string.Format("[RequestEventInfo Event: Headers, Source: {0}]", this.SourceRequest.Context.Hash);
                default:
                    throw new NotImplementedException(this.Event.ToString());
            }
        }
    }

    class ProgressFlattener
    {
        struct FlattenedProgress
        {
            public HTTPRequest request;
            public OnProgressDelegate onProgress;
            public long progress;
            public long length;
        }

        private FlattenedProgress[] progresses;
        private bool hasProgress;

        public void InsertOrUpdate(RequestEventInfo info, OnProgressDelegate onProgress)
        {
            if (progresses == null)
                progresses = new FlattenedProgress[1];

            hasProgress = true;

            var newProgresss = new FlattenedProgress { request = info.SourceRequest, progress = info.Progress, length = info.ProgressLength, onProgress = onProgress };

            int firstEmptyIdx = -1;
            for (int i = 0; i < progresses.Length; i++)
            {
                var progress = progresses[i];
                if (object.ReferenceEquals(progress.request, info.SourceRequest))
                {
                    progresses[i] = newProgresss;
                    return;
                }

                if (firstEmptyIdx == -1 && progress.request == null)
                    firstEmptyIdx = i;
            }

            if (firstEmptyIdx == -1)
            {
                Array.Resize(ref progresses, progresses.Length + 1);
                progresses[progresses.Length - 1] = newProgresss;
            }
            else
                progresses[firstEmptyIdx] = newProgresss;
        }

        public void DispatchProgressCallbacks()
        {
            if (progresses == null || !hasProgress)
                return;

            for (int i = 0; i < progresses.Length; ++i)
            {
                var @event = progresses[i];
                var source = @event.request;
                if (source != null && @event.onProgress != null)
                {
                    try
                    {
                        @event.onProgress(source, @event.progress, @event.length);
                    }
                    catch (Exception ex)
                    {
                        HTTPManager.Logger.Exception("ProgressFlattener", "DispatchProgressCallbacks", ex, source.Context);
                    }
                }
            }

            Array.Clear(progresses, 0, progresses.Length);
            hasProgress = false;
        }
    }

    public static class RequestEventHelper
    {
        private static ConcurrentQueue<RequestEventInfo> requestEventQueue = new ConcurrentQueue<RequestEventInfo>();

#pragma warning disable 0649
        public static Action<RequestEventInfo> OnEvent;
#pragma warning restore

        // Low frame rate and hight download/upload speed can add more download/upload progress events to dispatch in one frame.
        // This can add higher CPU usage as it might cause updating the UI/do other things unnecessary in the same frame.
        // To avoid this, instead of calling the events directly, we store the last event's data and call download/upload callbacks only once per frame.

        private static ProgressFlattener downloadProgress;
        private static ProgressFlattener uploadProgress;

        public static void EnqueueRequestEvent(RequestEventInfo ev)
        {
            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Information("RequestEventHelper", "Enqueue " + ev.ToString(), ev.SourceRequest.Context);

            requestEventQueue.Enqueue(ev);
        }

        internal static void Clear()
        {
            requestEventQueue.Clear();
        }

        internal static void ProcessQueue()
        {
            RequestEventInfo requestEvent;
            while (requestEventQueue.TryDequeue(out requestEvent))
            {
                HTTPRequest source = requestEvent.SourceRequest;

                //if (HTTPManager.Logger.Level == Loglevels.All)
                //    HTTPManager.Logger.Information("RequestEventHelper", "Processing request event: " + requestEvent.ToString(), source.Context);

                if (OnEvent != null)
                {
                    try
                    {
                        using (var _ = new Unity.Profiling.ProfilerMarker(nameof(OnEvent)).Auto())
                            OnEvent(requestEvent);
                    }
                    catch (Exception ex)
                    {
                        HTTPManager.Logger.Exception("RequestEventHelper", "ProcessQueue", ex, source.Context);
                    }
                }
                
                switch (requestEvent.Event)
                {
                    case RequestEvents.DownloadProgress:
                        try
                        {
                            if (source.DownloadSettings.OnDownloadProgress != null)
                            {
                                if (downloadProgress == null)
                                    downloadProgress = new ProgressFlattener();
                                downloadProgress.InsertOrUpdate(requestEvent, source.DownloadSettings.OnDownloadProgress);
                            }
                        }
                        catch (Exception ex)
                        {
                            HTTPManager.Logger.Exception("RequestEventHelper", "Process RequestEventQueue - RequestEvents.DownloadProgress", ex, source.Context);
                        }
                        break;

                    case RequestEvents.UploadProgress:
                        try
                        {
                            if (source.UploadSettings.OnUploadProgress != null)
                            {
                                if (uploadProgress == null)
                                    uploadProgress = new ProgressFlattener();
                                uploadProgress.InsertOrUpdate(requestEvent, source.UploadSettings.OnUploadProgress);
                            }
                        }
                        catch (Exception ex)
                        {
                            HTTPManager.Logger.Exception("RequestEventHelper", "Process RequestEventQueue - RequestEvents.UploadProgress", ex, source.Context);
                        }
                        break;

                    case RequestEvents.Resend:
                        source.State = HTTPRequestStates.Initial;

                        var host = HostManager.GetHostVariant(source);

                        host.Send(source);

                        break;

                    case RequestEvents.Headers:
                        {
                            try
                            {
                                var response = source.Response;
                                if (source.DownloadSettings.OnHeadersReceived != null && response != null)
                                    source.DownloadSettings.OnHeadersReceived(source, response, requestEvent.Headers);
                            }
                            catch (Exception ex)
                            {
                                HTTPManager.Logger.Exception("RequestEventHelper", "Process RequestEventQueue - RequestEvents.Headers", ex, source.Context);
                            }
                            break;
                        }

                    case RequestEvents.DownloadStarted:
                        try
                        {
                            var response = source.Response;
                            source.DownloadSettings.OnDownloadStarted?.Invoke(source, response, response.DownStream);
                        }
                        catch(Exception ex)
                        {
                            HTTPManager.Logger.Exception("RequestEventHelper", "DownloadStarted", ex, source.Context);
                        }
                        break;

                    case RequestEvents.SetState:
                        // In a case where the request is aborted its state is set to a >= Finished state then,
                        // on another thread the reqest processing will fail too queuing up a >= Finished state again.
                        if (source.State >= HTTPRequestStates.Finished && requestEvent.State >= HTTPRequestStates.Finished)
                            continue;

                        // It's different from the next condition! (this is >= and the next is only >)
                        if (requestEvent.State >= HTTPRequestStates.Finished)
                            source?.Response?.DownStream?.CompleteAdding(requestEvent.Error);

                        if (requestEvent.State > HTTPRequestStates.Finished)
                        {
                            HTTPManager.Logger.Information("RequestEventHelper", $"{requestEvent.State}: discarding response!", source.Response?.Context ?? source.Context);

                            source.Response?.Dispose();
                            source.Response = null;
                        }

                        source.Exception = requestEvent.Error;
                        source.State = requestEvent.State;

                        // https://www.rfc-editor.org/rfc/rfc5861.html#section-1
                        // The stale-if-error HTTP Cache-Control extension allows a cache to
                        // return a stale response when an error -- e.g., a 500 Internal Server
                        // Error, a network segment, or DNS failure -- is encountered, rather
                        // than returning a "hard" error.
                        if (requestEvent.State > HTTPRequestStates.Finished && requestEvent.State != HTTPRequestStates.Aborted)
                        {
                            if (HTTPManager.LocalCache != null)
                            {
                                var hash = Caching.HTTPCache.CalculateHash(source.MethodType, source.CurrentUri);
                                if (HTTPManager.LocalCache.CanServeWithoutValidation(hash, ErrorTypeForValidation.ConnectionError, source.Context))
                                {
                                    HTTPManager.LocalCache.Redirect(source, hash);
                                    goto case RequestEvents.Resend;
                                }
                            }
                        }

                        goto case RequestEvents.StateChange;

                    case RequestEvents.StateChange:
                        try
                        {
                            using (var _ = new Unity.Profiling.ProfilerMarker(nameof(RequestEventHelper.HandleRequestStateChange)).Auto())
                                RequestEventHelper.HandleRequestStateChange(requestEvent);
                        }
                        catch(Exception ex)
                        {
                            HTTPManager.Logger.Exception("RequestEventHelper", "HandleRequestStateChange", ex, source.Context);
                        }
                        break;
                }
            }

            uploadProgress?.DispatchProgressCallbacks();
            downloadProgress?.DispatchProgressCallbacks();
        }

        // TODO: don't start/repeat if can't time out?
        private static bool AbortRequestWhenTimedOut(DateTime now, object context)
        {
            HTTPRequest request = context as HTTPRequest;

            if (request.State >= HTTPRequestStates.Finished)
                return false; // don't repeat

            var downStream= request.Response?.DownStream;

            if (downStream != null && downStream.DoFullCheck(limit: 2))
            {
                HTTPManager.Logger.Warning(nameof(RequestEventHelper), $"Request's download stream is full({downStream.Length:N0}/{downStream.MaxBuffered:N0}) without any Read attempt! You can either increase HTTPRequest's DownloadSettings.ContentStreamMaxBuffered or use streaming. Request's uri: {request.Uri}", request.Context);
                request.Abort();
                return false;
            }

            // Upgradable protocols will shut down themselves
            if (request?.Response?.IsUpgraded is bool upgraded && upgraded)
                return false;

            if (request.TimeoutSettings.IsTimedOut(HTTPManager.CurrentFrameDateTime))
            {
                HTTPManager.Logger.Information("RequestEventHelper", "AbortRequestWhenTimedOut - Request timed out. CurrentUri: " + request.CurrentUri.ToString(), request.Context);
                request.Abort();

                return false; // don't repeat
            }

            return true;  // repeat
        }

        static readonly string[] RequestStateNames = new string[] { "Initial", "Queued", "Processing", "Finished", "Error", "Aborted", "ConnectionTimedOut", "TimedOut" };

        private static void HandleRequestStateChange(RequestEventInfo @event)
        {
            HTTPRequest source = @event.SourceRequest;

            // Because there's a race condition between setting the request's State in its Abort() function running on Unity's main thread
            //  and the HTTP1/HTTP2 handlers running on an another one.
            // Because of these race conditions cases violating expectations can be:
            //  1.) State is finished but the response null
            //  2.) State is (Connection)TimedOut and the response non-null
            // We have to make sure that no callbacks are called twice and in the request must be in a consistent state!

            //    State        | Request
            //   ---------     +---------
            // 1                  Null
            //   Finished      |   Skip
            //   Timeout/Abort |   Deliver
            //                 
            // 2                 Non-Null
            //   Finished      |    Deliver
            //   Timeout/Abort |    Skip

            using var _ = new Unity.Profiling.ProfilerMarker(RequestStateNames[(int)@event.State]).Auto();

            switch (@event.State)
            {
                case HTTPRequestStates.Queued:
                    source.Timing.StartNext(TimingEventNames.Queued);

                    source.TimeoutSettings.QueuedAt = HTTPManager.CurrentFrameDateTime;
                    Timer.Add(new TimerData(TimeSpan.FromSeconds(1), @event.SourceRequest, AbortRequestWhenTimedOut));
                    break;

                case HTTPRequestStates.ConnectionTimedOut:
                case HTTPRequestStates.TimedOut:
                case HTTPRequestStates.Error:
                case HTTPRequestStates.Aborted:
                    HTTPManager.Logger.Information("RequestEventHelper", $"{@event.State}: discarding response!", source.Response?.Context ?? source.Context);

                    source.Response?.Dispose();
                    source.Response = null;
                    goto case HTTPRequestStates.Finished;

                case HTTPRequestStates.Finished:
                    // Dispatch any collected download/upload progress, otherwise they would _after_ the callback!
                    uploadProgress?.DispatchProgressCallbacks();
                    downloadProgress?.DispatchProgressCallbacks();

                    if (source.Callback != null)
                    {
                        source.Timing.AddEvent(new TimingEventInfo(source, TimingEvents.Finish, null));
                        source.Timing.StartNext(TimingEventNames.Callback);
                        try
                        {
                            using (var __ = new Unity.Profiling.ProfilerMarker(nameof(source.Callback)).Auto())
                                source.Callback(source, source.Response);
                        }
                        catch (Exception ex)
                        {
                            HTTPManager.Logger.Exception("RequestEventHelper", "HandleRequestStateChange " + @event.State, ex, source.Context);
                        }
                    }

                    source.Timing.Finish();

                    // This delay required because with coroutines these lines are executed first
                    //  before the coroutine has a chance to do something with a finished request.
                    // By adding a delay there's a time window that the coroutine can run its logic too inbetween.
                    Timer.Add(new TimerData(TimeSpan.FromSeconds(1), source, OnDelayedDisposeTimer));
                    
                    HostManager.GetHostVariant(source)
                            .TryToSendQueuedRequests();
                    break;
            }
        }

        private static bool OnDelayedDisposeTimer(DateTime time, object request)
        {
            var source = request as HTTPRequest;
            HTTPManager.Logger.Information("RequestEventHelper", $"{nameof(OnDelayedDisposeTimer)} - disposing response!", source.Context);
            source.Dispose();

            return false;
        }
    }
}

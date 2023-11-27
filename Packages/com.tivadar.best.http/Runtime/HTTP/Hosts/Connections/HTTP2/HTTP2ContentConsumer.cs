#if (!UNITY_WEBGL || UNITY_EDITOR) && !BESTHTTP_DISABLE_ALTERNATE_SSL && !BESTHTTP_DISABLE_HTTP2

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.HTTP.Shared.PlatformSupport.Threading;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Network.Tcp;
using Best.HTTP.Shared.Streams;

namespace Best.HTTP.Hosts.Connections.HTTP2
{
    public delegate HTTP2Stream CustomHTTP2StreamFactory(HTTPRequest request, uint streamId, HTTP2ContentConsumer parentHandler, HTTP2SettingsManager registry, HPACKEncoder hpackEncoder);

    public sealed class HTTP2ContentConsumer : IHTTPRequestHandler, IContentConsumer, IThreadSignaler
    {
        public KeepAliveHeader KeepAlive { get { return null; } }

        public bool CanProcessMultiple { get { return !this.SentGoAwayFrame && this.isRunning; } }

        // Connection preface starts with the string PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n).
        private static readonly byte[] MAGIC = new byte[24] { 0x50, 0x52, 0x49, 0x20, 0x2a, 0x20, 0x48, 0x54, 0x54, 0x50, 0x2f, 0x32, 0x2e, 0x30, 0x0d, 0x0a, 0x0d, 0x0a, 0x53, 0x4d, 0x0d, 0x0a, 0x0d, 0x0a };
        public const UInt32 MaxValueFor31Bits = 0xFFFFFFFF >> 1;

        public double Latency { get; private set; }

        public HTTP2SettingsManager settings;
        public HPACKEncoder HPACKEncoder;

        public LoggingContext Context { get; private set; }

        public PeekableContentProviderStream ContentProvider { get; private set; }

        private DateTime lastPingSent = DateTime.MinValue;
        private int waitingForPingAck = 0;

        public static int RTTBufferCapacity = 5;
        private CircularBuffer<double> rtts = new CircularBuffer<double>(RTTBufferCapacity);

        private volatile bool isRunning;

        private AutoResetEvent newFrameSignal = new AutoResetEvent(false);

        private ConcurrentQueue<HTTPRequest> requestQueue = new ConcurrentQueue<HTTPRequest>();

        private List<HTTP2Stream> clientInitiatedStreams = new List<HTTP2Stream>();

        private ConcurrentQueue<HTTP2FrameHeaderAndPayload> newFrames = new ConcurrentQueue<HTTP2FrameHeaderAndPayload>();

        private List<HTTP2FrameHeaderAndPayload> outgoingFrames = new List<HTTP2FrameHeaderAndPayload>();

        private UInt32 remoteWindow;
        private DateTime lastInteraction;
        private DateTime goAwaySentAt = DateTime.MaxValue;
        private bool SentGoAwayFrame { get => this.goAwaySentAt != DateTime.MaxValue; }

        private HTTPOverTCPConnection conn;

        private TimeSpan MaxGoAwayWaitTime { get { return !this.SentGoAwayFrame ? TimeSpan.MaxValue : TimeSpan.FromMilliseconds(Math.Max(this.Latency * 2.5, 1500)); } }

        // https://httpwg.org/specs/rfc7540.html#StreamIdentifiers
        // Streams initiated by a client MUST use odd-numbered stream identifiers
        // With an initial value of -1, the first client initiated stream's id going to be 1.
        private long LastStreamId = -1;

        private HTTP2ConnectionSettings _connectionSettings;

        public HTTP2ContentConsumer(HTTPOverTCPConnection conn)
        {
            this.Context = new LoggingContext(this);
            this.Context.Add("Parent", conn.Context);

            this.conn = conn;
            this.isRunning = true;

            this._connectionSettings = HTTPManager.PerHostSettings.Get(conn.HostKey).HTTP2ConnectionSettings;
            this.settings = new HTTP2SettingsManager(this.Context, this._connectionSettings);

            Process(this.conn.CurrentRequest);
        }

        public void Process(HTTPRequest request)
        {
            HTTPManager.Logger.Information(nameof(HTTP2ContentConsumer), "Process request called", this.Context);

            request.TimeoutSettings.QueuedAt = DateTime.MinValue;
            request.TimeoutSettings.ProcessingStarted = this.lastInteraction = DateTime.Now;

            this.requestQueue.Enqueue(request);
            SignalThread();
        }

        public void SignalThread()
        {
            this.newFrameSignal?.Set();
        }

        public void RunHandler()
        {
            HTTPManager.Logger.Information(nameof(HTTP2ContentConsumer), "Processing thread up and running!", this.Context);

            ThreadedRunner.SetThreadName("Best.HTTP2 Process");

            string abortWithMessage = string.Empty;

            try
            {
                bool atLeastOneStreamHasAFrameToSend = true;

                this.HPACKEncoder = new HPACKEncoder(this.Context, this.settings);

                // https://httpwg.org/specs/rfc7540.html#InitialWindowSize
                // The connection flow-control window is also 65,535 octets.
                this.remoteWindow = this.settings.RemoteSettings[HTTP2Settings.INITIAL_WINDOW_SIZE];

                // we want to pack as many data as we can in one tcp segment, but setting the buffer's size too high
                //  we might keep data too long and send them in bursts instead of in a steady stream.
                // Keeping it too low might result in a full tcp segment and one with very low payload
                // Is it possible that one full tcp segment sized buffer would be the best, or multiple of it.
                // It would keep the network busy without any fragments. The ethernet layer has a maximum of 1500 bytes,
                // but there's two layers of 20 byte headers each, so as a theoretical maximum it's 1500-20-20 bytes.
                // On the other hand, if the buffer is small (1-2), that means that for larger data, we have to do a lot
                // of system calls, in that case a larger buffer might be better. Still, if we are not cpu bound,
                // a well saturated network might serve us better.
                using (WriteOnlyBufferedStream bufferedStream = new WriteOnlyBufferedStream(this.conn.TopStream, 1024 * 1024 /*1500 - 20 - 20*/, this.Context))
                {
                    // The client connection preface starts with a sequence of 24 octets
                    bufferedStream.Write(MAGIC, 0, MAGIC.Length);

                    // This sequence MUST be followed by a SETTINGS frame (Section 6.5), which MAY be empty.
                    // The client sends the client connection preface immediately upon receipt of a
                    // 101 (Switching Protocols) response (indicating a successful upgrade)
                    // or as the first application data octets of a TLS connection

                    this.settings.InitiatedMySettings[HTTP2Settings.INITIAL_WINDOW_SIZE] = this._connectionSettings.InitialStreamWindowSize;
                    this.settings.InitiatedMySettings[HTTP2Settings.MAX_CONCURRENT_STREAMS] = this._connectionSettings.MaxConcurrentStreams;
                    this.settings.InitiatedMySettings[HTTP2Settings.ENABLE_CONNECT_PROTOCOL] = (uint)(this._connectionSettings.EnableConnectProtocol ? 1 : 0);
                    this.settings.InitiatedMySettings[HTTP2Settings.ENABLE_PUSH] = 0;
                    this.settings.SendChanges(this.outgoingFrames);
                    this.settings.RemoteSettings.OnSettingChangedEvent += OnRemoteSettingChanged;

                    // The default window size for the whole connection is 65535 bytes,
                    // but we want to set it to the maximum possible value.
                    Int64 initialConnectionWindowSize = this._connectionSettings.InitialConnectionWindowSize;

                    // yandex.ru returns with an FLOW_CONTROL_ERROR (3) error when the plugin tries to set the connection window to 2^31 - 1
                    // and works only with a maximum value of 2^31 - 10Mib (10 * 1024 * 1024).
                    if (initialConnectionWindowSize == HTTP2ContentConsumer.MaxValueFor31Bits)
                        initialConnectionWindowSize -= 10 * 1024 * 1024;

                    if (initialConnectionWindowSize > 65535)
                    {
                        Int64 initialConnectionWindowSizeDiff = initialConnectionWindowSize - 65535;
                        if (initialConnectionWindowSizeDiff > 0)
                            this.outgoingFrames.Add(HTTP2FrameHelper.CreateWindowUpdateFrame(0, (UInt32)initialConnectionWindowSizeDiff, this.Context));
                    }

                    initialConnectionWindowSize -= 65535;

                    // local, per-connection window
                    long localConnectionWindow = initialConnectionWindowSize;
                    UInt32 updateConnectionWindowAt = (UInt32)(localConnectionWindow / 2);

                    while (this.isRunning)
                    {
                        DateTime now = DateTime.Now;

                        if (!atLeastOneStreamHasAFrameToSend)
                        {
                            // buffered stream will call flush automatically if its internal buffer is full.
                            // But we have to make it sure that we flush remaining data before we go to sleep.
                            bufferedStream.Flush();

                            // Wait until we have to send the next ping, OR a new frame is received on the read thread.
                            //                lastPingSent             Now           lastPingSent+frequency       lastPingSent+Ping timeout
                            //----|---------------------|---------------|----------------------|----------------------|------------|
                            // lastInteraction                                                                                    lastInteraction + MaxIdleTime

                            var sendPingAt = this.lastPingSent + this._connectionSettings.PingFrequency;
                            var timeoutAt = this.waitingForPingAck != 0 ? this.lastPingSent + this._connectionSettings.Timeout : DateTime.MaxValue;

                            // sendPingAt can be in the past if Timeout is larger than PingFrequency
                            var nextPingInteraction = sendPingAt < timeoutAt && sendPingAt >= now ? sendPingAt : timeoutAt;

                            var disconnectByIdleAt = this.lastInteraction + this._connectionSettings.MaxIdleTime;

                            var nextDueClientInteractionAt = nextPingInteraction < disconnectByIdleAt ? nextPingInteraction : disconnectByIdleAt;
                            int wait = (int)(nextDueClientInteractionAt - now).TotalMilliseconds;

                            wait = (int)Math.Min(wait, this.MaxGoAwayWaitTime.TotalMilliseconds);

                            TimeSpan nextStreamInteraction = TimeSpan.MaxValue;
                            for (int i = 0; i < this.clientInitiatedStreams.Count; i++)
                            {
                                var streamInteraction = this.clientInitiatedStreams[i].NextInteraction;
                                if (streamInteraction < nextStreamInteraction)
                                    nextStreamInteraction = streamInteraction;
                            }

                            wait = (int)Math.Min(wait, nextStreamInteraction.TotalMilliseconds);
                            wait = (int)Math.Min(wait, 1000);

                            if (wait >= 1)
                            {
                                //if (HTTPManager.Logger.Level <= Logger.Loglevels.All)
                                //    HTTPManager.Logger.Information(nameof(HTTP2ContentConsumer), string.Format("Sleeping for {0:N0}ms", wait), this.Context);
                                this.newFrameSignal.WaitOne(wait);

                                now = DateTime.Now;
                            }
                        }

                        //  Don't send a new ping until a pong isn't received for the last one
                        if (now - this.lastPingSent >= this._connectionSettings.PingFrequency && Interlocked.CompareExchange(ref this.waitingForPingAck, 1, 0) == 0)
                        {
                            this.lastPingSent = now;

                            var frame = HTTP2FrameHelper.CreatePingFrame(HTTP2PingFlags.None, this.Context);
                            BufferHelper.SetLong(frame.Payload.Data, 0, now.Ticks);

                            HTTPManager.Logger.Information(nameof(HTTP2ContentConsumer), $"PING frame created with payload: {frame.Payload.Slice(0, 8)}", this.Context);

                            this.outgoingFrames.Add(frame);
                        }

                        // Process received frames
                        HTTP2FrameHeaderAndPayload header;
                        while (this.newFrames.TryDequeue(out header))
                        {
                            if (header.StreamId > 0)
                            {
                                switch (header.Type)
                                {
                                    case HTTP2FrameTypes.DATA:
                                        localConnectionWindow -= header.Payload.Count;
                                        break;
                                }

                                HTTP2Stream http2Stream = FindStreamById(header.StreamId);

                                // Add frame to the stream, so it can process it when its Process function is called
                                if (http2Stream != null)
                                {
                                    http2Stream.AddFrame(header, this.outgoingFrames);
                                }
                                else
                                {
                                    // Error? It's possible that we closed and removed the stream while the server was in the middle of sending frames
                                    if (HTTPManager.Logger.Level == Loglevels.All)
                                        HTTPManager.Logger.Warning(nameof(HTTP2ContentConsumer), $"Can't deliver frame: {header}, because no stream could be found for its Id!", this.Context);

                                    BufferPool.Release(header.Payload);
                                }
                            }
                            else
                            {
                                switch (header.Type)
                                {
                                    case HTTP2FrameTypes.SETTINGS:
                                        this.settings.Process(header, this.outgoingFrames);

                                        /*
                                        PluginEventHelper.EnqueuePluginEvent(
                                            new PluginEventInfo(PluginEvents.HTTP2ConnectProtocol,
                                                new HTTP2ConnectProtocolInfo(this.conn.HostKey,
                                                    this.settings.MySettings[HTTP2Settings.ENABLE_CONNECT_PROTOCOL] == 1 && this.settings.RemoteSettings[HTTP2Settings.ENABLE_CONNECT_PROTOCOL] == 1)));
                                        */
                                        break;

                                    case HTTP2FrameTypes.PING:
                                        var pingFrame = HTTP2FrameHelper.ReadPingFrame(header);

                                        if ((pingFrame.Flags & HTTP2PingFlags.ACK) != 0)
                                        {
                                            if (Interlocked.CompareExchange(ref this.waitingForPingAck, 0, 1) == 0)
                                                break; // waitingForPingAck was 0 == aren't expecting a ping ack!

                                            // it was an ack, payload must contain what we sent

                                            var ticks = BufferHelper.ReadLong(pingFrame.OpaqueData, 0);

                                            // the difference between the current time and the time when the ping message is sent
                                            TimeSpan diff = TimeSpan.FromTicks(now.Ticks - ticks);

                                            if (diff.TotalSeconds > 10 || diff.TotalSeconds < 0)
                                                HTTPManager.Logger.Warning(nameof(HTTP2ContentConsumer), $"Pong received with weird diff: {diff}! Payload: {pingFrame.OpaqueData}", this.Context);

                                            // add it to the buffer
                                            this.rtts.Add(diff.TotalMilliseconds);

                                            // and calculate the new latency
                                            this.Latency = CalculateLatency();

                                            HTTPManager.Logger.Verbose(nameof(HTTP2ContentConsumer), string.Format("Latency: {0:F2}ms, RTT buffer: {1}", this.Latency, this.rtts.ToString()), this.Context);
                                        }
                                        else if ((pingFrame.Flags & HTTP2PingFlags.ACK) == 0)
                                        {
                                            // https://httpwg.org/specs/rfc7540.html#PING
                                            // if it wasn't an ack for our ping, we have to send one

                                            var frame = HTTP2FrameHelper.CreatePingFrame(HTTP2PingFlags.ACK, this.Context);
                                            Array.Copy(pingFrame.OpaqueData.Data, 0, frame.Payload.Data, 0, pingFrame.OpaqueData.Count);

                                            this.outgoingFrames.Add(frame);
                                        }

                                        BufferPool.Release(pingFrame.OpaqueData);
                                        break;

                                    case HTTP2FrameTypes.WINDOW_UPDATE:
                                        var windowUpdateFrame = HTTP2FrameHelper.ReadWindowUpdateFrame(header);
                                        this.remoteWindow += windowUpdateFrame.WindowSizeIncrement;
                                        break;

                                    case HTTP2FrameTypes.GOAWAY:
                                        // parse the frame, so we can print out detailed information
                                        HTTP2GoAwayFrame goAwayFrame = HTTP2FrameHelper.ReadGoAwayFrame(header);

                                        HTTPManager.Logger.Information(nameof(HTTP2ContentConsumer), "Received GOAWAY frame: " + goAwayFrame.ToString(), this.Context);

                                        abortWithMessage = string.Format("Server closing the connection! Error code: {0} ({1}) Additonal Debug Data: {2}",
                                            goAwayFrame.Error, goAwayFrame.ErrorCode, goAwayFrame.AdditionalDebugData);

                                        for (int i = 0; i < this.clientInitiatedStreams.Count; ++i)
                                            this.clientInitiatedStreams[i].Abort(abortWithMessage);
                                        this.clientInitiatedStreams.Clear();

                                        // set the running flag to false, so the thread can exit
                                        this.isRunning = false;

                                        BufferPool.Release(goAwayFrame.AdditionalDebugData);

                                        //this.conn.State = HTTPConnectionStates.Closed;
                                        break;

                                    case HTTP2FrameTypes.ALT_SVC:
                                        //HTTP2AltSVCFrame altSvcFrame = HTTP2FrameHelper.ReadAltSvcFrame(header);

                                        // Implement
                                        //HTTPManager.EnqueuePluginEvent(new PluginEventInfo(PluginEvents.AltSvcHeader, new AltSvcEventInfo(altSvcFrame.Origin, ))
                                        break;
                                }

                                if (header.Payload != null)
                                    BufferPool.Release(header.Payload);
                            }
                        }

                        //  If no pong received in a (configurable) reasonable time, treat the connection broken
                        if (this.waitingForPingAck != 0 && now - this.lastPingSent >= this._connectionSettings.Timeout)
                            throw new TimeoutException("Ping ACK isn't received in time!");

                        UInt32 maxConcurrentStreams = Math.Min(this._connectionSettings.MaxConcurrentStreams, this.settings.RemoteSettings[HTTP2Settings.MAX_CONCURRENT_STREAMS]);

                        // pre-test stream count to lock only when truly needed.
                        if (this.clientInitiatedStreams.Count < maxConcurrentStreams && this.isRunning)
                        {
                            // grab requests from queue
                            HTTPRequest request;
                            while (this.clientInitiatedStreams.Count < maxConcurrentStreams && this.requestQueue.TryDequeue(out request))
                            {
                                HTTP2Stream newStream = null;

                                if (request.Tag is CustomHTTP2StreamFactory factory)
                                {
                                    newStream = factory(request, (UInt32)Interlocked.Add(ref LastStreamId, 2), this, this.settings, this.HPACKEncoder);
                                }
                                else
                                {
                                    newStream = new HTTP2Stream((UInt32)Interlocked.Add(ref LastStreamId, 2), this, this.settings, this.HPACKEncoder);
                                }

                                newStream.Assign(request);
                                this.clientInitiatedStreams.Add(newStream);
                            }
                        }

                        // send any settings changes
                        this.settings.SendChanges(this.outgoingFrames);

                        atLeastOneStreamHasAFrameToSend = false;

                        // process other streams
                        for (int i = 0; i < this.clientInitiatedStreams.Count; ++i)
                        {
                            var stream = this.clientInitiatedStreams[i];
                            stream.Process(this.outgoingFrames);

                            // remove closed, empty streams (not enough to check the closed flag, a closed stream still can contain frames to send)
                            if (stream.State == HTTP2StreamStates.Closed && !stream.HasFrameToSend)
                            {
                                this.clientInitiatedStreams.RemoveAt(i--);
                                stream.Removed();
                            }

                            atLeastOneStreamHasAFrameToSend |= stream.HasFrameToSend;

                            this.lastInteraction = now;
                        }

                        // If we encounter a data frame that too large for the current remote window, we have to stop
                        // sending all data frames as we could send smaller data frames before the large ones.
                        // Room for improvement: An improvement would be here to stop data frame sending per-stream.
                        bool haltDataSending = false;

                        if (this.ShutdownType == ShutdownTypes.Running && !this.SentGoAwayFrame && now - this.lastInteraction >= this._connectionSettings.MaxIdleTime)
                        {
                            this.lastInteraction = now;
                            HTTPManager.Logger.Information(nameof(HTTP2ContentConsumer), "Reached idle time, sending GoAway frame!", this.Context);
                            this.outgoingFrames.Add(HTTP2FrameHelper.CreateGoAwayFrame(0, HTTP2ErrorCodes.NO_ERROR, this.Context));
                            this.goAwaySentAt = now;
                        }

                        // https://httpwg.org/specs/rfc7540.html#GOAWAY
                        // Endpoints SHOULD always send a GOAWAY frame before closing a connection so that the remote peer can know whether a stream has been partially processed or not.
                        if (this.ShutdownType == ShutdownTypes.Gentle && !this.SentGoAwayFrame)
                        {
                            HTTPManager.Logger.Information(nameof(HTTP2ContentConsumer), "Connection abort requested, sending GoAway frame!", this.Context);

                            this.outgoingFrames.Clear();
                            this.outgoingFrames.Add(HTTP2FrameHelper.CreateGoAwayFrame(0, HTTP2ErrorCodes.NO_ERROR, this.Context));
                            this.goAwaySentAt = now;
                        }

                        if (this.isRunning && this.SentGoAwayFrame && now - goAwaySentAt >= this.MaxGoAwayWaitTime)
                        {
                            HTTPManager.Logger.Information(nameof(HTTP2ContentConsumer), "No GoAway frame received back. Really quitting now!", this.Context);
                            this.isRunning = false;
                            continue;
                        }

                        if (localConnectionWindow < updateConnectionWindowAt)
                        {
                            UInt32 diff = (UInt32)(initialConnectionWindowSize - localConnectionWindow);

                            if (HTTPManager.Logger.IsDiagnostic)
                                HTTPManager.Logger.Information(nameof(HTTP2ContentConsumer), $"Updating local connection window by {diff:N0} ({initialConnectionWindowSize:N0} - {localConnectionWindow:N0})", this.Context);

                            this.outgoingFrames.Add(HTTP2FrameHelper.CreateWindowUpdateFrame(0, diff, this.Context));
                            localConnectionWindow = initialConnectionWindowSize;
                        }

                        // Go through all the collected frames and send them.
                        for (int i = 0; i < this.outgoingFrames.Count; ++i)
                        {
                            var frame = this.outgoingFrames[i];

                            if (HTTPManager.Logger.IsDiagnostic && frame.Type != HTTP2FrameTypes.DATA /*&& frame.Type != HTTP2FrameTypes.PING*/)
                                HTTPManager.Logger.Information(nameof(HTTP2ContentConsumer), "Sending frame: " + frame.ToString(), this.Context);

                            // post process frames
                            switch (frame.Type)
                            {
                                case HTTP2FrameTypes.DATA:
                                    if (haltDataSending)
                                        continue;

                                    // if the tracked remoteWindow is smaller than the frame's payload, we stop sending
                                    // data frames until we receive window-update frames
                                    if (frame.Payload.Count > this.remoteWindow)
                                    {
                                        haltDataSending = true;
                                        HTTPManager.Logger.Warning(nameof(HTTP2ContentConsumer), string.Format("Data sending halted for this round. Remote Window: {0:N0}, frame: {1}", this.remoteWindow, frame.ToString()), this.Context);
                                        continue;
                                    }

                                    break;
                            }

                            this.outgoingFrames.RemoveAt(i--);

                            using (var buffer = HTTP2FrameHelper.HeaderAsBinary(frame))
                                bufferedStream.Write(buffer.Data, 0, buffer.Count);

                            if (frame.Payload.Count > 0)
                            {
                                bufferedStream.Write(frame.Payload.Data, frame.Payload.Offset, frame.Payload.Count);

                                if (!frame.DontUseMemPool)
                                    BufferPool.Release(frame.Payload);
                            }

                            if (frame.Type == HTTP2FrameTypes.DATA)
                                this.remoteWindow -= (uint)frame.Payload.Count;
                        }

                        bufferedStream.Flush();
                    } // while (this.isRunning)

                    bufferedStream.Flush();
                }
            }
            catch (Exception ex)
            {
                abortWithMessage = ex.ToString();
                // Log out the exception if it's a non-expected one.
                if (this.ShutdownType == ShutdownTypes.Running && this.isRunning && !this.SentGoAwayFrame && !HTTPManager.IsQuitting)
                    HTTPManager.Logger.Exception(nameof(HTTP2ContentConsumer), "Sender thread", ex, this.Context);
            }
            finally
            {
                this.isRunning = false;

                HTTPManager.Logger.Information(nameof(HTTP2ContentConsumer), $"Sender thread closing - cleaning up remaining requests({this.clientInitiatedStreams.Count})...", this.Context);

                if (string.IsNullOrEmpty(abortWithMessage))
                    abortWithMessage = "Connection closed unexpectedly";

                for (int i = 0; i < this.clientInitiatedStreams.Count; ++i)
                    this.clientInitiatedStreams[i].Abort(abortWithMessage);
                this.clientInitiatedStreams.Clear();

                HTTPManager.Logger.Information(nameof(HTTP2ContentConsumer), "Sender thread closing", this.Context);

                ConnectionEventHelper.EnqueueConnectionEvent(new ConnectionEventInfo(this.conn, HTTPConnectionStates.Closed));
            }
        }

        private void OnRemoteSettingChanged(HTTP2SettingsRegistry registry, HTTP2Settings setting, uint oldValue, uint newValue)
        {
            switch (setting)
            {
                case HTTP2Settings.INITIAL_WINDOW_SIZE:
                    this.remoteWindow = newValue - (oldValue - this.remoteWindow);
                    break;
            }
        }

        public void SetBinding(PeekableContentProviderStream contentProvider) => this.ContentProvider = contentProvider;
        public void UnsetBinding() => this.ContentProvider = null;

        public void OnContent()
        {
            try
            {
                while (this.isRunning && HTTP2FrameHelper.CanReadFullFrame(this.ContentProvider))
                {
                    HTTP2FrameHeaderAndPayload header = HTTP2FrameHelper.ReadHeader(this.ContentProvider, this.Context);

                    if (HTTPManager.Logger.IsDiagnostic /*&& header.Type != HTTP2FrameTypes.DATA /*&& header.Type != HTTP2FrameTypes.PING*/)
                        HTTPManager.Logger.Information(nameof(HTTP2ContentConsumer), "New frame received: " + header.ToString(), this.Context);

                    // Add the new frame to the queue. Processing it on the write thread gives us the advantage that
                    //  we don't have to deal with too much locking.
                    this.newFrames.Enqueue(header);
                }
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception(nameof(HTTP2ContentConsumer), "", ex, this.Context);
            }
            finally
            {
                // ping write thread to process the new frame
                this.newFrameSignal?.Set();
            }
        }

        public void OnConnectionClosed()
        {
            HTTPManager.Logger.Verbose(nameof(HTTP2ContentConsumer), $"{nameof(OnConnectionClosed)}({this.isRunning})", this.Context);
            this.isRunning = false;
            this.newFrameSignal?.Set();
        }

        public void OnError(Exception ex)
        {
            HTTPManager.Logger.Exception(nameof(HTTP2ContentConsumer), $"{nameof(OnError)}({this.isRunning}, {ex})", ex, this.Context);
            this.isRunning = false;
            this.newFrameSignal?.Set();
        }

        private double CalculateLatency()
        {
            if (this.rtts.Count == 0)
                return 0;

            double sumLatency = 0;
            for (int i = 0; i < this.rtts.Count; ++i)
                sumLatency += this.rtts[i];

            return sumLatency / this.rtts.Count;
        }

        HTTP2Stream FindStreamById(UInt32 streamId)
        {
            for (int i = 0; i < this.clientInitiatedStreams.Count; ++i)
            {
                var stream = this.clientInitiatedStreams[i];
                if (stream.Id == streamId)
                    return stream;
            }

            return null;
        }

        public ShutdownTypes ShutdownType { get; private set; }

        public void Shutdown(ShutdownTypes type)
        {
            this.ShutdownType = type;

            switch (this.ShutdownType)
            {
                case ShutdownTypes.Gentle:
                    this.newFrameSignal.Set();
                    break;

                case ShutdownTypes.Immediate:
                    this.conn?.TopStream?.Dispose();
                    break;
            }
        }

        public void Dispose()
        {
            HTTPManager.Logger.Information(nameof(HTTP2ContentConsumer), "Dispose", this.Context);

            while (this.newFrames.TryDequeue(out var frame))
                BufferPool.Release(frame.Payload);

            foreach (var frame in this.outgoingFrames)
                BufferPool.Release(frame.Payload);
            this.outgoingFrames.Clear();

            HTTPRequest request = null;
            while (this.requestQueue.TryDequeue(out request))
            {
                HTTPManager.Logger.Information(nameof(HTTP2ContentConsumer), string.Format("Dispose - Request '{0}' IsCancellationRequested: {1}", request.CurrentUri.ToString(), request.IsCancellationRequested.ToString()), this.Context);
                RequestEventHelper.EnqueueRequestEvent(request.IsCancellationRequested ? new RequestEventInfo(request, HTTPRequestStates.Aborted, null) : new RequestEventInfo(request, RequestEvents.Resend));
            }

            this.newFrameSignal?.Close();
            this.newFrameSignal = null;
        }
    }
}

#endif

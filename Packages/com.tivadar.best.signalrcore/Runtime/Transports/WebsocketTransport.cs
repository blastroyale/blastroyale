using System;

using Best.HTTP.Hosts.Connections;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.SignalR.Messages;
using Best.WebSockets;

namespace Best.SignalR.Transports
{
    /// <summary>
    /// WebSockets transport implementation.
    /// https://github.com/dotnet/aspnetcore/blob/master/src/SignalR/docs/specs/TransportProtocols.md#websockets-full-duplex
    /// </summary>
    internal sealed class WebSocketTransport : TransportBase
    {
        public override TransportTypes TransportType { get { return TransportTypes.WebSocket; } }

        private WebSocket webSocket;

        internal WebSocketTransport(HubConnection con)
            :base(con)
        {
        }

        public override void StartConnect()
        {
            HTTPManager.Logger.Verbose("WebSocketTransport", "StartConnect", this.Context);

            if (this.webSocket == null)
            {
                Uri uri = this.connection.Uri;
                string scheme = HTTPProtocolFactory.IsSecureProtocol(uri) ? "wss" : "ws";
                int port = uri.Port != -1 ? uri.Port : (scheme.Equals("wss", StringComparison.OrdinalIgnoreCase) ? 443 : 80);

                // Somehow if i use the UriBuilder it's not the same as if the uri is constructed from a string...
                uri = new Uri(scheme + "://" + uri.Host + ":" + port + uri.GetRequestPathAndQueryURL());

                uri = BuildUri(uri);

                // Also, if there's an authentication provider it can alter further our uri.
                if (this.connection.AuthenticationProvider != null)
                    uri = this.connection.AuthenticationProvider.PrepareUri(uri) ?? uri;

                HTTPManager.Logger.Verbose("WebSocketTransport", "StartConnect connecting to Uri: " + uri.ToString(), this.Context);

                this.webSocket = new WebSocket(uri, string.Empty, string.Empty
#if !UNITY_WEBGL || UNITY_EDITOR
                    , (this.connection.Options.WebsocketOptions?.ExtensionsFactory ?? WebSocket.GetDefaultExtensions)?.Invoke()
#endif
                    );

                this.webSocket.Context.Add("Transport", this.Context);
            }

#if !UNITY_WEBGL || UNITY_EDITOR
            if (this.connection.Options.WebsocketOptions?.PingIntervalOverride is TimeSpan ping)
            {
                if (ping > TimeSpan.Zero)
                {
                    this.webSocket.SendPings = true;
                    this.webSocket.PingFrequency = ping;
                }
                else
                    this.webSocket.SendPings = false;
            }
            else
                this.webSocket.SendPings = true;

            if (this.connection.Options.WebsocketOptions?.CloseAfterNoMessageOverride is TimeSpan closeAfterNoMessageOverride)
                this.webSocket.CloseAfterNoMessage = closeAfterNoMessageOverride;

            // prepare the internal http request
            if (this.connection.AuthenticationProvider != null)
                webSocket.OnInternalRequestCreated = (ws, internalRequest) => this.connection.AuthenticationProvider.PrepareRequest(internalRequest);
#endif
            this.webSocket.OnOpen += OnOpen;
            this.webSocket.OnMessage += OnMessage;
            this.webSocket.OnBinary += OnBinaryNoAlloc;
            this.webSocket.OnClosed += OnClosed;

            this.webSocket.Open();
            
            this.State = TransportStates.Connecting;
        }

        public override void Send(BufferSegment msg)
        {
            if (this.webSocket == null || !this.webSocket.IsOpen)
            {
                BufferPool.Release(msg.Data);

                //this.OnError(this.webSocket, "Send called while the websocket is null or isn't open! Transport's State: " + this.State);
                return;
            }

            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Verbose("WebSocketTransport", "Send: " + msg.ToString(), this.Context);
            this.webSocket.SendAsBinary(msg);
        }

        // The websocket connection is open
        private void OnOpen(WebSocket webSocket)
        {
            HTTPManager.Logger.Verbose("WebSocketTransport", "OnOpen", this.Context);

            // https://github.com/dotnet/aspnetcore/blob/master/src/SignalR/docs/specs/HubProtocol.md#overview
            // When our websocket connection is open, send the 'negotiation' message to the server.
            (this as ITransport).Send(JsonProtocol.WithSeparator(string.Format("{{\"protocol\":\"{0}\", \"version\": 1}}", this.connection.Protocol.Name)));
        }

        private void OnMessage(WebSocket webSocket, string data)
        {
            if (this.State == TransportStates.Closing)
                return;

            this.messages.Clear();
            try
            {
                int len = System.Text.Encoding.UTF8.GetByteCount(data);

                byte[] buffer = BufferPool.Get(len, true);
                try
                {
                    // Clear the buffer, it might have previous messages in it with the record separator somewhere it doesn't gets overwritten by the new data
                    Array.Clear(buffer, 0, buffer.Length);
                    System.Text.Encoding.UTF8.GetBytes(data, 0, data.Length, buffer, 0);

                    this.connection.Protocol.ParseMessages(new BufferSegment(buffer, 0, len), ref this.messages);

                    if (this.State == TransportStates.Connecting)
                    {
                        // we expect a handshake response in this case

                        if (this.messages.Count == 0)
                        {
                            this.ErrorReason = $"Expecting handshake response, but message({data}) couldn't be parsed!";
                            this.State = TransportStates.Failed;
                            return;
                        }

                        var message = this.messages[0];
                        if (message.type != MessageTypes.Handshake)
                        {
                            this.ErrorReason = $"Expecting handshake response, but the first message is {message.type}!";
                            this.State = TransportStates.Failed;
                            return;
                        }

                        this.ErrorReason = message.error;
                        this.State = string.IsNullOrEmpty(message.error) ? TransportStates.Connected : TransportStates.Failed;
                    }
                }
                finally
                {
                    BufferPool.Release(buffer);
                }                

                this.connection.OnMessages(this.messages);
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception("WebSocketTransport", "OnMessage(string)", ex, this.Context);
            }
            finally
            {
                this.messages.Clear();
            }
        }

        private void OnBinaryNoAlloc(WebSocket webSocket, BufferSegment data)
        {
            if (this.State == TransportStates.Closing)
                return;

            if (this.State == TransportStates.Connecting)
            {
                int recordSeparatorIdx = Array.FindIndex(data.Data, data.Offset, data.Count, (b) => b == JsonProtocol.Separator);

                if (recordSeparatorIdx == -1)
                {
                    this.ErrorReason = $"Expecting handshake response, but message({data}) has no record separator(0x1E)!";
                    this.State = TransportStates.Failed;
                    return;
                }
                else
                {
                    HandleHandshakeResponse(System.Text.Encoding.UTF8.GetString(data.Data, data.Offset, recordSeparatorIdx - data.Offset));

                    // Skip any other messages sent if handshake is failed
                    if (this.State != TransportStates.Connected)
                        return;

                    recordSeparatorIdx++;
                    if (recordSeparatorIdx == data.Offset + data.Count)
                        return;

                    data = new BufferSegment(data.Data, data.Offset + recordSeparatorIdx, data.Count - recordSeparatorIdx);
                }
            }

            this.messages.Clear();
            try
            {
                this.connection.Protocol.ParseMessages(data, ref this.messages);

                if (this.State == TransportStates.Connecting)
                {
                    // we expect a handshake response in this case

                    if (this.messages.Count == 0)
                    {
                        this.ErrorReason = $"Expecting handshake response, but message({data}) couldn't be parsed!";
                        this.State = TransportStates.Failed;
                        return;
                    }

                    var message = this.messages[0];
                    if (message.type != MessageTypes.Handshake)
                    {
                        this.ErrorReason = $"Expecting handshake response, but the first message is {message.type}!";
                        this.State = TransportStates.Failed;
                        return;
                    }

                    this.ErrorReason = message.error;
                    this.State = string.IsNullOrEmpty(message.error) ? TransportStates.Connected : TransportStates.Failed;
                }

                this.connection.OnMessages(this.messages);
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception("WebSocketTransport", "OnMessage(byte[])", ex, this.Context);
            }
            finally
            {
                this.messages.Clear();
            }
        }

        private void OnClosed(WebSocket webSocket, WebSocketStatusCodes code, string message)
        {
            HTTPManager.Logger.Verbose("WebSocketTransport", $"OnClosed({code}, {message})", this.Context);

            if (code == WebSocketStatusCodes.NormalClosure)
            {
                this.webSocket = null;

                this.State = TransportStates.Closed;
            }
            else
            {
                if (this.State == TransportStates.Closing)
                {
                    this.State = TransportStates.Closed;
                }
                else
                {
                    this.ErrorReason = message;
                    this.State = TransportStates.Failed;
                }
            }
        }

        public override void StartClose()
        {
            HTTPManager.Logger.Verbose("WebSocketTransport", "StartClose", this.Context);

            if (this.webSocket != null && this.webSocket.IsOpen)
            {
                this.State = TransportStates.Closing;
                this.webSocket.Close();
            }
            else
                this.State = TransportStates.Closed;
        }
    }
}

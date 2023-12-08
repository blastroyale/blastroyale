using System;
using System.Collections.Generic;

using Best.HTTP.Shared.PlatformSupport.Memory;

namespace Best.SignalR
{
    /// <summary>
    /// Specifies the various transport mechanisms that can be used in a <see cref="HubConnection"/> context.
    /// </summary>
    public enum TransportTypes
    {
#if !BESTHTTP_DISABLE_WEBSOCKET
        /// <summary>
        /// Represents the WebSocket transport mechanism.
        /// </summary>
        WebSocket,
#endif

        /// <summary>
        /// Represents the LongPolling transport mechanism.
        /// </summary>
        LongPolling
    }

    /// <summary>
    /// Encoding modes a transport is capable to communicate with.
    /// </summary>
    public enum TransferModes
    {
        /// <summary>
        /// The protocol is able to send and/or receive byte arrays. Usually the most performant mode.
        /// </summary>
        Binary,

        /// <summary>
        /// The protocol can send and/or receive textual representation of the messages.
        /// </summary>
        Text
    }

    /*
```mermaid
stateDiagram
    Initial --> Connecting
    Connecting --> Connected
    Connecting --> Failed
    Connected --> Closing
    Closing --> Closed
    Closed --> Connecting
    Connected --> Failed
    Failed --> Connecting
```
     * */
    /// <summary>
    /// Represents the possible states of a <see cref="HubConnection"/>'s transport (<see cref="Best.SignalR.Transports.WebSocketTransport">websocket</see> or <see cref="Best.SignalR.Transports.LongPollingTransport">long-polling</see>).
    /// </summary>
    public enum TransportStates
    {
        /// <summary>
        /// The initial state of the transport, before any connection attempts have been made.
        /// </summary>
        Initial,

        /// <summary>
        /// The state when the transport is in the process of establishing a connection.
        /// </summary>
        Connecting,

        /// <summary>
        /// The state when the transport has successfully established a connection.
        /// </summary>
        Connected,

        /// <summary>
        /// The state when the transport is in the process of closing the connection.
        /// </summary>
        Closing,

        /// <summary>
        /// The state when an attempt to establish or maintain a connection has failed.
        /// </summary>
        Failed,

        /// <summary>
        /// The state when the transport has successfully closed the connection.
        /// </summary>
        Closed
    }

    /// <summary>
    /// Possible states of a HubConnection
    /// </summary>
    public enum ConnectionStates
    {
        Initial,
        Authenticating,
        Negotiating,
        Redirected,
        Reconnecting,
        Connected,
        CloseInitiated,
        Closed
    }

    /// <summary>
    /// States that a transport can goes trough as seen from 'outside'.
    /// </summary>
    public enum TransportEvents
    {
        /// <summary>
        /// Transport is selected to try to connect to the server
        /// </summary>
        SelectedToConnect,

        /// <summary>
        /// Transport failed to connect to the server. This event can occur after SelectedToConnect, when already connected and an error occurs it will be a ClosedWithError one.
        /// </summary>
        FailedToConnect,

        /// <summary>
        /// The transport successfully connected to the server.
        /// </summary>
        Connected,

        /// <summary>
        /// Transport gracefully terminated.
        /// </summary>
        Closed,

        /// <summary>
        /// Unexpected error occured and the transport can't recover from it.
        /// </summary>
        ClosedWithError
    }

    /// <summary>
    /// Defines the basic structure and operations for a transport mechanism in a <see cref="HubConnection"/> context.
    /// Current implemtations are <see cref="Transports.WebSocketTransport"/> and <see cref="Transports.LongPollingTransport"/>.
    /// </summary>
    public interface ITransport
    {
        /// <summary>
        /// Gets the transfer mode used by the transport, which defines whether it's <see cref="TransferModes.Binary">binary</see> or <see cref="TransferModes.Text">text</see>.
        /// </summary>
        TransferModes TransferMode { get; }

        /// <summary>
        /// Gets the type of the transport, such as <see cref="TransportTypes.WebSocket">websocket</see> or <see cref="TransportTypes.LongPolling">long-polling</see>.
        /// </summary>
        TransportTypes TransportType { get; }

        /// <summary>
        /// Gets the current <see cref="TransportStates">state</see> of the transport, which could be connecting, connected, closing, etc.
        /// </summary>
        TransportStates State { get; }

        /// <summary>
        /// Gets a string representation of the reason for any errors that might have occurred during the transport's operations.
        /// </summary>
        string ErrorReason { get; }

        /// <summary>
        /// An event that's triggered whenever the state of the transport changes.
        /// It provides the previous state and the new state as its parameters.
        /// </summary>
        event Action<TransportStates, TransportStates> OnStateChanged;

        /// <summary>
        /// Initiates the connection process for the transport.
        /// </summary>
        void StartConnect();

        /// <summary>
        /// Initiates the process to close the transport's connection.
        /// </summary>
        void StartClose();

        /// <summary>
        /// Sends data over the transport using the provided <see cref="BufferSegment">buffer segment</see>.
        /// </summary>
        /// <param name="bufferSegment">The segment of the buffer that contains the data to be sent.</param>
        void Send(BufferSegment bufferSegment);
    }

    /// <summary>
    /// Common interface for communication protocol encoders.
    /// </summary>
    public interface IEncoder
    {
        /// <summary>
        /// Function to encode the received value as a byte representation, returned as a <see cref="BufferSegment"/>.
        /// </summary>
        /// <typeparam name="T">Type of the value parameter.</typeparam>
        /// <param name="value">The value that must be encoded.</param>
        /// <returns>A byte representation, returned as a <see cref="BufferSegment"/>.</returns>
        BufferSegment Encode<T>(T value);

        /// <summary>
        /// Function to create a strongly typed object from the received <see cref="BufferSegment"/>.
        /// </summary>
        /// <typeparam name="T">The concrete type the function must decode the buffer to.</typeparam>
        /// <param name="buffer">Contains the received message as a binary.</param>
        /// <returns>An object with the type T.</returns>
        T DecodeAs<T>(BufferSegment buffer);

        /// <summary>
        /// Function to convert the received object to another type, or make sure its already in that one.
        /// </summary>
        /// <param name="toType">The type to convert to.</param>
        /// <param name="obj">The object that will be converted.</param>
        /// <returns>The object with type toType.</returns>
        object ConvertTo(Type toType, object obj);
    }

    public sealed class StreamItemContainer<T>
    {
        public readonly long id;

        public List<T> Items { get; private set; }
        public T LastAdded { get; private set; }

        public bool IsCanceled;

        public StreamItemContainer(long _id)
        {
            this.id = _id;
            this.Items = new List<T>();
        }

        public void AddItem(T item)
        {
            if (this.Items == null)
                this.Items = new List<T>();

            this.Items.Add(item);
            this.LastAdded = item;
        }
    }

    public struct CallbackDescriptor
    {
        public readonly Type[] ParamTypes;
        public readonly Action<object[]> Callback;
        public CallbackDescriptor(Type[] paramTypes, Action<object[]> callback)
        {
            this.ParamTypes = paramTypes;
            this.Callback = callback;
        }
    }

    public struct FunctionCallbackDescriptor
    {
        public readonly Type ReturnType;
        public readonly Type[] ParamTypes;
        public readonly Func<object[], object> Callback;

        public FunctionCallbackDescriptor(Type returnType, Type[] paramTypes, Func<object[], object> callback)
        {
            this.ReturnType = returnType;
            this.ParamTypes = paramTypes;
            this.Callback = callback;
        }
    }

    internal struct InvocationDefinition
    {
        public Action<Messages.Message> callback;
        public Type returnType;
    }

    public sealed class Subscription
    {
        public List<CallbackDescriptor> callbacks = new List<CallbackDescriptor>(1);
        public List<FunctionCallbackDescriptor> functionCallbacks;

        public void Add(Type[] paramTypes, Action<object[]> callback)
        {
            this.callbacks.Add(new CallbackDescriptor(paramTypes, callback));
        }

        public void AddFunc(Type resultType, Type[] paramTypes, Func<object[], object> callback)
        {
            if (this.functionCallbacks == null)
                this.functionCallbacks = new List<FunctionCallbackDescriptor>(1);

            this.functionCallbacks.Add(new FunctionCallbackDescriptor(resultType, paramTypes, callback));
        }
    }

    /// <summary>
    /// Represents configuration options specific to the WebSocket transport.
    /// </summary>
    public sealed class WebsocketOptions
    {
#if !BESTHTTP_DISABLE_WEBSOCKET && (!UNITY_WEBGL || UNITY_EDITOR)
        /// <summary>
        /// Gets or sets the factory method to create WebSocket extensions.
        /// Defaults to <see cref="WebSockets.WebSocket.GetDefaultExtensions"/>.
        /// </summary>
        public Func<WebSockets.Extensions.IExtension[]> ExtensionsFactory { get; set; } = WebSockets.WebSocket.GetDefaultExtensions;

        /// <summary>
        /// Gets or sets the interval for sending ping messages to keep the <see cref="WebSockets.WebSocket"/> connection alive.
        /// If set to <see cref="TimeSpan.Zero"/>, it means there's no specific interval set and the default or global settings should be used.
        /// </summary>
        public TimeSpan? PingIntervalOverride { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets the maximum time allowed after not receiving any message on the WebSocket connection before considering closing it.
        /// If set to <c>null</c>, it means there's no specific limit set and the connection won't be closed due to inactivity.
        /// </summary>
        public TimeSpan? CloseAfterNoMessageOverride { get; set; } = null;
#endif
    }

    /// <summary>
    /// Represents the configuration options for a <see cref="HubConnection"/>.
    /// </summary>
    public sealed class HubOptions
    {
        /// <summary>
        /// When this is set to true, the plugin will skip the negotiation request if the PreferedTransport is WebSocket. Its default value is false.
        /// </summary>
        public bool SkipNegotiation { get; set; }

        /// <summary>
        /// The preferred transport to choose when more than one available. Its default value is TransportTypes.WebSocket.
        /// </summary>
        public TransportTypes PreferedTransport { get; set; }

        /// <summary>
        /// A ping message is only sent if the interval has elapsed without a message being sent. Its default value is 15 seconds.
        /// </summary>
        public TimeSpan PingInterval { get; set; }

        /// <summary>
        /// If the client doesn't see any message in this interval, considers the connection broken. Its default value is 30 seconds.
        /// </summary>
        public TimeSpan PingTimeoutInterval { get; set; }

        /// <summary>
        /// The maximum count of redirect negotiation result that the plugin will follow. Its default value is 100.
        /// </summary>
        public int MaxRedirects { get; set; }

        /// <summary>
        /// The maximum time that the plugin allowed to spend trying to connect. Its default value is 1 minute.
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; }

        /// <summary>
        /// Customization options for the websocket transport.
        /// </summary>
        public WebsocketOptions WebsocketOptions { get; set; } = new WebsocketOptions();

        /// <summary>
        /// Initializes a new instance of the HubOptions class with default values.
        /// </summary>
        public HubOptions()
        {
            this.SkipNegotiation = false;
#if !BESTHTTP_DISABLE_WEBSOCKET
            this.PreferedTransport = TransportTypes.WebSocket;
#else
            this.PreferedTransport = TransportTypes.LongPolling;
#endif
            this.PingInterval = TimeSpan.FromSeconds(15);
            this.PingTimeoutInterval = TimeSpan.FromSeconds(30);
            this.MaxRedirects = 100;
            this.ConnectTimeout = TimeSpan.FromSeconds(60);
        }
    }

    /// <summary>
    /// Defines a contract for implementing retry policies in case of connection failures.
    /// </summary>
    public interface IRetryPolicy
    {
        /// <summary>
        /// Determines the delay duration before the next connection attempt based on the given <see cref="RetryContext"/>.
        /// </summary>
        /// <param name="context">The context information related to the retry attempt.</param>
        /// <returns>The delay duration for the next retry attempt, or <c>null</c> if no more retries should be made.</returns>
        TimeSpan? GetNextRetryDelay(RetryContext context);
    }

    /// <summary>
    /// Represents context information related to a retry attempt.
    /// </summary>
    public struct RetryContext
    {
        /// <summary>
        /// Previous reconnect attempts. A successful connection sets it back to zero.
        /// </summary>
        public uint PreviousRetryCount;

        /// <summary>
        /// Elapsed time since the original connection error.
        /// </summary>
        public TimeSpan ElapsedTime;

        /// <summary>
        /// String representation of the connection error.
        /// </summary>
        public string RetryReason;
    }

    /// <summary>
    /// Provides a default retry policy with predefined backoff times (0, 2, 10, 30 seconds).
    /// </summary>
    public sealed class DefaultRetryPolicy : IRetryPolicy
    {
        private static TimeSpan?[] DefaultBackoffTimes = new TimeSpan?[]
        {
            TimeSpan.Zero,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(30),
            null
        };

        TimeSpan?[] backoffTimes;

        /// <summary>
        /// Initializes a new instance of the DefaultRetryPolicy class with default backoff times.
        /// </summary>
        public DefaultRetryPolicy() => this.backoffTimes = DefaultBackoffTimes;

        /// <summary>
        /// Initializes a new instance of the DefaultRetryPolicy class with custom backoff times.
        /// </summary>
        /// <param name="customBackoffTimes">An array of custom backoff times.</param>
        public DefaultRetryPolicy(TimeSpan?[] customBackoffTimes) => this.backoffTimes = customBackoffTimes;

        /// <summary>
        /// Determines the delay duration before the next connection attempt based on the given <see cref="RetryContext"/>.
        /// </summary>
        /// <param name="context">The context information related to the retry attempt.</param>
        /// <returns>The delay duration for the next retry attempt, or <c>null</c> if no more retries should be made.</returns>
        public TimeSpan? GetNextRetryDelay(RetryContext context)
        {
            if (this.backoffTimes == null || context.PreviousRetryCount >= this.backoffTimes.Length)
                return null;

            return this.backoffTimes[context.PreviousRetryCount];
        }
    }
}

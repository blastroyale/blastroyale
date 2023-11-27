using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#if WITH_UNITASK
using Cysharp.Threading.Tasks;
#endif

using Best.HTTP;
using Best.HTTP.Futures;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.Threading;
using Best.SignalR.Authentication;
using Best.SignalR.Messages;

namespace Best.SignalR
{
    /// <summary>
    /// Represents the main entry point for a SignalR Core connection.
    /// </summary>
    public sealed class HubConnection : Best.HTTP.Shared.Extensions.IHeartbeat
    {
        internal static readonly object[] EmptyArgs = new object[0];

        /// <summary>
        /// Gets the URI of the Hub endpoint.
        /// </summary>
        public Uri Uri { get; private set; }

        /// <summary>
        /// Gets the current state of this connection.
        /// </summary>
        public ConnectionStates State
        {
            get { return (ConnectionStates)this._state; }
            private set
            {
                Interlocked.Exchange(ref this._state, (int)value);
            }
        }
        private volatile int _state;

        /// <summary>
        /// Gets the current active <see cref="ITransport"/> instance.
        /// </summary>
        public ITransport Transport { get; private set; }

        /// <summary>
        /// Gets the <see cref="IProtocol"/> implementation that will parse, encode, and decode messages.
        /// </summary>
        public IProtocol Protocol { get; private set; }

        /// <summary>
        /// Called when the connection is redirected to a new URI.
        /// </summary>
        public event Action<HubConnection, Uri, Uri> OnRedirected;

        /// <summary>
        /// Called when successfully connected to the hub.
        /// </summary>
        public event Action<HubConnection> OnConnected;

        /// <summary>
        /// Called when an unexpected error happens and the connection is closed.
        /// </summary>
        public event Action<HubConnection, string> OnError;

        /// <summary>
        /// Called when the connection is gracefully terminated.
        /// </summary>
        public event Action<HubConnection> OnClosed;

        /// <summary>
        /// Called for every server-sent message. The return value determines further processing of the message.
        /// </summary>
        /// <returns><c>true</c> if the message should be processed; otherwise, <c>false</c>.</returns>
        public event Func<HubConnection, Message, bool> OnMessage;

        /// <summary>
        /// Called when the HubConnection starts its reconnection process after losing its underlying connection.
        /// </summary>
        public event Action<HubConnection, string> OnReconnecting;

        /// <summary>
        /// Called after a successful reconnection.
        /// </summary>
        public event Action<HubConnection> OnReconnected;

        /// <summary>
        /// Called for transport-related events.
        /// </summary>
        public event Action<HubConnection, ITransport, TransportEvents> OnTransportEvent;

        /// <summary>
        /// Gets or sets the <see cref="IAuthenticationProvider"/> implementation that will be used to authenticate the connection.
        /// </summary>
        /// <value>Its default value is an instance of <see cref="DefaultAccessTokenAuthenticator"/>.</value>
        public IAuthenticationProvider AuthenticationProvider { get; set; }

        /// <summary>
        /// Gets the negotiation response sent by the server.
        /// </summary>
        public NegotiationResult NegotiationResult { get; private set; }

        /// <summary>
        /// Gets the <see cref="HubOptions"/> instance that were used to create the HubConnection.
        /// </summary>
        public HubOptions Options { get; private set; }

        /// <summary>
        /// Gets how many times this connection has been redirected.
        /// </summary>
        public int RedirectCount { get; private set; }

        /// <summary>
        /// Gets or sets the reconnect policy that will be used when the underlying connection is lost. Defaults to null.
        /// </summary>
        /// <value>Its default value is an instance of <see cref="DefaultRetryPolicy"/>.</value>
        public IRetryPolicy ReconnectPolicy { get; set; }

        /// <summary>
        /// Logging context of this HubConnection instance.
        /// </summary>
        public LoggingContext Context { get; private set; }

        /// <summary>
        /// This will be increment to add a unique id to every message the plugin will send.
        /// </summary>
        private long lastInvocationId = 1;

        /// <summary>
        /// Id of the last streaming parameter.
        /// </summary>
        private int lastStreamId = 1;

        /// <summary>
        ///  Store the callback for all sent message that expect a return value from the server. All sent message has
        ///  a unique invocationId that will be sent back from the server.
        /// </summary>
        private ConcurrentDictionary<long, InvocationDefinition> invocations = new ConcurrentDictionary<long, InvocationDefinition>();

        /// <summary>
        /// This is where we store the methodname => callback mapping.
        /// </summary>
        private ConcurrentDictionary<string, Subscription> subscriptions = new ConcurrentDictionary<string, Subscription>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// When we sent out the last message to the server.
        /// </summary>
        private DateTime lastMessageSentAt;
        private DateTime lastMessageReceivedAt;

        private DateTime connectionStartedAt;

        private RetryContext currentContext;
        private DateTime reconnectStartTime = DateTime.MinValue;
        private DateTime reconnectAt;

        private List<TransportTypes> triedoutTransports = new List<TransportTypes>();

        private ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private bool pausedInLastFrame;
#if WITH_UNITASK
        private UniTaskCompletionSource<HubConnection> connectAsyncTaskCompletionSource;
        private UniTaskCompletionSource<HubConnection> closeAsyncTaskCompletionSource;
#else
        private TaskCompletionSource<HubConnection> connectAsyncTaskCompletionSource;
        private TaskCompletionSource<HubConnection> closeAsyncTaskCompletionSource;
#endif

        private List<Message> delayedMessages;
        private bool defaultReconnect = true;

        /// <summary>
        /// Initializes a new instance of the HubConnection class.
        /// </summary>
        /// <param name="hubUri">The Uri of the Hub.</param>
        /// <param name="protocol">An <see cref="IProtocol"/> instance used for parsing and encoding messages.</param>
        public HubConnection(Uri hubUri, IProtocol protocol)
            : this(hubUri, protocol, new HubOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the HubConnection class with specified <see cref="HubOptions"/> instance.
        /// </summary>
        /// <param name="hubUri">The URI of the Hub.</param>
        /// <param name="protocol">An <see cref="IProtocol"/> instance used for parsing and encoding messages.</param>
        /// <param name="options">The <see cref="HubOptions"/> instance for connection related settings.</param>
        public HubConnection(Uri hubUri, IProtocol protocol, HubOptions options)
        {
            this.Context = new LoggingContext(this);

            this.Uri = hubUri;
            this.State = ConnectionStates.Initial;
            this.Options = options;
            this.Protocol = protocol;
            this.Protocol.Connection = this;
            this.AuthenticationProvider = new DefaultAccessTokenAuthenticator(this);
            this.ReconnectPolicy = new DefaultRetryPolicy();
        }

        /// <summary>
        /// Initiates the connection process to the Hub.
        /// </summary>
        public void StartConnect()
        {
            if (this.State != ConnectionStates.Initial &&
                this.State != ConnectionStates.Redirected &&
                this.State != ConnectionStates.Reconnecting)
            {
                HTTPManager.Logger.Warning("HubConnection", "StartConnect - Expected Initial or Redirected state, got " + this.State.ToString(), this.Context);
                return;
            }

            if (this.State == ConnectionStates.Initial)
            {
                this.connectionStartedAt = DateTime.Now;
                HTTPManager.Heartbeats.Subscribe(this);
            }

            HTTPManager.Logger.Verbose("HubConnection", $"StartConnect State: {this.State}, connectionStartedAt: {this.connectionStartedAt.ToString(System.Globalization.CultureInfo.InvariantCulture)}", this.Context);

            if (this.AuthenticationProvider != null && this.AuthenticationProvider.IsPreAuthRequired)
            {
                HTTPManager.Logger.Information("HubConnection", "StartConnect - Authenticating", this.Context);

                SetState(ConnectionStates.Authenticating, null, this.defaultReconnect);

                this.AuthenticationProvider.OnAuthenticationSucceded += OnAuthenticationSucceded;
                this.AuthenticationProvider.OnAuthenticationFailed += OnAuthenticationFailed;

                // Start the authentication process
                this.AuthenticationProvider.StartAuthentication();
            }
            else
                StartNegotiation();
        }

        /// <summary>
        /// Initiates an asynchronous connection to the Hub and returns a task representing the operation.
        /// </summary>
        /// <exception cref="Exception">Thrown when the connection is not in an initial or redirected state or if the connection process has already started.</exception>
        /// <returns>A task that represents the asynchronous connection operation and returns the established connection.</returns>
#if WITH_UNITASK
        public UniTask<HubConnection> ConnectAsync()
#else
        public Task<HubConnection> ConnectAsync()
#endif
        {
            if (this.State != ConnectionStates.Initial && this.State != ConnectionStates.Redirected && this.State != ConnectionStates.Reconnecting)
                throw new Exception("HubConnection - ConnectAsync - Expected Initial or Redirected state, got " + this.State.ToString());

            if (this.connectAsyncTaskCompletionSource != null)
                throw new Exception("Connect process already started!");

#if WITH_UNITASK
            this.connectAsyncTaskCompletionSource = new UniTaskCompletionSource<HubConnection>();
#else
            this.connectAsyncTaskCompletionSource = new TaskCompletionSource<HubConnection>();
#endif

            this.OnConnected += OnAsyncConnectedCallback;
            this.OnError += OnAsyncConnectFailedCallback;

            this.StartConnect();

            return connectAsyncTaskCompletionSource.Task;
        }

        /// <summary>
        /// Begins the process to gracefully close the connection.
        /// </summary>
        public void StartClose()
        {
            HTTPManager.Logger.Verbose("HubConnection", "StartClose", this.Context);
            this.defaultReconnect = false;

            switch (this.State)
            {
                case ConnectionStates.Initial:
                    SetState(ConnectionStates.Closed, null, this.defaultReconnect);
                    break;

                case ConnectionStates.Authenticating:
                    this.AuthenticationProvider.OnAuthenticationSucceded -= OnAuthenticationSucceded;
                    this.AuthenticationProvider.OnAuthenticationFailed -= OnAuthenticationFailed;
                    this.AuthenticationProvider.Cancel();
                    SetState(ConnectionStates.Closed, null, this.defaultReconnect);
                    break;

                case ConnectionStates.Reconnecting:
                    SetState(ConnectionStates.Closed, null, this.defaultReconnect);
                    break;

                case ConnectionStates.CloseInitiated:
                case ConnectionStates.Closed:
                    // Already initiated/closed
                    break;

                default:
                    if (HTTPManager.IsQuitting)
                    {
                        SetState(ConnectionStates.Closed, null, this.defaultReconnect);
                    }
                    else
                    {
                        SetState(ConnectionStates.CloseInitiated, null, this.defaultReconnect);

                        if (this.Transport != null)
                            this.Transport.StartClose();
                    }
                    break;
            }
        }

        /// <summary>
        /// Initiates an asynchronous close operation for the connection and returns a task representing the operation.
        /// </summary>
        /// <exception cref="Exception">Thrown when the <c>CloseAsync</c> method is called multiple times.</exception>
        /// <returns>A task that represents the asynchronous close operation and returns the closed connection.</returns>
#if WITH_UNITASK
        public UniTask<HubConnection> CloseAsync()
#else
        public Task<HubConnection> CloseAsync()
#endif
        {
            if (this.closeAsyncTaskCompletionSource != null)
                throw new Exception("CloseAsync already called!");

#if WITH_UNITASK
            this.closeAsyncTaskCompletionSource = new UniTaskCompletionSource<HubConnection>();
#else
            this.closeAsyncTaskCompletionSource = new TaskCompletionSource<HubConnection>();
#endif

            this.OnClosed += OnClosedAsyncCallback;
            this.OnError += OnClosedAsyncErrorCallback;

            // Avoid race condition by caching task prior to StartClose,
            // which asynchronously calls OnClosedAsyncCallback, which nulls
            // this.closeAsyncTaskCompletionSource immediately before we have
            // a chance to read from it.
            var task = this.closeAsyncTaskCompletionSource.Task;

            this.StartClose();

            return task;
        }

        /// <summary>
        /// Invokes the specified method on the server and returns an <see cref="IFuture"/> instance to subscribe for various events.
        /// </summary>
        /// <typeparam name="TResult">The type of the result expected from the server method.</typeparam>
        /// <param name="target">The name of the server method to invoke.</param>
        /// <param name="args">The arguments to pass to the server method.</param>
        /// <returns>An <see cref="IFuture"/> instance that represents the result from the server method.</returns>
        /// <exception cref="Exception">Thrown when the connection is not in a connected state.</exception>
        public IFuture<TResult> Invoke<TResult>(string target, params object[] args)
        {
            Future<TResult> future = new Future<TResult>();

            try
            {
                long id = InvokeImp(target,
                    args,
                    (message) =>
                    {
                        bool isSuccess = string.IsNullOrEmpty(message.error);
                        if (isSuccess)
                            future.Assign((TResult)this.Protocol.ConvertTo(typeof(TResult), message.result));
                        else
                            future.Fail(new Exception(message.error));
                    },
                    typeof(TResult));

                if (id < 0)
                    future.Fail(new Exception("Not in Connected state! Current state: " + this.State));
            }
            catch (Exception ex)
            {
                future.Fail(ex);
            }

            return future;
        }

        /// <summary>
        /// Asynchronously invokes the specified method on the and returns the result.
        /// </summary>
        /// <typeparam name="TResult">The type of the result expected from the server method.</typeparam>
        /// <param name="target">The name of the server method to invoke.</param>
        /// <param name="args">The arguments to pass to the server method.</param>
        /// <returns>A task that represents the asynchronous invoke operation and contains the result from the server method.</returns>
        /// <exception cref="Exception">Thrown when the connection is not in a connected state or if there are any issues during the invoke process.</exception>
#if WITH_UNITASK
        public UniTask<TResult> InvokeAsync<TResult>(string target, params object[] args)
#else
        public Task<TResult> InvokeAsync<TResult>(string target, params object[] args)
#endif
        {
            return InvokeAsync<TResult>(target, default(CancellationToken), args);
        }

        /// <summary>
        /// Asynchronously invokes the specified method on the server with cancellation support and returns the result.
        /// </summary>
        /// <typeparam name="TResult">The type of the result expected from the server method.</typeparam>
        /// <param name="target">The name of the server method to invoke.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
        /// <param name="args">The arguments to pass to the server method.</param>
        /// <returns>A task that represents the asynchronous invoke operation and contains the result from the server method.</returns>
        /// <exception cref="Exception">Thrown when the connection is not in a connected state, the operation is canceled, or if there are any issues during the invoke process.</exception>
#if WITH_UNITASK
        public UniTask<TResult> InvokeAsync<TResult>(string target, CancellationToken cancellationToken = default, params object[] args)
#else
        public Task<TResult> InvokeAsync<TResult>(string target, CancellationToken cancellationToken = default, params object[] args)
#endif
        {
#if WITH_UNITASK
            UniTaskCompletionSource<TResult> tcs = new UniTaskCompletionSource<TResult>();
#else
            TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
#endif

            try
            {
                long id = InvokeImp(target,
                    args,
                    (message) =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            tcs.TrySetCanceled(cancellationToken);
                            return;
                        }

                        bool isSuccess = string.IsNullOrEmpty(message.error);
                        if (isSuccess)
                            tcs.TrySetResult((TResult)this.Protocol.ConvertTo(typeof(TResult), message.result));
                        else
                            tcs.TrySetException(new Exception(message.error));
                    },
                    typeof(TResult));

                if (id < 0)
                    tcs.TrySetException(new Exception("Not in Connected state! Current state: " + this.State));
                else
                    cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        /// <summary>
        /// Invokes the specified method on the server without expecting a return value.
        /// </summary>
        /// <param name="target">The name of the server method to invoke.</param>
        /// <param name="args">The arguments to send to the server method.</param>
        /// <returns>A future that indicates the completion of the send operation.</returns>
        /// <exception cref="Exception">Thrown when the connection is not in a connected state or if there are any issues during the send process.</exception>
        public IFuture<object> Send(string target, params object[] args)
        {
            Future<object> future = new Future<object>();

            try
            {
                long id = InvokeImp(target,
                    args,
                    (message) =>
                    {
                        bool isSuccess = string.IsNullOrEmpty(message.error);
                        if (isSuccess)
                            future.Assign(message.item);
                        else
                            future.Fail(new Exception(message.error));
                    },
                    typeof(object));

                if (id < 0)
                    future.Fail(new Exception("Not in Connected state! Current state: " + this.State));
            }
            catch (Exception ex)
            {
                future.Fail(ex);
            }

            return future;
        }

        /// <summary>
        /// Invokes the specified method on the server without expecting a return value.
        /// </summary>
        /// <param name="target">The name of the server method to send the message to.</param>
        /// <param name="args">The arguments to send to the server method.</param>
        /// <returns>A task representing the asynchronous send operation.</returns>
#if WITH_UNITASK
        public UniTask<object> SendAsync(string target, params object[] args)
#else
        public Task<object> SendAsync(string target, params object[] args) 
#endif
            => SendAsync(target, default(CancellationToken), args);

        /// <summary>
        /// Invokes the specified method on the server without expecting a return value, with an option to cancel.
        /// </summary>
        /// <param name="target">The name of the server method to send the message to.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <param name="args">The arguments to send to the server method.</param>
        /// <returns>A task representing the asynchronous send operation.</returns>
#if WITH_UNITASK
        public UniTask<object> SendAsync(string target, CancellationToken cancellationToken = default, params object[] args)
#else
        public Task<object> SendAsync(string target, CancellationToken cancellationToken = default, params object[] args)
#endif
        {
#if WITH_UNITASK
            UniTaskCompletionSource<object> tcs = new UniTaskCompletionSource<object>();
#else
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
#endif

            try
            {
                long id = InvokeImp(target,
                    args,
                    (message) =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            tcs.TrySetCanceled(cancellationToken);
                            return;
                        }

                        bool isSuccess = string.IsNullOrEmpty(message.error);
                        if (isSuccess)
                            tcs.TrySetResult(message.item);
                        else
                            tcs.TrySetException(new Exception(message.error));
                    },
                    typeof(object));
                if (id < 0)
                    tcs.TrySetException(new Exception("Not in Connected state! Current state: " + this.State));
                else
                    cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        /// <summary>
        /// Initializes and retrieves a new downstream controller for the specified target. 
        /// This is used for handling server methods that send multiple items over time.
        /// </summary>
        /// <typeparam name="TDown">The type of items expected from the server.</typeparam>
        /// <param name="target">The name of the server method to connect to.</param>
        /// <param name="args">The arguments to send to the server method.</param>
        /// <returns>A controller for the downstream data.</returns>
        public DownStreamItemController<TDown> GetDownStreamController<TDown>(string target, params object[] args)
        {
            long invocationId = System.Threading.Interlocked.Increment(ref this.lastInvocationId);

            var future = new Future<TDown>();
            future.BeginProcess();

            var controller = new DownStreamItemController<TDown>(this, invocationId, future);

            Action<Message> callback = (Message msg) =>
            {
                switch (msg.type)
                {
                    // StreamItem message contains only one item.
                    case MessageTypes.StreamItem:
                        {
                            if (controller.IsCanceled)
                                break;

                            TDown item = (TDown)this.Protocol.ConvertTo(typeof(TDown), msg.item);

                            future.AssignItem(item);
                            break;
                        }

                    case MessageTypes.Completion:
                        {
                            bool isSuccess = string.IsNullOrEmpty(msg.error);
                            if (isSuccess)
                            {
                                // While completion message must not contain any result, this should be future-proof
                                if (!controller.IsCanceled && msg.result != null)
                                {
                                    TDown result = (TDown)this.Protocol.ConvertTo(typeof(TDown), msg.result);

                                    future.AssignItem(result);
                                }

                                future.Finish();
                            }
                            else
                                future.Fail(new Exception(msg.error));
                            break;
                        }
                }
            };

            var message = new Message
            {
                type = MessageTypes.StreamInvocation,
                invocationId = invocationId.ToString(),
                target = target,
                arguments = args,
                nonblocking = false,
            };

            try
            {
                SendMessage(message);
            }
            catch (Exception ex)
            {
                future.Fail(ex);
            }

            if (callback != null)
                if (!this.invocations.TryAdd(invocationId, new InvocationDefinition { callback = callback, returnType = typeof(TDown) }))
                    HTTPManager.Logger.Warning("HubConnection", "GetDownStreamController - invocations already contains id: " + invocationId, this.Context);

            return controller;
        }

        /// <summary>
        /// Initializes and retrieves a new upstream controller for sending multiple items to the server over time.
        /// </summary>
        /// <typeparam name="TResult">The type of the result expected once all items have been sent.</typeparam>
        /// <param name="target">The name of the server method to connect to.</param>
        /// <param name="paramCount">The count of items to be sent upstream.</param>
        /// <param name="downStream">Flag indicating whether this is a downstream operation. If false, it's an upstream operation.</param>
        /// <param name="args">The arguments to send to the server method.</param>
        /// <returns>A controller for the upstream data.</returns>
        public UpStreamItemController<TResult> GetUpStreamController<TResult>(string target, int paramCount, bool downStream, object[] args)
        {
            Future<TResult> future = new Future<TResult>();
            future.BeginProcess();

            long invocationId = System.Threading.Interlocked.Increment(ref this.lastInvocationId);

            string[] streamIds = new string[paramCount];
            for (int i = 0; i < paramCount; i++)
                streamIds[i] = System.Threading.Interlocked.Increment(ref this.lastStreamId).ToString();

            var controller = new UpStreamItemController<TResult>(this, invocationId, streamIds, future);

            Action<Message> callback = (Message msg) => {
                switch (msg.type)
                {
                    // StreamItem message contains only one item.
                    case MessageTypes.StreamItem:
                        {
                            if (controller.IsCanceled)
                                break;

                            TResult item = (TResult)this.Protocol.ConvertTo(typeof(TResult), msg.item);

                            future.AssignItem(item);
                            break;
                        }

                    case MessageTypes.Completion:
                        {
                            bool isSuccess = string.IsNullOrEmpty(msg.error);
                            if (isSuccess)
                            {
                                // While completion message must not contain any result, this should be future-proof
                                if (!controller.IsCanceled && msg.result != null)
                                {
                                    TResult result = (TResult)this.Protocol.ConvertTo(typeof(TResult), msg.result);

                                    future.AssignItem(result);
                                }

                                future.Finish();
                            }
                            else
                            {
                                var ex = new Exception(msg.error);
                                future.Fail(ex);
                            }
                            break;
                        }
                }
            };

            var messageToSend = new Message
            {
                type = downStream ? MessageTypes.StreamInvocation : MessageTypes.Invocation,
                invocationId = invocationId.ToString(),
                target = target,
                arguments = args,
                streamIds = streamIds,
                nonblocking = false,
            };

            try
            {
                SendMessage(messageToSend);
            }
            catch (Exception ex)
            {
                future.Fail(ex);
            }

            if (!this.invocations.TryAdd(invocationId, new InvocationDefinition { callback = callback, returnType = typeof(TResult) }))
                HTTPManager.Logger.Warning("HubConnection", "GetUpStreamController - invocations already contains id: " + invocationId, this.Context);

            return controller;
        }

        /// <summary>
        /// Registers a callback to be invoked when the server calls the specified method with no parameters.
        /// </summary>
        /// <param name="methodName">The name of the method to listen to.</param>
        /// <param name="callback">The action to be executed when the method is called by the server.</param>
        public void On(string methodName, Action callback) => On(methodName, null, (args) => callback());

        /// <summary>
        /// Registers a callback to be invoked when the server calls the specified method.
        /// </summary>
        /// <typeparam name="T1">The type of the parameter the server will send.</typeparam>
        /// <param name="methodName">The name of the method to listen to.</param>
        /// <param name="callback">The action to be executed when the method is called by the server.</param>
        public void On<T1>(string methodName, Action<T1> callback) => On(methodName, new Type[] { typeof(T1) }, (args) => callback((T1)args[0]));

        /// <summary>
        /// Registers a callback to be invoked when the server calls the specified method.
        /// </summary>
        /// <typeparam name="T1">The type of the parameter the server will send.</typeparam>
        /// <typeparam name="T2">The type of the parameter the server will send.</typeparam>
        /// <param name="methodName">The name of the method to listen to.</param>
        /// <param name="callback">The action to be executed when the method is called by the server.</param>
        public void On<T1, T2>(string methodName, Action<T1, T2> callback) => On(methodName, new Type[] { typeof(T1), typeof(T2) }, (args) => callback((T1)args[0], (T2)args[1]));

        /// <summary>
        /// Registers a callback to be invoked when the server calls the specified method.
        /// </summary>
        /// <typeparam name="T1">The type of the parameter the server will send.</typeparam>
        /// <typeparam name="T2">The type of the parameter the server will send.</typeparam>
        /// <typeparam name="T3">The type of the parameter the server will send.</typeparam>
        /// <param name="methodName">The name of the method to listen to.</param>
        /// <param name="callback">The action to be executed when the method is called by the server.</param>
        public void On<T1, T2, T3>(string methodName, Action<T1, T2, T3> callback) => On(methodName, new Type[] { typeof(T1), typeof(T2), typeof(T3) }, (args) => callback((T1)args[0], (T2)args[1], (T3)args[2]));

        /// <summary>
        /// Registers a callback to be invoked when the server calls the specified method.
        /// </summary>
        /// <typeparam name="T1">The type of the parameter the server will send.</typeparam>
        /// <typeparam name="T2">The type of the parameter the server will send.</typeparam>
        /// <typeparam name="T3">The type of the parameter the server will send.</typeparam>
        /// <typeparam name="T4">The type of the parameter the server will send.</typeparam>
        /// <param name="methodName">The name of the method to listen to.</param>
        /// <param name="callback">The action to be executed when the method is called by the server.</param>
        public void On<T1, T2, T3, T4>(string methodName, Action<T1, T2, T3, T4> callback) => On(methodName, new Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, (args) => callback((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3]));

        /// <summary>
        /// Registers a function callback to be invoked when the server calls the specified function with no parameters, but expects a return value.
        /// </summary>
        /// <typeparam name="Result">The type of the return value.</typeparam>
        /// <param name="methodName">The name of the function to listen to.</param>
        /// <param name="callback">The function to be executed when the function is called by the server.</param>
        public void On<Result>(string methodName, Func<Result> callback) => OnFunc<Result>(methodName, null, (args) => callback());

        /// <summary>
        /// Registers a function callback to be invoked when the server calls the specified function with no parameters.
        /// </summary>
        /// <typeparam name="Result">The type of the return value.</typeparam>
        /// <typeparam name="T1">The type of the parameter the server will send.</typeparam>
        /// <param name="methodName">The name of the function to listen to.</param>
        /// <param name="callback">The function to be executed when the function is called by the server.</param>
        public void On<T1, Result>(string methodName, Func<T1, Result> callback) => OnFunc<Result>(methodName, new Type[] { typeof(T1) }, (args) => callback((T1)args[0]));

        /// <summary>
        /// Registers a function callback to be invoked when the server calls the specified function with no parameters.
        /// </summary>
        /// <typeparam name="Result">The type of the return value.</typeparam>
        /// <typeparam name="T1">The type of the parameter the server will send.</typeparam>
        /// <typeparam name="T2">The type of the parameter the server will send.</typeparam>
        /// <param name="methodName">The name of the function to listen to.</param>
        /// <param name="callback">The function to be executed when the function is called by the server.</param>
        public void On<T1, T2, Result>(string methodName, Func<T1, T2, Result> callback) => OnFunc<Result>(methodName, new Type[] { typeof(T1), typeof(T2) }, (args) => callback((T1)args[0], (T2)args[1]));

        /// <summary>
        /// Registers a function callback to be invoked when the server calls the specified function with no parameters.
        /// </summary>
        /// <typeparam name="Result">The type of the return value.</typeparam>
        /// <typeparam name="T1">The type of the parameter the server will send.</typeparam>
        /// <typeparam name="T2">The type of the parameter the server will send.</typeparam>
        /// <typeparam name="T3">The type of the parameter the server will send.</typeparam>
        /// <param name="methodName">The name of the function to listen to.</param>
        /// <param name="callback">The function to be executed when the function is called by the server.</param>
        public void On<T1, T2, T3, Result>(string methodName, Func<T1, T2, T3, Result> callback) => OnFunc<Result>(methodName, new Type[] { typeof(T1), typeof(T2), typeof(T3) }, (args) => callback((T1)args[0], (T2)args[1], (T3)args[2]));

        /// <summary>
        /// Registers a function callback to be invoked when the server calls the specified function with no parameters.
        /// </summary>
        /// <typeparam name="Result">The type of the return value.</typeparam>
        /// <typeparam name="T1">The type of the parameter the server will send.</typeparam>
        /// <typeparam name="T2">The type of the parameter the server will send.</typeparam>
        /// <typeparam name="T3">The type of the parameter the server will send.</typeparam>
        /// <typeparam name="T4">The type of the parameter the server will send.</typeparam>
        /// <param name="methodName">The name of the function to listen to.</param>
        /// <param name="callback">The function to be executed when the function is called by the server.</param>
        public void On<T1, T2, T3, T4, Result>(string methodName, Func<T1, T2, T3, T4, Result> callback) => OnFunc<Result>(methodName, new Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, (args) => callback((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3]));

        private void OnAsyncConnectedCallback(HubConnection hub)
        {
            this.OnConnected -= OnAsyncConnectedCallback;
            this.OnError -= OnAsyncConnectFailedCallback;

            this.connectAsyncTaskCompletionSource.TrySetResult(this);
            this.connectAsyncTaskCompletionSource = null;
        }

        private void OnAsyncConnectFailedCallback(HubConnection hub, string error)
        {
            this.OnConnected -= OnAsyncConnectedCallback;
            this.OnError -= OnAsyncConnectFailedCallback;

            this.connectAsyncTaskCompletionSource.TrySetException(new Exception(error));
            this.connectAsyncTaskCompletionSource = null;
        }

        private void OnAuthenticationSucceded(IAuthenticationProvider provider)
        {
            HTTPManager.Logger.Verbose("HubConnection", "OnAuthenticationSucceded", this.Context);

            this.AuthenticationProvider.OnAuthenticationSucceded -= OnAuthenticationSucceded;
            this.AuthenticationProvider.OnAuthenticationFailed -= OnAuthenticationFailed;

            StartNegotiation();
        }

        private void OnAuthenticationFailed(IAuthenticationProvider provider, string reason)
        {
            HTTPManager.Logger.Error("HubConnection", "OnAuthenticationFailed: " + reason, this.Context);

            this.AuthenticationProvider.OnAuthenticationSucceded -= OnAuthenticationSucceded;
            this.AuthenticationProvider.OnAuthenticationFailed -= OnAuthenticationFailed;

            SetState(ConnectionStates.Closed, reason, this.defaultReconnect);
        }

        private void StartNegotiation()
        {
            HTTPManager.Logger.Verbose("HubConnection", "StartNegotiation", this.Context);

            if (this.State == ConnectionStates.CloseInitiated)
            {
                SetState(ConnectionStates.Closed, null, this.defaultReconnect);
                return;
            }

#if !BESTHTTP_DISABLE_WEBSOCKET
            if (this.Options.SkipNegotiation && this.Options.PreferedTransport == TransportTypes.WebSocket)
            {
                HTTPManager.Logger.Verbose("HubConnection", "Skipping negotiation", this.Context);
                ConnectImpl(this.Options.PreferedTransport);

                return;
            }
#endif

            SetState(ConnectionStates.Negotiating, null, this.defaultReconnect);

            // https://github.com/dotnet/aspnetcore/blob/master/src/SignalR/docs/specs/TransportProtocols.md#post-endpoint-basenegotiate-request
            // Send out a negotiation request. While we could skip it and connect right with the websocket transport
            //  it might return with additional information that could be useful.

            UriBuilder builder = new UriBuilder(this.Uri);
            if (builder.Path.EndsWith("/"))
                builder.Path += "negotiate";
            else
                builder.Path += "/negotiate";

            string query = builder.Query;
            if (string.IsNullOrEmpty(query))
                query = "negotiateVersion=1";
            else
                query = query.Remove(0, 1) + "&negotiateVersion=1";

            builder.Query = query;

            var request = new HTTPRequest(builder.Uri, HTTPMethods.Post, OnNegotiationRequestFinished);
            request.DownloadSettings.DisableCache = true;
            request.Context.Add("Hub", this.Context);

            if (this.AuthenticationProvider != null)
                this.AuthenticationProvider.PrepareRequest(request);

            request.Send();
        }

        private void ConnectImpl(TransportTypes transport)
        {
            HTTPManager.Logger.Verbose("HubConnection", "ConnectImpl - " + transport, this.Context);

            switch (transport)
            {
#if !BESTHTTP_DISABLE_WEBSOCKET
                case TransportTypes.WebSocket:
                    if (this.NegotiationResult != null && !IsTransportSupported("WebSockets"))
                    {
                        SetState(ConnectionStates.Closed, "Couldn't use preferred transport, as the 'WebSockets' transport isn't supported by the server!", this.defaultReconnect);
                        return;
                    }

                    this.Transport = new Transports.WebSocketTransport(this);
                    this.Transport.OnStateChanged += Transport_OnStateChanged;
                    break;
#endif

                case TransportTypes.LongPolling:
                    if (this.NegotiationResult != null && !IsTransportSupported("LongPolling"))
                    {
                        SetState(ConnectionStates.Closed, "Couldn't use preferred transport, as the 'LongPolling' transport isn't supported by the server!", this.defaultReconnect);
                        return;
                    }

                    this.Transport = new Transports.LongPollingTransport(this);
                    this.Transport.OnStateChanged += Transport_OnStateChanged;
                    break;

                default:
                    SetState(ConnectionStates.Closed, "Unsupported transport: " + transport, this.defaultReconnect);
                    break;
            }

            try
            {
                if (this.OnTransportEvent != null)
                    this.OnTransportEvent(this, this.Transport, TransportEvents.SelectedToConnect);
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception("HubConnection", "ConnectImpl - OnTransportEvent exception in user code!", ex, this.Context);
            }

            this.Transport.StartConnect();
        }

        private bool IsTransportSupported(string transportName)
        {
            // https://github.com/dotnet/aspnetcore/blob/master/src/SignalR/docs/specs/TransportProtocols.md#post-endpoint-basenegotiate-request
            // If the negotiation response contains only the url and accessToken, no 'availableTransports' list is sent
            if (this.NegotiationResult.SupportedTransports == null)
                return true;

            for (int i = 0; i < this.NegotiationResult.SupportedTransports.Count; ++i)
                if (this.NegotiationResult.SupportedTransports[i].Name.Equals(transportName, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        private void OnNegotiationRequestFinished(HTTPRequest req, HTTPResponse resp)
        {
            if (this.State == ConnectionStates.Closed)
                return;

            if (this.State == ConnectionStates.CloseInitiated)
            {
                SetState(ConnectionStates.Closed, null, this.defaultReconnect);
                return;
            }

            string errorReason = null;

            switch (req.State)
            {
                // The request finished without any problem.
                case HTTPRequestStates.Finished:
                    if (resp.IsSuccess)
                    {
                        HTTPManager.Logger.Information("HubConnection", "Negotiation Request Finished Successfully! Response: " + resp.DataAsText, this.Context);

                        // Parse negotiation
                        this.NegotiationResult = NegotiationResult.Parse(resp, out errorReason, this);

                        // Room for improvement: check validity of the negotiation result:
                        //  If url and accessToken is present, the other two must be null.
                        //  https://github.com/dotnet/aspnetcore/blob/master/src/SignalR/docs/specs/TransportProtocols.md#post-endpoint-basenegotiate-request

                        if (string.IsNullOrEmpty(errorReason))
                        {
                            if (this.NegotiationResult.Url != null)
                            {
                                this.SetState(ConnectionStates.Redirected, null, this.defaultReconnect);

                                if (++this.RedirectCount >= this.Options.MaxRedirects)
                                    errorReason = string.Format("MaxRedirects ({0:N0}) reached!", this.Options.MaxRedirects);
                                else
                                {
                                    var oldUri = this.Uri;
                                    this.Uri = this.NegotiationResult.Url;

                                    if (this.OnRedirected != null)
                                    {
                                        try
                                        {
                                            this.OnRedirected(this, oldUri, Uri);
                                        }
                                        catch (Exception ex)
                                        {
                                            HTTPManager.Logger.Exception("HubConnection", "OnNegotiationRequestFinished - OnRedirected", ex, this.Context);
                                        }
                                    }

                                    StartConnect();
                                }
                            }
                            else
                                ConnectImpl(this.Options.PreferedTransport);
                        }
                    }
                    else // Internal server error?
                        errorReason = string.Format("Negotiation Request Finished Successfully, but the server sent an error. Status Code: {0}-{1} Message: {2}",
                                                        resp.StatusCode,
                                                        resp.Message,
                                                        resp.DataAsText);
                    break;

                // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
                case HTTPRequestStates.Error:
                    errorReason = "Negotiation Request Finished with Error! " + (req.Exception != null ? (req.Exception.Message + "\n" + req.Exception.StackTrace) : "No Exception");
                    break;

                // The request aborted, initiated by the user.
                case HTTPRequestStates.Aborted:
                    errorReason = "Negotiation Request Aborted!";
                    break;

                // Connecting to the server is timed out.
                case HTTPRequestStates.ConnectionTimedOut:
                    errorReason = "Negotiation Request - Connection Timed Out!";
                    break;

                // The request didn't finished in the given time.
                case HTTPRequestStates.TimedOut:
                    errorReason = "Negotiation Request - Processing the request Timed Out!";
                    break;
            }

            if (errorReason != null)
            {
                if (this.ReconnectPolicy != null)
                {
                    RetryContext context = new RetryContext
                    {
                        ElapsedTime = HTTPManager.CurrentFrameDateTime - this.connectionStartedAt,
                        PreviousRetryCount = this.currentContext.PreviousRetryCount,
                        RetryReason = errorReason
                    };

                    TimeSpan? nextAttempt = null;
                    try
                    {
                        nextAttempt = this.ReconnectPolicy.GetNextRetryDelay(context);
                    }
                    catch (Exception ex)
                    {
                        HTTPManager.Logger.Exception("HubConnection", "ReconnectPolicy.GetNextRetryDelay", ex, this.Context);
                    }

                    if (nextAttempt == null)
                    {
                        this.NegotiationResult = new NegotiationResult();
                        this.NegotiationResult.NegotiationResponse = resp;

                        SetState(ConnectionStates.Closed, errorReason, this.defaultReconnect);
                    }
                    else
                    {
                        HTTPManager.Logger.Information("HubConnection", "Next reconnect attempt after " + nextAttempt.Value.ToString(), this.Context);

                        this.currentContext = context;
                        this.currentContext.PreviousRetryCount += 1;

                        this.reconnectAt = HTTPManager.CurrentFrameDateTime + nextAttempt.Value;

                        this.SetState(ConnectionStates.Reconnecting, null, this.defaultReconnect);
                    }
                }
                else
                {
                    this.NegotiationResult = new NegotiationResult();
                    this.NegotiationResult.NegotiationResponse = resp;

                    SetState(ConnectionStates.Closed, errorReason, this.defaultReconnect);
                }
            }
        }

        private void OnClosedAsyncCallback(HubConnection hub)
        {
            this.OnClosed -= OnClosedAsyncCallback;
            this.OnError -= OnClosedAsyncErrorCallback;

            this.closeAsyncTaskCompletionSource.TrySetResult(this);
            this.closeAsyncTaskCompletionSource = null;
        }

        private void OnClosedAsyncErrorCallback(HubConnection hub, string error)
        {
            this.OnClosed -= OnClosedAsyncCallback;
            this.OnError -= OnClosedAsyncErrorCallback;

            this.closeAsyncTaskCompletionSource.TrySetException(new Exception(error));
            this.closeAsyncTaskCompletionSource = null;
        }

        private long InvokeImp(string target, object[] args, Action<Message> callback, Type itemType, bool isStreamingInvocation = false)
        {
            if (this.State != ConnectionStates.Connected)
                return -1;

            bool blockingInvocation = callback == null;

            long invocationId = blockingInvocation ? 0 : System.Threading.Interlocked.Increment(ref this.lastInvocationId);
            var message = new Message
            {
                type = isStreamingInvocation ? MessageTypes.StreamInvocation : MessageTypes.Invocation,
                invocationId = blockingInvocation ? null : invocationId.ToString(),
                target = target,
                arguments = args,
                nonblocking = callback == null,
            };

            SendMessage(message);

            if (!blockingInvocation)
                if (!this.invocations.TryAdd(invocationId, new InvocationDefinition { callback = callback, returnType = itemType }))
                    HTTPManager.Logger.Warning("HubConnection", "InvokeImp - invocations already contains id: " + invocationId, this.Context);

            return invocationId;
        }

        internal void SendMessage(Message message)
        {
            // https://github.com/Benedicht/BestHTTP-Issues/issues/146
            if (this.State == ConnectionStates.Closed)
                return;

            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Verbose("HubConnection", "SendMessage: " + message.ToString(), this.Context);

            try
            {
                using (new WriteLock(this.rwLock))
                {
                    var encoded = this.Protocol.EncodeMessage(message);
                    if (encoded.Data != null)
                    {
                        this.lastMessageSentAt = DateTime.Now;
                        this.Transport.Send(encoded);
                    }
                }
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception("HubConnection", "SendMessage", ex, this.Context);
                throw;
            }
        }

        private void On(string methodName, Type[] paramTypes, Action<object[]> callback) => this.subscriptions.GetOrAdd(methodName, _ => new Subscription()).Add(paramTypes, callback);

        /// <summary>
        /// <seealso href="https://github.com/dotnet/aspnetcore/issues/5280">[Epic]: Support returning values from client invocations</seealso>
        /// </summary>
        private void OnFunc<Result>(string methodName, Type[] paramTypes, Func<object[], object> callback) => this.subscriptions.GetOrAdd(methodName, _ => new Subscription()).AddFunc(typeof(Result), paramTypes, callback);

        /// <summary>
        /// Remove all event handlers for <paramref name="methodName"/> that subscribed with an On call.
        /// </summary>
        public void Remove(string methodName) => this.subscriptions.TryRemove(methodName, out var _);

        internal Subscription GetSubscription(string methodName) => this.subscriptions.TryGetValue(methodName, out var subscribtion) ? subscribtion : null;

        internal Type GetItemType(long invocationId) => this.invocations.TryGetValue(invocationId, out var def) ? def.returnType : null;

        internal void OnMessages(List<Message> messages)
        {
            this.lastMessageReceivedAt = HTTPManager.CurrentFrameDateTime;

            if (pausedInLastFrame)
            {
                if (this.delayedMessages == null)
                    this.delayedMessages = new List<Message>(messages.Count);
                foreach (var msg in messages)
                    delayedMessages.Add(msg);

                messages.Clear();
            }

            for (int messageIdx = 0; messageIdx < messages.Count; ++messageIdx)
            {
                var message = messages[messageIdx];

                if (this.OnMessage != null)
                {
                    try
                    {
                        if (!this.OnMessage(this, message))
                            continue;
                    }
                    catch (Exception ex)
                    {
                        HTTPManager.Logger.Exception("HubConnection", "Exception in OnMessage user code!", ex, this.Context);
                    }
                }

                switch (message.type)
                {
                    case MessageTypes.Handshake:
                        break;

                    case MessageTypes.Invocation:
                        {
                            Subscription subscribtion = null;
                            if (this.subscriptions.TryGetValue(message.target, out subscribtion))
                            {
                                if (subscribtion.callbacks?.Count == 0 && subscribtion.functionCallbacks?.Count == 0)
                                    HTTPManager.Logger.Warning("HubConnection", $"No callback for invocation '{message.ToString()}'", this.Context);

                                for (int i = 0; i < subscribtion.callbacks.Count; ++i)
                                {
                                    var callbackDesc = subscribtion.callbacks[i];

                                    object[] realArgs = null;
                                    try
                                    {
                                        realArgs = this.Protocol.GetRealArguments(callbackDesc.ParamTypes, message.arguments);
                                    }
                                    catch (Exception ex)
                                    {
                                        HTTPManager.Logger.Exception("HubConnection", "OnMessages - Invocation - GetRealArguments", ex, this.Context);
                                    }

                                    try
                                    {
                                        callbackDesc.Callback.Invoke(realArgs);
                                    }
                                    catch (Exception ex)
                                    {
                                        HTTPManager.Logger.Exception("HubConnection", "OnMessages - Invocation - Invoke", ex, this.Context);
                                    }
                                }

                                if (subscribtion.functionCallbacks != null)
                                {
                                    for (int i = 0; i < subscribtion.functionCallbacks.Count; ++i)
                                    {
                                        var callbackDesc = subscribtion.functionCallbacks[i];

                                        object[] realArgs = null;
                                        try
                                        {
                                            realArgs = this.Protocol.GetRealArguments(callbackDesc.ParamTypes, message.arguments);
                                        }
                                        catch (Exception ex)
                                        {
                                            HTTPManager.Logger.Exception("HubConnection", "OnMessages - Function Invocation - GetRealArguments", ex, this.Context);
                                        }

                                        try
                                        {
                                            var result = callbackDesc.Callback(realArgs);

#if WITH_UNITASK
                                            if (result.GetType() is Type uniTaskType && uniTaskType.Name == "UniTask`1")
                                            {
                                                throw new NotImplementedException("UnitTask<T>");
                                            }
                                            else
#endif

                                            if (result is System.Threading.Tasks.Task task && task.GetType() is Type taskType && taskType.IsGenericType)
                                            {
                                                task.ContinueWith((t) =>
                                                {
                                                    Exception error = null;
                                                    try
                                                    {
                                                        if (!t.IsCanceled && !t.IsFaulted)
                                                        {
                                                            var prop = taskType.GetProperty("Result");
                                                            var taskResult = prop.GetValue(t);
                                                
                                                            SendMessage(new Message { type = MessageTypes.Completion, invocationId = message.invocationId, result = taskResult });
                                                        }
                                                        else
                                                            error = t.Exception.InnerException ?? new TaskCanceledException();
                                                    }
                                                    catch(Exception ex)
                                                    {
                                                        error = ex;
                                                    }
                                                
                                                    if (error != null)
                                                        SendMessage(new Message { type = MessageTypes.Completion, invocationId = message.invocationId, error = error.Message });
                                                });
                                            }
                                            else
                                                SendMessage(new Message { type = MessageTypes.Completion, invocationId = message.invocationId, result = result });
                                        }
                                        catch (Exception ex)
                                        {
                                            HTTPManager.Logger.Exception("HubConnection", "OnMessages - Function Invocation - Invoke", ex, this.Context);

                                            SendMessage(new Message { type = MessageTypes.Completion, invocationId = message.invocationId, error = ex.Message });
                                        }
                                    }
                                }
                            }
                            else
                                HTTPManager.Logger.Warning("HubConnection", $"No subscription could be found for invocation '{message.ToString()}'", this.Context);

                            break;
                        }

                    case MessageTypes.StreamItem:
                        {
                            long invocationId;
                            if (long.TryParse(message.invocationId, out invocationId))
                            {
                                InvocationDefinition def;
                                if (this.invocations.TryGetValue(invocationId, out def) && def.callback != null)
                                {
                                    try
                                    {
                                        def.callback(message);
                                    }
                                    catch (Exception ex)
                                    {
                                        HTTPManager.Logger.Exception("HubConnection", "OnMessages - StreamItem - callback", ex, this.Context);
                                    }
                                }
                            }
                            break;
                        }

                    case MessageTypes.Completion:
                        {
                            long invocationId;
                            if (long.TryParse(message.invocationId, out invocationId))
                            {
                                InvocationDefinition def;
                                if (this.invocations.TryRemove(invocationId, out def) && def.callback != null)
                                {
                                    try
                                    {
                                        def.callback(message);
                                    }
                                    catch (Exception ex)
                                    {
                                        HTTPManager.Logger.Exception("HubConnection", "OnMessages - Completion - callback", ex, this.Context);
                                    }
                                }
                            }
                            break;
                        }

                    case MessageTypes.Ping:
                        // Send back an answer
                        SendMessage(new Message() { type = MessageTypes.Ping });
                        break;

                    case MessageTypes.Close:
                        SetState(ConnectionStates.Closed, message.error, message.allowReconnect);
                        if (this.Transport != null)
                            this.Transport.StartClose();
                        return;
                }
            }
        }

        private void Transport_OnStateChanged(TransportStates oldState, TransportStates newState)
        {
            HTTPManager.Logger.Verbose("HubConnection", string.Format("Transport_OnStateChanged - oldState: {0} newState: {1}", oldState.ToString(), newState.ToString()), this.Context);

            if (this.State == ConnectionStates.Closed)
            {
                HTTPManager.Logger.Verbose("HubConnection", "Transport_OnStateChanged - already closed!", this.Context);
                return;
            }

            switch (newState)
            {
                case TransportStates.Connected:
                    try
                    {
                        if (this.OnTransportEvent != null)
                            this.OnTransportEvent(this, this.Transport, TransportEvents.Connected);
                    }
                    catch (Exception ex)
                    {
                        HTTPManager.Logger.Exception("HubConnection", "Exception in OnTransportEvent user code!", ex, this.Context);
                    }

                    SetState(ConnectionStates.Connected, null, this.defaultReconnect);
                    break;

                case TransportStates.Failed:
                    if (this.State == ConnectionStates.Negotiating && !HTTPManager.IsQuitting)
                    {
                        try
                        {
                            if (this.OnTransportEvent != null)
                                this.OnTransportEvent(this, this.Transport, TransportEvents.FailedToConnect);
                        }
                        catch (Exception ex)
                        {
                            HTTPManager.Logger.Exception("HubConnection", "Exception in OnTransportEvent user code!", ex, this.Context);
                        }

                        this.triedoutTransports.Add(this.Transport.TransportType);

                        var nextTransport = GetNextTransportToTry();
                        if (nextTransport == null)
                        {
                            var reason = this.Transport.ErrorReason;
                            this.Transport = null;

                            SetState(ConnectionStates.Closed, reason, this.defaultReconnect);
                        }
                        else
                            ConnectImpl(nextTransport.Value);
                    }
                    else
                    {
                        try
                        {
                            if (this.OnTransportEvent != null)
                                this.OnTransportEvent(this, this.Transport, TransportEvents.ClosedWithError);
                        }
                        catch (Exception ex)
                        {
                            HTTPManager.Logger.Exception("HubConnection", "Exception in OnTransportEvent user code!", ex, this.Context);
                        }

                        var reason = this.Transport.ErrorReason;
                        this.Transport = null;

                        SetState(ConnectionStates.Closed, HTTPManager.IsQuitting ? null : reason, this.defaultReconnect);
                    }
                    break;

                case TransportStates.Closed:
                    {
                        try
                        {
                            if (this.OnTransportEvent != null)
                                this.OnTransportEvent(this, this.Transport, TransportEvents.Closed);
                        }
                        catch (Exception ex)
                        {
                            HTTPManager.Logger.Exception("HubConnection", "Exception in OnTransportEvent user code!", ex, this.Context);
                        }

                        // Check wheter we have any delayed message and a Close message among them. If there's one, delay the SetState(Close) too.
                        if (this.delayedMessages == null || this.delayedMessages.FindLast(dm => dm.type == MessageTypes.Close).type != MessageTypes.Close)
                            SetState(ConnectionStates.Closed, null, this.defaultReconnect);
                    }
                    break;
            }
        }

        private TransportTypes? GetNextTransportToTry()
        {
            foreach (TransportTypes val in Enum.GetValues(typeof(TransportTypes)))
                if (!this.triedoutTransports.Contains(val) && IsTransportSupported(val.ToString()))
                    return val;

            return null;
        }

        private void SetState(ConnectionStates state, string errorReason, bool allowReconnect)
        {
            HTTPManager.Logger.Information("HubConnection", string.Format("SetState - from State: '{0}' to State: '{1}', errorReason: '{2}', allowReconnect: {3}, isQuitting: {4}", this.State, state, errorReason, allowReconnect, HTTPManager.IsQuitting), this.Context);

            if (this.State == state)
                return;

            var previousState = this.State;

            this.State = state;

            switch (state)
            {
                case ConnectionStates.Initial:
                case ConnectionStates.Authenticating:
                case ConnectionStates.Negotiating:
                case ConnectionStates.CloseInitiated:
                    break;

                case ConnectionStates.Reconnecting:
                    break;

                case ConnectionStates.Connected:
                    // If reconnectStartTime isn't its default value we reconnected
                    if (this.reconnectStartTime != DateTime.MinValue)
                    {
                        try
                        {
                            if (this.OnReconnected != null)
                                this.OnReconnected(this);
                        }
                        catch (Exception ex)
                        {
                            HTTPManager.Logger.Exception("HubConnection", "OnReconnected", ex, this.Context);
                        }
                    }
                    else
                    {
                        try
                        {
                            if (this.OnConnected != null)
                                this.OnConnected(this);
                        }
                        catch (Exception ex)
                        {
                            HTTPManager.Logger.Exception("HubConnection", "Exception in OnConnected user code!", ex, this.Context);
                        }
                    }

                    this.lastMessageSentAt = HTTPManager.CurrentFrameDateTime;
                    this.lastMessageReceivedAt = HTTPManager.CurrentFrameDateTime;

                    // Clean up reconnect related fields
                    this.currentContext = new RetryContext();
                    this.reconnectStartTime = DateTime.MinValue;
                    this.reconnectAt = DateTime.MinValue;

                    HTTPUpdateDelegator.OnApplicationForegroundStateChanged -= this.OnApplicationForegroundStateChanged;
                    HTTPUpdateDelegator.OnApplicationForegroundStateChanged += this.OnApplicationForegroundStateChanged;

                    break;

                case ConnectionStates.Closed:
                    // Go through all invocations and cancel them.
                    var error = new Message();
                    error.type = MessageTypes.Close;
                    error.error = errorReason;

                    foreach (var kvp in this.invocations)
                    {
                        try
                        {
                            kvp.Value.callback(error);
                        }
                        catch
                        { }
                    }

                    this.invocations.Clear();

                    // No errorReason? It's an expected closure.
                    if (errorReason == null && (!allowReconnect || HTTPManager.IsQuitting))
                    {
                        if (this.OnClosed != null)
                        {
                            try
                            {
                                this.OnClosed(this);
                            }
                            catch (Exception ex)
                            {
                                HTTPManager.Logger.Exception("HubConnection", "Exception in OnClosed user code!", ex, this.Context);
                            }
                        }
                    }
                    else
                    {
                        // If possible, try to reconnect
                        if (allowReconnect && this.ReconnectPolicy != null && (previousState == ConnectionStates.Connected || this.reconnectStartTime != DateTime.MinValue))
                        {
                            // It's the first attempt after a successful connection
                            if (this.reconnectStartTime == DateTime.MinValue)
                            {
                                this.connectionStartedAt = this.reconnectStartTime = HTTPManager.CurrentFrameDateTime;

                                try
                                {
                                    if (this.OnReconnecting != null)
                                        this.OnReconnecting(this, errorReason);
                                }
                                catch (Exception ex)
                                {
                                    HTTPManager.Logger.Exception("HubConnection", "SetState - ConnectionStates.Reconnecting", ex, this.Context);
                                }
                            }

                            RetryContext context = new RetryContext
                            {
                                ElapsedTime = HTTPManager.CurrentFrameDateTime - this.reconnectStartTime,
                                PreviousRetryCount = this.currentContext.PreviousRetryCount,
                                RetryReason = errorReason
                            };

                            TimeSpan? nextAttempt = null;
                            try
                            {
                                nextAttempt = this.ReconnectPolicy.GetNextRetryDelay(context);
                            }
                            catch (Exception ex)
                            {
                                HTTPManager.Logger.Exception("HubConnection", "ReconnectPolicy.GetNextRetryDelay", ex, this.Context);
                            }

                            // No more reconnect attempt, we are closing
                            if (nextAttempt == null)
                            {
                                HTTPManager.Logger.Warning("HubConnection", "No more reconnect attempt!", this.Context);

                                // Clean up everything
                                this.currentContext = new RetryContext();
                                this.reconnectStartTime = DateTime.MinValue;
                                this.reconnectAt = DateTime.MinValue;
                            }
                            else
                            {
                                HTTPManager.Logger.Information("HubConnection", "Next reconnect attempt after " + nextAttempt.Value.ToString(), this.Context);

                                this.currentContext = context;
                                this.currentContext.PreviousRetryCount += 1;

                                this.reconnectAt = HTTPManager.CurrentFrameDateTime + nextAttempt.Value;

                                this.SetState(ConnectionStates.Reconnecting, null, this.defaultReconnect);

                                return;
                            }
                        }

                        if (this.OnError != null)
                        {
                            try
                            {
                                this.OnError(this, errorReason);
                            }
                            catch (Exception ex)
                            {
                                HTTPManager.Logger.Exception("HubConnection", "Exception in OnError user code!", ex, this.Context);
                            }
                        }
                    }
                    break;
            }
        }

        private void OnApplicationForegroundStateChanged(bool isPaused)
        {
            pausedInLastFrame = !isPaused;

            HTTPManager.Logger.Information("HubConnection", $"OnApplicationForegroundStateChanged isPaused: {isPaused} pausedInLastFrame: {pausedInLastFrame}", this.Context);
        }

        void Best.HTTP.Shared.Extensions.IHeartbeat.OnHeartbeatUpdate(DateTime now, TimeSpan dif)
        {
            switch (this.State)
            {
                case ConnectionStates.Negotiating:
                case ConnectionStates.Authenticating:
                case ConnectionStates.Redirected:
                    if (HTTPManager.CurrentFrameDateTime >= this.connectionStartedAt + this.Options.ConnectTimeout)
                    {
                        if (this.AuthenticationProvider != null)
                        {
                            this.AuthenticationProvider.OnAuthenticationSucceded -= OnAuthenticationSucceded;
                            this.AuthenticationProvider.OnAuthenticationFailed -= OnAuthenticationFailed;

                            try
                            {
                                this.AuthenticationProvider.Cancel();
                            }
                            catch (Exception ex)
                            {
                                HTTPManager.Logger.Exception("HubConnection", "Exception in AuthenticationProvider.Cancel !", ex, this.Context);
                            }
                        }

                        if (this.Transport != null)
                        {
                            this.Transport.OnStateChanged -= Transport_OnStateChanged;
                            this.Transport.StartClose();
                        }

                        SetState(ConnectionStates.Closed, string.Format("Couldn't connect in the given time({0})!", this.Options.ConnectTimeout), this.defaultReconnect);
                    }
                    break;

                case ConnectionStates.Connected:
                    if (this.delayedMessages?.Count > 0)
                    {
                        pausedInLastFrame = false;
                        try
                        {
                            // if there's any Close message clear any other one.
                            int idx = this.delayedMessages.FindLastIndex(dm => dm.type == MessageTypes.Close);
                            if (idx > 0)
                                this.delayedMessages.RemoveRange(0, idx);

                            OnMessages(this.delayedMessages);
                        }
                        finally
                        {
                            this.delayedMessages.Clear();
                        }
                    }

                    // Still connected? Check pinging.
                    if (this.State == ConnectionStates.Connected)
                    {
                        if (this.Options.PingInterval != TimeSpan.Zero && HTTPManager.CurrentFrameDateTime - this.lastMessageReceivedAt >= this.Options.PingTimeoutInterval)
                        {
                            // The transport itself can be in a failure state or in a completely valid one, so while we do not want to receive anything from it, we have to try to close it
                            if (this.Transport != null)
                            {
                                this.Transport.OnStateChanged -= Transport_OnStateChanged;
                                this.Transport.StartClose();
                            }

                            SetState(ConnectionStates.Closed,
                                string.Format("PingInterval set to '{0}' and no message is received since '{1}'. PingTimeoutInterval: '{2}'", this.Options.PingInterval, this.lastMessageReceivedAt, this.Options.PingTimeoutInterval),
                                this.defaultReconnect);
                        }
                        else if (this.Options.PingInterval != TimeSpan.Zero && HTTPManager.CurrentFrameDateTime - this.lastMessageSentAt >= this.Options.PingInterval)
                            SendMessage(new Message() { type = MessageTypes.Ping });
                    }
                    break;

                case ConnectionStates.Reconnecting:
                    if (this.reconnectAt != DateTime.MinValue && HTTPManager.CurrentFrameDateTime >= this.reconnectAt)
                    {
                        this.delayedMessages?.Clear();
                        this.connectionStartedAt = HTTPManager.CurrentFrameDateTime;
                        this.reconnectAt = DateTime.MinValue;
                        this.triedoutTransports.Clear();
                        this.StartConnect();
                    }
                    break;

                case ConnectionStates.Closed:
                    CleanUp();
                    break;
            }
        }

        private void CleanUp()
        {
            HTTPManager.Logger.Information("HubConnection", "CleanUp", this.Context);

            this.delayedMessages?.Clear();
            HTTPManager.Heartbeats.Unsubscribe(this);
            HTTPUpdateDelegator.OnApplicationForegroundStateChanged -= this.OnApplicationForegroundStateChanged;

            this.rwLock?.Dispose();
            this.rwLock = null;
        }
    }
}

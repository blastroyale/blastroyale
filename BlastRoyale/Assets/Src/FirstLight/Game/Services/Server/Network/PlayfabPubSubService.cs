using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Best.HTTP.Shared.Extensions;
using Best.SignalR;
using Best.SignalR.Authentication;
using Best.SignalR.Encoders;
using Best.TLSSecurity;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Messages;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Modules;
using JetBrains.Annotations;
using PlayFab;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Allows to subscribe to topics in the PlayFab provided PubSub service
	/// This service only connects when is needed, nothing is done at startup time
	/// </summary>
	public interface IPlayfabPubSubService
	{
		public delegate void OnReconnectedHandler();

		/// <summary>
		/// Triggered when the Pubsub connection is reestablished
		/// </summary>
		public event OnReconnectedHandler OnReconnected;

		/// <summary>
		/// Listen to messages published by PlayFab
		/// </summary>
		public void ListenTopic<T>(string topic, Action<T> handler);

		/// <summary>
		/// Listen to changes in the subscription changes, like subscriptions, unsubscriptions
		/// Subscriptions are the way PlayFab send messages, you subscribe to a topic then you start to receive messages
		/// </summary>
		public void ListenSubscriptionStatus(string topic, Action<SubscriptionChangeMessage> handler);

		/// <summary>
		/// Get the SignalR connection handler string 
		/// </summary>
		public UniTask<String> GetConnectionHandle(bool noCache = false);

		/// <summary>
		/// Clear all listeners added a topic by <see cref="ListenTopic{T}"/> and <see cref="ListenSubscriptionStatus"/>
		/// </summary>
		public void ClearListeners(string topic);

		/// <summary>
		/// Model of the subscription change messages
		/// </summary>
		public class SubscriptionChangeMessage
		{
			// What topic is the subscription pointing to
			public String Topic;

			// The status of the subscription
			public string Status;

			// If the status is unsubscribed this field will contain the reason
			[CanBeNull] public string UnsubscribeReason;
		}
	}

	/// <inheritdoc/>
	public class PlayfabPubSubService : IPlayfabPubSubService
	{
		private HubConnection _connection;
		private bool _connected;
		private bool _connecting;
		private string _connectionHandle;

		private SemaphoreSlim _accessSemaphore = new (1, 1);

		private LitJsonEncoder _jsonEncoder = new ();
		private Dictionary<string, List<Action<byte[]>>> _onMessageListeners = new ();
		private Dictionary<string, List<Action<IPlayfabPubSubService.SubscriptionChangeMessage>>> _onSubscriptionStatus = new ();
		public event IPlayfabPubSubService.OnReconnectedHandler OnReconnected;

		public PlayfabPubSubService(IMessageBrokerService msgBroker)
		{
			Best.HTTP.Response.Decompression.DeflateDecompressor.IsSupported = false;
#pragma warning disable CS4014
			msgBroker.Subscribe<SuccessAuthentication>(_ => { OnSuccessAuthentication(); });
#pragma warning restore CS4014
		}

		private async Task OnSuccessAuthentication()
		{
			try
			{
				await _accessSemaphore.WaitAsync();
				if (_connection != null)
				{
					await _connection.CloseAsync();
					ResetConnectionFields();
					_onMessageListeners.Clear();
					_onSubscriptionStatus.Clear();
				}
			}
			finally
			{
				_accessSemaphore.Release();
			}
		}

		/// <inheritdoc/>
		public async UniTask<String> GetConnectionHandle(bool noCache = false)
		{
			if (noCache)
			{
				await FetchConnectionHandle();
			}

			return _connectionHandle;
		}


		/// <inheritdoc/>
		public void ListenSubscriptionStatus(string topic, Action<IPlayfabPubSubService.SubscriptionChangeMessage> handler)
		{
			var currentHandlers = _onSubscriptionStatus.TryGetValue(topic, out var handlers) ? handlers : new ();
			currentHandlers.Add(handler);
			_onSubscriptionStatus[topic] = currentHandlers;
		}


		/// <inheritdoc/>
		public void ListenTopic<T>(string topic, Action<T> handler)
		{
			var currentHandlers = _onMessageListeners.TryGetValue(topic, out var handlers) ? handlers : new ();
			currentHandlers.Add(o =>
			{
				var message = _jsonEncoder.DecodeAs<T>(o.AsBuffer());
				handler(message);
			});
			_onMessageListeners[topic] = currentHandlers;
		}

		/// <inheritdoc/>
		public void ClearListeners(string topic)
		{
			_onMessageListeners.Remove(topic);
			_onSubscriptionStatus.Remove(topic);
		}

		private async UniTask Connect()
		{
			if (_connected || _connecting)
			{
				return;
			}

			//TLSSecurity.Setup();
			FLog.Verbose("PubSub", "Connecting to PubSub");
			_connecting = true;
			var url = $"https://"+PlayFabSettings.TitleId+".playfabapi.com/PubSub";
			_connection = new HubConnection(new Uri(url), new JsonProtocol(_jsonEncoder), new HubOptions()
			{
				PingInterval = TimeSpan.FromSeconds(2),
				PingTimeoutInterval = TimeSpan.FromSeconds(15),
				WebsocketOptions =
				{
				
				}
			});
			_connection.ReconnectPolicy = new DefaultRetryPolicy();
			_connection.AuthenticationProvider = new PlayFabAuthenticator(_connection, PlayFabSettings.staticPlayer.EntityToken);
			_connection.On<PlayfabPubSubMessage>("ReceiveMessage", MessageHandler);
			_connection.On<IPlayfabPubSubService.SubscriptionChangeMessage>("ReceiveSubscriptionChangeMessage", SubscriptionHandler);

			_connection.OnConnected += a =>
			{
				FLog.Info("PubSub", "Connected");
			};
			_connection.OnClosed += _ =>
			{
				FLog.Error("PubSub", "OnClosed");
				ResetConnectionFields();
			};
			_connection.OnError += (_, errorString) =>
			{
				FLog.Error("PubSub", "PlayfabPubSubError: " + errorString);
				ResetConnectionFields();
			};
			_connection.OnReconnecting += (a, s) =>
			{
				FLog.Verbose("PubSub", $"Reconnecting {a.State}: {s}");
			};
			_connection.OnReconnected += _ =>
			{
				FLog.Info("PubSub", "OnReconnected");
				OnReconnected?.Invoke();
			};
			await _connection.ConnectAsync();
			_connected = true;
			_connecting = false;
		}

		private void ResetConnectionFields()
		{
			_connected = false;
			_connecting = false;
		}

		private void SubscriptionHandler(IPlayfabPubSubService.SubscriptionChangeMessage obj)
		{
			FLog.Info(ModelSerializer.Serialize(obj).Value);
			if (_onSubscriptionStatus.TryGetValue(obj.Topic, out var handlers))
			{
				foreach (var handler in handlers)
				{
					handler(obj);
				}
			}
		}

		private void MessageHandler(PlayfabPubSubMessage obj)
		{
			var base64EncodedBytes = Convert.FromBase64String(obj.payload);
			FLog.Info("PubSub", Encoding.UTF8.GetString(base64EncodedBytes));
			if (_onMessageListeners.TryGetValue(obj.topic, out var handlers))
			{
				foreach (var handler in handlers)
				{
					handler(base64EncodedBytes);
				}
			}
		}
		[Serializable]
		private class StartOrRecoverySessionResponse
		{
			public string newConnectionHandle { get; set; }
			public List<string> recoveredTopics { get; set; }
			public string status { get; set; }
			public string traceId { get; set; }
		}

		private async UniTask FetchConnectionHandle()
		{
			try
			{
				await _accessSemaphore.WaitAsync();
				await Connect();
				var body = new StartOrRecoverySessionRequest()
				{
					//TODO how to create a trace id or use a pre existing one
					traceParent = "01-84678fd69ae13e41fce1333289bcf482-22d157fb94ea4827-01",
				};
				var response = await _connection.InvokeAsync<StartOrRecoverySessionResponse>("StartOrRecoverSession", body);
				_connectionHandle = response.newConnectionHandle;
			}
			finally
			{
				_accessSemaphore.Release();
			}
		}


		[Serializable]
		private class StartOrRecoverySessionRequest
		{
			public string oldConnectionHandle { get; set; }
			public string traceParent { get; set; }
		}

	


		private class PlayfabPubSubMessage
		{
			public string topic { get; set; }
			public string payload { get; set; }
			public string traceId { get; set; }
		}
	}
}
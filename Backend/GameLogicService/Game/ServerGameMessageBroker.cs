using System;
using System.Collections.Generic;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;
using Microsoft.Extensions.Logging;

namespace GameLogicService.Game
{
	/// <summary>
	/// Represents server logic message broker class. It collects all messages fired from game logic (inside commands)
	/// and fires them wrapped with ServerGameLogicMessage which contains the executing user.
	/// Those messages can be then listened on server plugins to perform server driven actions.
	/// </summary>
	public class GameServerLogicMessageBroker : IMessageBrokerService
	{
		private readonly string _executingUserId;
		private readonly List<object> _publishedMessages = new ();
		private readonly IEventManager _pluginEvents;
		private readonly ILogger _log;
		
		public GameServerLogicMessageBroker(string executingUserId, IEventManager pluginEvents, ILogger log)
		{
			_executingUserId = executingUserId;
			_pluginEvents = pluginEvents;
			_log = log;
		}
		
		public void Publish<T>(T message) where T : IMessage
		{
			try
			{
				_pluginEvents.CallEvent(new GameLogicMessageEvent<T>(_executingUserId, message));
			}
			catch (Exception e)
			{
				_log.LogError(e, $"Error Running Game Logic Message {message.GetType().Name}");
			}
		}

		public void Subscribe<T>(Action<T> action) where T : IMessage
		{
			throw new NotImplementedException("Server logic message broker cannot be subscribed.");
		}

		public void Unsubscribe<T>(Action<T> action) where T : IMessage
		{
			throw new NotImplementedException("Server logic message broker cannot be subscribed.");
		}

		public void Unsubscribe<T>() where T : IMessage
		{
			throw new NotImplementedException("Server logic message broker cannot be subscribed.");
		}

		public void UnsubscribeAll(object subscriber = null)
		{
			throw new NotImplementedException("Server logic message broker cannot be subscribed.");
		}
	}
}
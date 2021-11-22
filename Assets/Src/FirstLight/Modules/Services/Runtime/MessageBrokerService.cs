using System;
using System.Collections;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace

namespace FirstLight.Services
{
	/// <summary>
	/// The message contract that must be used for all messages being published via the <see cref="IMessageBrokerService"/>
	/// </summary>
	public interface IMessage { }

	/// <summary>
	/// This services provides the execution of the Message Broker.
	/// It provides a easy way to decouple objects across the system with an independent channel of communication, by dispatching
	/// message events to be caught by all it's observer subscribers.
	/// </summary>
	/// <remarks>
	/// Follows the "Message Broker Pattern" <see cref="https://en.wikipedia.org/wiki/Message_broker"/>
	/// </remarks>
	public interface IMessageBrokerService
	{
		/// <summary>
		/// Publish a message in the message broker.
		/// If there is no object subscribing the message type, nothing will happen
		/// </summary>
		void Publish<T>(T message) where T : IMessage;
		
		/// <summary>
		/// Subscribes to the message type.
		/// Will invoke the <paramref name="action"/> every time the message of the subscribed type is published.
		/// </summary>
		void Subscribe<T>(Action<T> action) where T : IMessage;
		
		/// <summary>
		/// Unsubscribe the <paramref name="action"/> from the message broker.
		/// </summary>
		void Unsubscribe<T>(Action<T> action) where T : IMessage;
		
		/// <summary>
		/// Unsubscribe all actions from the message broker from of the given message type.
		/// </summary>
		void Unsubscribe<T>() where T : IMessage;
		
		/// <summary>
		/// Unsubscribe from all messages.
		/// If <paramref name="subscriber"/> is null then will unsubscribe from EVERYTHING, other wise only from the given subscriber
		/// </summary>
		void UnsubscribeAll(object subscriber = null);
	}

	/// <inheritdoc />
	public class MessageBrokerService : IMessageBrokerService
	{
		private readonly IDictionary<Type, IDictionary<object, IList>> _subscriptions = new Dictionary<Type, IDictionary<object, IList>>();

		/// <inheritdoc />
		public void Publish<T>(T message) where T : IMessage
		{
			if (!_subscriptions.TryGetValue(typeof(T), out var subscriptionObjects))
			{
				return;
			}

			var subscriptionCopy = new IList[subscriptionObjects.Count];
			
			subscriptionObjects.Values.CopyTo(subscriptionCopy, 0);

			for (var i = 0; i < subscriptionCopy.Length; i++)
			{
				var actions = (List<Action<T>>) subscriptionCopy[i];

				for (var index = 0; index < actions.Count; index++)
				{
					actions[index](message);
				}
			}
		}

		/// <inheritdoc />
		public void Subscribe<T>(Action<T> action) where T : IMessage
		{
			var type = typeof(T);
			var subscriber = action.Target;

			if (subscriber == null)
			{
				throw new ArgumentException("Subscribe static functions to a message is not supported!");
			}

			if (!_subscriptions.TryGetValue(type, out var subscriptionObjects))
			{
				subscriptionObjects = new Dictionary<object, IList>();
				_subscriptions.Add(type, subscriptionObjects);
			}

			if (!subscriptionObjects.TryGetValue(subscriber, out IList actions))
			{
				actions = new List<Action<T>>();
				subscriptionObjects.Add(subscriber, actions);
			}

			actions.Add(action);
		}

		/// <inheritdoc />
		public void Unsubscribe<T>(Action<T> action) where T : IMessage
		{
			var type = typeof(T);
			var subscriber = action.Target;

			if (subscriber == null)
			{
				throw new ArgumentException("Subscribe static functions to a message is not supported!");
			}

			if (!_subscriptions.TryGetValue(type, out var subscriptionObjects) || 
			    !subscriptionObjects.TryGetValue(subscriber, out var actions))
			{
				return;
			}

			actions.Remove(action);

			if (actions.Count == 0)
			{
				subscriptionObjects.Remove(subscriber);
			}

			if (subscriptionObjects.Count == 0)
			{
				_subscriptions.Remove(type);
			}
		}

		/// <inheritdoc />
		public void Unsubscribe<T>() where T : IMessage
		{
			_subscriptions.Remove(typeof(T));
		}

		/// <inheritdoc />
		public void UnsubscribeAll(object subscriber = null)
		{
			if (subscriber == null)
			{
				_subscriptions.Clear();
				return;
			}

			foreach (var subscriptionObjects in _subscriptions.Values)
			{
				if (subscriptionObjects.ContainsKey(subscriber))
				{
					subscriptionObjects.Remove(subscriber);
				}
			}
		}
	}
}
using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.SDK.Services;

namespace FirstLight.SDK.Modules
{
	/// <summary>
	/// In-Memory implementation of message broker service.
	/// This object is intended to be used by client to listen to messages as needed.
	/// </summary>
	public class InMemoryMessageBrokerService : IMessageBrokerService
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
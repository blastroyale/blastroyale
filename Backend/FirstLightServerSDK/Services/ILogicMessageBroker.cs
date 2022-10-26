using System;

namespace FirstLight.SDK.Services
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
}

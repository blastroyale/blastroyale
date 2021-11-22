using FirstLight.Services;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.Services.Tests
{
	public class MessageBrokerServiceTest
	{
		public interface IMockSubscriber
		{
			void MockMessageCall(MessageType1 message);
			void MockMessageCall2(MessageType1 message);
			void MockMessageAlternativeCall(MessageType2 message);
			void MockMessageAlternativeCall2(MessageType2 message);
		}
		
		public struct MessageType1 : IMessage {}
		public struct MessageType2 : IMessage {}

		private MessageType1 _messageType1;
		private MessageType2 _messageType2;
		private IMockSubscriber _subscriber;
		private MessageBrokerService _messageBroker;

		[SetUp]
		public void Init()
		{
			_messageBroker = new MessageBrokerService();
			_subscriber = Substitute.For<IMockSubscriber>();
			_messageType1 = new MessageType1();
			_messageType2 = new MessageType2();
		}

		[Test]
		public void Subscribe_Publish_Successfully()
		{
			_messageBroker.Subscribe<MessageType1>(_subscriber.MockMessageCall);
			_messageBroker.Publish(_messageType1);

			_subscriber.Received().MockMessageCall(_messageType1);
		}

		[Test]
		public void Publish_WithoutSubscription_DoesNothing()
		{
			_messageBroker.Publish(_messageType1);

			_subscriber.DidNotReceive().MockMessageCall(_messageType1);
		}

		[Test]
		public void Unsubscribe_Successfully()
		{
			_messageBroker.Subscribe<MessageType1>(_subscriber.MockMessageCall);
			_messageBroker.Unsubscribe<MessageType1>(_subscriber.MockMessageCall);
			_messageBroker.Publish(_messageType1);

			_subscriber.DidNotReceive().MockMessageCall(_messageType1);
		}

		[Test]
		public void UnsubscribeWithAction_KeepsSubscriptionSameType_Successfully()
		{
			_messageBroker.Subscribe<MessageType1>(_subscriber.MockMessageCall);
			_messageBroker.Subscribe<MessageType1>(_subscriber.MockMessageCall2);
			_messageBroker.Unsubscribe<MessageType1>(_subscriber.MockMessageCall);
			_messageBroker.Publish(_messageType1);

			_subscriber.DidNotReceive().MockMessageCall(_messageType1);
			_subscriber.Received().MockMessageCall2(_messageType1);
		}

		[Test]
		public void UnsubscribeWithoutAction_KeepsSubscriptionDifferentType_Successfully()
		{
			_messageBroker.Subscribe<MessageType1>(_subscriber.MockMessageCall);
			_messageBroker.Subscribe<MessageType2>(_subscriber.MockMessageAlternativeCall);
			_messageBroker.Unsubscribe<MessageType1>();
			_messageBroker.Publish(_messageType2);

			_subscriber.DidNotReceive().MockMessageCall(_messageType1);
			_subscriber.Received().MockMessageAlternativeCall(_messageType2);
		}

		[Test]
		public void UnsubscribeAll_Successfully()
		{
			_messageBroker.Subscribe<MessageType1>(_subscriber.MockMessageCall);
			_messageBroker.Subscribe<MessageType1>(_subscriber.MockMessageCall2);
			_messageBroker.Subscribe<MessageType2>(_subscriber.MockMessageAlternativeCall);
			_messageBroker.Subscribe<MessageType2>(_subscriber.MockMessageAlternativeCall2);
			_messageBroker.UnsubscribeAll();
			_messageBroker.Publish(_messageType2);
			_messageBroker.Publish(_messageType2);

			_subscriber.DidNotReceive().MockMessageCall(_messageType1);
			_subscriber.DidNotReceive().MockMessageCall2(_messageType1);
			_subscriber.DidNotReceive().MockMessageAlternativeCall(_messageType2);
			_subscriber.DidNotReceive().MockMessageAlternativeCall2(_messageType2);
		}

		[Test]
		public void Unsubscribe_WithoutSubscription_DoesNothing()
		{
			Assert.DoesNotThrow(() => _messageBroker.Unsubscribe<MessageType1>(_subscriber.MockMessageCall));
			Assert.DoesNotThrow(() => _messageBroker.Unsubscribe<MessageType1>());
			Assert.DoesNotThrow(() => _messageBroker.UnsubscribeAll());
		}
	}
}

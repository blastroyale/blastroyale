using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.SDK.Services;

namespace FirstLight.Game.TestCases.Helpers
{
	public class MessageBrokerHelper : TestHelper
	{
		public IEnumerator WaitForMessage<T>(Predicate<T> validate = null, float timeout = 30) where T : IMessage
		{
			yield return WaitForGameAwaken();
			
			bool arrived = false;

			void MessageProcessor(T message)
			{
				if (validate == null || validate.Invoke(message))
				{
					arrived = true;
				}
			}

			Services.MessageBrokerService.Subscribe<T>(MessageProcessor);
			yield return TestTools.Until(() => arrived, timeout, $"Not received {typeof(T).Name} in message broker!");
			Services.MessageBrokerService.Unsubscribe<T>(MessageProcessor);
		}

		public MessageBrokerHelper(FLGTestRunner testRunner) : base(testRunner)
		{
		}
	}
}
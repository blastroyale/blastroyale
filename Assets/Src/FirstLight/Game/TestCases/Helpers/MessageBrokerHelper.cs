using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.SDK.Services;

namespace FirstLight.Game.TestCases.Helpers
{
	public class MessageBrokerHelper : TestHelper
	{
		public IEnumerator WaitForMessage<T>(Predicate<T> validate, float timeout) where T : IMessage
		{
			yield return WaitForGameAwaken();
			
			bool arrived = false;

			void MessageProcessor(T message)
			{
				if (validate.Invoke(message))
				{
					arrived = true;
				}
			}

			Services.MessageBrokerService.Subscribe<T>(MessageProcessor);
			yield return TestTools.Until(() => arrived, timeout, true);
			Services.MessageBrokerService.Unsubscribe<T>(MessageProcessor);
		}

		public MessageBrokerHelper(FLGTestRunner testRunner) : base(testRunner)
		{
		}
	}
}
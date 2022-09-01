using System;
using FirstLight.Services;

namespace FirstLight.Tests.EditorMode
{
	/// <summary>
	/// Todo: Adapt Tick Service to allow us to abstract it without loosing usability
	/// </summary>
	public class StubTickService : ITickService, IDisposable
	{
		public void SubscribeOnUpdate(Action<float> action, float deltaTime = 0, bool timeOverflowToNextTick = false, bool realTime = false)
		{
			
		}

		public void SubscribeOnLateUpdate(Action<float> action, float deltaTime = 0, bool timeOverflowToNextTick = false,
			bool realTime = false)
		{
			
		}

		public void SubscribeOnFixedUpdate(Action<float> action)
		{
		
		}

		public void Unsubscribe(Action<float> action)
		{
		
		}

		public void UnsubscribeOnUpdate(Action<float> action)
		{
			
		}

		public void UnsubscribeOnFixedUpdate(Action<float> action)
		{
		
		}

		public void UnsubscribeOnLateUpdate(Action<float> action)
		{
		
		}

		public void UnsubscribeAllOnUpdate()
		{
			
		}

		public void UnsubscribeAllOnUpdate(object subscriber)
		{

		}

		public void UnsubscribeAllOnFixedUpdate()
		{
			
		}

		public void UnsubscribeAllOnFixedUpdate(object subscriber)
		{
			
		}

		public void UnsubscribeAllOnLateUpdate()
		{
	
		}

		public void UnsubscribeAllOnLateUpdate(object subscriber)
		{
	
		}

		public void UnsubscribeAll()
		{
	
		}

		public void UnsubscribeAll(object subscriber)
		{
		
		}

		public void Dispose()
		{
			
		}
	}
}
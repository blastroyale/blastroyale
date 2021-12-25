using Quantum.Collections;

namespace Quantum
{
	public unsafe partial struct BlackboardEntry
	{
		/// <summary>
		/// Iterate through all Decorators that watches this Blackboard entry
		/// Re-check the Decorators so it can check if an abort is needed
		/// </summary>
		/// <param name="btParams"></param>
		public void TriggerDecorators(BTParams btParams)
		{
			var frame = btParams.FrameThreadSafe;

			// If the reactive decorators list was already allocated...
			if (ReactiveDecorators.Ptr != default)
			{
				// Solve it and trigger the decorators checks
				var reactiveDecorators = frame.ResolveList(ReactiveDecorators);
				for (int i = 0; i < reactiveDecorators.Count; i++)
				{
					var reactiveDecoratorRef = reactiveDecorators[i];
					var decoratorInstance = frame.FindAsset<BTDecorator>(reactiveDecoratorRef.Id);
					btParams.Agent->OnDecoratorReaction(btParams, decoratorInstance, decoratorInstance.AbortType, out bool abortSelf, out bool abortLowerPriority);

					// If at least one Decorator resulted in abort, we stop and return already
					if (abortSelf == true)
					{
						btParams.Agent->AbortNodeId = decoratorInstance.Id;
					}
				}
			}
		}
	}
}

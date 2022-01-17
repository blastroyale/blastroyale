using System;

namespace Quantum
{
	/// <summary>
	/// Reactive Decorator sample. Listens to changes on two Blackboard entries.
	/// </summary>
	[Serializable]
	public unsafe class BTBlackboardCompare : BTDecorator
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		// We let the user define, on the Visual Editor, which Blackboard entries
		// shall be observed by this Decorator
		public AIBlackboardValueKey BlackboardKeyA;
		public AIBlackboardValueKey BlackboardKeyB;

		// ========== BTNode INTERFACE ================================================================================

		public override void OnEnter(BTParams btParams)
		{
			base.OnEnter(btParams);

			// Whenever we enter this Decorator...
			// We register it as a Reactive Decorator so, whenever the entries are changed,
			// the DryRun is executed again, possibly aborting the current execution
			btParams.Blackboard->RegisterReactiveDecorator(btParams.Frame, BlackboardKeyA.Key, this);
			btParams.Blackboard->RegisterReactiveDecorator(btParams.Frame, BlackboardKeyB.Key, this);
		}

		public override void OnExit(BTParams btParams)
		{
			base.OnExit(btParams);
			// Whenever the execution goes higher, it means that this Decorator isn't in the current subtree anymore
			// So we unregister this Decorator from the Reactive list. This means that if the Blackboard entries
			// get changed, this Decorator will not react anymore
			btParams.Blackboard->UnregisterReactiveDecorator(btParams.Frame, BlackboardKeyA.Key, this);
			btParams.Blackboard->UnregisterReactiveDecorator(btParams.Frame, BlackboardKeyB.Key, this);
		}

		// We just check if A is greater than B. If that's the case
		// PS: this gets called in THREE possible situations:
		// 1 - When the execution is goign DOWN on the tree and this Decorator is found
		// 2 - If changes to the observed blackboard entries happen
		// 3 - If this is inside a Dynamic Composite node
		public override Boolean DryRun(BTParams btParams)
		{
			var blackboard = btParams.Blackboard;
			var A = blackboard->GetInteger(btParams.Frame, BlackboardKeyA.Key);
			var B = blackboard->GetInteger(btParams.Frame, BlackboardKeyB.Key);

			return A > B;
		}
	}
}

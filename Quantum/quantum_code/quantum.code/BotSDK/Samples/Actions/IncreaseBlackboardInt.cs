using System;

namespace Quantum
{
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public unsafe partial class IncreaseBlackboardInt : AIAction
	{
		public AIBlackboardValueKey Key;
		public AIParamInt IncrementAmount;

		public override unsafe void Update(Frame frame, EntityRef entity)
		{
			var blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);

			var agent = frame.Unsafe.GetPointer<HFSMAgent>(entity);
			var aiConfig = agent->GetConfig(frame);

			var incrementValue = IncrementAmount.Resolve(frame, entity, blackboard, aiConfig);

			var currentAmount = blackboard->GetInteger(frame, Key.Key);
			currentAmount += incrementValue;

			blackboard->Set(frame, Key.Key, currentAmount);
		}
	}
}
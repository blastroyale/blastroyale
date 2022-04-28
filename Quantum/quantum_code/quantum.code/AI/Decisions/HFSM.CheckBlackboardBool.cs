using System;

namespace Quantum
{
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public partial class CheckBlackboardBool : HFSMDecision
	{
		public AIBlackboardValueKey Key;
		public AIParamBool DesiredValue;

		public override unsafe bool Decide(Frame frame, EntityRef entity)
		{
			var blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);

			var agent = frame.Unsafe.GetPointer<HFSMAgent>(entity);
			var aiConfig = agent->GetConfig(frame);

			var comparisonValue = DesiredValue.Resolve(frame, entity, blackboard, aiConfig);
			var currentAmount = blackboard->GetBoolean(frame, Key.Key);

			return comparisonValue == currentAmount;
		}
	}
}
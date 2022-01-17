using System;

namespace Quantum
{
	public enum EValueComparison
	{
		None,
		LessThan,
		MoreThan,
		EqualTo,
	}

	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public partial class CheckBlackboardInt : HFSMDecision
	{
		public AIBlackboardValueKey Key;
		public EValueComparison Comparison = EValueComparison.MoreThan;
		public AIParamInt DesiredValue = 1;

		public override unsafe bool Decide(Frame frame, EntityRef entity)
		{
			var blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);

			var agent = frame.Unsafe.GetPointer<HFSMAgent>(entity);
			var aiConfig = agent->GetConfig(frame);

			var comparisonValue = DesiredValue.Resolve(frame, entity, blackboard, aiConfig);
			var currentAmount = blackboard->GetInteger(frame, Key.Key);

			switch (Comparison)
			{
				case EValueComparison.LessThan: return currentAmount < comparisonValue;
				case EValueComparison.MoreThan: return currentAmount > comparisonValue;
				case EValueComparison.EqualTo: return currentAmount == comparisonValue;
				default: return false;
			}
		}
	}
}
using System;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public partial class CheckBlackboardFP : HFSMDecision
	{
		public AIBlackboardValueKey Key;
		public EValueComparison Comparison = EValueComparison.MoreThan;
		public AIParamFP DesiredValue = FP._1;

		public override unsafe bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			var blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);

			var agent = frame.Unsafe.GetPointer<HFSMAgent>(entity);
			var aiConfig = agent->GetConfig(frame);

			var comparisonValue = DesiredValue.Resolve(frame, entity, blackboard, aiConfig);
			var currentAmount = blackboard->GetFP(frame, Key.Key);

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
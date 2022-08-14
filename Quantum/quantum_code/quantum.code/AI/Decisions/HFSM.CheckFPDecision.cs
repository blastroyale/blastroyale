using System;

namespace Quantum
{
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public partial class CheckFPDecision : HFSMDecision
	{
		public AIParamFP Source;
		public EValueComparison Comparison = EValueComparison.MoreThan;
		public AIParamFP DesiredValue;

		public override unsafe bool Decide(Frame frame, EntityRef entity)
		{
			var blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);

			var agent = frame.Unsafe.GetPointer<HFSMAgent>(entity);
			var aiConfig = agent->GetConfig(frame);

			var comparisonValue = DesiredValue.Resolve(frame, entity, blackboard, aiConfig);
			var currentValue = Source.Resolve(frame, entity, blackboard, aiConfig);
			
			switch (Comparison)
			{
				case EValueComparison.LessThan: return currentValue < comparisonValue;
				case EValueComparison.MoreThan: return currentValue > comparisonValue;
				case EValueComparison.EqualTo: return currentValue == comparisonValue;
				default: return false;
			}
		}
	}
}
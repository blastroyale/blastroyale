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

		public override unsafe bool Decide(Frame f, EntityRef e)
		{
			var bbComponent = f.Unsafe.GetPointer<AIBlackboardComponent>(e);

			var hfsmAgent = f.Unsafe.GetPointer<HFSMAgent>(e);
			var config = hfsmAgent->GetConfig(f);

			var comparisonValue = DesiredValue.Resolve(f, bbComponent, config);
			var currentAmount = bbComponent->GetInteger(f, Key.Key);

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
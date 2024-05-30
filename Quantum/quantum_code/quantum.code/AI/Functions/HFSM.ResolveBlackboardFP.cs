using System;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
		GenerateAssetResetMethod = false)]
	public unsafe class ResolveBlackboardFP : AIFunction<FP>
	{
		public AIBlackboardValueKey Key;

		public override FP Execute(Frame f, EntityRef e, ref AIContext aiContext)
		{
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			return bb->GetFP(f, Key.Key);
		}
	}
}
using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Takes in two <see cref="FP"/>'s and sums them.
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public unsafe class FPSumFunction : AIFunction<FP>
	{
		public AIParamFP First;
		public AIParamFP Second;

		/// <inheritdoc />
		public override FP Execute(Frame f, EntityRef e, ref AIContext aiContext)
		{
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			return First.Resolve(f, e, bb, null) + Second.Resolve(f, e, bb, null);
		}
	}
}
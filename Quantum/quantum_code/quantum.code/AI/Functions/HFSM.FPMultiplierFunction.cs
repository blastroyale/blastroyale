using Photon.Deterministic;
using System;

namespace Quantum
{
	/// <summary>
	/// Takes in two <see cref="FP"/>'s and multiplies them.
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
					   GenerateAssetResetMethod = false)]
	public unsafe class FPMultiplierFunction : AIFunction<FP>
	{
		public AIParamFP First;
		public AIParamFP Second;

		/// <inheritdoc />
		public override FP Execute(Frame f, EntityRef e, ref AIContext aiContext)
		{
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);

			return First.Resolve(f, e, bb, null) * Second.Resolve(f, e, bb, null);
		}
	}
}

using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Takes in two <see cref="FPVector3"/>'s and sums them.
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public unsafe class FPVector3SumFunction : AIFunction<FPVector3>
	{
		public AIParamFPVector3 First;
		public AIParamFPVector3 Second;

		/// <inheritdoc />
		public override FPVector3 Execute(Frame f, EntityRef e)
		{
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			return First.Resolve(f, e, bb, null) + Second.Resolve(f, e, bb, null);
		}
	}
}
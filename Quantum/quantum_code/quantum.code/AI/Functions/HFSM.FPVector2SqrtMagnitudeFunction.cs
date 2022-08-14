using Photon.Deterministic;
using System;

namespace Quantum
{
	/// <summary>
	/// Returns a value of a specified <see cref="StatType"/> of the current entity.
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public unsafe class FPVector2SqrtMagnitudeFunction : AIFunction<FP>
	{
		public AIParamFPVector2 Vector2;

		/// <inheritdoc />
		public override FP Execute(Frame f, EntityRef e)
		{
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			
			return Vector2.Resolve(f, e, bb, null).SqrMagnitude;
		}
	}
}
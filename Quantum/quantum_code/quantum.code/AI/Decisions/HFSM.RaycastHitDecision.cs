using System;

namespace Quantum
{
	/// <summary>
	/// Does a raycast based on the input <see cref="AIParam{T}"/>'s and
	/// returns true if it hit anything.
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public unsafe class RaycastHitDecision : HFSMDecision
	{
		public AIParamFPVector3 Position;
		public AIParamFPVector3 Direction;
		public AIParamFP Distance;

		/// <inheritdoc />
		public override bool Decide(Frame f, EntityRef e)
		{
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);

			var pos = Position.Resolve(f, e, bb, null);
			var dir = Direction.Resolve(f, e, bb, null);
			var dist = Distance.Resolve(f, e, bb, null);

			return f.Physics3D.Raycast(pos, dir, dist).HasValue;
		}
	}
}
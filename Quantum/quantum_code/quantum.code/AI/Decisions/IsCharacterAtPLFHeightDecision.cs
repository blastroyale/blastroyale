using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Checks if the player is at the height where we start
	/// a Parachute Landing Fall.
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public partial class IsCharacterAtPLFHeightDecision : HFSMDecision
	{
		/// <inheritdoc />
		public override bool Decide(Frame f, EntityRef e)
		{
			var transform = f.Get<Transform3D>(e);
			var hit = f.Physics3D.Raycast(transform.Position + FPVector3.Down, FPVector3.Down,
			                              f.GameConfig.SkydivePLFHeight);
			return hit.HasValue;
		}
	}
}
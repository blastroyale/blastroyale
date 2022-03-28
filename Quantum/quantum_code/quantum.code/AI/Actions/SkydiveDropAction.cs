using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Handles player logic while doing a skydive / checks
	/// if the player is near the ground to start a PLF (Parachute Landing Fall).
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public unsafe class SkydiveDropAction : AIAction
	{
		public override void Update(Frame f, EntityRef e)
		{
			var transform = f.Unsafe.GetPointer<Transform3D>(e);

			// TODO: Use layer mask that only includes ground
			var hit = f.Physics3D.Raycast(transform->Position + FPVector3.Down, FPVector3.Down,
			                              f.GameConfig.SkydivePLFHeight);

			if (hit.HasValue)
			{
				var player = f.Unsafe.GetPointer<PlayerCharacter>(e);
				f.Events.OnLocalPlayerSkydivePLF(player->Player, e);
				f.Events.OnPlayerSkydivePLF(player->Player, e);
			}
		}
	}
}
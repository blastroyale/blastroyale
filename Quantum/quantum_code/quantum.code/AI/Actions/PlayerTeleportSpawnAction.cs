using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Teleports the player and spawns them
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public unsafe class PlayerTeleportSpawnAction : AIAction
	{
		public override void Update(Frame f, EntityRef e, ref AIContext aiContext)
		{
			var player = f.Unsafe.GetPointer<PlayerCharacter>(e);
			if (f.Unsafe.TryGetPointer<BotCharacter>(e, out var bot))
			{
				if (bot->SpawnWithPlayer)
				{
					player->Spawn(f, e);
					return;
				}
			}
			var transform = f.Unsafe.GetPointer<Transform3D>(e);
			var spawnPoint = QuantumHelpers.GetPlayerSpawnTransform(f, e);

			transform->Position = spawnPoint.Component.Position;

			player->Spawn(f, e);
		}
	}
}
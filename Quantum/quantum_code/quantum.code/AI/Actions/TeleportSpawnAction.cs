using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Teleports the player and spawns them
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public unsafe class TeleportSpawnAction : AIAction
	{
		public override void Update(Frame f, EntityRef e)
		{
			var player = f.Unsafe.GetPointer<PlayerCharacter>(e);
			var transform = f.Unsafe.GetPointer<Transform3D>(e);

			var spawnPoint = QuantumHelpers.GetPlayerSpawnTransform(f);
			transform->Position = spawnPoint.Component.Position;
			
			player->Activate(f, e);
			
			HFSMManager.TriggerEvent(f, e, Constants.SpawnedEvent);
			f.Events.OnPlayerSpawned(player->Player, e, false);
		}
	}
}
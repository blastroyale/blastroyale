using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Teleports the player to a preconfigured height, i.e.
	/// starts the parachute drop.
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public unsafe class SkydiveStartAction : AIAction
	{
		public override void Update(Frame f, EntityRef e)
		{
			var player = f.Unsafe.GetPointer<PlayerCharacter>(e);
			player->Activate(f, e);

			var transform = f.Unsafe.GetPointer<Transform3D>(e);
			transform->Position += FPVector3.Up * f.GameConfig.SkydiveDropHeight;

			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			bb->Set(f, Constants.SpeedModifierKey, f.GameConfig.SkydiveSpeedModifier);

			HFSMManager.TriggerEvent(f, e, Constants.SpawnedEvent);
			f.Events.OnLocalPlayerSkydiveDrop(player->Player, e);
			f.Events.OnPlayerSkydiveDrop(player->Player, e);
		}
	}
}
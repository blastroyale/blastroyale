using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// Spawns Killers DeathFlag on player's death
	/// </summary>
	public unsafe class DeathFlagSystem : SystemSignalsOnly, ISignalPlayerKilledPlayer
	{
		public void PlayerKilledPlayer(Frame f, PlayerRef playerDead, EntityRef entityDead, PlayerRef playerKiller,
									   EntityRef entityKiller)
		{
			if (f.Context.IsTutorial)
			{
				return;
			}
			
			var playerPosition = f.Unsafe.GetPointer<Transform2D>(entityDead)->Position;
			if (!f.Unsafe.TryGetPointer<CosmeticsHolder>(entityKiller, out var cosmetics))
			{
				return;
			}

			var equippedFlag = cosmetics->GetEquipped(f, GameIdGroup.DeathMarker);
			if (equippedFlag == null) return;
			
			Spawn(f, equippedFlag.Value, playerPosition);
		}
		
		private static void Spawn(Frame f, GameId id, FPVector2 position)
		{
			var entity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.DeathFlagPrototype.Id));

			f.Unsafe.GetPointer<DeathFlag>(entity)->ID = id;
			f.Unsafe.GetPointer<Transform2D>(entity)->Position = position;
		}

	}
}
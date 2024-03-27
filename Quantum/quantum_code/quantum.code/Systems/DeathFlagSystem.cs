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
			var playerPosition = f.Unsafe.GetPointer<Transform3D>(entityDead)->Position;
			
			var killerDeathFlag = f.Unsafe.GetPointerSingleton<GameContainer>()->PlayersData[playerKiller].DeathFlag;
			Spawn(f, killerDeathFlag, playerPosition);
		}
		
		private static void Spawn(Frame f, GameId id, FPVector3 position)
		{
			var entity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.DeathFlagPrototype.Id));

			f.Unsafe.GetPointer<DeathFlag>(entity)->ID = id;
			f.Unsafe.GetPointer<Transform3D>(entity)->Position = position;
		}

	}
}
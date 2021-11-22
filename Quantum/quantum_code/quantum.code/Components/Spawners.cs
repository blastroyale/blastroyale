using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct PlayerSpawner
	{
		/// <summary>
		/// Requests the state of the PlayerSpawner. The spawner can be either activated and available to spawn a new
		/// player in it's position or not available for it.
		/// </summary>
		public bool IsActive(Frame f)
		{
			return f.Time >= ActivationTime;
		}
	}
	
	public unsafe partial struct CollectablePlatformSpawner
	{
		/// <summary>
		/// Requests the interval time to the next collectable to be spawn
		/// </summary>
		public FP IntervalTime => SpawnCount == 0 ? InitialSpawnDelayInSec : RespawnTimeInSec;

		/// <summary>
		/// Cache collectable entity as spawned and increment spawn count
		/// </summary>
		public void MarkSpawned(EntityRef collectable)
		{
			Collectable = collectable;
			
			SpawnCount++;
		}

		/// <summary>
		/// Mark collectable entity as not spawned and set the next spawn time
		/// </summary>
		public void MarkCollected(Frame f)
		{
			Collectable = EntityRef.None;
			NextSpawnTime = f.Time + RespawnTimeInSec;
		}
	}
}
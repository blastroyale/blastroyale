using Photon.Deterministic;

namespace Quantum.Commands
{
	/// <summary>
	/// This command creates a <see cref="Collectable"/> pickup in in the given position
	/// </summary>
	public unsafe class CollectablePlatformSpawnCommand : CommandBase
	{
		public FPVector3 Position;
		public GameId Collectable;
		public uint RespawnTimeInSec;
		public uint InitialSpawnDelayInSec;
		
		/// <inheritdoc />
		public override void Serialize(BitStream stream)
		{
			var collectable = (int) Collectable;
			
			stream.Serialize(ref Position);
			stream.Serialize(ref RespawnTimeInSec);
			stream.Serialize(ref InitialSpawnDelayInSec);
			stream.Serialize(ref collectable);

			Collectable = (GameId) collectable;
		}

		/// <inheritdoc />
		internal override void Execute(Frame f, PlayerRef playerRef)
		{
			var configs = f.AssetConfigs;
			var asset = Collectable.IsInGroup(GameIdGroup.Weapon) ? configs.WeaponPlatformPrototype : configs.ConsumablePlatformPrototype;
			var entity = f.Create(f.FindAsset<EntityPrototype>(asset.Id));
			var spawner = f.Unsafe.GetPointer<CollectablePlatformSpawner>(entity);

			spawner->GameId = Collectable;
			spawner->RespawnTimeInSec = RespawnTimeInSec;
			spawner->InitialSpawnDelayInSec = InitialSpawnDelayInSec;
			
			f.Unsafe.GetPointer<Transform3D>(entity)->Position = Position;
		}
	}
}
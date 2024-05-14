using Photon.Deterministic;

namespace Quantum.Commands
{
	/// <summary>
	/// This command creates a weapon pickup in in the given position
	/// </summary>
	public unsafe class CheatWeaponSpawnCommand : CommandBase
	{
		public FPVector3 Position;
		public GameId Weapon;

		/// <inheritdoc />
		public override void Serialize(BitStream stream)
		{
			var weapon = (int) Weapon;

			stream.Serialize(ref Position);
			stream.Serialize(ref weapon);

			Weapon = (GameId) weapon;
		}

		/// <inheritdoc />
		internal override void Execute(Frame f, PlayerRef playerRef)
		{
			var config = f.WeaponConfigs.GetConfig(Weapon);
			var entity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.EquipmentPickUpPrototype.Id));

			var equipment = Equipment.Create(f, config.Id, EquipmentRarity.Common, 1);
			f.Unsafe.GetPointer<EquipmentCollectable>(entity)->Init(f, entity, Position, FPQuaternion.Identity, Position,
				equipment, EntityRef.None);
		}
	}
}
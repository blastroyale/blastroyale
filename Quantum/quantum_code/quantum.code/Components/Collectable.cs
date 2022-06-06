using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct Collectable
	{
		/// <summary>
		/// Drops a consumable of the given <paramref name="gameId"/> in the given <paramref name="position"/>
		/// </summary>
		public static void DropConsumable(Frame f, GameId gameId, FPVector3 position, int angleDropStep, bool isWeapon)
		{
			var dropPosition = GetPointOnNavMesh(f, position, angleDropStep);

			var configConsumable = f.ConsumableConfigs.GetConfig(gameId);
			var entityConsumable = f.Create(f.FindAsset<EntityPrototype>(configConsumable.AssetRef.Id));
			f.Unsafe.GetPointer<Consumable>(entityConsumable)->Init(f, entityConsumable, dropPosition,
			                                                        FPQuaternion.Identity, configConsumable);
		}

		/// <summary>
		/// Drops an equipment item (weapon / gear) from <paramref name="equipment"/> in the given <paramref name="position"/>
		/// </summary>
		public static void DropEquipment(Frame f, Equipment equipment, FPVector3 position, int angleDropStep,
		                                 PlayerRef owner = new PlayerRef())
		{
			var dropPosition = GetPointOnNavMesh(f, position, angleDropStep);

			if (equipment.IsWeapon())
			{
				var config = f.WeaponConfigs.GetConfig(equipment.GameId);
				var entity = f.Create(f.FindAsset<EntityPrototype>(config.AssetRef.Id));
				f.Unsafe.GetPointer<EquipmentCollectable>(entity)->Init(f, entity, dropPosition, FPQuaternion.Identity,
				                                                        equipment, owner);
			}

			// TODO: Implement for gear
		}

		/// <summary>
		/// Checks if the given <paramref name="playerRef"/> is collecting the collectable
		/// </summary>
		public bool IsCollecting(PlayerRef playerRef)
		{
			return CollectorsEndTime[playerRef] > FP._0;
		}

		private static FPVector3 GetPointOnNavMesh(Frame f, FPVector3 position, int angleDropStep)
		{
			var angleStep = FPVector2.Rotate(FPVector2.Left, FP.PiTimes2 * angleDropStep / 5);
			var dropPosition = (angleStep * Constants.DROP_OFFSET_RADIUS).XOY + position;

			QuantumHelpers.TryFindPosOnNavMesh(f, dropPosition, out var newPosition);
			return newPosition;
		}
	}
}
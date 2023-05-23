using System;
using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct Collectable
	{
		/// <summary>
		/// Drops a consumable of the given <paramref name="gameId"/> in the given <paramref name="position"/>
		/// </summary>
		public static void DropConsumable(Frame f, GameId gameId, FPVector3 position, int angleDropStep, bool isConsiderNavMesh, int dropAngles)
		{
			var dropPosition = GetPointOnNavMesh(f, position, angleDropStep, isConsiderNavMesh, dropAngles);

			var configConsumable = f.ConsumableConfigs.GetConfig(gameId);
			var entityConsumable = f.Create(f.FindAsset<EntityPrototype>(configConsumable.AssetRef.Id));
			f.Unsafe.GetPointer<Consumable>(entityConsumable)->Init(f, entityConsumable, dropPosition,
			                                                        FPQuaternion.Identity, ref configConsumable, EntityRef.None);
		}

		/// <summary>
		/// Drops an equipment item (weapon / gear) from <paramref name="equipment"/> in the given <paramref name="position"/>
		/// </summary>
		public static void DropEquipment(Frame f, Equipment equipment, FPVector3 position, int angleDropStep, bool isConsiderNavMesh,
										 int dropAngles, PlayerRef owner = new PlayerRef())
		{
			if (equipment.IsDefaultItem())
			{
				Log.Error($"Trying to drop a default item, skipping: {equipment.GameId}!");
				return;
			}

			var dropPosition = GetPointOnNavMesh(f, position, angleDropStep, isConsiderNavMesh, dropAngles);

			var entity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.EquipmentPickUpPrototype.Id));
			f.Unsafe.GetPointer<EquipmentCollectable>(entity)->Init(f, entity, dropPosition, FPQuaternion.Identity,
			                                                        ref equipment, EntityRef.None, owner);
		}

		/// <summary>
		/// Checks if the given <paramref name="playerRef"/> is collecting the collectable
		/// </summary>
		public bool IsCollecting(PlayerRef playerRef)
		{
			return CollectorsEndTime[playerRef] > FP._0;
		}

		private static FPVector3 GetPointOnNavMesh(Frame f, FPVector3 position, int angleDropStep, bool isConsiderNavMesh, int dropAngles)
		{
			var angleLevel = (angleDropStep / dropAngles);
			var angleGranularity = FP.PiTimes2 / dropAngles;
			var angleStep = FPVector2.Rotate(FPVector2.Left,
			                                 (angleGranularity * angleDropStep) +
			                                 (angleLevel % 2) * angleGranularity / 2);
			var dropPosition = (angleStep * Constants.DROP_OFFSET_RADIUS * (angleLevel + 1)).XOY + position;
			
			if (!isConsiderNavMesh || f.NavMesh.Contains(dropPosition, NavMeshRegionMask.Default, true))
			{
				return dropPosition;
			}
			
			QuantumHelpers.TryFindPosOnNavMesh(f, dropPosition, Constants.DROP_OFFSET_RADIUS, out var newPosition);
			
			return newPosition;
		}
	}
}
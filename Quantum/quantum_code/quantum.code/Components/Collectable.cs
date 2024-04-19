using System;
using System.Linq;
using Photon.Deterministic;
using Quantum.Collections;

namespace Quantum
{
	public unsafe partial struct Collectable
	{
		/// <summary>
		/// Drops a consumable of the given <paramref name="gameId"/> in the given <paramref name="position"/>
		/// </summary>
		public static void DropConsumable(Frame f, GameId gameId, FPVector3 position, int angleDropStep, bool isConsiderNavMesh, int dropAngles)
		{
			var dropPosition = dropAngles == 1 ? position : GetPointOnNavMesh(f, position, angleDropStep, isConsiderNavMesh, dropAngles);

			// Setting Y to a fixed value to avoid consumable being too low or too high 
			if (f.Context.GameModeConfig.Id != "Tutorial") // TODO: Remove this after we make a new flat tutorial level
			{
				dropPosition.Y = Constants.DROP_Y_POSITION;
			}

			var configConsumable = f.ConsumableConfigs.GetConfig(gameId);
			if (configConsumable.ConsumableType == ConsumableType.GameItem && !f.RuntimeConfig.AllowedRewards.Contains((int)gameId)) return;
			var entityConsumable = f.Create(f.FindAsset<EntityPrototype>(configConsumable.AssetRef.Id));

			f.Unsafe.GetPointer<Consumable>(entityConsumable)->Init(f, entityConsumable, dropPosition,
				FPQuaternion.Identity, ref configConsumable, EntityRef.None, position);
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

			var dropPosition = dropAngles == 1 ? position : GetPointOnNavMesh(f, position, angleDropStep, isConsiderNavMesh, dropAngles);

			// Setting Y to a fixed value to avoid weapon being too low or too high 
			if (f.Context.GameModeConfig.Id != "Tutorial") // TODO: Remove this after we make a new flat tutorial level
			{
				dropPosition.Y = Constants.DROP_Y_POSITION;
			}

			var entity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.EquipmentPickUpPrototype.Id));
			f.Unsafe.GetPointer<EquipmentCollectable>(entity)->Init(f, entity, dropPosition, FPQuaternion.Identity, position,
				ref equipment, EntityRef.None, owner);
		}

		/// <summary>
		/// Checks if the given <paramref name="possibleCollector"/> is collecting the collectable
		/// </summary>
		public bool IsCollecting(Frame f, EntityRef possibleCollector)
		{
			if (TryGetCollectingEndTime(f, possibleCollector, out var endTime))
			{
				return endTime > FP._0;
			}

			return false;
		}

		public bool TryGetCollectingEndTime(Frame f, EntityRef collectorRef, out FP endTime)
		{
			var dict = f.ResolveDictionary(CollectorsEndTime);
			if (dict.TryGetValue(collectorRef, out endTime))
			{
				return endTime > FP._0;
			}

			return false;
		}

		public void StartCollecting(Frame f, EntityRef collector, FP collectTime)
		{
			var dict = f.ResolveDictionary(CollectorsEndTime);
			dict[collector] = f.Time + collectTime;
		}

		public void StopCollecting(Frame f, EntityRef collector)
		{
			if (CollectorsEndTime.Ptr == Ptr.Null)
			{
				return;
			}

			var dict = f.ResolveDictionary(CollectorsEndTime);
			dict.Remove(collector);
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
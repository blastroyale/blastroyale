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
		public static void DropConsumable(Frame f, GameId gameId, FPVector2 position, int angleDropStep, bool isConsiderNavMesh, int dropAngles)
		{
			DropConsumable(f, gameId, position, angleDropStep, isConsiderNavMesh, dropAngles, Constants.DROP_OFFSET_RADIUS);
		}

		/// <summary>
		/// Drops a consumable of the given <paramref name="gameId"/> in the given <paramref name="position"/>
		/// </summary>
		public static void DropConsumable(Frame f, GameId gameId, FPVector2 position, int angleDropStep, bool isConsiderNavMesh, int dropAngles, FP offsetRadius)
		{
			var dropPosition = dropAngles == 1
				? position
				: GetPointOnNavMesh(f, position, angleDropStep, isConsiderNavMesh, dropAngles, offsetRadius);

			var configConsumable = f.ConsumableConfigs.GetConfig(gameId);
			var entityConsumable = f.Create(f.FindAsset<EntityPrototype>(configConsumable.AssetRef.Id));

			f.Unsafe.GetPointer<Consumable>(entityConsumable)->Init(f, entityConsumable, dropPosition,
				0, configConsumable, EntityRef.None, position);
		}

		/// <summary>
		/// Drops an equipment item (weapon / gear) from <paramref name="equipment"/> in the given <paramref name="position"/>
		/// </summary>
		public static void DropEquipment(Frame f, in Equipment equipment, FPVector2 position, int angleDropStep, bool isConsiderNavMesh,
										 int dropAngles, PlayerRef owner = new PlayerRef())
		{
			if (equipment.IsDefaultItem())
			{
				Log.Error($"Trying to drop a default item, skipping: {equipment.GameId}!");
				return;
			}

			var dropPosition = dropAngles == 1 ? position : GetPointOnNavMesh(f, position, angleDropStep, isConsiderNavMesh, dropAngles, Constants.DROP_OFFSET_RADIUS);

			var entity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.EquipmentPickUpPrototype.Id));
			f.Unsafe.GetPointer<EquipmentCollectable>(entity)->Init(f, entity, dropPosition, 0, position,
				equipment, EntityRef.None, owner);
		}

		/// <summary>
		/// Checks if the given <paramref name="possibleCollector"/> is collecting the collectable
		/// </summary>
		public bool IsCollecting(Frame f, EntityRef collectable, EntityRef possibleCollector)
		{
			if (TryGetCollectingEndTime(f, collectable, possibleCollector, out var endTime))
			{
				return endTime > FP._0;
			}

			return false;
		}

		public bool TryGetCollectingEndTime(Frame f, EntityRef collectable, EntityRef collectorRef, out FP endTime)
		{
			var dict = CollectorsEndTime(f, collectable);
			if (dict.TryGetValue(collectorRef, out endTime))
			{
				return endTime > FP._0;
			}
			return false;
		}
		
		public bool HasCollector(Frame f, EntityRef collectable, params EntityRef [] entities)
		{
			var dict = CollectorsEndTime(f, collectable);
			foreach (var e in entities)
			{
				if (dict.ContainsKey(e)) return true;
			}
			return false;
		}

		public QDictionary<EntityRef, FP> CollectorsEndTime(Frame f, EntityRef e)
		{
			if (!f.TryGet<CollectableTime>(e, out var collectableTime))
			{
				f.Add<CollectableTime>(e);
				collectableTime = f.Get<CollectableTime>(e);
			}

			return f.ResolveDictionary(collectableTime.CollectorsEndTime);
		}

		public void StartCollecting(Frame f, EntityRef collectable, EntityRef collector, FP collectTime)
		{
			var dict = CollectorsEndTime(f, collectable);
			dict[collector] = f.Time + collectTime;
		}

		public void StopCollecting(Frame f, EntityRef collectable, EntityRef collector)
		{
			if (!f.Has<CollectableTime>(collectable))
			{
				return;
			}
			var collectors = CollectorsEndTime(f, collectable);
			collectors.Remove(collector);
			if (collectors.Count == 0)
			{
				f.Remove<CollectableTime>(collectable);
			}
		}

		private static FPVector2 GetPointOnNavMesh(Frame f, FPVector2 position, int angleDropStep, bool isConsiderNavMesh, int dropAngles, FP radiusOffset)
		{
			var angleLevel = (angleDropStep / dropAngles);
			var angleGranularity = FP.PiTimes2 / dropAngles;
			var angleStep = FPVector2.Rotate(FPVector2.Left,
				(angleGranularity * angleDropStep) +
				(angleLevel % 2) * angleGranularity / 2);
			var dropPosition = (angleStep * radiusOffset * (angleLevel + 1)) + position;

			if (!isConsiderNavMesh || f.NavMesh.Contains(dropPosition.XOY, NavMeshRegionMask.Default, true))
			{
				return dropPosition;
			}

			QuantumHelpers.TryFindPosOnNavMesh(f, dropPosition, Constants.DROP_OFFSET_RADIUS, out var newPosition);

			return newPosition;
		}
	}
}
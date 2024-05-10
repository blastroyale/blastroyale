using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;

namespace Quantum.Systems
{
	public unsafe class ChestContentsGenerationContext
	{
		public Chest* Chest;
		public QuantumChestConfig Config;
	}
	
	public unsafe class ChestSystem : SystemSignalsOnly, ISignalOnComponentAdded<Chest>
	{
		

		public static EntityRef SpawnChest(Frame f, GameId chestId, FPVector3 position)
		{
			var config = f.ChestConfigs.GetConfig(chestId);
			var e = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.ChestPrototype.Id));
			var chest = f.Unsafe.GetPointer<Chest>(e);
			var transform = f.Unsafe.GetPointer<Transform3D>(e);
			chest->Id = config.Id;
			chest->ChestType = config.ChestType;
			chest->CollectTime = config.CollectTime;
			transform->Position = position;
			transform->Rotation = FPQuaternion.Identity;
			f.Add(e,
				new Collectable
				{
					GameId = chestId, PickupRadius = config.CollectableChestPickupRadius,
					AllowedToPickupTime = f.Time + Constants.CONSUMABLE_POPOUT_DURATION
				});
			var collider = f.Unsafe.GetPointer<PhysicsCollider3D>(e);
			collider->Shape.Sphere.Radius = config.CollectableChestPickupRadius; 
			return e;
		}
		
		/// <summary>
		/// Generates chest contents given a chest and its configs.
		/// </summary>
		public static List<SimulationItem> GenerateItems(Frame f, EntityRef chestEntity)
		{
			var chest = f.Unsafe.GetPointer<Chest>(chestEntity);
			var ctx = new ChestContentsGenerationContext()
			{
				Chest = chest,
				Config = f.ChestConfigs.GetConfig(chest->ChestType),
			};
			var items = new List<SimulationItem>();
			
			RollDropTables(f, ref ctx, items);
			
			if (f.Context.TryGetMutatorByType(MutatorType.SpecialsMayhem, out _) && !f.Context.TryGetMutatorByType(MutatorType.DoNotDropSpecials, out _))
			{
				items.Add(SimulationItem.CreateSimple(Special.GetRandomSpecialId(f)));
			}
			return items;
		}

		private static void RollDropTables(Frame f, ref ChestContentsGenerationContext ctx, List<SimulationItem> items)
		{
			var isHammerTimeMutator = f.Context.TryGetMutatorByType(MutatorType.HammerTime, out _);
			var dontDropSpecialsMutator = f.Context.TryGetMutatorByType(MutatorType.DoNotDropSpecials, out _);
			var gameContainer = f.Unsafe.GetPointerSingleton<GameContainer>();
			foreach (var droptable in ctx.Config.DropTables)
			{
				if (droptable.Amount == 0) continue;
				if (f.RNG->Next() > droptable.Chance) continue;
				var count = droptable.Amount;
				var list = new WeightedList<SimulationItemConfig>(f);
				list.Add(droptable.Pool.Select(s => new WeightItem<SimulationItemConfig>(s.ItemConfig, s.Weight)).ToList());
				while (list.Count > 0 && count > 0)
				{
					var drop = list.Next();
					var item = SimulationItem.FromConfig(drop);

					if (item.ItemType == ItemType.Simple
						&& item.SimpleItem->Id.IsInGroup(GameIdGroup.Special)
						&& dontDropSpecialsMutator)
					{
						count--;
						continue;
					}

					if (item.ItemType == ItemType.Equipment)
					{
						if (isHammerTimeMutator)
						{
							count--;
							continue;
						}
						else if (f.Context.TryGetWeaponLimiterMutator(out var forcedWeaponId))
						{
							item.EquipmentItem->GameId = forcedWeaponId;
						}
						else if (item.EquipmentItem->GameId == GameId.Any)
						{
							item.EquipmentItem->GameId = gameContainer->GenerateNextWeapon(f).GameId;
						}
					}
					items.Add(item);
					count--;
				}
			}
		}

		public void OnAdded(Frame f, EntityRef entity, Chest* component)
		{
			var config = f.ChestConfigs.GetConfig(component->Id);
			if (config.AutoOpen)
			{
				component->Open(f, entity, EntityRef.None, PlayerRef.None);
			}
		}
	}
}
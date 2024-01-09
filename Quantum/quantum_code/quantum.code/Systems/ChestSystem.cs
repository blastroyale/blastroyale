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
			GenerateEquipment(f, ctx, items);
			RollDropTables(f, ctx, items);
			if (f.Context.TryGetMutatorByType(MutatorType.SpecialsMayhem, out _))
			{
				items.Add(SimulationItem.CreateSimple(Special.GetRandomSpecialId(f)));
			}
			return items;
		}

		private static void RollDropTables(Frame f, ChestContentsGenerationContext ctx, List<SimulationItem> items)
		{
			foreach (var droptable in ctx.Config.DropTables)
			{
				if (droptable.Amount == 0) continue;
				if (f.RNG->Next() > droptable.Chance) continue;
				var count = droptable.Amount;
				var list = new WeightedList<GameId>(f);
				list.Add(droptable.Pool.Select(s => new WeightItem<GameId>(s.Id, s.Weight)).ToList());
				while (list.Count > 0 && count > 0)
				{
					var drop = SimulationItem.CreateSimple(list.Next());
					items.Add(drop);
					count--;
				}
			}
		}
		
		private static void GenerateEquipment(Frame f, ChestContentsGenerationContext ctx, List<SimulationItem> items)
		{
			var gameContainer = f.Unsafe.GetPointerSingleton<GameContainer>();
			if (ctx.Config.GoldenGunChance > 0 && f.RNG->Next() < ctx.Config.GoldenGunChance)
			{
				var golden = gameContainer->GenerateNextWeapon(f);
				golden.Material = EquipmentMaterial.Golden;
				golden.Rarity = EquipmentRarity.Legendary;
				items.Add(SimulationItem.CreateEquipment(golden));
				return;
			}
			
			foreach (var (chance, count) in ctx.Config.RandomEquipment)
			{
				if (f.RNG->Next() > chance) continue;
				for (uint i = 0; i < count; i++)
				{
					var weapon = gameContainer->GenerateNextWeapon(f);
					weapon.Material = EquipmentMaterial.Steel;
					items.Add(SimulationItem.CreateEquipment(weapon));
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
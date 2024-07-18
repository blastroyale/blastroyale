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
					GameId = chestId
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

			RollDropTables(f, ctx, items);

			DropPlace place;
			switch (chest->ChestType)
			{
				case ChestType.Legendary:
					place = DropPlace.Airdrop;
					break;
				case ChestType.Equipment:
					place = DropPlace.Chest;
					break;
				default:
					return items;
			}

			foreach (var metaItemDropOverwrite in f.RuntimeConfig.MatchConfigs.MetaItemDropOverwrites
						 .Where(d => d.Place == place))
			{
				var rnd = f.RNG->Next();
				if (rnd <= metaItemDropOverwrite.DropRate)
				{
					var amount = f.RNG->Next(metaItemDropOverwrite.MinDropAmount, metaItemDropOverwrite.MaxDropAmount);
					for (var i = 0; i < amount; i++)
					{
						items.Add(SimulationItem.CreateSimple(metaItemDropOverwrite.Id));
					}
				}
			}

			return items;
		}

		private static void RollDropTables(Frame f, in ChestContentsGenerationContext ctx, List<SimulationItem> items)
		{
			var isHammerTimeMutator = f.Context.Mutators.HasFlagFast(Mutator.HammerTime);
			var dontDropSpecialsMutator = f.Context.Mutators.HasFlagFast(Mutator.DoNotDropSpecials);
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
						else if (item.EquipmentItem->GameId == GameId.Any || f.RuntimeConfig.MatchConfigs.WeaponsSelectionOverwrite.Length > 0)
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

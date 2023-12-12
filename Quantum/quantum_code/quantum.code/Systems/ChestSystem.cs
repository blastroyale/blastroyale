using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;

namespace Quantum.Systems
{
	public unsafe class ChestContentsGenerationContext
	{
		public EntityRef PlayerEntity;
		public PlayerCharacter* Player;
		public Chest* Chest;
		public bool IsBot;
		public QuantumChestConfig Config;
	}
	
	public unsafe class ChestSystem : SystemSignalsOnly
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
		public static List<SimulationItem> GenerateItemsForPlayer(Frame f, EntityRef chestEntity, EntityRef playerEntity)
		{
			var chest = f.Unsafe.GetPointer<Chest>(chestEntity);
			var ctx = new ChestContentsGenerationContext()
			{
				Player = f.Unsafe.GetPointer<PlayerCharacter>(playerEntity),
				Chest = chest,
				Config = f.ChestConfigs.GetConfig(chest->ChestType),
				IsBot = f.Has<BotCharacter>(playerEntity),
				PlayerEntity = playerEntity
			};
			var items = new List<SimulationItem>();
			GenerateEquipment(f, ctx, items);
			AddSpecialDrops(f, ctx, items);
			GenerateConsumables(f, ctx, items);
			return items;
		}
		
				/// <summary>
		/// Add special abilities drops based on the chest config
		/// </summary>
		private static bool AddSpecialDrops(Frame f, ChestContentsGenerationContext ctx, List<SimulationItem> drops)
		{
			if (f.Context.TryGetMutatorByType(MutatorType.SpecialsMayhem, out _))
			{
				drops.Add(SimulationItem.CreateSimple(Special.GetRandomSpecialId(f)));
			}
			if (ctx.Config.Specials.Amount == 0) return false;
			if (f.RNG->Next() > ctx.Config.Specials.Chance) return false;
			var count = ctx.Config.Specials.Amount;
			var list = new WeightedList<GameId>(f);
			list.Add(ctx.Config.Specials.Pool.Select(s => new WeightItem<GameId>(s.Id, s.Weight)).ToList());
			while (list.Count > 0 && count > 0)
			{
				var drop = SimulationItem.CreateSimple(list.Next());
				drops.Add(drop);
				count--;
			}
			return true;
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
			
			var loadoutWeapon = ctx.IsBot ? Equipment.None : ctx.Player->GetLoadoutWeapon(f);
			var hasLoadoutWeapon = loadoutWeapon.IsValid() && !loadoutWeapon.IsDefaultItem() &&
				!f.Context.TryGetWeaponLimiterMutator(out _);
			foreach (var (chance, count) in ctx.Config.RandomEquipment)
			{
				if (f.RNG->Next() > chance) continue;
				for (uint i = 0; i < count; i++)
				{
					// Do not drop guns if we have HammerTime mutator
					if (!f.Context.TryGetMutatorByType(MutatorType.HammerTime, out _))
					{
						// Dropping player loadout weapon if have not already 
						if (ctx.Player->WeaponSlots[1].Weapon.GameId == GameId.Random &&
							!ctx.Player->HasDroppedItemForSlot(Constants.GEAR_INDEX_WEAPON) &&
							!gameContainer->DropPool.IsPoolEmpty)
						{
							var weaponItem = hasLoadoutWeapon ? loadoutWeapon : gameContainer->GenerateNextWeapon(f);
							weaponItem.Material = EquipmentMaterial.Steel;
							items.Add(SimulationItem.CreateEquipment(weaponItem));
							ctx.Player->SetDroppedLoadoutItem(&weaponItem);
							continue;
						}
					}

					if (!f.Context.TryGetMutatorByType(MutatorType.ForceLevelPlayingField, out _) &&
						f.Context.MapConfig.LootingVersion != 2)
					{
						var drop = GetNextLoadoutGearItem(f, ctx.Player, ctx.Player->GetLoadoutGear(f));
						drop.Material = EquipmentMaterial.Steel;
						if (drop.GameId != GameId.Random && drop.IsValid())
						{
							ctx.Player->SetDroppedLoadoutItem(&drop);
							items.Add(SimulationItem.CreateEquipment(drop));
						}
					}
				}
			}
		}

		private static void GenerateConsumables(Frame f, ChestContentsGenerationContext ctx, List<SimulationItem> items)
		{
			var potentialConsumables = new List<GameId>();
			var stats = f.Get<Stats>(ctx.PlayerEntity);
			var ammoFilled = stats.CurrentAmmoPercent;
			var shieldFilled = stats.CurrentShield / stats.GetStatData(StatType.Shield).StatValue;
			var healthFilled = stats.CurrentHealth / stats.GetStatData(StatType.Health).StatValue;
			foreach (var (chance, count) in ctx.Config.SmallConsumable)
			{
				if (f.RNG->Next() > chance) continue;
				for (uint i = 0; i < count + (f.Context.MapConfig.LootingVersion == 2 ? 1 : 0); i++)
				{
					potentialConsumables.Clear();
					if (healthFilled < FP._1)
					{
						potentialConsumables.Add(GameId.Health);
					}

					if (shieldFilled < FP._1) potentialConsumables.Add(GameId.ShieldSmall);
					if (ammoFilled < FP._1 || potentialConsumables.Count == 0)
					{
						potentialConsumables.Add(GameId.AmmoSmall);
					}

					var drop = potentialConsumables[f.RNG->Next(0, potentialConsumables.Count)];
					if (drop == GameId.Health)
					{
						healthFilled += f.ConsumableConfigs.GetConfig(drop).Amount.Get(f) /
							stats.GetStatData(StatType.Health).StatValue;
					}
					else if (drop == GameId.ShieldSmall)
					{
						shieldFilled += f.ConsumableConfigs.GetConfig(drop).Amount.Get(f) /
							stats.GetStatData(StatType.Shield).StatValue;
					}
					else if (drop == GameId.AmmoSmall)
					{
						ammoFilled += f.ConsumableConfigs.GetConfig(drop).Amount.Get(f);
					}

					items.Add(SimulationItem.CreateSimple(drop));
				}
			}
		}
		
		
		private static Equipment GetNextLoadoutGearItem(Frame f, PlayerCharacter* playerCharacter, Equipment[] loadout)
		{
			var flags = playerCharacter->DroppedLoadoutFlags;
			// Set bits of loadout items we have
			int loadoutFlags = 0;
			if (loadout != null)
			{
				foreach (var e in loadout)
				{
					loadoutFlags |= 1 << (PlayerCharacter.GetGearSlot(&e) + 1);
				}
			}

			// Flip it around so only missing gear bits are set
			loadoutFlags = ~loadoutFlags & ~(~0 << (Constants.MAX_GEAR + 1));

			// Trick flags into thinking we have dropped the items we don't currently have in the loadout
			flags |= loadoutFlags;

			// Flip it around so only missing gear bits are set
			flags = ~flags & ~(~0 << (Constants.MAX_GEAR + 1));

			int bitCount = BitUtil.CountSetBits(flags);
			if (bitCount == 0)
			{
				return Equipment.None;
			}

			var index = (int) BitUtil.GetNthBitIndex((ulong) flags, (uint) f.RNG->Next(0, bitCount));
			var group = PlayerCharacter.GetEquipmentGroupForSlot(index - 1);

			foreach (var e in loadout)
			{
				if (e.GameId.IsInGroup(group))
				{
					return e;
				}
			}

			throw new NotSupportedException($"Could not find random gear item with index({index}), group{group}");
		}
	}
}
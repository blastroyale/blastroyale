using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;
using Quantum.Collections;

namespace Quantum
{
	public unsafe partial struct Chest
	{
		/// <summary>
		/// Initializes this Chest with all the necessary data
		/// </summary>
		internal void Init(Frame f, EntityRef e, FPVector3 position, FPQuaternion rotation,
						   ref QuantumChestConfig config, bool makeCollectable = true)
		{
			var transform = f.Unsafe.GetPointer<Transform3D>(e);

			Id = config.Id;
			ChestType = config.ChestType;
			CollectTime = config.CollectTime;

			transform->Position = position;
			transform->Rotation = rotation;

			if (makeCollectable)
			{
				MakeCollectable(f, e, config.CollectableChestPickupRadius);
			}
		}

		/// <summary>
		/// Adds a <see cref="Collectable"/> component to <paramref name="e"/>.
		/// </summary>
		internal void MakeCollectable(Frame f, EntityRef e, FP collectableChestPickupRadius)
		{
			f.Add(e,
				new Collectable
				{
					GameId = Id, PickupRadius = collectableChestPickupRadius,
					AllowedToPickupTime = f.Time + Constants.CONSUMABLE_POPOUT_DURATION
				});

			var collider = f.Unsafe.GetPointer<PhysicsCollider3D>(e);
			collider->Shape.Sphere.Radius = collectableChestPickupRadius;
		}

		public void Open(Frame f, EntityRef e, EntityRef playerEntity, PlayerRef playerRef)
		{
			var anglesToDrop = 0;
			var chestPosition = f.Get<Transform3D>(e).Position;
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(playerEntity);
			var isBot = f.Has<BotCharacter>(playerEntity);
			var loadoutWeapon = isBot ? Equipment.None : playerCharacter->GetLoadoutWeapon(f);
			var hasLoadoutWeapon = loadoutWeapon.IsValid() && !loadoutWeapon.IsDefaultItem();
			var minimumRarity = hasLoadoutWeapon ? loadoutWeapon.Rarity : EquipmentRarity.Common;
			var config = f.ChestConfigs.GetConfig(ChestType);
			var stats = f.Get<Stats>(playerEntity);

			// As max ammo is a very high value, we treat fraction of ammo as full ammo, but drop ammo as a fallback as well
			var ammoFilled = stats.CurrentAmmoPercent * Constants.LOW_AMMO_THRESHOLD_TO_DROP_MORE;

			var shieldFilled = stats.CurrentShield / stats.GetStatData(StatType.Shield).StatValue;
			var healthFilled = stats.CurrentHealth / stats.GetStatData(StatType.Health).StatValue;
			var chestItems = new List<ChestItemDropped>();
			var gameContainer = f.Unsafe.GetPointerSingleton<GameContainer>();

			var equipmentToDrop = new Dictionary<Equipment, int>();
			var consumablesToDrop = new List<GameId>();

			//if we have an override component to change what spawns within the chest
			if (f.Unsafe.TryGetPointer<ChestOverride>(e, out var overrideComponent) &&
				overrideComponent->ContentsOverride != new QList<GameId>())
			{
				foreach (var item in f.ResolveList(overrideComponent->ContentsOverride))
				{
					if (item.IsInGroup(GameIdGroup.Equipment))
					{
						var equipment = Equipment.Create(f, item, overrideComponent->Rarity, 1);
						equipmentToDrop.Add(equipment, -1);
						anglesToDrop++;
						// Collectable.DropEquipment(f, equipment, chestPosition, angleStep++, false);
					}
					else if (item.IsInGroup(GameIdGroup.Consumable))
					{
						consumablesToDrop.Add(item);
						anglesToDrop++;
						// Collectable.DropConsumable(f, item, chestPosition, angleStep++, false, false);
					}
					else
					{
						continue;
					}

					chestItems.Add(new ChestItemDropped()
					{
						ChestType = config.Id,
						ChestPosition = chestPosition,
						Player = playerRef,
						PlayerEntity = playerEntity,
						ItemType = item,
						Amount = 1,
						AngleStepAroundChest = anglesToDrop
					});
				}
			}
			else
			{
				DropPowerUps(f, playerEntity, ref config, playerCharacter, gameContainer, minimumRarity, &loadoutWeapon,
					chestPosition, ref anglesToDrop, chestItems, chestItems.Count, ref equipmentToDrop,
					ref consumablesToDrop);
				DropSmallConsumable(f, playerEntity, playerRef, ref config, ref ammoFilled, ref shieldFilled,
					ref healthFilled,
					chestPosition, ref anglesToDrop, chestItems, ref equipmentToDrop, ref consumablesToDrop);
			}

			if (f.Context.TryGetMutatorByType(MutatorType.SpecialsMayhem, out _))
			{
				consumablesToDrop.Add(Special.GetRandomSpecialId(f));
				anglesToDrop++;
			}

			var step = 0;
			foreach (var drop in equipmentToDrop)
			{
				if (drop.Value != -1)
				{
					Collectable.DropEquipment(f, drop.Key, chestPosition, step, true, anglesToDrop);
				}
				else
				{
					Collectable.DropEquipment(f, drop.Key, chestPosition, step, true, anglesToDrop, drop.Value);
				}

				step++;
			}

			foreach (var drop in consumablesToDrop)
			{
				Collectable.DropConsumable(f, drop, chestPosition, step, true, anglesToDrop);
				step++;
			}

			f.Signals.ChestOpened(config.Id, chestPosition, playerRef, playerEntity);
			f.Events.OnChestOpened(config.Id, chestPosition, playerRef, playerEntity, chestItems);
		}

		private void DropSmallConsumable(Frame f, EntityRef playerEntity, PlayerRef playerRef,
										 ref QuantumChestConfig config, ref FP ammoFilled, ref FP shieldFilled,
										 ref FP healthFilled,
										 FPVector3 chestPosition, ref int angleStep, List<ChestItemDropped> chestItems,
										 ref Dictionary<Equipment, int> equipmentToDrop,
										 ref List<GameId> consumablesToDrop)
		{
			var stats = f.Get<Stats>(playerEntity);
			foreach (var (chance, count) in config.SmallConsumable)
			{
				if (f.RNG->Next() > chance)
				{
					continue;
				}

				for (uint i = 0; i < count; i++)
				{
					var drop = GameId.Random;
					if (healthFilled < ammoFilled && healthFilled < shieldFilled &&
						ChestType != ChestType.Equipment) //health
					{
						drop = GameId.Health;
						healthFilled += f.ConsumableConfigs.GetConfig(drop).Amount.Get(f) /
							stats.GetStatData(StatType.Health).StatValue;
					}
					else if ((ammoFilled < healthFilled && ammoFilled < shieldFilled) ||
							 ChestType == ChestType.Equipment) //ammo
					{
						drop = GameId.AmmoSmall;
						ammoFilled += f.ConsumableConfigs.GetConfig(drop).Amount.Get(f);
					}
					else if (shieldFilled < healthFilled && shieldFilled < ammoFilled &&
							 ChestType != ChestType.Equipment) //shield
					{
						drop = GameId.ShieldSmall;
						shieldFilled += f.ConsumableConfigs.GetConfig(drop).Amount.Get(f) /
							stats.GetStatData(StatType.Shield).StatValue;
					}
					else
					{
						// Ammo is a fallback drop
						drop = GameId.AmmoSmall;
					}

					consumablesToDrop.Add(drop);
					angleStep++;

					chestItems.Add(new ChestItemDropped()
					{
						ChestType = config.Id,
						ChestPosition = chestPosition,
						Player = playerRef,
						PlayerEntity = playerEntity,
						ItemType = drop,
						Amount = 1,
						AngleStepAroundChest = angleStep
					});
				}
			}
		}

		private void DropPowerUps(Frame f, EntityRef playerEntity, ref QuantumChestConfig config,
								  PlayerCharacter* playerCharacter,
								  GameContainer* gameContainer, EquipmentRarity minimumRarity, Equipment* loadoutWeapon,
								  FPVector3 chestPosition, ref int angleStep, List<ChestItemDropped> chestItems,
								  int skipDropNumber,
								  ref Dictionary<Equipment, int> equipmentToDrop, ref List<GameId> consumablesToDrop)
		{
			var playerRef = playerCharacter->Player;
			var hasLoadoutWeapon = loadoutWeapon->IsValid() &&
				!loadoutWeapon->IsDefaultItem() &&
				!f.Context.TryGetWeaponLimiterMutator(out _);

			// In case we are giving equipment to a bot - we gather a random loadout based on LoadoutGearNumber of a bot
			var botLoadout = new List<Equipment>();
			if (f.Unsafe.TryGetPointer<BotCharacter>(playerEntity, out var botCharacter) &&
				botCharacter->LoadoutGearNumber > 0)
			{
				var gearRarity = botCharacter->LoadoutRarity;
				var helmetsList = new List<GameId>(GameIdGroup.Helmet.GetIds());
				botLoadout.Add(Equipment.Create(f, helmetsList[f.RNG->Next(0, helmetsList.Count)], gearRarity, 1));
				if (botCharacter->LoadoutGearNumber > 1)
				{
					var shieldsList = new List<GameId>(GameIdGroup.Shield.GetIds());
					botLoadout.Add(Equipment.Create(f, shieldsList[f.RNG->Next(0, shieldsList.Count)], gearRarity, 1));
				}

				if (botCharacter->LoadoutGearNumber > 2)
				{
					var armorsList = new List<GameId>(GameIdGroup.Armor.GetIds());
					botLoadout.Add(Equipment.Create(f, armorsList[f.RNG->Next(0, armorsList.Count)], gearRarity, 1));
				}

				if (botCharacter->LoadoutGearNumber > 3)
				{
					var amuletsList = new List<GameId>(GameIdGroup.Amulet.GetIds());
					botLoadout.Add(Equipment.Create(f, amuletsList[f.RNG->Next(0, amuletsList.Count)], gearRarity, 1));
				}
			}

			foreach (var (chance, count) in config.RandomEquipment)
			{
				if (f.RNG->Next() > chance)
				{
					continue;
				}

				for (uint i = 0; i < count; i++)
				{
					// Do not drop guns if we have HammerTime mutator
					if (!f.Context.TryGetMutatorByType(MutatorType.HammerTime, out _))
					{
						//only drop your loadout weaoon if you are getting dropped an equipment
						// Empty primary slot and hasn't ever dropped a weapon => drop the one from loadout or a random one
						// Empty primary slot and we dropped a weapon once => skip dropping a weapon here
						// Busy primary slot => skip dropping a weapon here
						// There are items in the pool to drop
						if (playerCharacter->WeaponSlots[1].Weapon.GameId == GameId.Random &&
							!playerCharacter->HasDroppedItemForSlot(Constants.GEAR_INDEX_WEAPON) &&
							!gameContainer->DropPool.IsPoolEmpty)
						{
							var weaponItem = hasLoadoutWeapon ? *loadoutWeapon : gameContainer->GenerateNextWeapon(f);

							ModifyEquipmentRarity(f, ref weaponItem, gameContainer->DropPool.AverageRarity,
								gameContainer->DropPool.AverageRarity);

							equipmentToDrop.Add(weaponItem, -1);
							angleStep++;

							playerCharacter->SetDroppedLoadoutItem(&weaponItem);
							skipDropNumber++;
							chestItems.Add(new ChestItemDropped()
							{
								ChestType = config.Id,
								ChestPosition = chestPosition,
								Player = playerRef,
								PlayerEntity = playerEntity,
								ItemType = weaponItem.GameId,
								Amount = 1,
								AngleStepAroundChest = angleStep
							});
						}
					}

					// If we dropped equipment before this method then we count those items and skip the equal amount of drops here
					if (skipDropNumber > 0)
					{
						skipDropNumber--;
						continue;
					}

					// Second, we drop equipment from their loadout if it is all valid and we haven't dropped them all already
					// NOTE: Level Playing Field mutator prevents gear from dropping
					if (!f.Context.TryGetMutatorByType(MutatorType.ForceLevelPlayingField, out _))
					{
						Equipment drop;
						if (!f.Has<BotCharacter>(playerEntity))
						{
							drop = GetNextLoadoutGearItem(f, playerCharacter, playerCharacter->GetLoadoutGear(f));
						}
						else
						{
							drop = GetNextLoadoutGearItem(f, playerCharacter, botLoadout.ToArray());
						}

						if (drop.GameId != GameId.Random && drop.IsValid())
						{
							playerCharacter->SetDroppedLoadoutItem(&drop);

							equipmentToDrop.Add(drop, playerRef);
							angleStep++;

							chestItems.Add(new ChestItemDropped
							{
								ChestType = config.Id,
								ChestPosition = chestPosition,
								Player = playerCharacter->Player,
								PlayerEntity = playerEntity,
								ItemType = drop.GameId,
								Amount = 1,
								AngleStepAroundChest = angleStep
							});

							continue;
						}
					}

					// If we dropped all equipment from loadout, then we drop energy cubes
					if (QuantumFeatureFlags.ENERGY_CUBES_REPLACE_SPECIALS)
					{
						consumablesToDrop.Add(GameId.EnergyCubeLarge);
						angleStep++;
					}
					else
					{
						consumablesToDrop.Add(Special.GetRandomSpecialId(f));
						angleStep++;
					}

					chestItems.Add(new ChestItemDropped
					{
						ChestType = config.Id,
						ChestPosition = chestPosition,
						Player = playerCharacter->Player,
						PlayerEntity = playerEntity,
						ItemType = GameId.EnergyCubeLarge,
						Amount = 1,
						AngleStepAroundChest = angleStep
					});
				}
			}
		}

		private void ModifyEquipmentRarity(Frame f, ref Equipment equipment, EquipmentRarity minimumRarity,
										   EquipmentRarity medianRarity)
		{
			var config = f.ChestConfigs.GetConfig(Id);
			var medianModifier = f.RNG->NextInclusive(-1, 1);
			var medianRarityInt = (int) medianRarity;

			var chosenRarity = FPMath.Clamp(medianRarityInt + medianModifier,
				(int) minimumRarity,
				(int) EquipmentRarity.TOTAL - 1);

			equipment.Rarity = (EquipmentRarity) chosenRarity;
		}

		private Equipment GetNextLoadoutGearItem(Frame f, PlayerCharacter* playerCharacter, Equipment[] loadout)
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
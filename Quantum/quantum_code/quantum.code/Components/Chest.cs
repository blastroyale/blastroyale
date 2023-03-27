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
		                   QuantumChestConfig config, bool makeCollectable = true)
		{
			var transform = f.Unsafe.GetPointer<Transform3D>(e);

			Id = config.Id;
			ChestType = config.ChestType;

			transform->Position = position;
			transform->Rotation = rotation;

			if (makeCollectable)
			{
				MakeCollectable(f, e);
			}
		}

		/// <summary>
		/// Adds a <see cref="Collectable"/> component to <paramref name="e"/>.
		/// </summary>
		internal void MakeCollectable(Frame f, EntityRef e)
		{
			f.Add(e, new Collectable { GameId = Id });
		}

		public void Open(Frame f, EntityRef e, EntityRef playerEntity, PlayerRef playerRef)
		{
			var angleStep = 0;
			var chestPosition = f.Get<Transform3D>(e).Position;
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(playerEntity);
			var isBot = f.Has<BotCharacter>(playerEntity);
			var loadoutWeapon = isBot ? Equipment.None : playerCharacter->GetLoadoutWeapon(f);
			var hasLoadoutWeapon = loadoutWeapon.IsValid() && !loadoutWeapon.IsDefaultItem();
			var minimumRarity = hasLoadoutWeapon ? loadoutWeapon.Rarity : EquipmentRarity.Common;
			var config = f.ChestConfigs.GetConfig(ChestType);
			var stats = f.Get<Stats>(playerEntity);
			var ammoFilled = playerCharacter->GetAmmoAmountFilled(f, playerEntity);
			var shieldFilled = stats.CurrentShield / stats.GetStatData(StatType.Shield).StatValue;
			var healthFilled = stats.CurrentHealth / stats.GetStatData(StatType.Health).StatValue;
			var chestItems = new List<ChestItemDropped>();
			var gameContainer = f.Unsafe.GetPointerSingleton<GameContainer>();

			//if we have an override component to change what spawns within the chest
			if(f.Unsafe.TryGetPointer<ChestOverride>(e, out var overrideComponent) && 
				overrideComponent->ContentsOverride != new QList<GameId>())
			{
				foreach (var item in f.ResolveList(overrideComponent->ContentsOverride))
				{
					if(item.IsInGroup(GameIdGroup.Equipment))
					{
						var equipment = Equipment.Create(item, f.ChestConfigs.GetChestRarity(config.ChestType), 1);
						Collectable.DropEquipment(f, equipment, chestPosition, angleStep++);

					} else if (item.IsInGroup(GameIdGroup.Consumable))
					{
						Collectable.DropConsumable(f, item, chestPosition, angleStep++, false);
					} else
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
						AngleStepAroundChest = angleStep
					});
				}
			} else
			{
				// Empty primary slot and hasn't ever dropped a weapon => drop the one from loadout or a random one
				// Empty primary slot and we dropped a weapon once => skip dropping a weapon here
				// Busy primary slot => skip dropping a weapon here
				// There are items in the pool to drop
				if (playerCharacter->WeaponSlots[1].Weapon.GameId == GameId.Random &&
					!playerCharacter->HasDroppedItemForSlot(Constants.GEAR_INDEX_WEAPON) &&
					!gameContainer->DropPool.IsPoolEmpty &&
					!isBot)
				{
					var weaponItem = hasLoadoutWeapon ? loadoutWeapon : gameContainer->GenerateNextWeapon(f);

					ModifyEquipmentRarity(f, ref weaponItem, minimumRarity, gameContainer->DropPool.AverageRarity);
					Collectable.DropEquipment(f, weaponItem, chestPosition, angleStep++);
					playerCharacter->SetDroppedLoadoutItem(weaponItem);

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

				DropPowerUps(f, playerEntity, config, playerCharacter, gameContainer, minimumRarity, loadoutWeapon,
							 chestPosition, ref angleStep, chestItems, chestItems.Count);
				DropSmallConsumable(f, playerEntity, playerRef, config, ref ammoFilled, ref shieldFilled, ref healthFilled,
									chestPosition, ref angleStep, chestItems);
				DropLargeConsumable(f, playerEntity, playerRef, config, ref ammoFilled, ref shieldFilled,
									chestPosition, ref angleStep, chestItems);
			}

			f.Signals.ChestOpened(config.Id, chestPosition, playerRef, playerEntity);
			f.Events.OnChestOpened(config.Id, chestPosition, playerRef, playerEntity, chestItems);
		}

		private void DropSmallConsumable(Frame f, EntityRef playerEntity, PlayerRef playerRef, QuantumChestConfig config, ref FP ammoFilled, ref FP shieldFilled, ref FP healthFilled,
		                                                   FPVector3 chestPosition, ref int angleStep, List<ChestItemDropped> chestItems)
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
					if (healthFilled + ammoFilled + shieldFilled > Constants.ENERGY_CHEST_DROP_THRESHOLD)
					{
						drop = GameId.EnergyCubeSmall;
					}
					else if (healthFilled < ammoFilled && healthFilled < shieldFilled) //health
					{
						drop = GameId.Health;
						healthFilled += f.ConsumableConfigs.GetConfig(drop).Amount.Get(f) /
							stats.GetStatData(StatType.Health).StatValue;
					}
					else if (ammoFilled < healthFilled && ammoFilled < shieldFilled) //ammo
					{
						drop = GameId.AmmoSmall;
						ammoFilled += f.ConsumableConfigs.GetConfig(drop).Amount.Get(f);
					}
					else if (shieldFilled < healthFilled && shieldFilled < ammoFilled) //shield
					{
						drop = GameId.ShieldSmall;
						shieldFilled += f.ConsumableConfigs.GetConfig(drop).Amount.Get(f) /
							stats.GetStatData(StatType.Shield).StatValue;
					}
					else
					{
						drop = QuantumHelpers.GetRandomItem(f, GameId.AmmoSmall, GameId.ShieldSmall, GameId.Health, GameId.EnergyCubeSmall);
					}

					Collectable.DropConsumable(f, drop, chestPosition, angleStep++, false);
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

		private void DropLargeConsumable(Frame f, EntityRef playerEntity, PlayerRef playerRef, QuantumChestConfig config, ref FP ammoFilled, ref FP shieldFilled, 
		                                                   FPVector3 chestPosition, ref int angleStep, List<ChestItemDropped> chestItems)
		{
			var stats = f.Get<Stats>(playerEntity);
			foreach (var (chance, count) in config.LargeConsumable)
			{
				if (f.RNG->Next() > chance)
				{
					continue;
				}

				for (uint i = 0; i < count; i++)
				{
					var drop = GameId.Random;
					//TODO: add a large health consumable drop
					if (ammoFilled > FP._0_75 && shieldFilled > Constants.ENERGY_CHEST_DROP_THRESHOLD - 1)
					{
						drop = GameId.EnergyCubeSmall;
						ammoFilled -= FP._0_20;
						shieldFilled -= FP._0_20;
					}
					if (ammoFilled < shieldFilled) //ammo
					{
						drop = GameId.AmmoSmall;
						ammoFilled += f.ConsumableConfigs.GetConfig(drop).Amount.Get(f);
					}
					else if (shieldFilled < ammoFilled) //shield
					{
						drop = GameId.ShieldSmall;
						shieldFilled += f.ConsumableConfigs.GetConfig(drop).Amount.Get(f) /
							stats.GetStatData(StatType.Shield).StatValue;
					}
					else
					{
						drop = QuantumHelpers.GetRandomItem(f, GameId.AmmoLarge, GameId.ShieldLarge, GameId.EnergyCubeLarge);
					}

					Collectable.DropConsumable(f, drop, chestPosition, angleStep++, false);
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

		private void DropPowerUps(Frame f, EntityRef playerEntity, QuantumChestConfig config, PlayerCharacter* playerCharacter, 
		                          GameContainer* gameContainer, EquipmentRarity minimumRarity, Equipment loadoutWeapon, 
		                          FPVector3 chestPosition, ref int angleStep, List<ChestItemDropped> chestItems, int skipDropNumber)
		{
			var playerRef = playerCharacter->Player;

			var allEquipment = new List<Equipment>
			{
				playerCharacter->WeaponSlots[1].Weapon,
				playerCharacter->WeaponSlots[2].Weapon,
				playerCharacter->Gear[0],
				playerCharacter->Gear[1],
				playerCharacter->Gear[2],
				playerCharacter->Gear[3],
				playerCharacter->Gear[4],
			};
			
			// In case we are giving equipment to a bot - we gather a random loadout based on LoadoutGearNumber of a bot
			var botLoadout = new List<Equipment>();
			if (f.Unsafe.TryGetPointer<BotCharacter>(playerEntity, out var botCharacter) && botCharacter->LoadoutGearNumber > 0)
			{
				var medianRarity = gameContainer->DropPool.MedianRarity;
				var helmetsList = new List<GameId>(GameIdGroup.Helmet.GetIds());
				var shieldsList = new List<GameId>(GameIdGroup.Shield.GetIds());
				var armorsList = new List<GameId>(GameIdGroup.Armor.GetIds());
				var amuletsList = new List<GameId>(GameIdGroup.Amulet.GetIds());

				
				botLoadout.Add(Equipment.Create(helmetsList[f.RNG->Next(0, helmetsList.Count)], medianRarity, 1));
				if (botCharacter->LoadoutGearNumber > 1)
				{
					botLoadout.Add(Equipment.Create(shieldsList[f.RNG->Next(0, shieldsList.Count)], medianRarity, 1));
				}
				if (botCharacter->LoadoutGearNumber > 2)
				{
					botLoadout.Add(Equipment.Create(armorsList[f.RNG->Next(0, armorsList.Count)], medianRarity, 1));
				}
				if (botCharacter->LoadoutGearNumber > 3)
				{
					botLoadout.Add(Equipment.Create(amuletsList[f.RNG->Next(0, amuletsList.Count)], medianRarity, 1));
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
					// If we dropped equipment before this method then we count those items and skip the equal amount of drops here
					if (skipDropNumber > 0)
					{
						skipDropNumber--;
						continue;
					}
					
					// Second, we drop equipment from their loadout if it is all valid and we haven't dropped them all already
					Equipment drop;
					if (!f.Has<BotCharacter>(playerEntity))
					{
						drop = GetNextLoadoutGearItem(f, playerCharacter, playerCharacter->GetLoadout(f));
					}
					else
					{
						drop = GetNextLoadoutGearItem(f, playerCharacter, botLoadout.ToArray());
					}
					
					if (drop.GameId != GameId.Random && drop.IsValid())
					{
						playerCharacter->SetDroppedLoadoutItem(drop);
						ModifyEquipmentRarity(f, ref drop, drop.Rarity, gameContainer->DropPool.AverageRarity);
						Collectable.DropEquipment(f, drop, chestPosition, angleStep++, playerRef);

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


					// If we dropped all equipment from loadout, then choose between upgrades for equipment or consumables
					// chances are: 25% equipment, 25% large shields consumable, 25% large ammo consumable, 25% health
					var furtherDrop = QuantumHelpers.GetRandomItem(f, GameId.Random, GameId.ShieldLarge, GameId.AmmoLarge, GameId.Health);

					// Drop equipment upgrades if you rolled it
					if (furtherDrop == GameId.Random)
					{
						// In the edge case when we dropped all possible equipment
						// upgrades from this chest already we just do nothing
						if (allEquipment.Count == 0)
						{
							continue;
						}
						
						// We loop through each piece of equipment in a random order
						var randomList = allEquipment.OrderBy(r => f.RNG->Next()).ToList();
						foreach (var equipment in randomList)
						{
							if (equipment.Rarity == EquipmentRarity.LegendaryPlus || equipment.GameId == GameId.Random)
							{
								continue;
							}

							// Modify the equipment rarity by the rarity of the chest being opened, and by 1 at minimum
							var higherRarityEquipment = equipment;
							var newMinimumRarity = (EquipmentRarity)((int)equipment.Rarity + 1);

							// We use "newMinimumRarity" as "median rarity" in this particular case to ensure
							// that higher quality chests affect rarity improvement stronger
							ModifyEquipmentRarity(f, ref higherRarityEquipment, newMinimumRarity, newMinimumRarity);
							Collectable.DropEquipment(f, higherRarityEquipment, chestPosition, angleStep++, playerRef);

							chestItems.Add(new ChestItemDropped
							{
								ChestType = config.Id,
								ChestPosition = chestPosition,
								Player = playerCharacter->Player,
								PlayerEntity = playerEntity,
								ItemType = higherRarityEquipment.GameId,
								Amount = 1,
								AngleStepAroundChest = angleStep
							});
							
							allEquipment.Remove(equipment);
							break;
						}
						continue;
					}
					
					// Drop consumable otherwise
					Collectable.DropConsumable(f, furtherDrop, chestPosition, angleStep++, false);
					chestItems.Add(new ChestItemDropped
					{
						ChestType = config.Id,
						ChestPosition = chestPosition,
						Player = playerCharacter->Player,
						PlayerEntity = playerEntity,
						ItemType = furtherDrop,
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
			var chestRarityModifier = f.RNG->NextInclusive(config.RarityModifierRange.Value1, config.RarityModifierRange.Value2);
			var medianModifier = f.RNG->NextInclusive(-1, 1);
			var medianRarityInt = (int) medianRarity;

			var chosenRarity = FPMath.Clamp(medianRarityInt + medianModifier + chestRarityModifier,
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
					loadoutFlags |= 1 << (PlayerCharacter.GetGearSlot(e) + 1);
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

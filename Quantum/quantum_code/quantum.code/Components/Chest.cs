using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;

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
			var hasLoadoutWeapon = loadoutWeapon.IsValid();
			var minimumRarity = hasLoadoutWeapon ? loadoutWeapon.Rarity : EquipmentRarity.Common;
			var config = f.ChestConfigs.GetConfig(ChestType);
			var stats = f.Get<Stats>(playerEntity);
			var ammoCheck = playerCharacter->GetAmmoAmountFilled(f, playerEntity) < FP._0_20;
			var shieldCheck = stats.CurrentShield / stats.GetStatData(StatType.Shield).StatValue < FP._0_20;
			var healthCheck = stats.CurrentHealth / stats.GetStatData(StatType.Health).StatValue < FP._0_20;
			var chestItems = new List<ChestItemDropped>();
			var nextGearItem = Equipment.None;
			var gameContainer = f.Unsafe.GetPointerSingleton<GameContainer>();

			//if the player has yet to be dropped thier primary weapon, we do so here
			if (!f.Has<BotCharacter>(playerEntity) && playerCharacter->WeaponSlots[1].Weapon.GameId == GameId.Random)
			{
				if(hasLoadoutWeapon && !playerCharacter->HasDroppedLoadoutItem(loadoutWeapon))
				{
					nextGearItem = loadoutWeapon;
					playerCharacter->SetDroppedLoadoutItem(nextGearItem);
					ModifyEquipmentRarity(f, ref nextGearItem, minimumRarity, gameContainer->DropPool.AverageRarity);
					Collectable.DropEquipment(f, nextGearItem, chestPosition, angleStep++);
				}else
				{
					nextGearItem = gameContainer->GenerateNextWeapon(f);
					ModifyEquipmentRarity(f, ref nextGearItem, minimumRarity, gameContainer->DropPool.AverageRarity);
					Collectable.DropEquipment(f, nextGearItem, chestPosition, angleStep++);
					
				}
				chestItems.Add(new ChestItemDropped()
				{
					ChestType = config.Id,
					ChestPosition = chestPosition,
					Player = playerRef,
					PlayerEntity = playerEntity,
					ItemType = nextGearItem.GameId,
					Amount = 1,
					AngleStepAroundChest = angleStep
				});
			} 

			// Drop "PowerUps" (equipment / shield upgrade)
			DropPowerUps(f, playerEntity, config, playerCharacter, gameContainer, minimumRarity, loadoutWeapon,
						 chestPosition, ref angleStep, chestItems);
			// Drop Small consumable
			DropSmallConsumable(f, playerEntity, playerRef, config, ammoCheck, shieldCheck, healthCheck, chestPosition, ref angleStep, chestItems);
			DropLargeConsumable(f, playerEntity, playerRef,config, ammoCheck, shieldCheck, chestPosition, ref angleStep, chestItems);
			
			f.Events.OnChestOpened(config.Id, chestPosition, playerRef, playerEntity, chestItems);
		}

		private void DropSmallConsumable(Frame f, EntityRef playerEntity, PlayerRef playerRef, QuantumChestConfig config, bool ammoCheck, bool shieldCheck, bool healthCheck,
		                                                   FPVector3 chestPosition, ref int angleStep, List<ChestItemDropped> chestItems)
		{
			foreach (var (chance, count) in config.SmallConsumable)
			{
				if (f.RNG->Next() > chance)
				{
					continue;
				}

				for (uint i = 0; i < count; i++)
				{
					var drop = GameId.Random;

					//modify the drop based on whether or not the player needs specific items
					if (ammoCheck)
					{
						drop = GameId.AmmoSmall;
					}
					else if (shieldCheck)
					{
						drop = GameId.ShieldSmall;
					}
					else if (healthCheck)
					{
						drop = GameId.Health;
					}
					else
					{
						drop = QuantumHelpers.GetRandomItem(f, GameId.AmmoSmall, GameId.ShieldSmall, GameId.Health);
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

		private void DropLargeConsumable(Frame f, EntityRef playerEntity, PlayerRef playerRef, QuantumChestConfig config, bool ammoCheck, bool shieldCheck, 
		                                                   FPVector3 chestPosition, ref int angleStep, List<ChestItemDropped> chestItems)
		{
			foreach (var (chance, count) in config.LargeConsumable)
			{
				if (f.RNG->Next() > chance)
				{
					continue;
				}

				for (uint i = 0; i < count; i++)
				{
					var drop = GameId.Random;

					if (ammoCheck)
					{
						drop = GameId.AmmoLarge;
					}
					else if (shieldCheck)
					{
						drop = GameId.ShieldLarge;
					}
					else
					{
						drop = QuantumHelpers.GetRandomItem(f, GameId.AmmoLarge, GameId.ShieldLarge);
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
		                          FPVector3 chestPosition, ref int angleStep, List<ChestItemDropped> chestItems)
		{
			var hasLoadoutWeapon = loadoutWeapon.IsValid();
			var noWeaponsEquipped = playerCharacter->WeaponSlots[1].Weapon.GameId == GameId.Random
				&& playerCharacter->WeaponSlots[2].Weapon.GameId == GameId.Random;
			var playerRef = playerCharacter->Player;
			var filledShieldCapacity = f.Get<Stats>(playerEntity).GetStatData(StatType.Shield).StatValue ==
				f.Get<Stats>(playerEntity).GetStatData(StatType.Shield).BaseValue;


			foreach (var (chance, count) in config.RandomEquipment)
			{
				if (f.RNG->Next() > chance)
				{
					continue;
				}
				
				for (uint i = 0; i < count; i++)
				{
					//if there is no shield capacity to be dropped, only ever drop the randomID, otherwise, drop shield capacity
					var drop = filledShieldCapacity ? GameId.Random :
						QuantumHelpers.GetRandomItem(f, GameId.Random, GameId.Random, GameId.ShieldCapacityLarge, GameId.ShieldCapacitySmall);

					if (drop == GameId.Random)
					{
						var nextGearItem = GetNextLoadoutGearItem(f, playerCharacter, playerCharacter->GetLoadout(f));
						//first try drop your next equipment
						if (nextGearItem.IsValid())
						{
							playerCharacter->SetDroppedLoadoutItem(nextGearItem);
							ModifyEquipmentRarity(f, ref nextGearItem, minimumRarity, gameContainer->DropPool.AverageRarity);
							Collectable.DropEquipment(f, nextGearItem, chestPosition, angleStep++, playerRef);
							chestItems.Add(new ChestItemDropped
							{
								ChestType = config.Id,
								ChestPosition = chestPosition,
								Player = playerCharacter->Player,
								PlayerEntity = playerEntity,
								ItemType = nextGearItem.GameId,
								Amount = 1,
								AngleStepAroundChest = angleStep
							});

						} //if you have no equipment then we try to drop a weapon if you dont have one equipped
						else if (noWeaponsEquipped)
						{
							var weapon = gameContainer->GenerateNextWeapon(f);
							drop = weapon.GameId;
							// TODO: This should happen when we pick up a weapon, not when we drop it 
							// I think this is silly, but "When a player picks up a weapon we inherit all NFT
							// attributes (except for the rarity) from the Record".
							if (hasLoadoutWeapon)
							{
								var originalGameId = weapon.GameId;
								weapon = loadoutWeapon;
								weapon.GameId = originalGameId;
							}

							ModifyEquipmentRarity(f, ref weapon, minimumRarity, gameContainer->DropPool.AverageRarity);
							Collectable.DropEquipment(f, weapon, chestPosition, angleStep++);
							chestItems.Add(new ChestItemDropped
							{
								ChestType = config.Id,
								ChestPosition = chestPosition,
								Player = playerCharacter->Player,
								PlayerEntity = playerEntity,
								ItemType = weapon.GameId,
								Amount = 1,
								AngleStepAroundChest = angleStep
							});

						} //finally if you have all your equipment, and a weapon equipped, drop better versions of your equipped gear
						else
						{
							var allEquipment = new List<Equipment>
							{
								playerCharacter->CurrentWeapon,
								playerCharacter->Gear[0],
								playerCharacter->Gear[1],
								playerCharacter->Gear[2],
								playerCharacter->Gear[3],
								playerCharacter->Gear[4],
							};

							//we loop through each piece of equipment in a random order
							var randomList = allEquipment.OrderBy(r => f.RNG->Next()).ToList();
							foreach (var equipment in randomList)
							{
								var rarity = equipment.Rarity;
								var equipmentUpgrade = equipment;
								if (rarity == EquipmentRarity.LegendaryPlus || equipment.GameId == GameId.Random)
								{
									continue;
								}
								//modify the equipment rairty by the rarity of the chest being opened, and by at minimum 1
								var rarityModifier = FPMath.Max(1, f.RNG->NextInclusive(config.RarityModifierRange.Value1, config.RarityModifierRange.Value2)).AsInt;
								ModifyEquipmentRarity(f, ref equipmentUpgrade, rarity + rarityModifier, gameContainer->DropPool.AverageRarity);
								Collectable.DropEquipment(f, equipmentUpgrade, chestPosition, angleStep++, playerRef);

								chestItems.Add(new ChestItemDropped
								{
									ChestType = config.Id,
									ChestPosition = chestPosition,
									Player = playerCharacter->Player,
									PlayerEntity = playerEntity,
									ItemType = equipmentUpgrade.GameId,
									Amount = 1,
									AngleStepAroundChest = angleStep
								});
								break;
							}
						}
					}
					else
					{
						Collectable.DropConsumable(f, drop, chestPosition, angleStep++, false);
						chestItems.Add(new ChestItemDropped
						{
							ChestType = config.Id,
							ChestPosition = chestPosition,
							Player = playerCharacter->Player,
							PlayerEntity = playerEntity,
							ItemType = drop,
							Amount = 1,
							AngleStepAroundChest = angleStep
						});
					}
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

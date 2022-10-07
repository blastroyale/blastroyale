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
			var gameContainer = f.Unsafe.GetPointerSingleton<GameContainer>();
			
			// Empty primary slot and hasn't ever dropped a weapon => drop the one from loadout or a random one
			// Empty primary slot and we dropped a weapon once => skip dropping a weapon here
			// Busy primary slot => skip dropping a weapon here
			if (!isBot && playerCharacter->WeaponSlots[1].Weapon.GameId == GameId.Random
			           && !playerCharacter->HasDroppedItemForSlot(Constants.GEAR_INDEX_WEAPON))
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

			// Drop "PowerUps" (equipment / shield upgrade)
			DropPowerUps(f, playerEntity, config, playerCharacter, gameContainer, minimumRarity, loadoutWeapon,
			             chestPosition, ref angleStep, chestItems, chestItems.Count);
			// Drop small and large consumables
			DropSmallConsumable(f, playerEntity, playerRef, config, ammoCheck, shieldCheck, healthCheck,
			                    chestPosition, ref angleStep, chestItems);
			DropLargeConsumable(f, playerEntity, playerRef,config, ammoCheck, shieldCheck,
			                    chestPosition, ref angleStep, chestItems);
			
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

					// Modify the drop based on whether or not the player needs specific items
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

					// Modify the drop based on whether or not the player needs specific items
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
		                          FPVector3 chestPosition, ref int angleStep, List<ChestItemDropped> chestItems, int skipDropNumber)
		{
			var hasLoadoutWeapon = loadoutWeapon.IsValid();
			var noWeaponsEquipped = playerCharacter->WeaponSlots[1].Weapon.GameId == GameId.Random
				&& playerCharacter->WeaponSlots[2].Weapon.GameId == GameId.Random;
			var playerRef = playerCharacter->Player;
			var statsShields = f.Get<Stats>(playerEntity).GetStatData(StatType.Shield);
			var drop = GetNextLoadoutGearItem(f, playerCharacter, playerCharacter->GetLoadout(f)).GameId;

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

					// Equipment drop logic
					if (drop != GameId.Random)
					{
						//TODO: Currently a player has only 1 chance to get Weapon - from their first crate; this needs to be fixed, otherwise a player will have to pickup all gear before getting another weapon
						//TODO: Currently this logic can drop two same equipment items, which shouldn't be the case
						
						// First - try to drop player's next equipment from their loadout
						var nextGearItem = GetNextLoadoutGearItem(f, playerCharacter, playerCharacter->GetLoadout(f));
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
							
							continue;
						}

						// If a player hasn't reached full shields capacity then for the drop
						// chances are: 50% equipment, 25% big shields capacity, 25% small shields capacity
						if (statsShields.StatValue < statsShields.BaseValue && drop == GameId.Random)
						{
							drop = QuantumHelpers.GetRandomItem(f, GameId.Random, GameId.Random, GameId.ShieldCapacityLarge, GameId.ShieldCapacitySmall);
						}

						// Second - drop a weapon if a player has no weapons equipped
						if (noWeaponsEquipped)
						{
							var weapon = gameContainer->GenerateNextWeapon(f);
							
							// TODO: This should happen when we pick up a weapon, not when we drop it 
							// When a player picks up a weapon we inherit all NFT
							// attributes (except for the rarity and GameId) from the Record
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
							
							continue;
						}
						
						// Third - if you have all your gear, and both weapons equipped, then we drop better versions of your equipped items
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
							
							break;
						}
						
						// In the edge case when a player has everything equipped and everything is of highest rarity we drop nothing
						
						continue;
					}
					
					// Drop shields capacity otherwise
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

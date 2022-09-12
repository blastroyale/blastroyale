using System;
using System.Collections.Generic;
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
			var nextGearItem = isBot ? Equipment.None : 
				                   (hasLoadoutWeapon && !playerCharacter->HasDroppedLoadoutItem(loadoutWeapon)? 
					                    loadoutWeapon : GetNextLoadoutGearItem(f, playerCharacter, playerCharacter->GetLoadout(f)));
			var weaponPool = f.Context.GetPlayerWeapons(f, out var medianRarity);
			var config = f.ChestConfigs.GetConfig(ChestType);
			var stats = f.Get<Stats>(playerEntity);
			var ammoCheck = playerCharacter->GetAmmoAmountFilled(f, playerEntity) < FP._0_20;
			var shieldCheck = stats.CurrentShield / stats.GetStatData(StatType.Shield).StatValue < FP._0_20;
			var healthCheck = stats.CurrentHealth / stats.GetStatData(StatType.Health).StatValue < FP._0_20;

			var chestItems = new List<ChestItemDropped>();

			if (nextGearItem.IsValid())
			{
				playerCharacter->SetDroppedLoadoutItem(nextGearItem);
				ModifyEquipmentRarity(f, ref nextGearItem, minimumRarity, medianRarity);
				Collectable.DropEquipment(f, nextGearItem, chestPosition, angleStep++, playerRef);
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
			else
			{
				// Drop "PowerUps" (equipment / shield upgrade)
				chestItems.AddRange(DropPowerUps(f, playerEntity, config, playerCharacter, weaponPool,
				                                 minimumRarity, medianRarity, loadoutWeapon, chestPosition,
				                                 ref angleStep));
			}

			// Drop Small consumable
			chestItems.AddRange(DropSmallConsumable(f, playerEntity, playerRef, config, ammoCheck, shieldCheck, healthCheck, chestPosition, ref angleStep));
			chestItems.AddRange(DropLargeConsumable(f, playerEntity, playerRef,config, ammoCheck, shieldCheck, chestPosition, ref angleStep));
			
			f.Events.OnChestOpened(config.Id, chestPosition, playerRef, playerEntity, chestItems);
		}

		private List<ChestItemDropped> DropSmallConsumable(Frame f, EntityRef playerEntity, PlayerRef playerRef, QuantumChestConfig config, bool ammoCheck, bool shieldCheck, bool healthCheck,
		                                                   FPVector3 chestPosition, ref int angleStep)
		{
			var chestItems = new List<ChestItemDropped>();
			
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
			
			return chestItems;
		}

		private List<ChestItemDropped> DropLargeConsumable(Frame f, EntityRef playerEntity, PlayerRef playerRef, QuantumChestConfig config, bool ammoCheck, bool shieldCheck, 
		                                                   FPVector3 chestPosition, ref int angleStep)
		{
			var chestItems = new List<ChestItemDropped>();
			
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
			
			return chestItems;
		}

		private List<ChestItemDropped> DropPowerUps(Frame f, EntityRef playerEntity, QuantumChestConfig config, PlayerCharacter* playerCharacter, 
		                                               IReadOnlyList<Equipment> weaponPool, EquipmentRarity minimumRarity, EquipmentRarity medianRarity, 
		                                               Equipment loadoutWeapon, FPVector3 chestPosition, ref int angleStep)
		{
			var chestItems = new List<ChestItemDropped>();
			var hasLoadoutWeapon = loadoutWeapon.IsValid();
			var noWeaponsEquipped = playerCharacter->WeaponSlots[1].Weapon.GameId == GameId.Random &&
										playerCharacter->WeaponSlots[2].Weapon.GameId == GameId.Random;

			foreach (var (chance, count) in config.RandomEquipment)
			{
				if (f.RNG->Next() > chance)
				{
					continue;
				}

				for (uint i = 0; i < count; i++)
				{
					// For GameId.Random we drop equipment
					var drop = noWeaponsEquipped ?
						GameId.Random :
						QuantumHelpers.GetRandomItem(f, GameId.Random, GameId.ShieldCapacityLarge, GameId.ShieldCapacitySmall);

					if (drop == GameId.Random)
					{
						var weapon = weaponPool[f.RNG->Next(0, weaponPool.Count)];

						// TODO: This should happen when we pick up a weapon, not when we drop it 
						// I think this is silly, but "When a player picks up a weapon we inherit all NFT
						// attributes (except for the rarity) from the Record".
						if (hasLoadoutWeapon)
						{
							var originalGameId = weapon.GameId;
							weapon = loadoutWeapon;
							weapon.GameId = originalGameId;
						}

						ModifyEquipmentRarity(f, ref weapon, minimumRarity, medianRarity);

						Collectable.DropEquipment(f, weapon, chestPosition, angleStep++);
						chestItems.Add(new ChestItemDropped()
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
					else
					{
						Collectable.DropConsumable(f, drop, chestPosition, angleStep++, false);
						chestItems.Add(new ChestItemDropped()
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

			return chestItems;
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

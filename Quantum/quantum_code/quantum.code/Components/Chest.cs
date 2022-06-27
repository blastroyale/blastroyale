using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		                   QuantumChestConfig config)
		{
			var collectable = new Collectable {GameId = config.Id};
			var transform = f.Unsafe.GetPointer<Transform3D>(e);

			ChestType = config.ChestType;

			transform->Position = position;
			transform->Rotation = rotation;

			f.Add(e, collectable);
		}

		public void Open(Frame f, EntityRef e, EntityRef playerEntity, PlayerRef playerRef)
		{
			var playerData = f.GetPlayerData(playerRef);
			
			var chestPosition = f.Get<Transform3D>(e).Position;
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(playerEntity);
			var isBot = f.Has<BotCharacter>(playerEntity);
			var config = f.ChestConfigs.GetConfig(ChestType);
			var hasPrimaryWeaponEquipped = playerCharacter->WeaponSlots[Constants.WEAPON_INDEX_PRIMARY].Weapon.IsValid();
			var loadoutWeapon = isBot ? Equipment.None : playerData.Loadout.FirstOrDefault(item => item.IsWeapon());
			var hasLoadoutWeapon = loadoutWeapon.IsValid();
			var minimumRarity = hasLoadoutWeapon ? loadoutWeapon.Rarity : EquipmentRarity.Common;
			var nextGearItem = isBot ? Equipment.None : GetNextLoadoutGearItem(f, playerCharacter, playerData.Loadout);
			var weaponPool = f.Context.GetPlayerWeapons(f, out var medianRarity);

			var angleStep = 0;

			if (!hasPrimaryWeaponEquipped && hasLoadoutWeapon)
			{
				// Drop primary weapon if it's in loadout and not equipped
				playerCharacter->SetDroppedLoadoutItem(loadoutWeapon);
				ModifyEquipmentRarity(f, ref loadoutWeapon, minimumRarity, medianRarity);
				Collectable.DropEquipment(f, loadoutWeapon, chestPosition, angleStep++, playerRef);
			}
			else if (nextGearItem.IsValid())
			{
				playerCharacter->SetDroppedLoadoutItem(nextGearItem);
				ModifyEquipmentRarity(f, ref nextGearItem, minimumRarity, medianRarity);
				Collectable.DropEquipment(f, nextGearItem, chestPosition, angleStep++, playerRef);
			}
			else
			{
				// Drop "PowerUps" (equipment / shield upgrade)
				foreach (var (chance, count) in config.RandomEquipment)
				{
					if (f.RNG->Next() > chance)
					{
						continue;
					}

					for (uint i = 0; i < count; i++)
					{
						// For GameId.Random we drop equipment
						var drop = QuantumHelpers.GetRandomItem(f, GameId.Random, GameId.ShieldCapacityLarge,
						                                        GameId.ShieldCapacitySmall);

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
						}
						else
						{
							Collectable.DropConsumable(f, drop, chestPosition, angleStep++, false);
						}
					}
				}
			}

			// Drop Small consumable
			foreach (var (chance, count) in config.SmallConsumable)
			{
				if (f.RNG->Next() > chance)
				{
					continue;
				}

				for (uint i = 0; i < count; i++)
				{
					var drop = QuantumHelpers.GetRandomItem(f, GameId.AmmoSmall, GameId.ShieldSmall, GameId.Health);
					Collectable.DropConsumable(f, drop, chestPosition, angleStep++, false);
				}
			}

			// Drop Large consumable
			foreach (var (chance, count) in config.LargeConsumable)
			{
				if (f.RNG->Next() > chance)
				{
					continue;
				}

				for (uint i = 0; i < count; i++)
				{
					var drop = QuantumHelpers.GetRandomItem(f, GameId.AmmoLarge, GameId.ShieldLarge);
					Collectable.DropConsumable(f, drop, chestPosition, angleStep++, false);
				}
			}
		}

		private void ModifyEquipmentRarity(Frame f, ref Equipment equipment, EquipmentRarity minimumRarity,
		                                   EquipmentRarity medianRarity)
		{
			var chestRarityModifier = GetChestRarityModifier();
			var medianModifier = f.RNG->NextInclusive(-1, 1);
			var medianRarityInt = (int) medianRarity;

			var chosenRarity = FPMath.Clamp(medianRarityInt + medianModifier + chestRarityModifier,
			                                (int) minimumRarity,
			                                (int) EquipmentRarity.TOTAL - 1);

			equipment.Rarity = (EquipmentRarity) chosenRarity;
		}

		private int GetChestRarityModifier()
		{
			return ChestType switch
			{
				ChestType.Common => -2,
				ChestType.Uncommon => -1,
				ChestType.Rare => 0,
				ChestType.Epic => 1,
				ChestType.Legendary => 2,
				_ => throw new ArgumentOutOfRangeException(nameof(ChestType), ChestType, null)
			};
		}

		private Equipment GetNextLoadoutGearItem(Frame f, PlayerCharacter* playerCharacter, Equipment[] loadout)
		{
			var flags = playerCharacter->DroppedLoadoutFlags;

			// Set bits of loadout items we have
			int loadoutFlags = 0;
			foreach (var e in loadout)
			{
				if (e.IsWeapon())
				{
					continue;
				}

				loadoutFlags |= 1 << (PlayerCharacter.GetGearSlot(e) + 1);
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

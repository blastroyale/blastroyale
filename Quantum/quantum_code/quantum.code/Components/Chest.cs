using System;
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
			var playerCharacter = f.Get<PlayerCharacter>(playerEntity);
			var isBot = f.Has<BotCharacter>(playerEntity);
			var config = f.ChestConfigs.GetConfig(ChestType);

			var hasPrimaryWeaponEquipped = playerCharacter.Weapons[Constants.WEAPON_INDEX_PRIMARY].IsValid();
			var loadoutWeapon = isBot ? Equipment.None : playerData.Loadout.FirstOrDefault(item => item.IsWeapon());
			var hasLoadoutWeapon = loadoutWeapon.IsValid();
			var minimumRarity = hasLoadoutWeapon ? loadoutWeapon.Rarity : EquipmentRarity.Common;

			var medianRarity = f.Context.MedianRarity;
			var weapons = f.Context.PlayerWeapons;

			var angleStep = 0;

			if (!hasPrimaryWeaponEquipped && hasLoadoutWeapon)
			{
				// Drop primary weapon if it's in loadout and not equipped
				ModifyEquipmentRarity(f, ref loadoutWeapon, minimumRarity, medianRarity);
				Collectable.DropEquipment(f, loadoutWeapon, chestPosition, angleStep++, playerRef);
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
							var weapon = weapons[f.RNG->Next(0, weapons.Length)];

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
	}
}
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

			var hasPrimaryWeaponEquipped = playerCharacter.Weapons[Constants.WEAPON_INDEX_PRIMARY].IsValid();
			var loadoutWeapon = isBot ? Equipment.None : playerData.Loadout.FirstOrDefault(item => item.IsWeapon());
			var hasLoadoutWeapon = loadoutWeapon.IsValid();
			var minimumRarity = hasLoadoutWeapon ? loadoutWeapon.Rarity : EquipmentRarity.Common;

			GatherAllPlayersEquipment(f, out var medianRarity, out var weapons);

			// Decide on weapon drop
			if (!hasPrimaryWeaponEquipped && hasLoadoutWeapon)
			{
				ModifyEquipmentRarity(f, ref loadoutWeapon, minimumRarity, medianRarity);
				Collectable.DropEquipment(f, loadoutWeapon, chestPosition, 0, playerRef);
			}
			else
			{
				var weapon = weapons[f.RNG->Next(0, weapons.Count - 1)];

				// I think this is silly, but "When a player picks up a weapon we inherit all NFT
				// attributes (except for the rarity) from the Record".
				if (hasLoadoutWeapon)
				{
					var originalGameId = weapon.GameId;
					weapon = loadoutWeapon;
					weapon.GameId = originalGameId;
				}

				ModifyEquipmentRarity(f, ref weapon, minimumRarity, medianRarity);
				Collectable.DropEquipment(f, loadoutWeapon, chestPosition, 0,
				                          hasLoadoutWeapon ? PlayerRef.None : playerRef);
			}

			// Decide on other stuff to drop
			DropConsumables(f, chestPosition);
		}

		private void GatherAllPlayersEquipment(Frame f, out EquipmentRarity medianRarity, out List<Equipment> weapons)
		{
			weapons = new List<Equipment>();
			var rarities = new List<EquipmentRarity>();

			var players = f.GetSingleton<GameContainer>().PlayersData;
			for (int i = 0; i < players.Length; i++)
			{
				var player = players[i];
				if (!player.IsValid || player.IsBot)
				{
					continue;
				}

				var playerData = f.GetPlayerData(player.Player);

				var weapon = playerData.Loadout.FirstOrDefault(e => e.IsWeapon());
				if (weapon.IsValid())
				{
					weapons.Add(weapon);
					rarities.Add(weapon.Rarity);
				}
				else
				{
					rarities.Add(EquipmentRarity.Common);
				}
			}

			rarities.Sort();
			medianRarity = rarities[(int) Math.Floor((decimal) rarities.Count / 2)];
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

		private void DropConsumables(Frame f, FPVector3 stashPosition)
		{
			switch (ChestType)
			{
				case ChestType.Common:
				case ChestType.Uncommon:
					if (f.RNG->Next() <= FP._0_50 + FP._0_20)
					{
						Collectable.DropConsumable(f, GameId.AmmoSmall, stashPosition, 0, false);
					}
					else
					{
						Collectable.DropConsumable(f, GameId.Health, stashPosition, 0, false);
					}

					if (f.RNG->Next() <= FP._0_10)
					{
						Collectable.DropConsumable(f, GameId.AmmoLarge, stashPosition, 1, false);
					}
					else if (f.RNG->Next() <= FP._0_50)
					{
						Collectable.DropConsumable(f, GameId.ShieldSmall, stashPosition, 1, false);
					}
					else
					{
						Collectable.DropConsumable(f, GameId.Health, stashPosition, 1, false);
					}

					if (f.RNG->Next() <= FP._0_50 + FP._0_20 + FP._0_20)
					{
						Collectable.DropConsumable(f, GameId.AmmoSmall, stashPosition, 2, false);
					}
					else
					{
						Collectable.DropConsumable(f, GameId.Health, stashPosition, 2, false);
					}

					break;
				case ChestType.Rare:
					var armour = f.RNG->Next() < FP._0_25 ? GameId.ShieldLarge : GameId.ShieldSmall;
					var ammoOrHealthChance = f.RNG->Next();

					Collectable.DropConsumable(f, armour, stashPosition, 1, false);

					Collectable.DropConsumable(f, GameId.AmmoSmall, stashPosition, 1, false);
					Collectable.DropConsumable(f, GameId.AmmoSmall, stashPosition, 1, false);

					if (ammoOrHealthChance < FP._0_20 + FP._0_10)
					{
						Collectable.DropConsumable(f, GameId.AmmoLarge, stashPosition, 2, false);
					}
					else if (ammoOrHealthChance < FP._0_50 + FP._0_20 + FP._0_20)
					{
						Collectable.DropConsumable(f, GameId.AmmoSmall, stashPosition, 2, false);
					}
					else
					{
						Collectable.DropConsumable(f, GameId.Health, stashPosition, 2, false);
					}

					break;
				case ChestType.Epic:
				case ChestType.Legendary:
					var armourType = f.RNG->Next() < FP._0_75 ? GameId.ShieldLarge : GameId.ShieldSmall;
					Collectable.DropConsumable(f, armourType, stashPosition, 2, false);

					var ammoType = f.RNG->Next() < FP._0_50 ? GameId.AmmoLarge : GameId.AmmoSmall;
					Collectable.DropConsumable(f, ammoType, stashPosition, 3, false);
					Collectable.DropConsumable(f, GameId.AmmoSmall, stashPosition, 3, false);

					Collectable.DropConsumable(f, GameId.Health, stashPosition, 4, false);
					Collectable.DropConsumable(f, GameId.Health, stashPosition, 4, false);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
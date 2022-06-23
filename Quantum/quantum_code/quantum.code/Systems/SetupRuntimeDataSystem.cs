using System;
using System.Collections.Generic;
using System.Linq;

namespace Quantum.Systems
{
	/// <summary>
	/// Sets up any data / does calculations that need to be done before the game is started.
	/// </summary>
	public unsafe class SetupRuntimeDataSystem : SystemSignalsOnly,
	                                             ISignalOnPlayerDataSet
	{
		public void OnPlayerDataSet(Frame f, PlayerRef player)
		{
			if (f.ComponentCount<PlayerCharacter>() == f.PlayerCount)
			{
				SetupEquipmentValues(f);
			}
		}

		/// <summary>
		/// Calculate median weapon rarity and gather all weapons from all the players.
		/// </summary>
		private void SetupEquipmentValues(Frame f)
		{
			var weapons = new List<Equipment>();
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

			// Fill up weapon pool to a minimum size
			var weaponIds = GameIdGroup.Weapon.GetIds();
			while (weapons.Count < f.GameConfig.MinOffhandWeaponPoolSize)
			{
				var chosenId = weaponIds[f.RNG->Next(0, weaponIds.Count)];
				if (chosenId == GameId.Hammer)
				{
					// Better to do a few more loops than to convert weaponIds to a mutable list, causing an allocation
					continue;
				}

				weapons.Add(new Equipment(chosenId));
			}

			f.Context.MedianRarity = rarities[(int) Math.Floor((decimal) rarities.Count / 2)];
			f.Context.PlayerWeapons = weapons.ToArray();
		}
	}
}
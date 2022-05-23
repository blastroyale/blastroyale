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

			SetupTargetAllMask(f);
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

			f.Context.MedianRarity = rarities[(int) Math.Floor((decimal) rarities.Count / 2)];
			f.Context.PlayerWeapons = weapons.ToArray();
		}

		/// <summary>
		/// Sets up the <see cref="FrameContextUser.TargetAllLayerMask"/> for targeting everything.
		/// </summary>
		private void SetupTargetAllMask(Frame f)
		{
			f.Context.TargetAllLayerMask = f.Layers.GetLayerMask("Default", "Playable Target", "Non Playable Target",
			                                                     "Prop", "World", "Environment No Silhouette");
		}
	}
}
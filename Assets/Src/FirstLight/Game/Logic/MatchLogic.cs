using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Services;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This logic provides the necessary behaviour to manage the game's app
	/// </summary>
	public interface IMatchDataProvider
	{
		/// <summary>
		/// Requests the player's current selected Id
		/// </summary>
		IObservableField<int> SelectedMapId { get; }

		/// <summary>
		/// Requests the player's current selected slot <see cref="MapConfig"/>.
		/// </summary>
		MapConfig SelectedMapConfig { get; }

		/// <summary>
		/// Request the player's current trophy count.
		/// </summary>
		IObservableFieldReader<uint> Trophies { get; }
	}

	/// <inheritdoc />
	public interface IMatchLogic : IMatchDataProvider
	{
		void UpdateTrophies(QuantumPlayerMatchData[] players, QuantumPlayerMatchData localPlayer);
	}

	/// <inheritdoc cref="IAppLogic"/>
	public class MatchLogic : AbstractBaseLogic<PlayerData>, IMatchLogic, IGameLogicInitializer
	{
		/// <inheritdoc />
		public IObservableField<int> SelectedMapId { get; private set; }

		/// <inheritdoc />
		public MapConfig SelectedMapConfig => GameLogic.ConfigsProvider.GetConfig<MapConfig>(SelectedMapId.Value);

		public IObservableFieldReader<uint> Trophies { get; private set; }

		private QuantumGameConfig GameConfig => GameLogic.ConfigsProvider.GetConfig<GameConfigs>().Config;

		public MatchLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		/// <inheritdoc />
		public void Init()
		{
			var configs = GameLogic.ConfigsProvider.GetConfigsDictionary<MapConfig>();

			SelectedMapId = new ObservableField<int>(configs[configs.Count - 1].Id);

			Trophies = new ObservableField<uint>(Data.Trophies);
		}

		public void UpdateTrophies(QuantumPlayerMatchData[] players, QuantumPlayerMatchData localPlayer)
		{
			var trophyChange = 0f;

			var localPlayerIndex = 0;
			for (int i = 0; i < players.Length; i++)
			{
				if (players[localPlayerIndex].IsLocalPlayer)
				{
					localPlayerIndex = i;
					break;
				}
			}

			// Losses
			for (int i = 0; i < localPlayerIndex; i++)
			{
				trophyChange += CalculateEloChange(0, players[i].Data.PlayerTrophies, localPlayer.Data.PlayerTrophies);
			}

			// Wins
			for (int i = localPlayerIndex + 1; i < players.Length; i++)
			{
				trophyChange += CalculateEloChange(1, players[i].Data.PlayerTrophies, localPlayer.Data.PlayerTrophies);
			}


			Data.Trophies = (uint) Math.Max((int) Data.Trophies + Mathf.RoundToInt(trophyChange), 0);
		}

		private float CalculateEloChange(float score, uint trophiesOpponent, uint trophiesPlayer)
		{
			return GameConfig.TrophyEloK * (score - 1f /
			                                (1f +
			                                 Mathf.Pow(10,
			                                           (trophiesOpponent - trophiesPlayer) /
			                                           (float) GameConfig.TrophyEloRange)));
		}
	}
}
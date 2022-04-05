using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Utils;
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

		// TODO - Move this functionality elsewhere? Probably.
		/// <summary>
		/// Requests the index of map currently in rotation for a particular gamemode
		/// </summary>
		/// <param name="mode">Game mode for the map index</param>
		/// <returns>Index of map in rotation</returns>
		int GetCurrentMapInTimedRotation(GameMode mode);
	}

	/// <inheritdoc />
	public interface IMatchLogic : IMatchDataProvider
	{
		void UpdateTrophies(QuantumPlayerMatchData[] players, uint localPlayerRank);
	}

	/// <inheritdoc cref="IAppLogic"/>
	public class MatchLogic : AbstractBaseLogic<PlayerData>, IMatchLogic, IGameLogicInitializer
	{
		/// <inheritdoc />
		public IObservableField<int> SelectedMapId { get; private set; }

		/// <inheritdoc />
		public MapConfig SelectedMapConfig => GameLogic.ConfigsProvider.GetConfig<MapConfig>(SelectedMapId.Value);

		public IObservableFieldReader<uint> Trophies { get; private set; }

		private QuantumGameConfig GameConfig => GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();

		private ObservableResolverField<uint> _trophiesResolver;

		public MatchLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		/// <inheritdoc />
		public void Init()
		{
			var configs = GameLogic.ConfigsProvider.GetConfigsDictionary<MapConfig>();

			SelectedMapId = new ObservableField<int>(configs[configs.Count - 1].Id);
			Trophies = _trophiesResolver =
				           new ObservableResolverField<uint>(() => Data.Trophies, val => Data.Trophies = val);
		}

		/// <inheritdoc />
		public void UpdateTrophies(QuantumPlayerMatchData[] players, uint localPlayerRank)
		{
			var trophyChange = 0f;

			var sortedPlayers = players.ToList();
			sortedPlayers.SortByPlayerRank();

			var localPlayerIndex = (int) localPlayerRank;
			var localPlayer = sortedPlayers[localPlayerIndex];

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

			_trophiesResolver.Value = (uint) Math.Max((int) Data.Trophies + Mathf.RoundToInt(trophyChange), 0);
		}

		public int GetCurrentMapInTimedRotation(GameMode mode)
		{
			List<int> compatibleMaps = new List<int>();

			if (mode == GameMode.BattleRoyale)
			{
				compatibleMaps.AddRange(GameConstants.BATTLE_ROYALE_MAP_IDS);
			}
			else if (mode == GameMode.Deathmatch)
			{
				compatibleMaps.AddRange(GameConstants.DEATMATCH_MAP_IDS);
			}
			
			int targetMapListIndex = 0;
			DateTime morning = DateTime.Today;
			DateTime now = DateTime.UtcNow;
			TimeSpan span = now - morning;
			int timeSegmentIndex = Mathf.RoundToInt((float)span.TotalMinutes / GameConstants.MAP_ROTATION_TIME_MINUTES);
			
			if (timeSegmentIndex >= compatibleMaps.Count)
			{
				timeSegmentIndex -= (compatibleMaps.Count * (timeSegmentIndex/compatibleMaps.Count));
			}
			
			return compatibleMaps[timeSegmentIndex];
		}

		private float CalculateEloChange(float score, uint trophiesOpponent, uint trophiesPlayer)
		{
			var eloBracket = Mathf.Pow(10, (trophiesOpponent - trophiesPlayer) / (float) GameConfig.TrophyEloRange);
			return GameConfig.TrophyEloK * (score - 1f / (1f + eloBracket));
		}
	}
}
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
		/// Request the player's current trophy count.
		/// </summary>
		IObservableFieldReader<uint> Trophies { get; }
	}

	/// <inheritdoc />
	public interface IMatchLogic : IMatchDataProvider
	{
		/// <summary>
		/// Updates player's trophies (Elo) based on their ranking in the match
		/// </summary>
		void UpdateTrophies(QuantumPlayerMatchData[] players, uint localPlayerRank);
	}

	/// <inheritdoc cref="IAppLogic"/>
	public class MatchLogic : AbstractBaseLogic<PlayerData>, IMatchLogic, IGameLogicInitializer
	{
		private ObservableResolverField<uint> _trophiesResolver;
		
		/// <inheritdoc />
		public IObservableFieldReader<uint> Trophies { get; private set; }
		
		private QuantumGameConfig GameConfig => GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();


		public MatchLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		/// <inheritdoc />
		public void Init()
		{
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

		private float CalculateEloChange(float score, uint trophiesOpponent, uint trophiesPlayer)
		{
			var eloBracket = Mathf.Pow(10, (trophiesOpponent - trophiesPlayer) / (float) GameConfig.TrophyEloRange);
			return GameConfig.TrophyEloK * (score - 1f / (1f + eloBracket));
		}
	}
}
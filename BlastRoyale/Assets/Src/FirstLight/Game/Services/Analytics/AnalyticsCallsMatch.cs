using System;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Ids;
using FirstLight.Game.Services.Analytics.Events;
using FirstLight.Game.Utils;
using Quantum;

namespace FirstLight.Game.Services.Analytics
{
	/// <summary>
	/// Analytics helper class regarding match events
	/// </summary>
	public class AnalyticsCallsMatch : AnalyticsCalls
	{
		private readonly IGameServices _services;

		private string _matchId;
		private string _mutators;
		private string _matchType;
		private string _gameModeId;
		private string _mapId;

		private int _playerNumAttacks;

		public AnalyticsCallsMatch(IAnalyticsService analyticsService,
								   IGameServices services) : base(analyticsService)
		{
			_services = services;

			QuantumEvent.SubscribeManual<EventOnCollectableCollected>(MatchPickupAction);
			QuantumEvent.SubscribeManual<EventOnPlayerAttack>(TrackPlayerAttack);
		}

		private void FetchPropertiesFromRoom()
		{
			var room = _services.RoomService.CurrentRoom;
			if (room == null)
			{
				return;
			}

			var matchConfig = room.Properties.SimulationMatchConfig.Value;
			// We create lookups so we don't have boxing situations happening during the gameplay
			_matchId = _services.NetworkService.QuantumClient.CurrentRoom.Name;
			_mutators = string.Join(",", matchConfig.Mutators);
			_matchType = matchConfig.MatchType.ToString();
			var rewards = new List<GameId>();
			if (matchConfig.MatchType == MatchType.Matchmaking)
			{
				_matchType = rewards.Contains(GameId.Trophies) ? "Ranked" : "Casual";
			}

			_gameModeId = matchConfig.GameModeID;
			var config = room.MapConfig;
			_mapId = config.Map.ToString();
		}

		/// <summary>
		/// Logs when we start the match
		/// </summary>
		public void MatchStart()
		{
			if (IsSpectator())
			{
				return;
			}

			try
			{
				_playerNumAttacks = 0;

				var room = _services.RoomService.CurrentRoom;
				var totalPlayers = room.PlayerCount;

				FetchPropertiesFromRoom();

				_analyticsService.LogEvent(new MatchStartEvent(_matchId, _matchType, _gameModeId, _mutators, totalPlayers, _mapId,
					(int)room.Properties.SimulationMatchConfig.Value.TeamSize));
			}
			catch (Exception e)
			{
				FLog.Error("Analytics exception raised. Execution not interrupted", e);
			}
		}

		/// <summary>
		/// Logs when finish the match
		/// </summary>
		public void MatchEnd(QuantumGame game, bool playerQuit, uint playerRank)
		{
			if (IsSpectator())
			{
				return;
			}

			if (!QuantumRunner.Default.IsDefinedAndRunning(false))
			{
				return;
			}

			var f = game.Frames.Verified;
			var localPlayerData = new QuantumPlayerMatchData(f, game.GetLocalPlayerRef());
			var totalPlayers = 0;

			for (var i = 0; i < f.PlayerCount; i++)
			{
				if (f.GetPlayerData(i) != null)
				{
					totalPlayers++;
				}
			}

			FetchPropertiesFromRoom();

			var kills = (int) localPlayerData.Data.PlayersKilledCount;
			var endState = playerQuit ? "quit" : "ended";

			_analyticsService.LogEvent(new MatchEndEvent(_matchId, _matchType, _gameModeId, _mutators, _mapId, totalPlayers,
				kills, endState, f.Time.AsFloat, (int) playerRank, _playerNumAttacks));

			_playerNumAttacks = 0;
		}

		/// <summary>
		/// Logs when an item is picked up
		/// </summary>
		private void MatchPickupAction(EventOnCollectableCollected callback)
		{
			var playerData = callback.Game.GetLocalPlayerData(true, out _);

			if (playerData.Entity != callback.CollectorEntity)
			{
				return;
			}

			FetchPropertiesFromRoom();

			_analyticsService.LogEvent(new MatchPickupActionEvent(_matchId, _matchType, _gameModeId, _mutators, _mapId,
				callback.CollectableId.ToString()));
		}

		private bool IsSpectator()
		{
			return _services.RoomService.IsLocalPlayerSpectator;
		}

		private void TrackPlayerAttack(EventOnPlayerAttack callback)
		{
			// TODO: Note: This reports incorrect information if a reconnect happens
			if (!callback.Game.PlayerIsLocal(callback.Player))
			{
				return;
			}

			_playerNumAttacks++;
		}
	}
}
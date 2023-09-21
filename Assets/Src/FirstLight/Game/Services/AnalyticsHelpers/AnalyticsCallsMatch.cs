using System;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using Newtonsoft.Json;
using Quantum;

namespace FirstLight.Game.Services.AnalyticsHelpers
{
	/// <summary>
	/// Analytics helper class regarding match events
	/// </summary>
	public class AnalyticsCallsMatch : AnalyticsCalls
	{
		private struct AnalyticsMatchQueuedEvent
		{
			public string EventName { get; }
			public Dictionary<string, object> Parameters { get; }

			public AnalyticsMatchQueuedEvent(string eventName, Dictionary<string, object> parameters)
			{
				EventName = eventName;
				Parameters = parameters;
			}
		}
		
		public string PresentedMapPath { get; set; }
		public Vector2IntSerializable DefaultDropPosition { get; set; }
		public Vector2IntSerializable SelectedDropPosition { get; set; }
		
		private IGameServices _services;
		private IGameDataProvider _gameData;

		private string _matchId;
		private string _mutators;
		private string _matchType;
		private string _gameModeId;
		private string _mapId;

		private Dictionary<GameId, string> _gameIdsLookup = new();

		private List<AnalyticsMatchQueuedEvent> _queue = new ();

		private int _playerNumAttacks;

		public AnalyticsCallsMatch(IAnalyticsService analyticsService,
		                           IGameServices services,
		                           IGameDataProvider gameDataProvider) : base(analyticsService)
		{
			_gameData = gameDataProvider;
			_services = services;

			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(MatchKillAction);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerDead>(MatchDeadAction);
			QuantumEvent.SubscribeManual<EventOnChestOpened>(this, MatchChestOpenAction);
			QuantumEvent.SubscribeManual<EventOnCollectableCollected>(MatchPickupAction);
			QuantumEvent.SubscribeManual<EventOnPlayerAttack>(TrackPlayerAttack);
			
			foreach (var gameId in (GameId[]) Enum.GetValues(typeof(GameId)))
			{
				_gameIdsLookup.Add(gameId, gameId.ToString());
			}
		}
		
		/// <summary>
		/// Logs when we entered the matchmaking room
		/// </summary>
		public void MatchInitiate()
		{
			var room = _services.RoomService.CurrentRoom;
			if (room == null)
			{
				return;
			}
			// We create lookups so we don't have boxing situations happening during the gameplay
			_matchId = _services.NetworkService.QuantumClient.CurrentRoom.Name;
			_mutators = string.Join(",", room.Properties.Mutators.Value);
			_matchType = room.Properties.MatchType.ToString();
			_gameModeId = room.Properties.GameModeId.Value;
			var config = room.MapConfig;
			var gameModeConfig = room.GameModeConfig;
			_mapId = ((int) config.Map).ToString();
			
			var data = new Dictionary<string, object>
			{
				{"match_id", _matchId},
				{"match_type", _matchType},
				{"game_mode", _gameModeId},
				{"mutators", _mutators},
				{"team_size", gameModeConfig.MaxPlayersInTeam },
				{"is_spectator", IsSpectator().ToString()},
				{"playfab_player_id", _gameData.AppDataProvider.PlayerId } // must be named PlayFabPlayerId or will create error
			};
			
			_analyticsService.LogEvent(AnalyticsEvents.MatchInitiate, data);
			
			_queue.Clear();
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
				var config = room.MapConfig;
				var gameModeConfig = room.GameModeConfig;
				var totalPlayers = room.PlayerCount;
				var loadout = _gameData.EquipmentDataProvider.Loadout;
				var ids = _gameData.UniqueIdDataProvider.Ids;

				loadout.TryGetValue(GameIdGroup.Weapon, out var weaponId);
				loadout.TryGetValue(GameIdGroup.Helmet, out var helmetId);
				loadout.TryGetValue(GameIdGroup.Shield, out var shieldId);
				loadout.TryGetValue(GameIdGroup.Armor, out var armorId);
				loadout.TryGetValue(GameIdGroup.Amulet, out var amuletId);

				var data = new Dictionary<string, object>
				{
					{"match_id", _matchId},
					{"match_type", _matchType},
					{"game_mode", _gameModeId},
					{"mutators", _mutators},
					{"player_level", _gameData.PlayerDataProvider.Level.Value.ToString()},
					{"total_players", totalPlayers.ToString()},
					{"total_bots", (room.GetMaxPlayers(false) - totalPlayers).ToString()},
					{"map_id", _gameIdsLookup[config.Map]},
					{"team_size", gameModeConfig.MaxPlayersInTeam},
					{"trophies_start", _gameData.PlayerDataProvider.Trophies.Value.ToString()},
					{"item_weapon", weaponId == UniqueId.Invalid ? "" : _gameIdsLookup[ids[weaponId]]},
					{"item_helmet", helmetId == UniqueId.Invalid ? "" : _gameIdsLookup[ids[helmetId]]},
					{"item_shield", shieldId == UniqueId.Invalid ? "" : _gameIdsLookup[ids[shieldId]]},
					{"item_armour", armorId == UniqueId.Invalid ? "" : _gameIdsLookup[ids[armorId]]},
					{"item_amulet", amuletId == UniqueId.Invalid ? "" : _gameIdsLookup[ids[amuletId]]},
					{"drop_location_default", JsonConvert.SerializeObject(DefaultDropPosition)},
					{"drop_location_final", JsonConvert.SerializeObject(SelectedDropPosition)}
				};

				if (PresentedMapPath != null)
				{
					data.Add("drop_open_grid", PresentedMapPath);
				}

				_analyticsService.LogEvent(AnalyticsEvents.MatchStart, data);
			}
			catch (Exception e)
			{
				FLog.Error("Analytics exception raised. Execution not interrupted", e);
			}
		}

		/// <summary>
		/// Logs when finish the match
		/// </summary>
		public void MatchEndBRPlayerDead(QuantumGame game, uint playerRank)
		{
			if (IsSpectator())
			{
				return;
			}

			SendQueue();
			
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
			
			var data = new Dictionary<string, object>
			{
				{"match_id", _matchId},
				{"match_type", _matchType},
				{"game_mode", _gameModeId},
				{"mutators", _mutators},
				{"map_id", _mapId},
				{"players_left", totalPlayers.ToString()},
				{"suicide",localPlayerData.Data.SuicideCount.ToString()},
				{"kills", localPlayerData.Data.PlayersKilledCount.ToString()},
				{"match_time", f.Time.ToString()},
				{"player_rank", playerRank.ToString()},
				{"team_id", localPlayerData.Data.TeamId },
				{"team_size", f.Context.GameModeConfig.MaxPlayersInTeam },
				{"player_attacks", _playerNumAttacks.ToString()}
			};
			
			_analyticsService.LogEvent(AnalyticsEvents.MatchEndBattleRoyalePlayerDead, data);
			
			_playerNumAttacks = 0;
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

			SendQueue();

			if (!QuantumRunner.Default.IsDefinedAndRunning())
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
			
			var data = new Dictionary<string, object>
			{
				{"match_id", _matchId},
				{"match_type", _matchType},
				{"game_mode", _gameModeId},
				{"mutators", _mutators},
				{"map_id", _mapId},
				{"players_left", totalPlayers.ToString()},
				{"suicide",localPlayerData.Data.SuicideCount.ToString()},
				{"kills", localPlayerData.Data.PlayersKilledCount.ToString()},
				{"end_state", playerQuit ? "quit" : "ended"},
				{"match_time", f.Time.ToString()},
				{"player_rank", playerRank.ToString()},
				{"player_attacks", _playerNumAttacks.ToString()},
				{"team_id", localPlayerData.Data.TeamId },
				{"team_size", f.Context.GameModeConfig.MaxPlayersInTeam }
			};
			
			_analyticsService.LogEvent(AnalyticsEvents.MatchEnd, data);
			
			_playerNumAttacks = 0;
		}

		/// <summary>
		/// Logs when a player kills another player
		/// </summary>
		public void MatchKillAction(EventOnPlayerKilledPlayer playerKilledEvent)
		{
			// We cannot send this event for everyone every time so we only send if we are the killer or its a suicide
			if (!playerKilledEvent.Game.PlayerIsLocal(playerKilledEvent.PlayerKiller) || 
					playerKilledEvent.Game.PlayerIsLocal(playerKilledEvent.PlayerDead))
			{
				return;
			}
			
			var killerData = playerKilledEvent.PlayersMatchData[playerKilledEvent.PlayerKiller];
			
			// We send fixed name in case of offline Tutorial match
			var deadName = playerKilledEvent.PlayersMatchData.Count <= 1 ?
				               "Dummy" :
				               playerKilledEvent.PlayersMatchData[playerKilledEvent.PlayerDead].GetPlayerName();

			var data = new Dictionary<string, object>
			{
				{"match_id", _matchId},
				{"match_type", _matchType},
				{"game_mode", _gameModeId},
				{"mutators", _mutators},
				{"killed_name", deadName},
				{"killed_reason", "player"},
				{"player_name", killerData.GetPlayerName()}
			};
			
			QueueEvent(AnalyticsEvents.MatchKillAction, data);
		}
		
		/// <summary>
		/// Logs when a player dies
		/// </summary>
		private void MatchDeadAction(EventOnLocalPlayerDead playerDeadEvent)
		{
			// We cannot send this event for everyone every time so we only send if we are the killer or we were killed by a bot
			if (!playerDeadEvent.Game.PlayerIsLocal(playerDeadEvent.Player))
			{
				return;
			}

			var frame = playerDeadEvent.Game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var playerData = container.GeneratePlayersMatchData(frame, out _, out _);
			
			var deadData = playerData[playerDeadEvent.Player];
			
			string killerName = "";
			bool isKillerBot = false;
			if (playerDeadEvent.PlayerKiller.IsValid)
			{
				var killerData = playerData[playerDeadEvent.PlayerKiller];
				killerName = killerData.GetPlayerName();
				isKillerBot = killerData.IsBot;
			}
			
			var data = new Dictionary<string, object>
			{
				{"match_id", _matchId},
				{"match_type", _matchType},
				{"game_mode", _gameModeId},
				{"mutators", _mutators},
				{"killed_name", deadData.GetPlayerName()},
				{"killed_reason", playerDeadEvent.Entity == playerDeadEvent.EntityKiller? "suicide":(isKillerBot?"bot":"player")},
				{"killer_name", killerName}
			};
			
			QueueEvent(AnalyticsEvents.MatchDeadAction, data);
		}


		/// <summary>
		/// Logs when a chest is opened
		/// </summary>
		public void MatchChestOpenAction(EventOnChestOpened callback)
		{
			if (!(callback.Game.PlayerIsLocal(callback.Player)))
			{
				return;
			}
			
			var data = new Dictionary<string, object>
			{
				{"match_id", _matchId},
				{"match_type", _matchType},
				{"game_mode", _gameModeId},
				{"mutators", _mutators},
				{"chest_type", _gameIdsLookup[callback.ChestType]},
				{"chest_coordinates", callback.ChestPosition.ToString()},
				{"player_name", _gameData.AppDataProvider.DisplayNameTrimmed }
			};
			
			QueueEvent(AnalyticsEvents.MatchChestOpenAction, data);

			foreach (var item in callback.Items)
			{
				MatchChestItemDrop(item, callback.Game);
			}
		}
		
		/// <summary>
		/// Logs when a chest item is dropped
		/// </summary>
		public void MatchChestItemDrop(ChestItemDropped chestItemDropped, QuantumGame game)
		{
			if (!(game.PlayerIsLocal(chestItemDropped.Player)))
			{
				return;
			}

			var data = new Dictionary<string, object>
			{
				{"match_id", _matchId},
				{"match_type", _matchType},
				{"game_mode", _gameModeId},
				{"mutators", _mutators},
				{"chest_type", _gameIdsLookup[chestItemDropped.ChestType]},
				{"chest_coordinates", chestItemDropped.ChestPosition.ToString()},
				{"item_type", _gameIdsLookup[chestItemDropped.ItemType]},
				{"amount", chestItemDropped.Amount.ToString()},
				{"angle_step_around_chest", chestItemDropped.AngleStepAroundChest.ToString()}
			};
			
			QueueEvent(AnalyticsEvents.MatchChestItemDrop, data);
		}
		
		/// <summary>
		/// Logs when an item is picked up
		/// </summary>
		public void MatchPickupAction(EventOnCollectableCollected callback)
		{
			if (!(callback.Game.PlayerIsLocal(callback.Player)))
			{
				return;
			}
			
			var data = new Dictionary<string, object>
			{
				{"match_id", _matchId},
				{"match_type", _matchType},
				{"game_mode", _gameModeId},
				{"mutators", _mutators},
				{"item_type", _gameIdsLookup[callback.CollectableId]},
				{"amount", "1"},
				{"player_name", _gameData.AppDataProvider.DisplayNameTrimmed }
			};
			
			QueueEvent(AnalyticsEvents.MatchPickupAction, data);
		}
		
		private bool IsSpectator()
		{
			return _services.NetworkService.LocalPlayer.IsSpectator();
		}

		private void TrackPlayerAttack(EventOnPlayerAttack callback)
		{
			if (!callback.Game.PlayerIsLocal(callback.Player))
			{
				return;
			}
			
			_playerNumAttacks++;
		}

		private void QueueEvent(string eventName, Dictionary<string, object> parameters = null)
		{
			parameters?.Add("custom_event_timestamp", DateTime.UtcNow);
			
			_queue.Add(new AnalyticsMatchQueuedEvent(eventName, parameters));
		}

		private void SendQueue()
		{
			foreach (var matchEvent in _queue)
			{
				_analyticsService.LogEvent(matchEvent.EventName, matchEvent.Parameters);
			}
			
			_queue.Clear();
		}
	}
}

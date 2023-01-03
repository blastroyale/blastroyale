using System;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using Newtonsoft.Json;
using PlayFab;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Services.AnalyticsHelpers
{
	/// <summary>
	/// Analytics helper class regarding match events
	/// </summary>
	public class AnalyticsCallsMatch : AnalyticsCalls
	{
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

		private int _playerNumAttacks;

		public AnalyticsCallsMatch(IAnalyticsService analyticsService,
		                           IGameServices services,
		                           IGameDataProvider gameDataProvider) : base(analyticsService)
		{
			_gameData = gameDataProvider;
			_services = services;

			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(MatchKillAction);
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
			var room = _services.NetworkService.QuantumClient.CurrentRoom;
			
			// We create lookups so we don't have boxing situations happening during the gameplay
			_matchId = _services.NetworkService.QuantumClient.CurrentRoom.Name;
			_mutators = string.Join(",", room.GetMutatorIds());
			_matchType = room.GetMatchType().ToString();
			_gameModeId = room.GetGameModeId();
			var config = _services.ConfigsProvider.GetConfig<QuantumMapConfig>(room.GetMapId());
			_mapId = ((int) config.Map).ToString();
			
			var data = new Dictionary<string, object>
			{
				{"match_id", _matchId},
				{"match_type", _matchType},
				{"game_mode", _gameModeId},
				{"mutators", _mutators},
				{"is_spectator", IsSpectator().ToString()},
				{"playfab_player_id", _gameData.AppDataProvider.PlayerId } // must be named PlayFabPlayerId or will create error
			};
			
			_analyticsService.LogEvent(AnalyticsEvents.MatchInitiate, data);
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
			
			_playerNumAttacks = 0;
			
			var room = _services.NetworkService.QuantumClient.CurrentRoom;
			var config = _services.ConfigsProvider.GetConfig<QuantumMapConfig>(room.GetMapId());
			var gameModeConfig = _services.ConfigsProvider.GetConfig<QuantumGameModeConfig>(room.GetGameModeId().GetHashCode());
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
				{"player_level", _gameData.PlayerDataProvider.PlayerInfo.Level.ToString()},
				{"total_players", totalPlayers.ToString()},
				{"total_bots", (NetworkUtils.GetMaxPlayers(gameModeConfig, config) - totalPlayers).ToString()},
				{"map_id", _gameIdsLookup[config.Map]},
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

		/// <summary>
		/// Logs when finish the match
		/// </summary>
		public void MatchEndBRPlayerDead(QuantumGame game)
		{
			if (IsSpectator())
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
				{"match_time", f.Time.ToString()},
				{"player_rank", localPlayerData.PlayerRank.ToString()},
				{"player_attacks", _playerNumAttacks.ToString()}
			};
			
			_analyticsService.LogEvent(AnalyticsEvents.MatchEndBattleRoyalePlayerDead, data);
			
			_playerNumAttacks = 0;
		}

		/// <summary>
		/// Logs when finish the match
		/// </summary>
		public void MatchEnd(QuantumGame game, bool playerQuit)
		{
			if (IsSpectator())
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
				{"player_rank", localPlayerData.PlayerRank.ToString()},
				{"player_attacks", _playerNumAttacks.ToString()}
			};
			
			_analyticsService.LogEvent(AnalyticsEvents.MatchEnd, data);
			
			_playerNumAttacks = 0;
		}

		/// <summary>
		/// Logs when a player kills another player
		/// </summary>
		public void MatchKillAction(EventOnPlayerKilledPlayer playerKilledEvent)
		{
			var killerData = playerKilledEvent.PlayersMatchData[playerKilledEvent.PlayerKiller];

			// We cannot send this event for everyone every time so we only send if we are the killer or we were killed by a bot
			if (!(playerKilledEvent.Game.PlayerIsLocal(playerKilledEvent.PlayerKiller) || 
			    (killerData.IsBot && playerKilledEvent.Game.PlayerIsLocal(playerKilledEvent.PlayerDead))))
			{
				return;
			}

			var deadData = playerKilledEvent.PlayersMatchData[playerKilledEvent.PlayerDead];

			var data = new Dictionary<string, object>
			{
				{"match_id", _matchId},
				{"match_type", _matchType},
				{"game_mode", _gameModeId},
				{"mutators", _mutators},
				{"killed_name", deadData.GetPlayerName()},
				{"killed_reason", playerKilledEvent.EntityDead == playerKilledEvent.EntityKiller? "suicide":(killerData.IsBot?"bot":"player")},
				{"killer_name", killerData.GetPlayerName()}
			};
			
			_analyticsService.LogEvent(AnalyticsEvents.MatchKillAction, data, false);
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
			
			_analyticsService.LogEvent(AnalyticsEvents.MatchChestOpenAction, data, false);

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
			
			_analyticsService.LogEvent(AnalyticsEvents.MatchChestItemDrop, data, false);
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
			
			_analyticsService.LogEvent(AnalyticsEvents.MatchPickupAction, data, false);
		}
		
		private bool IsSpectator()
		{
			return _services.NetworkService.QuantumClient.LocalPlayer.IsSpectator();
		}

		private void TrackPlayerAttack(EventOnPlayerAttack callback)
		{
			if (!callback.Game.PlayerIsLocal(callback.Player))
			{
				return;
			}
			
			_playerNumAttacks++;
		}
	}
}

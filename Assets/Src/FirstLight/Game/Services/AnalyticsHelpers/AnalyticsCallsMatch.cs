using System.Collections.Generic;
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
		}

		/// <summary>
		/// Logs when we entered the matchmaking room
		/// </summary>
		public void MatchInitiate()
		{
			var room = _services.NetworkService.QuantumClient.CurrentRoom;
			
			var data = new Dictionary<string, object>
			{
				{"match_id", _services.NetworkService.QuantumClient.CurrentRoom.Name},
				{"match_type", room.GetMatchType().ToString()},
				{"game_mode", room.GetGameModeId()},
				{"mutators", string.Join(",",room.GetMutatorIds())},
				{"PlayerId", PlayFabSettings.staticPlayer.PlayFabId}
			};
			
			_analyticsService.LogEvent(AnalyticsEvents.MatchInitiate, data);
		}
		
		/// <summary>
		/// Logs when we start the match
		/// </summary>
		public void MatchStart()
		{
			_playerNumAttacks = 0;
			
			var room = _services.NetworkService.QuantumClient.CurrentRoom;
			var config = _services.ConfigsProvider.GetConfig<QuantumMapConfig>(room.GetMapId());
			var gameModeConfig = _services.ConfigsProvider.GetConfig<QuantumGameModeConfig>(room.GetGameModeId().GetHashCode());
			var totalPlayers = room.PlayerCount;
			var loadout = _gameData.EquipmentDataProvider.Loadout;

			loadout.TryGetValue(GameIdGroup.Weapon, out var weaponId);
			loadout.TryGetValue(GameIdGroup.Helmet, out var helmetId);
			loadout.TryGetValue(GameIdGroup.Shield, out var shieldId);
			loadout.TryGetValue(GameIdGroup.Armor, out var armorId);
			loadout.TryGetValue(GameIdGroup.Amulet, out var amuletId);
			
			var data = new Dictionary<string, object>
			{
				{"match_id", room.Name},
				{"match_type", room.GetMatchType().ToString()},
				{"game_mode", room.GetGameModeId()},
				{"mutators", string.Join(",",room.GetMutatorIds())},
				{"player_level", _gameData.PlayerDataProvider.PlayerInfo.Level},
				{"total_players", totalPlayers},
				{"total_bots", NetworkUtils.GetMaxPlayers(gameModeConfig, config) - totalPlayers},
				{"map_id", (int) config.Map},
				{"trophies_start", _gameData.PlayerDataProvider.Trophies.Value},
				{"item_weapon", weaponId},
				{"item_helmet", helmetId},
				{"item_shield", shieldId},
				{"item_armour", armorId},
				{"item_amulet", amuletId},
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
		public void MatchEnd(int totalPlayers, bool playerQuit, float matchTime, QuantumPlayerMatchData matchData)
		{
			var room = _services.NetworkService.QuantumClient.CurrentRoom;
			var config = _services.ConfigsProvider.GetConfig<QuantumMapConfig>(room.GetMapId());
			
			var data = new Dictionary<string, object>
			{
				{"match_id", room.Name},
				{"match_type", room.GetMatchType().ToString()},
				{"game_mode", room.GetGameModeId()},
				{"mutators", string.Join(",",room.GetMutatorIds())},
				{"map_id", (int) config.Map},
				{"players_left", totalPlayers},
				{"suicide",matchData.Data.SuicideCount},
				{"kills", matchData.Data.PlayersKilledCount},
				{"end_state", playerQuit ? "quit" : "ended"},
				{"match_time", matchTime},
				{"player_rank", matchData.PlayerRank},
				{"player_attacks", _playerNumAttacks}
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
			
			var room = _services.NetworkService.QuantumClient.CurrentRoom;
			var deadData = playerKilledEvent.PlayersMatchData[playerKilledEvent.PlayerDead];

			var data = new Dictionary<string, object>
			{
				{"match_id", room.Name},
				{"match_type", room.GetMatchType().ToString()},
				{"game_mode", room.GetGameModeId()},
				{"mutators", string.Join(",",room.GetMutatorIds())},
				{"killed_name", (deadData.IsBot?"Bot":"") + deadData.PlayerName},
				{"killed_reason", playerKilledEvent.EntityDead == playerKilledEvent.EntityKiller? "suicide":(killerData.IsBot?"bot":"player")},
				{"killer_name", (killerData.IsBot?"Bot":"") + killerData.PlayerName}
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
			
			var room = _services.NetworkService.QuantumClient.CurrentRoom;
			var frame = callback.Game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			
			var playerData = container.GetPlayersMatchData(frame, out var leader)[callback.Player];

			var data = new Dictionary<string, object>
			{
				{"match_id", room.Name},
				{"match_type", room.GetMatchType().ToString()},
				{"game_mode", room.GetGameModeId()},
				{"mutators", string.Join(",",room.GetMutatorIds())},
				{"chest_type", callback.ChestType.ToString()},
				{"chest_coordinates", callback.ChestPosition.ToString()},
				{"player_name", playerData.PlayerName }
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
			
			var room = _services.NetworkService.QuantumClient.CurrentRoom;

			var data = new Dictionary<string, object>
			{
				{"match_id", room.Name},
				{"match_type", room.GetMatchType().ToString()},
				{"game_mode", room.GetGameModeId()},
				{"mutators", string.Join(",",room.GetMutatorIds())},
				{"chest_type", chestItemDropped.ChestType.ToString()},
				{"chest_coordinates", chestItemDropped.ChestPosition.ToString()},
				{"item_type", chestItemDropped.ItemType.ToString()},
				{"amount", chestItemDropped.Amount},
				{"angle_step_around_chest", chestItemDropped.AngleStepAroundChest}
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
			
			var room = _services.NetworkService.QuantumClient.CurrentRoom;
			var data = new Dictionary<string, object>
			{
				{"match_id", room.Name},
				{"match_type", room.GetMatchType().ToString()},
				{"game_mode", room.GetGameModeId()},
				{"mutators", string.Join(",",room.GetMutatorIds())},
				{"item_type", callback.CollectableId.ToString()},
				{"amount", 1},
				{"player_name", _gameData.AppDataProvider.DisplayNameTrimmed }
			};
			
			_analyticsService.LogEvent(AnalyticsEvents.MatchPickupAction, data, false);
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

using System.Collections.Generic;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using PlayFab;
using Quantum;

namespace FirstLight.Game.Services.AnalyticsHelpers
{
	/// <summary>
	/// Analytics helper class regarding match events
	/// </summary>
	public class AnalyticsCallsMatch : AnalyticsCalls
	{
		private IGameServices _services;
		private IGameDataProvider _gameData;
		
		public string PresentedMapPath { get; set; }
		public Vector2IntSerializable DefaultDropPosition { get; set; }
		public Vector2IntSerializable SelectedDropPosition { get; set; }

		public AnalyticsCallsMatch(IAnalyticsService analyticsService,
		                           IGameServices services,
		                           IGameDataProvider gameDataProvider) : base(analyticsService)
		{
			_gameData = gameDataProvider;
			_services = services;

			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(MatchKillAction);
			QuantumEvent.SubscribeManual<EventOnChestOpened>(this, MatchChestOpenAction);
			QuantumEvent.SubscribeManual<EventOnChestItemDropped>(MatchChestItemDrop);
			QuantumEvent.SubscribeManual<EventOnCollectableCollected>(MatchPickupAction);
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
				{"match_type", room.GetMatchType()},
				{"game_mode", _services.GameModeService.SelectedGameMode.Value.Entry.GameModeId},
				{"mutators", string.Join(",",_services.GameModeService.SelectedGameMode.Value.Entry.Mutators)},
				{"PlayerId", PlayFabSettings.staticPlayer.PlayFabId}
			};
			
			_analyticsService.LogEvent(AnalyticsEvents.MatchInitiate, data);
		}
		
		/// <summary>
		/// Logs when we start the match
		/// </summary>
		public void MatchStart()
		{
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
				{"match_type",room.GetMatchType()},
				{"game_mode", _services.GameModeService.SelectedGameMode.Value.Entry.GameModeId},
				{"mutators", string.Join(",",_services.GameModeService.SelectedGameMode.Value.Entry.Mutators)},
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
				{"drop_open_grid", PresentedMapPath},
				{"drop_location_default", DefaultDropPosition},
				{"drop_location_final", SelectedDropPosition}
			};
			
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
				{"match_type",room.GetMatchType()},
				{"game_mode", _services.GameModeService.SelectedGameMode.Value.Entry.GameModeId},
				{"mutators", string.Join(",",_services.GameModeService.SelectedGameMode.Value.Entry.Mutators)},
				{"map_id", (int) config.Map},
				{"players_left", totalPlayers},
				{"suicide",matchData.Data.SuicideCount},
				{"kills", matchData.Data.PlayersKilledCount},
				{"end_state", playerQuit ? "quit" : "ended"},
				{"match_time", matchTime},
				{"player_rank", matchData.PlayerRank},
			};
			
			_analyticsService.LogEvent(AnalyticsEvents.MatchEnd, data);
		}

		/// <summary>
		/// Logs when a player kills another player
		/// </summary>
		public void MatchKillAction(EventOnPlayerKilledPlayer playerKilledEvent)
		{
			var killerData = playerKilledEvent.PlayersMatchData.Find(data => data.Data.Player.Equals(playerKilledEvent.PlayerKiller));

			// We cannot send this event for everyone every time so we only send if we are the killer or we were killed by a bot
			if (!(playerKilledEvent.Game.PlayerIsLocal(playerKilledEvent.PlayerKiller) || 
			    (killerData.Data.IsBot && playerKilledEvent.Game.PlayerIsLocal(playerKilledEvent.PlayerDead))))
			{
				return;
			}
			
			var room = _services.NetworkService.QuantumClient.CurrentRoom;
			var deadData = playerKilledEvent.PlayersMatchData.Find(data => data.Data.Player.Equals(playerKilledEvent.PlayerDead));

			var data = new Dictionary<string, object>
			{
				{"match_id", room.Name},
				{"match_type",room.GetMatchType()},
				{"game_mode", _services.GameModeService.SelectedGameMode.Value.Entry.GameModeId},
				{"mutators", string.Join(",",_services.GameModeService.SelectedGameMode.Value.Entry.Mutators)},
				{"killed_name", (deadData.Data.IsBot?"Bot":"") + deadData.PlayerName},
				{"killed_reason", playerKilledEvent.EntityDead == playerKilledEvent.EntityKiller? "suicide":(killerData.Data.IsBot?"bot":"player")},
				{"killer_name", (killerData.Data.IsBot?"Bot":"") + killerData.PlayerName}
			};
			
			_analyticsService.LogEvent(AnalyticsEvents.MatchKillAction, data);
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

			var data = new Dictionary<string, object>
			{
				{"match_id", room.Name},
				{"match_type",room.GetMatchType()},
				{"game_mode", _services.GameModeService.SelectedGameMode.Value.Entry.GameModeId},
				{"mutators", string.Join(",",_services.GameModeService.SelectedGameMode.Value.Entry.Mutators)},
				{"chest_type", callback.ChestType.ToString()},
				{"chest_coordinates", callback.ChestPosition.ToString()}
			};
			
			_analyticsService.LogEvent(AnalyticsEvents.MatchChestOpenAction, data);
		}
		
		/// <summary>
		/// Logs when a chest item is dropped
		/// </summary>
		public void MatchChestItemDrop(EventOnChestItemDropped callback)
		{
			if (!(callback.Game.PlayerIsLocal(callback.Player)))
			{
				return;
			}
			
			var room = _services.NetworkService.QuantumClient.CurrentRoom;

			var data = new Dictionary<string, object>
			{
				{"match_id", room.Name},
				{"match_type",room.GetMatchType()},
				{"game_mode", _services.GameModeService.SelectedGameMode.Value.Entry.GameModeId},
				{"mutators", string.Join(",",_services.GameModeService.SelectedGameMode.Value.Entry.Mutators)},
				{"chest_type", callback.ChestType.ToString()},
				{"chest_coordinates", callback.ChestPosition.ToString()},
				{"item_type", callback.ItemType.ToString()},
				{"amount", callback.Amount},
				{"angle_step_around_chest", callback.AngleStepAroundChest}
			};
			
			_analyticsService.LogEvent(AnalyticsEvents.MatchChestItemDrop, data);
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
			var frame = callback.Game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			
			var playerData = container.GetPlayersMatchData(frame, out var leader).Find(data => data.Data.Player.Equals(callback.Player));

			var data = new Dictionary<string, object>
			{
				{"match_id", room.Name},
				{"match_type",room.GetMatchType()},
				{"game_mode", _services.GameModeService.SelectedGameMode.Value.Entry.GameModeId},
				{"mutators", string.Join(",",_services.GameModeService.SelectedGameMode.Value.Entry.Mutators)},
				{"item_type", callback.CollectableId.ToString()},
				{"amount", 1},
				{"player_name", playerData.PlayerName }
			};
			
			_analyticsService.LogEvent(AnalyticsEvents.MatchChestItemDrop, data);
		}
	}
}

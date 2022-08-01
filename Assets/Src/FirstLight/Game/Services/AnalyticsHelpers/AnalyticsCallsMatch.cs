using System.Collections.Generic;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
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
		}

		/// <summary>
		/// Logs when we entered the matchmaking room
		/// </summary>
		public void MatchInitiate()
		{
			var data = new Dictionary<string, object>
			{
				{"match_id", _services.NetworkService.QuantumClient.CurrentRoom.Name},
				{"match_type",_gameData.AppDataProvider.SelectedGameMode.Value},
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
				{"player_level", _gameData.PlayerDataProvider.Level.Value},
				{"total_players", totalPlayers},
				{"total_bots", config.PlayersLimit - totalPlayers},
				{"map_id", config.Id},
				{"map_name", config.Map},
				{"trophies_start", _gameData.PlayerDataProvider.Trophies.Value},
				{"item_weapon", weaponId},
				{"item_helmet", helmetId},
				{"item_shield", shieldId},
				{"item_armour", armorId},
				{"item_amulet", amuletId},
				{"drop_open_grid", PresentedMapPath},
				{"drop_location_default", DefaultDropPosition},
				{"drop_location_final", SelectedDropPosition},
				{"match_type",_gameData.AppDataProvider.SelectedGameMode.Value}
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
				{"match_type",_gameData.AppDataProvider.SelectedGameMode.Value},
				{"map_id", config.Id},
				{"map_name", config.Map},
				{"players_left", totalPlayers},
				{"suicide",matchData.Data.SuicideCount},
				{"kills", matchData.Data.PlayersKilledCount},
				{"end_state", playerQuit ? "quit" : "ended"},
				{"match_time", matchTime},
				{"player_rank", matchData.PlayerRank},
			};
			
			_analyticsService.LogEvent(AnalyticsEvents.MatchEnd, data);
		}
	}
}
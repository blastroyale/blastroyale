using System.Collections.Generic;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using Quantum;

namespace FirstLight.Game.Services.AnalyticsHelpers
{
	public class AnalyticsCallsMatch : AnalyticsCalls
	{
		public AnalyticsCallsMatch(IAnalyticsService analyticsService) : base(analyticsService)
		{
		}
		
		public void MatchStart()
		{
			var gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			var services = MainInstaller.Resolve<IGameServices>();
			var room = services.NetworkService.QuantumClient.CurrentRoom;
			var config = services.ConfigsProvider.GetConfig<QuantumMapConfig>(room.GetMapId());
			var totalPlayers = services.NetworkService.QuantumClient.CurrentRoom.PlayerCount;

			gameDataProvider.EquipmentDataProvider.Loadout.TryGetValue(GameIdGroup.Weapon, out var weaponId);
			gameDataProvider.EquipmentDataProvider.Loadout.TryGetValue(GameIdGroup.Helmet, out var helmetId);
			gameDataProvider.EquipmentDataProvider.Loadout.TryGetValue(GameIdGroup.Shield, out var shieldId);
			gameDataProvider.EquipmentDataProvider.Loadout.TryGetValue(GameIdGroup.Armor, out var armorId);
			gameDataProvider.EquipmentDataProvider.Loadout.TryGetValue(GameIdGroup.Amulet, out var amuletId);
			
			var data = new Dictionary<string, object>
			{
				{"player_level", gameDataProvider.PlayerDataProvider.Level.Value},
				{"total_players", totalPlayers},
				{"total_bots", config.PlayersLimit - totalPlayers},
				{"map_id", config.Id},
				{"map_name", config.Map},
				{"trophies_start", gameDataProvider.PlayerDataProvider.Trophies.Value},
				{"item_weapon", weaponId},
				{"item_helmet", helmetId},
				{"item_shield", shieldId},
				{"item_armour", armorId},
				{"item_amulet", amuletId},
				{"drop_open_grid",gameDataProvider.AppDataProvider.PresentedMapPath},
				{"drop_location_default", gameDataProvider.AppDataProvider.DefaultDropPosition},
				{"drop_location_final", gameDataProvider.AppDataProvider.SelectedDropPosition},
				{"match_type",gameDataProvider.AppDataProvider.SelectedGameMode.Value}
			};
			
			_analyticsService.LogEvent(AnalyticsEvents.MatchStart, data);
		}
	}
}
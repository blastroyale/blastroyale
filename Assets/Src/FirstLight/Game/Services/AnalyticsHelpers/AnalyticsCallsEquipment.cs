using System.Collections.Generic;
using FirstLight.Game.Infos;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;

namespace FirstLight.Game.Services.AnalyticsHelpers
{
	public class AnalyticsCallsEquipment : AnalyticsCalls
	{
		private readonly IConfigsProvider _configsProvider;

		public AnalyticsCallsEquipment(IAnalyticsService analyticsService, IConfigsProvider configsProvider) :
			base(analyticsService)
		{
			_configsProvider = configsProvider;
		}

		public void EquipItem(EquipmentInfo info)
		{
			_analyticsService.LogEvent(AnalyticsEvents.ItemEquipAction, GetEquipActionData(info, true));
		}

		public void UnequipItem(EquipmentInfo info)
		{
			_analyticsService.LogEvent(AnalyticsEvents.ItemEquipAction, GetEquipActionData(info, false));
		}

		private Dictionary<string, object> GetEquipActionData(EquipmentInfo info, bool equip)
		{
			return new Dictionary<string, object>
			{
				{"item_type", info.Equipment.GetEquipmentGroup().ToString()},
				{"action", equip ? "equip" : "unequip"},
				{"item_id", info.Id},
				{"is_nft", info.IsNft},
				{"item_level", info.Equipment.Level},
				{"item_rarity", info.Equipment.Rarity.ToString()},
				{"replication_count", info.Equipment.ReplicationCounter},
				{"health", info.Stats[EquipmentStatType.Hp]},
				{"speed", info.Stats[EquipmentStatType.Speed]},
				{"armour", info.Stats[EquipmentStatType.Armor]},
				{"damage", info.Stats[EquipmentStatType.Power]},
				{"durability", info.CurrentDurability},
				{"power", new List<Equipment> {info.Equipment}.GetTotalMight(_configsProvider)}
			};
		}
	}
}
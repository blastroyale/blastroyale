using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using Quantum;

namespace FirstLight.Game.Infos
{
	public struct FusionInfo
	{
		public List<EquipmentDataInfo> FusingItems;
		public Dictionary<GameIdGroup, uint> ResultPercentages;
		public ItemRarity FusingRarity;
		public ItemRarity FusingResultRarity;
		public uint FusingCost;
	}
}
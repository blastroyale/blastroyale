using System.Collections.Generic;

namespace FirstLight.Game.Infos
{
	public struct EnhancementInfo
	{
		public List<EquipmentDataInfo> EnhancementItems;
		public EquipmentDataInfo EnhancementResult;
		public uint EnhancementCost;
		public uint EnhancementItemRequiredAmount;
		public bool HasEquippedItem;
	}
}
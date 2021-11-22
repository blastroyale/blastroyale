using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Services;

namespace FirstLight.Game.Messages
{
	public struct ItemEquippedMessage : IMessage
	{
		public UniqueId ItemId;
	}

	public struct ItemUnequippedMessage : IMessage
	{
		public UniqueId ItemId;
	}

	public struct ItemUpgradedMessage : IMessage
	{
		public UniqueId ItemId;
		public uint PreviousLevel;
		public uint NewLevel;
	}
	
	public struct ItemSoldMessage : IMessage
	{
		public UniqueId ItemId;
		public uint SellAmount;
	}
	
	public struct ItemsFusedMessage : IMessage
	{
		public EquipmentDataInfo ResultItem;
	}
	
	public struct ItemsEnhancedMessage : IMessage
	{
		public EquipmentDataInfo ResultItem;
	}
}
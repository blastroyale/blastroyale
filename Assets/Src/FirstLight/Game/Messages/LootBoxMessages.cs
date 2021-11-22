using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Services;
using Quantum;

namespace FirstLight.Game.Messages
{
	public struct LootBoxUnlockingMessage : IMessage
	{
		public UniqueId LootBoxId;
	}
	
	public struct LootBoxHurryCompletedMessage : IMessage
	{
		public UniqueId LootBoxId;
	}
	
	public struct LootBoxOpenedMessage : IMessage
	{
		public UniqueId LootBoxId;
		public LootBoxInfo LootBoxInfo;
		public List<EquipmentDataInfo> LootBoxContent;
	}

	public struct LootBoxCollectedAllMessage : IMessage { }

}
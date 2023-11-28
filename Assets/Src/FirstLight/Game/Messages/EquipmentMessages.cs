using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.SDK.Services;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Messages
{
	public struct ItemEquippedMessage : IMessage
	{
		public PlayerCharacterViewMonoComponent Character;
		public GameObject Item;
		public GameId Id;
	}
	
	public struct ItemScrappedMessage : IMessage
	{
		public UniqueId Id;
		public GameId GameId;
		public string Name;
		public float Durability;
		public Pair<GameId, uint> Reward;
	}
	
	public struct ItemUpgradedMessage : IMessage
	{
		public UniqueId Id;
		public GameId GameId;
		public string Name;
		public float Durability;
		public uint Level;
		public Pair<GameId, uint> Price;
	}

	public struct ItemFusedMessage : IMessage
	{
		public UniqueId Id;
		public GameId GameId;
		public string Name;
		public float Durability;
		public EquipmentRarity rarity;
		public Pair<GameId, uint>[] Price;
	}

	public struct ItemRepairedMessage : IMessage
	{
		public UniqueId Id;
		public GameId GameId;
		public string Name;
		public float Durability;
		public float DurabilityFinal;
		public Pair<GameId, uint> Price;
	}
}
using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using Quantum;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This logic provides the necessary behaviour to manage the player's equipment
	/// </summary>
	public interface IEquipmentDataProvider
	{
		/// <summary>
		/// Requests the player's Equipped Items.
		/// </summary>
		IObservableDictionaryReader<GameIdGroup, UniqueId> EquippedItems { get; }
		/// <summary>
		/// Requests the player's inventory.
		/// </summary>
		IObservableListReader<EquipmentData> Inventory { get; }

		/// <summary>
		/// Requests the <see cref="EquipmentData"/> representing the item with the given <paramref name="itemId"/> in the inventory
		/// </summary>
		/// <exception cref="LogicException">
		/// Thrown if the item with the given <paramref name="itemId"/> is not present in the inventory
		/// </exception>
		EquipmentDataInfo GetEquipmentDataInfo(UniqueId itemId);

		/// <summary>
		/// Requests the <see cref="EquipmentInfo"/> representing the given <paramref name="itemId"/>
		/// </summary>
		EquipmentInfo GetEquipmentInfo(UniqueId itemId);
		/// <summary>
		/// Requests the <see cref="EquipmentInfo"/> representing a generic equipment of the given <paramref name="gameId"/>
		/// </summary>
		EquipmentInfo GetEquipmentInfo(GameId gameId);
		/// <summary>
		/// Requests the <see cref="EquipmentInfo"/> representing a generic equipment of the given data
		/// </summary>
		EquipmentInfo GetEquipmentInfo(GameId gameId, ItemRarity rarity, uint level);

		/// <summary>
		/// Requests the list of items that can have the given <paramref name="rarity"/>
		/// </summary>
		List<EquipmentDataInfo> GetEquipmentDataInfoList(ItemRarity rarity);

		/// <summary>
		/// Requests the <see cref="WeaponInfo"/> representing the given <paramref name="itemId"/>
		/// </summary>
		WeaponInfo GetWeaponInfo(UniqueId itemId);
		/// <summary>
		/// Requests the <see cref="WeaponInfo"/> representing a generic equipment of the given data
		/// </summary>
		WeaponInfo GetWeaponInfo(GameId gameId);
		/// <summary>
		/// Requests the <see cref="WeaponInfo"/> representing a generic equipment of the given <paramref name="gameId"/>
		/// </summary>
		WeaponInfo GetWeaponInfo(GameId gameId, ItemRarity rarity, uint level);
		/// <summary>
		/// If the given <paramref name="itemId"/> is of gear type it will request the <see cref="GearInfo"/> representing it
		/// </summary>
		bool TryGetWeaponInfo(UniqueId itemId, out WeaponInfo info);

		/// <summary>
		/// Requests the <see cref="GearInfo"/> representing the given <paramref name="itemId"/>
		/// </summary>
		GearInfo GetGearInfo(UniqueId itemId);
		/// <summary>
		/// Requests the <see cref="GearInfo"/> representing a generic equipment of the given data
		/// </summary>
		GearInfo GetGearInfo(GameId gameId);
		/// <summary>
		/// Requests the <see cref="GearInfo"/> representing a generic equipment of the given <paramref name="gameId"/>
		/// </summary>
		GearInfo GetGearInfo(GameId gameId, ItemRarity rarity, uint level);
		/// <summary>
		/// If the given <paramref name="itemId"/> is of gear type it will request the <see cref="GearInfo"/> representing it
		/// </summary>
		bool TryGetGearInfo(UniqueId itemId, out GearInfo info);

		/// <summary>
		/// Requests the <see cref="EquipmentDataInfo"/> of all items in the inventory belonging to the given
		/// <paramref name="slot"/> type.
		/// </summary>
		List<EquipmentDataInfo> GetInventoryInfo(GameIdGroup slot);

		/// <summary>
		/// Requests the <see cref="EquipmentLoadOutInfo"/> for the player gear setup
		/// </summary>
		/// <returns></returns>
		EquipmentLoadOutInfo GetLoadOutInfo();

		/// <summary>
		/// Requests the <see cref="FusionInfo"/> for the given <paramref name="items"/>
		/// </summary>
		/// <exception cref="LogicException">
		/// Thrown if the all the given <paramref name="items"/> don't have the same <see cref="ItemRarity"/>
		/// </exception>
		FusionInfo GetFusionInfo(List<UniqueId> items);

		/// <summary>
		/// Requests the <see cref="EnhancementInfo"/> for the given <paramref name="items"/>
		/// </summary>
		EnhancementInfo GetEnhancementInfo(List<UniqueId> items);
		
		/// <summary>
		/// Requests the information if the given <paramref name="itemId"/> is equipped
		/// </summary>
		bool IsEquipped(UniqueId itemId);

		/// <summary>
		/// Requests the upgrade cost for a generic item with the given stats
		/// </summary>
		uint GetUpgradeCost(ItemRarity rarity, uint level);

		/// <summary>
		/// Requests the selling cost for a generic item with the given stats
		/// </summary>
		uint GetSellCost(ItemRarity rarity, uint level);

		/// <summary>
		/// Requests the item power for a generic item with the given stats
		/// </summary>
		uint GetItemPower(ItemRarity rarity, uint level);

		/// <summary>
		/// Requests the total amount of power granted by all currently equipped items.
		/// </summary>
		uint GetTotalEquippedItemPower();
	}
	
	/// <inheritdoc />
	public interface IEquipmentLogic : IEquipmentDataProvider
	{
		/// <summary>
		/// Adds the given <paramref name="item"/> with the given <paramref name="level"/> & <paramref name="rarity"/>
		/// to the player's Inventory, but doesn't equip it.
		/// </summary>
		EquipmentDataInfo AddToInventory(GameId item, ItemRarity rarity, uint level);

		/// <summary>
		/// Equips the given <paramref name="itemId"/> to the player's Equipment slot.
		/// </summary>
		void Equip(UniqueId itemId);
		
		/// <summary>
		/// Unequips the given <paramref name="itemId"/> from the player's Equipment slot.
		/// </summary>
		void Unequip(UniqueId itemId);
		
		/// <summary>
		/// Sells the given <paramref name="itemId"/> from the player's Equipment inventory.
		/// </summary>
		void Sell(UniqueId itemId);
		
		/// <summary>
		/// Upgrades the given <paramref name="itemId"/> from the player's Equipment inventory to the next level.
		/// </summary>
		/// <exception cref="LogicException">
		/// Thrown if the given <paramref name="itemId"/> is already at max level
		/// </exception>
		void Upgrade(UniqueId itemId);

		/// <summary>
		/// Fuses the list of given <paramref name="items"/> to generate a new item with higher rarity
		/// </summary>
		/// <exception cref="LogicException">
		/// Thrown if the item with the given <paramref name="items"/> is not present in the inventory
		/// </exception>
		EquipmentDataInfo Fuse(List<UniqueId> items);

		/// <summary>
		/// Enhances the list of given <paramref name="items"/> to generate a new item with higher rarity and new level
		/// </summary>
		EquipmentDataInfo Enhance(List<UniqueId> items);
	}
}
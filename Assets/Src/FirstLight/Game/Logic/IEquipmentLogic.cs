using System;
using System.Collections.Generic;
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
		/// Requests the player's loadout.
		/// </summary>
		IObservableDictionaryReader<GameIdGroup, UniqueId> Loadout { get; }

		/// <summary>
		/// Requests the player's inventory.
		/// </summary>
		IObservableDictionaryReader<UniqueId, Equipment> Inventory { get; }

		/// <summary>
		/// Requests the <see cref="EquipmentInfo"/> for all the loadout
		/// </summary>
		List<EquipmentInfo> GetLoadoutEquipmentInfo();

		/// <summary>
		/// Requests the <see cref="EquipmentInfo"/> for all the inventory
		/// </summary>
		List<EquipmentInfo> GetInventoryEquipmentInfo();

		/// <summary>
		/// Requests all the <see cref="EquipmentInfo"/> from the inventory that belonging to the given <paramref name="slot"/> type.
		/// </summary>
		List<EquipmentInfo> FindInInventory(GameIdGroup slot);

		/// <summary>
		/// Requests the <paramref name="stat"/> value of an equipment item.
		/// </summary>
		float GetItemStat(Equipment equipment, StatType stat);

		/// <summary>
		/// Request the stats a specific piece of equipment has
		/// </summary>
		Dictionary<EquipmentStatType, float> GetEquipmentStats(Equipment equipment);

		/// <summary>
		/// Requests the percentage average of durability on all equipment in <paramref name="items"/>
		/// </summary>
		double GetDurabilityAvgPercentage(List<Equipment> items);

		/// <summary>
		/// Requests to see if player has enough NFTs equipped for play
		/// </summary>
		bool EnoughLoadoutEquippedToPlay();
	}

	/// <inheritdoc />
	public interface IEquipmentLogic : IEquipmentDataProvider
	{
		/// <summary>
		/// Adds an item to the inventory and assigns it a new UniqueId.
		/// </summary>
		UniqueId AddToInventory(Equipment equipment);

		/// <summary>
		/// Tries to remove an item from inventory, and returns true if a removal was successful
		/// </summary>
		bool RemoveFromInventory(UniqueId equipment);

		/// <summary>
		/// Sets the loadout for each slot in given <paramref name="newLoadout"/>
		/// </summary>
		void SetLoadout(IDictionary<GameIdGroup, UniqueId> newLoadout);
	}
}
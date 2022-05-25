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
		/// Requests an array of all the quipped items the player has
		/// in his loadout.
		/// </summary>
		Equipment[] GetLoadoutItems();

		/// <summary>
		/// Requests all items from the inventory that belonging to the given
		/// <paramref name="slot"/> type.
		/// </summary>
		List<Equipment> FindInInventory(GameIdGroup slot);

		/// <summary>
		/// Requests the information if the given <paramref name="itemId"/> is equipped
		/// </summary>
		bool IsEquipped(UniqueId itemId);

		/// <summary>
		/// Requests the power value of an equipment item.
		/// </summary>
		uint GetItemPower(Equipment equipment);

		/// <summary>
		/// Requests the total amount of power granted by all currently equipped items.
		/// </summary>
		uint GetTotalEquippedItemPower();

		/// <summary>
		/// Request the stats a specific piece of equipment has, with an optional level
		/// parameter (Leave default (0) to use Equipment leve).
		/// TODO: This should be rethought.
		/// </summary>
		Dictionary<EquipmentStatType, float> GetEquipmentStats(Equipment equipment, uint level = 0);
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
		/// Equips the given <paramref name="itemId"/> to the player's Equipment slot.
		/// </summary>
		void Equip(UniqueId itemId);

		/// <summary>
		/// Unequips the given <paramref name="itemId"/> from the player's Equipment slot.
		/// </summary>
		void Unequip(UniqueId itemId);
	}
}
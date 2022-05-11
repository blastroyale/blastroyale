using System.Collections.Generic;
using FirstLight.Game.Ids;
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
		IObservableDictionaryReader<UniqueId, Equipment> Inventory { get; }

		/// <summary>
		/// TODO
		/// </summary>
		/// <returns></returns>
		Equipment GetEquippedWeapon();

		/// <summary>
		/// TODO
		/// </summary>
		/// <returns></returns>
		List<Equipment> GetEquippedGear();

		/// <summary>
		/// Requests all items from the inventory that belonging to the given
		/// <paramref name="slot"/> type.
		/// </summary>
		List<Equipment> FindInInventory(GameIdGroup slot);
		
		/// <summary>
		/// TODO
		/// </summary>
		/// <param name="equipment"></param>
		/// <returns></returns>
		uint GetUpgradeCost(Equipment equipment);

		/// <summary>
		/// Requests the information if the given <paramref name="itemId"/> is equipped
		/// </summary>
		bool IsEquipped(UniqueId itemId);

		/// <summary>
		/// TODO
		/// </summary>
		/// <param name="equipment"></param>
		/// <returns></returns>
		uint GetItemPower(Equipment equipment);

		/// <summary>
		/// Requests the total amount of power granted by all currently equipped items.
		/// </summary>
		uint GetTotalEquippedItemPower();
	}

	/// <inheritdoc />
	public interface IEquipmentLogic : IEquipmentDataProvider
	{
		/// <summary>
		/// TODO
		/// </summary>
		/// <param name="equipment"></param>
		void AddToInventory(Equipment equipment);

		/// <summary>
		/// Equips the given <paramref name="itemId"/> to the player's Equipment slot.
		/// </summary>
		void Equip(UniqueId itemId);

		/// <summary>
		/// Unequips the given <paramref name="itemId"/> from the player's Equipment slot.
		/// </summary>
		void Unequip(UniqueId itemId);

		/// <summary>
		/// Upgrades the given <paramref name="itemId"/> from the player's Equipment inventory to the next level.
		/// </summary>
		/// <exception cref="LogicException">
		/// Thrown if the given <paramref name="itemId"/> is already at max level
		/// </exception>
		void Upgrade(UniqueId itemId); // TODO: Is this gonna be a thing?
	}
}
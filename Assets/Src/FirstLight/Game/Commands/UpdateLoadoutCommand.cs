using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Services;
using Quantum;

namespace FirstLight.Game.Commands
{
	public struct UpdateLoadoutCommand : IGameCommand
	{
		public Dictionary<GameIdGroup, UniqueId> NewLoadout;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			List<UniqueId> itemsEquipped = new List<UniqueId>();
			List<UniqueId> itemsUnequipped = new List<UniqueId>();

			foreach (var modifiedKvp in NewLoadout)
			{
				UniqueId equippedInSlot = gameLogic.EquipmentLogic.GetEquippedItemForSlot(modifiedKvp.Key);

				// Just unequipping the item
				if (equippedInSlot != UniqueId.Invalid && modifiedKvp.Value == UniqueId.Invalid)
				{
					gameLogic.EquipmentLogic.Unequip(equippedInSlot);
					itemsUnequipped.Add(equippedInSlot);
					continue;
				}
				else if (modifiedKvp.Value != equippedInSlot)
				{
					gameLogic.EquipmentLogic.Equip(modifiedKvp.Value);
					itemsUnequipped.Add(modifiedKvp.Value);
					itemsEquipped.Add(modifiedKvp.Value);
				}
			}

			gameLogic.MessageBrokerService.Publish(new UpdatedLoadoutMessage()
				                                       {EquippedIds = itemsEquipped, UnequippedIds = itemsUnequipped});
		}
	}
}
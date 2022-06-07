using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Services;
using Quantum;

namespace FirstLight.Game.Commands
{
    public class UpdateLoadoutCommand : IGameCommand
    {
        public Dictionary<GameIdGroup, UniqueId> NewLoadout;

        /// <inheritdoc />
        public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
        {
            List<UniqueId> itemsEquipped = new List<UniqueId>();
            List<UniqueId> itemsUnequipped = new List<UniqueId>();

            foreach (var kvp in NewLoadout)
            {
                UniqueId equippedInSlot = gameLogic.EquipmentLogic.GetEquippedItemForSlot(kvp.Key);
                
                if (equippedInSlot != UniqueId.Invalid && NewLoadout[kvp.Key] == UniqueId.Invalid)
                {
                    gameLogic.EquipmentLogic.Unequip(equippedInSlot);
                    itemsUnequipped.Add(equippedInSlot);
                }

                if (kvp.Value != UniqueId.Invalid)
                {
                    gameLogic.EquipmentLogic.Equip(kvp.Value);
                    itemsUnequipped.Add(kvp.Value);
                }
            }
            
            gameLogic.MessageBrokerService.Publish(new LoadoutUpdatedMessage() { EquippedIds = itemsEquipped, UnequippedIds = itemsUnequipped});
        }
    }
}

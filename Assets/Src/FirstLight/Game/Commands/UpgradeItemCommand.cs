using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Services;
using Quantum;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Upgrades an Item in the player's current loadout.
	/// </summary>
	public struct UpgradeItemCommand : IGameCommand
	{
		public UniqueId ItemId;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			var equipment = gameLogic.EquipmentLogic.Inventory[ItemId];
			gameLogic.CurrencyLogic.DeductCurrency(GameId.SC, 1); // TODO: Where do we get this from?
			gameLogic.EquipmentLogic.Upgrade(ItemId);
			gameLogic.MessageBrokerService.Publish(new ItemUpgradedMessage
			{
				ItemId = ItemId,
				PreviousLevel = equipment.Level,
				NewLevel = equipment.Level + 1,
			});
		}
	}
}
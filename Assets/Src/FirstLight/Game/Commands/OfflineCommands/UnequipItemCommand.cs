using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Services;

namespace FirstLight.Game.Commands.OfflineCommands
{
	/// <summary>
	/// Equips an item int player's loadout.
	/// </summary>
	public struct UnequipItemCommand : IGameCommand
	{
		public UniqueId Item;

		/// <inheritdoc />
		public bool ExecuteServer => false;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			gameLogic.EquipmentLogic.Unequip(Item);
		}
	}
}
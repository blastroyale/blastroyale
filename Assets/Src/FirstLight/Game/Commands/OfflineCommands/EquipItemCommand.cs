using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Services;

namespace FirstLight.Game.Commands.OfflineCommands
{
	/// <summary>
	/// Equips an item int player's loadout.
	/// </summary>
	public struct EquipItemCommand : IGameCommand
	{
		public UniqueId Item;

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.ClientOnly;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			gameLogic.EquipmentLogic.Equip(Item);
		}
	}
}
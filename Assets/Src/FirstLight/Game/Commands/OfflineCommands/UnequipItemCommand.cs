using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Services;

namespace FirstLight.Game.Commands.OfflineCommands
{
	/// <summary>
	/// Equips an item int player's loadout.
	/// </summary>
	public struct UnequipItemCommand : IGameCommand
	{
		public UniqueId Item;

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.ClientOnly;


		/// <inheritdoc />
		public void Execute(CommandExecutionContext ctx)
		{
			var info = ctx.Logic.EquipmentLogic().GetInfo(Item);

			ctx.Logic.EquipmentLogic().Unequip(Item);

			ctx.Services.Get<IAnalyticsService>().EquipmentCalls.UnequipItem(info);
		}
	}
}
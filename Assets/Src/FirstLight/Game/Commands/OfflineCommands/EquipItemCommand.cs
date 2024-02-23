using Cysharp.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules.Commands;
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
		public UniTask Execute(CommandExecutionContext ctx)
		{
			ctx.Logic.EquipmentLogic().Equip(Item);
			ctx.Services.MessageBrokerService().Publish(new EquippedItemMessage(){ItemID = Item});
			return UniTask.CompletedTask;
		}
	}
}
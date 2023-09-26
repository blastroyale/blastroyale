using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Equip collection item command.
	/// </summary>
	public struct EquipCollectionItemCommand : IGameCommand
	{
		public ItemData Item;
		
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		/// <inheritdoc />
		public void Execute(CommandExecutionContext ctx)
		{
			var category = ctx.Logic.CollectionLogic().Equip(Item);
			ctx.Services.MessageBrokerService().Publish(new CollectionItemEquippedMessage()
			{
				Category = category,
				EquippedItem = Item
			});
		}
	}
}
using Cysharp.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Scraps an Non-NFT item and awards the player resources 
	/// </summary>
	public struct ScrapItemCommand : IGameCommand
	{
		public UniqueId Item;

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public UniTask Execute(CommandExecutionContext ctx)
		{
			var info = ctx.Logic.EquipmentLogic().GetInfo(Item);
			var reward = ctx.Logic.EquipmentLogic().Scrap(Item);
			ctx.Logic.UniqueIdLogic().MarkIdSeen(Item.Id);

			ctx.Logic.CurrencyLogic().AddCurrency(reward.Key, reward.Value);
			ctx.Services.MessageBrokerService().Publish(new CurrencyChangedMessage
			{
				Id = reward.Key,
				Change = (int) reward.Value,
				Category = "scrap",
				NewValue = ctx.Logic.CurrencyLogic().GetCurrencyAmount(reward.Key)
			});

			ctx.Services.MessageBrokerService().Publish(new ItemScrappedMessage
			{
				Id = Item,
				GameId = info.Equipment.GameId,
				Name = info.Equipment.GameId.ToString(),
				Durability = info.CurrentDurability,
				Reward = reward
			});
			return UniTask.CompletedTask;
		}
	}
}
using Cysharp.Threading.Tasks;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Models;
using FirstLight.Server.SDK.Modules.Commands;
using Quantum;

namespace FirstLight.Game.Commands
{
	public class BuyFromStoreCommand : IGameCommand
	{
		public string CatalogItemId;
		
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public async UniTask Execute(CommandExecutionContext ctx)
		{
			var catalogItem = await ctx.Services.CatalogService().GetCatalogItem(CatalogItemId);
			var storeSetup = await ctx.Services.StoreService().GetItemPrice(CatalogItemId);
		
			foreach (var p in storeSetup.Price)
			{
				var id = PlayfabCurrencies.GetCurrency(p.Key);
				var amt = p.Value;
				ctx.Logic.CurrencyLogic().DeductCurrency(id, amt);
			}
			
			ctx.Logic.RewardLogic().Reward(new [] { catalogItem });
			
			var msg = new PurchaseClaimedMessage
			{
				ItemPurchased = catalogItem,
				SupportingContentCreator = ctx.Logic.ContentCreatorLogic().SupportingCreatorCode.Value 
			};
			ctx.Services.MessageBrokerService().Publish(msg);
			
		}
	}
}
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Models;
using FirstLight.Server.SDK.Modules.Commands;
using Quantum;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Command used to process Logical purchases from the store, only used for InGame currencies
	/// for real money purchases it doesn't use any command and syncs directly with server see more at <see cref="IAPService"/>
	/// </summary>
	public class BuyFromStoreCommand : IGameCommand
	{
		public string CatalogItemId;

		public StoreItemData StoreItemData;

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public async UniTask Execute(CommandExecutionContext ctx)
		{
			var catalogItem = await ctx.Services.CatalogService().GetCatalogItem(CatalogItemId);

			var playerDailyDealsConfiguration = ctx.Data.GetData<PlayerStoreData>().PlayerDailyDealsConfiguration;
			var dealStore = playerDailyDealsConfiguration?.SpecialStoreList.FirstOrDefault(s => s.IsActive && s.SpecialStoreItemIDs.Contains(CatalogItemId));
			
			var storeSetup = await ctx.Services.StoreService().GetItemPrice(CatalogItemId, dealStore?.SpecialStoreName);

			if (!ctx.Logic.PlayerStoreLogic().IsPurchasedAllowed(CatalogItemId, StoreItemData))
			{
				return;
			}

			foreach (var p in storeSetup.Price)
			{
				var id = PlayfabCurrencies.GetCurrency(p.Key);
				var amt = p.Value;
				// This purchase is validated in BlastRoyale server plugin
				if (id == GameId.NOOB && ctx.Logic.Web3().CanUseWeb3())
				{
					var web3Data = ctx.Data.GetData<Web3PlayerData>();
					web3Data.NoobPurchases += amt;
					web3Data.Version++;
				}
				else
				{
					ctx.Logic.CurrencyLogic().DeductCurrency(id, amt);
				}
			}

			ctx.Logic.RewardLogic().Reward(new[] {catalogItem});
			ctx.Logic.PlayerStoreLogic().UpdateLastPlayerPurchase(CatalogItemId, StoreItemData);
			
			var mainPrice = storeSetup.Price.FirstOrDefault();
			var msg = new PurchaseClaimedMessage
			{
				ItemPurchased = catalogItem,
				SupportingContentCreator = ctx.Logic.ContentCreatorLogic().SupportingCreatorCode.Value,
				PriceCurrencyId = mainPrice.Key,
				PricePaid = mainPrice.Value.ToString(),
			};
			ctx.Services.MessageBrokerService().Publish(msg);
		}
	}
}
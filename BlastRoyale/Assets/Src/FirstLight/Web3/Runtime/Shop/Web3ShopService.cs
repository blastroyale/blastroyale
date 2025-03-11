using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using Nethereum.Contracts;
using PlayFab;
using Quantum;


namespace FirstLight.Web3.Runtime.SequenceContracts
{
	public class Web3ShopService
	{
		private IWeb3ExternalService _service;
		private Nethereum.Web3.Web3 _nethereum;
		private Event<PurchaseIntentCreatedEvent> _event;
		
		public Web3ShopService(IWeb3ExternalService service)
		{
			_service = service;
			var account = new Nethereum.Web3.Accounts.Account(_service.Wallet.GetWalletAddress());
			_nethereum = new Nethereum.Web3.Web3(account, service.Web3Config.RPC);
			_event = _nethereum.Eth.GetEvent<PurchaseIntentCreatedEvent>(service.Web3Config.FindCurrency(GameId.NOOB)!.ShopContract);
		}
		
		/// <summary>
		/// This is to solve a edge case where user pays smart contract and closes the game
		/// This routine will detect overpayments and retroactively purchase missing items for the player
		/// </summary>
		public async UniTask CheckIfOverpaid()
		{
			if (!FeatureFlags.WEB3_REBUY_LAST_MISS) return;
			
			var filter = _event.CreateFilterInput(new object []{_service.Wallet.GetWalletAddress().Value});
			var events = await _event.GetAllChangesAsync(filter);
			var totalOnChain = events.Sum(e =>
			{
				return (int)Web3Logic.ConvertFromWei(e.Event.Price);
			});
			var totalIngame = _service.GameData.Web3Data.NoobSpentInLogic();
			var difference = totalOnChain - totalIngame;

			var items = string.Join(',',events.Select(e => $"{Web3Logic.ConvertFromWei(e.Event.Price)} on {ExtractItemData(e)}\n"));
			FLog.Verbose("Total spend on player data " + totalIngame);
			FLog.Verbose("Total spent on chain: "+totalOnChain+" on \n"+items);
			if (difference > 0)
			{
				FLog.Warn($"User has {difference} unspent noob so he might try to re-purchase");
				Web3Analytics.SendEvent("unspent_noob", 
					("playerid", PlayFabSettings.staticPlayer.PlayFabId), 
					("amount", difference));
			} else if (difference < 0)
			{
				FLog.Warn($"User somehow managed to spend more noob than he paid !!!!");
				Web3Analytics.SendEvent("hacked_noob", 
					("playerid", PlayFabSettings.staticPlayer.PlayFabId), 
					("amount", -difference));
				return;
			}
			else
			{
				FLog.Verbose("No unclaimed noob to re-claim from store");
				return;
			}
			
			var lastItem = events.LastOrDefault();
			var lastPrice = Web3Logic.ConvertFromWei(lastItem.Event.Price);
			FLog.Verbose($"Checking if has amount to buy last spent: {lastPrice} and amount consumed: {difference}");
			if (lastPrice >= difference)
			{
				var product = IsStillForSale(ExtractItemData(lastItem));
				if (product == null)
				{
					FLog.Error("Bought something that was removed from store.");
					return;
				}
				FLog.Info("Retroactively purchasing missing purchase");
				_service.GameServices.CommandService.ExecuteCommand(new BuyFromStoreCommand()
				{
					CatalogItemId = product.PlayfabProductConfig.CatalogItem.ItemId,
					StoreItemData = product.PlayfabProductConfig.StoreItemData
				});
				Web3Analytics.SendEvent("reclaimed_noob", 
					("playerid", PlayFabSettings.staticPlayer.PlayFabId), 
					("amount", difference), 
					("item", ModelSerializer.Serialize(product.GameItem).Value));
			}
		}

		private ItemData ExtractItemData(EventLog<PurchaseIntentCreatedEvent> ev)
		{
			return Web3Logic.UnpackItem(new PackedItem()
			{
				GameId = BitConverter.GetBytes((ushort)ev.Event.GameId),
				Metadata = ev.Event.Metadata
			});
		}

		private GameProduct IsStillForSale(ItemData item)
		{
			return _service.GameServices.IAPService.AvailableProducts.FirstOrDefault(p => p.GameItem.Equals(item));
		}
	}
}
using System.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Models;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;
using FirstLightServerSDK.Services;
using Quantum;

namespace BlastRoyaleNFTPlugin.Shop;

public class Web3Shop
{
	public static readonly GameId Currency = GameId.NOOB;
	
	private BlockchainApi _api;
	private PluginContext _ctx;
	private IStoreService _store;
	
	public Web3Shop(BlockchainApi api, IStoreService store, PluginContext ctx)
	{
		_api = api;
		_store = store;
		_ctx = ctx;
	}
	
	public async Task<bool> ValidateWeb3Purchase(ServerState state, string itemId)
	{
		if (_api.Config == null)
		{
			return true;
		}
		var itemPrice = await _store.GetItemPrice(itemId);
		var playfabCurrency = PlayfabCurrencies.GetPlayfabCurrencyName(Currency);
		if (!itemPrice.Price.TryGetValue(playfabCurrency, out var cost))
		{
			return true; 
		}
		var web3State = state.DeserializeModel<Web3PlayerData>();
		var onChain = await _api.GetSpentOnShop(web3State.Wallet, _api.Config.FindCurrency(Currency)!.ShopContract);
		var offChain = web3State.NoobPurchases;
		var amountHave = onChain - offChain;
		var valid = cost <= amountHave;
		return valid;
	}
}
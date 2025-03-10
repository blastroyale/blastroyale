using System.Threading.Tasks;
using FirstLight.Game.Configs.Remote;
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
	
	private BlastRoyalePlugin _plg;
	private PluginContext _ctx;
	private IStoreService _store;
	public Web3Config Web3Config { get; set; }
	
	public Web3Shop(BlastRoyalePlugin plg, IStoreService store)
	{
		_plg = plg;
		_store = store;
		_ctx = _plg.Ctx;
	}
	
	public async Task<bool> ValidateWeb3Purchase(ServerState state, string itemId)
	{
		if (_plg.Web3Config == null)
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
		var onChain = await _plg.BlockchainApi.GetSpentOnShop(web3State.Wallet, Web3Config.FindCurrency(Currency)!.ShopContract);
		var offChain = web3State.NoobPurchases;
		var amountHave = onChain - offChain;
		var valid = cost <= amountHave;
		return valid;
	}
}
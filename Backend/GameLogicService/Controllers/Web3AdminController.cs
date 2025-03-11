using System;
using System.Linq;
using System.Threading.Tasks;
using Backend.Game.Services;
using Backend.Plugins;
using BlastRoyaleNFTPlugin;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Services;
using Microsoft.AspNetCore.Mvc;
using PlayFab;
using PlayFab.ServerModels;
using Quantum;
using ServerCommon.Authentication.ApiKey;


namespace ServerCommon.Controllers
{
	[ApiController]
	[RequiresApiKey]
	[Route("web3admin")]
	[Produces("application/json")]
	[Consumes("application/json")]
	public class Web3AdminController : ControllerBase
	{
		private BlastRoyalePlugin _plugin;
		private IGameConfigurationService _configs;
		private IErrorService<PlayFabError> _errorHandler;
		private IServerStateService _state;
		private BlockchainApi _api;

		public Web3AdminController(IPluginManager plugins, IEventManager evs, IServerStateService state, IGameConfigurationService cfg, IErrorService<PlayFabError> errors)
		{
			_configs = cfg;
			_errorHandler = errors;
			_state = state;
			var ps = plugins.GetPlugins();
			_plugin = ps.Where(p => p is BlastRoyalePlugin).Cast<BlastRoyalePlugin>().First();
			_api = _plugin.BlockchainApi;
		}

		[HttpGet]
		[Route("state")]
		public async Task<dynamic> GetPlayerState(string playerid)
		{
			var noobConfig = _plugin.Web3Config.FindCurrency(GameId.NOOB)!;
			var data = await _state.GetPlayerState(playerid);
			var pd = data.DeserializeModel<Web3PlayerData>();
			var vouchers = pd.Vouchers.Select(v => new
			{
				id= v.VoucherId,
				value=Web3Logic.ConvertFromWei(v.Value),
			});
			var t1 = _api.GetSpentOnShop(pd.Wallet, noobConfig.ShopContract);
			var t2 = _api.GetPurchaseIntents(pd.Wallet, noobConfig.ShopContract);
			await Task.WhenAll(t1, t2);
			return Content(ModelSerializer.Serialize(new
			{
				GameLogic=new
				{
					Vouchers=vouchers,
					wallet=pd.Wallet,
					NoobsUsedInGameLogic=pd.NoobPurchases
				},
				Blockchain=new
				{
					UsedOnChain=Web3Logic.ConvertFromWei(t1.Result),
					ItemsPurchased=t2.Result
				}
			}).Value, "application/json");
		}
	}
}
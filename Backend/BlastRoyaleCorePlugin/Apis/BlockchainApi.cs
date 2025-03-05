using System;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;
using BlastRoyaleNFTPlugin.Services;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using PlayFab;

namespace BlastRoyaleNFTPlugin
{
	/// <summary>
	/// Class that encapsulates external models and functionality needed to synchronize NFT's
	/// </summary>
	public class BlockchainApi
	{
		public Web3Config Config { get; private set; }
		internal PluginContext _ctx;
		internal BlastRoyalePlugin _blastRoyalePlugin;
		internal HttpClient _client;
		internal string _externalUrl;
		internal string _apiKey;
		public CollectionsSyncService CollectionsSyncService;

		public BlockchainApi(string baseUrl, string apiKey, PluginContext ctx, BlastRoyalePlugin blastRoyalePlugin)
		{
			_client = new HttpClient();
			_externalUrl = baseUrl;
			_ctx = ctx;
			_blastRoyalePlugin = blastRoyalePlugin;
			_apiKey = apiKey;
			CollectionsSyncService = new CollectionsSyncService(this);
			_ = Init();
		}

		private async Task Init()
		{
			var r = await PlayFabServerAPI.GetTitleDataAsync(new()
			{
				Keys = new [] {"ChainConfig"}.ToList(),
			});
			if (r.Error != null) throw new Exception(r.Error.GenerateErrorReport());
			if (!r.Result.Data.ContainsKey("ChainConfig"))
			{
				_ctx.Log.LogInformation("Web3 not enabled, web3 config not found in playfab");
				return;
			}
			Config = ModelSerializer.Deserialize<Web3Config>(r.Result.Data["ChainConfig"]);
			_ctx.Log.LogDebug("Web3 Config Loaded "+r.Result.Data["ChainConfig"]);
		}

		public async Task<BigInteger> GetSpentOnShop(string wallet, string contract)
		{
			var url = $"{_externalUrl}/shop/spent?wallet={wallet}&shopContract={contract}&key={_apiKey}";
			Console.WriteLine(url);
			var response = await _client.GetAsync(url);
			var responseString = await response.Content.ReadAsStringAsync();
			if (!response.IsSuccessStatusCode)
			{
				_ctx.Log!.LogError($"GetSpentOnShop Error: {response.ReasonPhrase} - {responseString}");
				return 0;
			}
			Console.WriteLine(responseString);
			return BigInteger.Parse(responseString);
		}

		/// <summary>
		/// Function that syncrhonizes blockchain data to game data.
		/// Will add missing NFT's and remove NFT's that are not owned anymore by the user.
		/// </summary>
		public async Task<bool> SyncData(ServerState serverState, string playfabId)
		{
			if (!serverState.Has<PlayerData>())
			{
				return false;
			}
			return await CollectionsSyncService.SyncCollections(playfabId, serverState);
		}

		public bool CanSyncCollection(string collectionName)
		{
			return _blastRoyalePlugin.CanSyncCollection(collectionName);
		}
	}
}
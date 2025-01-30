using System.Net.Http;
using System.Threading.Tasks;
using BlastRoyaleNFTPlugin.Services;
using FirstLight.Game.Data;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;

namespace BlastRoyaleNFTPlugin
{
	/// <summary>
	/// Class that encapsulates external models and functionality needed to synchronize NFT's
	/// </summary>
	public class BlockchainApi
	{
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
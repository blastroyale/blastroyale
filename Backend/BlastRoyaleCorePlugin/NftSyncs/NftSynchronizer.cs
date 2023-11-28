using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using FirstLight.Server.SDK.Models;

namespace BlastRoyaleNFTPlugin
{
	/// <summary>
	/// Class that encapsulates external models and functionality needed to synchronize NFT's
	/// </summary>
	public class NftSynchronizer : BaseEventDataSync<PlayerDataLoadEvent>
	{
		internal PluginContext _ctx;
		internal HttpClient _client;
		internal string _externalUrl;
		internal string _apiKey;
		protected EquipmentSync _equipmentSync;
		protected CorpoSync _corpoSync;

		public NftSynchronizer(string baseUrl, string apiKey, PluginContext ctx) : base(ctx)
		{
			_client = new HttpClient();
			_externalUrl = baseUrl;
			_ctx = ctx;
			_apiKey = apiKey;
			_equipmentSync = new EquipmentSync(this);
			_corpoSync = new CorpoSync(this);
		}

		/// <summary>
		/// Function that syncrhonizes blockchain data to game data.
		/// Will add missing NFT's and remove NFT's that are not owned anymore by the user.
		/// </summary>
		public override async Task<bool> SyncData(string playfabId)
		{
			try
			{
				await _ctx.PlayerMutex.Lock(playfabId);
				var serverState = await _ctx.ServerState.GetPlayerState(playfabId);
				var lastBlockchainUpdate = await RequestBlockchainLastUpdate(playfabId);

				if (!serverState.Has<PlayerData>())
				{
					return false;
				}

				var equipmentSync = _equipmentSync.SyncNftEquipment(playfabId, serverState, lastBlockchainUpdate);
				var corposSync = _corpoSync.SyncCorpos(playfabId, serverState, lastBlockchainUpdate);
				var values = await Task.WhenAll(equipmentSync, corposSync);
				if (serverState.HasDelta())
				{
					await _ctx.ServerState.UpdatePlayerState(playfabId, serverState);
				}
				return values.All(value => value);
			}
			finally
			{
				_ctx.PlayerMutex.Unlock(playfabId);
			}
		}


		/// <summary>
		/// Request the last time a given wallet was updated.
		/// </summary>
		protected virtual async Task<ulong> RequestBlockchainLastUpdate(string playerId)
		{
			var response = await _client.GetAsync($"{_externalUrl}/blast-royale-equipment/lastupdate?key={_apiKey}&playfabId={playerId}");
			if (response.StatusCode != HttpStatusCode.OK)
			{
				_ctx.Log.LogError($"Error obtaining indexed NFTS Last Update Response {response.StatusCode.ToString()}");
				return 0;
			}

			var responseString = await response.Content.ReadAsStringAsync();
			return ulong.Parse(responseString);
		}
	}
}
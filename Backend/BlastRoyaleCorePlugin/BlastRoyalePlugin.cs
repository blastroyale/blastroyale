using System.Threading.Tasks;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using FirstLight.Server.SDK.Models;

namespace BlastRoyaleNFTPlugin
{
	/// <summary>
	/// Server plugin to sync inventories../// </summary>
	public class BlastRoyalePlugin : ServerPlugin
	{
		private readonly IUserMutex _userMutex;
		private PluginContext _ctx;
		private BlockchainApi _blockchainApi;

		public BlastRoyalePlugin(IUserMutex userMutex)
		{
			this._userMutex = userMutex;
		}

		/// <summary>
		/// Server override called whenever the plugin is loaded.
		/// </summary>
		public override void OnEnable(PluginContext context)
		{
			_ctx = context;
			
			context.PluginEventManager.RegisterEventListener<PlayerDataLoadEvent>(OnDataLoad, EventPriority.LAST);
			context.PluginEventManager.RegisterEventListener<InventoryUpdatedEvent>(OnInventoryUpdate);

			SetupBlockchainAPI();
		}

		private void SetupBlockchainAPI()
		{
			if (!_ctx.ServerConfig.NftSync)
			{
				_ctx.Log?.LogInformation("NFT Sync EnvVar Flag is currently disabled, skipping BlockchainAPI configuration");
				return;
			}

			var baseUrl = ReadPluginConfig("API_URL");
			var apiSecret = ReadPluginConfig("API_KEY");
			
			_ctx.Log?.LogInformation($"Using blockchain URL at {baseUrl}");
			
			if (_ctx.ServerConfig.NftSync)
			{
				_blockchainApi = new BlockchainApi(baseUrl, apiSecret, _ctx, this);
			}
		}

		private async Task OnDataLoad(PlayerDataLoadEvent onLoad)
		{
			if (_blockchainApi != null)
				await _blockchainApi.SyncData(onLoad.PlayerState, onLoad.PlayerId);
			
			// It needs to be the last one, because it may fail and need to rollback items back to playfab
			await _ctx.InventorySync!.SyncData(onLoad.PlayerState, onLoad.PlayerId);
		}

		private async Task OnInventoryUpdate(InventoryUpdatedEvent onLoad)
		{
			await using (await _userMutex.LockUser(onLoad.PlayerId))
			{
				var state = await _ctx.ServerState.GetPlayerState(onLoad.PlayerId);
				await _ctx.InventorySync!.SyncData(state, onLoad.PlayerId);
				if (state.HasDelta())
				{
					await _ctx.ServerState.UpdatePlayerState(onLoad.PlayerId, state.GetOnlyUpdatedState());
				}
			}
		}

		public bool CanSyncCollection(string collectionName)
		{
			var syncEnabledConfig = ReadPluginConfig(string.Concat(collectionName.ToUpperInvariant(), "_SYNC_ENABLED"));

			bool.TryParse(syncEnabledConfig, out var canSync);
			return canSync;
		}
	}
}
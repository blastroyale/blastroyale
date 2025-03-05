using System;
using System.Threading.Tasks;
using BlastRoyaleNFTPlugin.Shop;
using FirstLight.Game.Commands;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using FirstLight.Server.SDK.Models;
using FirstLightServerSDK.Services;


namespace BlastRoyaleNFTPlugin
{
	/// <summary>
	/// Server plugin to sync inventories../// </summary>
	public class BlastRoyalePlugin : ServerPlugin
	{
		private readonly IUserMutex _userMutex;
		private PluginContext _ctx;
		private BlockchainApi _blockchainApi;
		private IInventorySyncService<ItemData> _inventorySyncService;
		private Web3Shop _shop;
		private IStoreService _store;
		
		public BlastRoyalePlugin(IStoreService store, IUserMutex userMutex, IInventorySyncService<ItemData> inventorySyncService)
		{
			_inventorySyncService = inventorySyncService;
			_userMutex = userMutex;
			_store = store;
		}

		/// <summary>
		/// Server override called whenever the plugin is loaded.
		/// </summary>
		public override void OnEnable(PluginContext context)
		{
			_ctx = context;

			context.PluginEventManager.RegisterEventListener<BeforeCommandRunsEvent>(OnBeforeCommand,
				EventPriority.FIRST);
			context.PluginEventManager.RegisterEventListener<PlayerDataLoadEvent>(OnDataLoad, EventPriority.LAST);
			SetupBlockchainAPI();
			_shop = new Web3Shop(_blockchainApi, _store, _ctx);
		}

		private void SetupBlockchainAPI()
		{
			if (!_ctx.ServerConfig.NftSync)
			{
				_ctx.Log?.LogInformation(
					"NFT Sync EnvVar Flag is currently disabled, skipping BlockchainAPI configuration");
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

		private async Task OnBeforeCommand(BeforeCommandRunsEvent ev)
		{
			if (!(ev.CommandInstance is BuyFromStoreCommand buyCommand)) return;
			
			var validPurchase = await _shop.ValidateWeb3Purchase(ev.State, buyCommand.CatalogItemId);
			if (!validPurchase)
			{
				ev.CancelCommand = true;
				throw new Exception("Could not buy item, player has not spend enough");
			}
		}

		private async Task OnDataLoad(PlayerDataLoadEvent onLoad)
		{
			if (_blockchainApi != null)
				await _blockchainApi.SyncData(onLoad.PlayerState, onLoad.PlayerId);

			// It needs to be the last one, because it may fail and need to rollback items back to playfab
			await _inventorySyncService!.SyncData(onLoad.PlayerState, onLoad.PlayerId);
		}

		public bool CanSyncCollection(string collectionName)
		{
			var syncEnabledConfig = ReadPluginConfig(string.Concat(collectionName.ToUpperInvariant(), "_SYNC_ENABLED"));

			bool.TryParse(syncEnabledConfig, out var canSync);
			return canSync;
		}
	}
}
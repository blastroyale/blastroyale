using System;
using System.Linq;
using System.Threading.Tasks;
using BlastRoyaleNFTPlugin.Services;
using BlastRoyaleNFTPlugin.Shop;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLightServerSDK.Services;
using PlayFab;


namespace BlastRoyaleNFTPlugin
{
	/// <summary>
	/// Server plugin to sync inventories../// </summary>
	public class BlastRoyalePlugin : ServerPlugin
	{
		public BlockchainApi BlockchainApi { get; private set; }
		public PluginContext Ctx { get; private set; }
		public Web3Config Web3Config { get; private set; }
		
		
		private IInventorySyncService<ItemData> _inventorySyncService;
		private Web3Shop _shop;
		
		private IStoreService _store;
		private CollectionsSyncService _collectionsSyncService;
		
		public BlastRoyalePlugin(IStoreService store, IInventorySyncService<ItemData> inventorySyncService)
		{
			_inventorySyncService = inventorySyncService;
			_store = store;
		}

		/// <summary>
		/// Server override called whenever the plugin is loaded.
		/// </summary>
		public override void OnEnable(PluginContext context)
		{
			Ctx = context;
			context.PluginEventManager.RegisterEventListener<BeforeCommandRunsEvent>(OnBeforeCommand,
				EventPriority.FIRST);
			context.PluginEventManager.RegisterEventListener<PlayerDataLoadEvent>(OnDataLoad, EventPriority.LAST);
			SetupBlockchainAPI();
			_shop = new Web3Shop(this, _store);
			_ = Init();
		}

		private void SetupBlockchainAPI()
		{
			if (!Ctx.ServerConfig.NftSync)
			{
				Ctx.Log?.LogInformation(
					"NFT Sync EnvVar Flag is currently disabled, skipping BlockchainAPI configuration");
				return;
			}

			var baseUrl = ReadPluginConfig("API_URL");
			Ctx.Log?.LogInformation($"Using blockchain URL at {baseUrl}");

			if (Ctx.ServerConfig.NftSync)
			{
				BlockchainApi = new BlockchainApi();
				_collectionsSyncService = new CollectionsSyncService(this);	
			}
		}
		
		private async Task Init()
		{
			var r = await PlayFabServerAPI.GetTitleDataAsync(new()
			{
				Keys = new [] {"ChainConfig"}.ToList(),
			});
			if (r.Error != null) throw new Exception(r.Error.GenerateErrorReport());
			Web3Config = ModelSerializer.Deserialize<Web3Config>(r.Result.Data["ChainConfig"]);
			Ctx.Log.LogDebug("Web3 Config Loaded "+r.Result.Data["ChainConfig"]);
			_shop.Web3Config = Web3Config;
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
			await SyncCollections(onLoad.PlayerState, onLoad.PlayerId);
			// It needs to be the last one, because it may fail and need to rollback items back to playfab
			await _inventorySyncService!.SyncData(onLoad.PlayerState, onLoad.PlayerId);
		}

		public async Task<bool> SyncCollections(ServerState state, string playerId)
		{
			var good = true;
			if (BlockchainApi != null)
			{
				if (state.Has<PlayerData>())
				{
					good = await _collectionsSyncService.SyncCollections(playerId, state);
				}
			}
			return good;
		}

		public bool CanSyncCollection(string collectionName)
		{
			var syncEnabledConfig = ReadPluginConfig(string.Concat(collectionName.ToUpperInvariant(), "_SYNC_ENABLED"));

			bool.TryParse(syncEnabledConfig, out var canSync);
			return canSync;
		}
	}
}
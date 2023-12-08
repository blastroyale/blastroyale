using System.Threading.Tasks;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;

namespace BlastRoyaleNFTPlugin
{
	/// <summary>
	/// Server plugin to sync inventories../// </summary>
	public class BlastRoyalePlugin : ServerPlugin
	{
		private PluginContext _ctx;
		/// <summary>
		/// Server override called whenever the plugin is loaded.
		/// </summary>
		public override void OnEnable(PluginContext context)
		{
			_ctx = context;
			var baseUrl = ReadPluginConfig("API_URL");
			var apiSecret = ReadPluginConfig("API_KEY");
			context.Log?.LogInformation($"Using blockchain URL at {baseUrl}");
			if (context.ServerConfig.NftSync)
			{
				context.DataSyncs?.RegisterSync(new NftSynchronizer(baseUrl, apiSecret, context));
			}
			context.PluginEventManager.RegisterEventListener<PlayerDataLoadEvent>(OnDataLoad);
			context.PluginEventManager.RegisterEventListener<InventoryUpdatedEvent>(OnInventoryUpdate);
		}

		private async Task OnDataLoad(PlayerDataLoadEvent onLoad)
		{
			await _ctx.InventorySync!.SyncData(onLoad.PlayerId);
		}
		
		private async Task OnInventoryUpdate(InventoryUpdatedEvent onLoad)
		{
			await _ctx.InventorySync!.SyncData(onLoad.PlayerId);
		}
	}
}


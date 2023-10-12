using FirstLight.Game.Utils;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;

namespace BlastRoyaleNFTPlugin
{
	/// <summary>
	/// Server plugin to sync NFT weapons on the user inventory.
	/// </summary>
	public class BlastRoyalePlugin : ServerPlugin
	{
		
		/// <summary>
		/// Server override called whenever the plugin is loaded.
		/// </summary>
		public override void OnEnable(PluginContext context)
		{
			var baseUrl = ReadPluginConfig("API_URL");
			var apiSecret = ReadPluginConfig("API_KEY");
			context.Log?.LogInformation($"Using blockchain URL at {baseUrl}");
			if (context.ServerConfig.NftSync)
			{
				context.DataSyncs?.RegisterSync(new NftSynchronizer(baseUrl, apiSecret, context));
			}
			context.DataSyncs?.RegisterSync(new PlayfabInventorySync(context));
		}
	}
}


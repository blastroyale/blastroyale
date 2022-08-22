using FirstLight.Game.Utils;
using ServerSDK;
using ServerSDK.Events;

namespace BlastRoyaleNFTPlugin
{
	/// <summary>
	/// Server plugin to sync NFT weapons on the user inventory.
	/// </summary>
	public class BlastRoyalePlugin : ServerPlugin
	{
		public NftSynchronizer NftSync = null!;
	
		/// <summary>
		/// Server override called whenever the plugin is loaded.
		/// </summary>
		public override void OnEnable(PluginContext context)
		{
			var baseUrl = ReadPluginConfig("API_URL");
			var blockchainUrl = ReadPluginConfig("API_BLOCKCHAIN_SERVICE");
			var apiSecret = ReadPluginConfig("API_SECRET");
			//var fullUrl = $"{baseUrl}/{blockchainUrl}/indexed?key={apiSecret}&playfabId=";
			var fullUrl = $"{baseUrl}/{blockchainUrl}";
			NftSync = new NftSynchronizer(fullUrl, apiSecret, context);
			context.PluginEventManager.RegisterListener<PlayerDataLoadEvent>(OnGetPlayerData);
			context.RegisterCustomConverter(this, new QuantumVector2Converter());
			context.RegisterCustomConverter(this, new QuantumVector3Converter());
		}

		/// <summary>
		/// Event delegate to be called before server loading player's data.
		/// </summary>
		private void OnGetPlayerData(PlayerDataLoadEvent ev)
		{
			NftSync.SyncAllNfts(ev.PlayerId).Wait();
		}
	}

}


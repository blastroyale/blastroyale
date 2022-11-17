﻿using FirstLight.Game.Utils;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;

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
			var apiSecret = ReadPluginConfig("API_KEY");
			var fullUrl = $"{baseUrl}/blast-royale-equipment";
			context.Log.LogInformation($"Using blockchain URL at {fullUrl}");
			NftSync = new NftSynchronizer(fullUrl, apiSecret, context);
			if (context.ServerConfig.NftSync)
			{
				context.PluginEventManager.RegisterListener<PlayerDataLoadEvent>(OnGetPlayerData);
			}
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


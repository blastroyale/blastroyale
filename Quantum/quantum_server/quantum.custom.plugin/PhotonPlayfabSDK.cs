using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using FirstLight.Game.Data;
using Photon.Hive.Plugin;
using PlayFab;
using PlayFab.Internal;
using PlayFab.ServerModels;
using PlayFab.Json;

namespace quantum.custom.plugin
{
	/// <summary>
	/// Minimal playfab api's consumed by this plugin.
	/// Implements Photon HTTP Wrappers.
	/// </summary>
	public class PhotonPlayfabSDK
	{
		private readonly IPluginHost _host;
		public readonly PlayfabPhotonHttp HttpWrapper;
	
		public PhotonPlayfabSDK(Dictionary<string, string> photonConfig, IPluginHost host)
		{
			_host = host;
			HttpWrapper = new PlayfabPhotonHttp(_host);
			PlayFabSettings.staticSettings.TitleId = photonConfig["PlayfabTitle"];
			PlayFabSettings.staticSettings.DeveloperSecretKey = photonConfig["PlayfabKey"];
		}

		public void GetProfileReadOnlyData(string playerId, HttpRequestCallback callback)
		{
			var request = new GetUserDataRequest()
			{
				PlayFabId = playerId,
				Keys = new string[] { typeof(NftEquipmentData).FullName }.ToList()
			};
			HttpWrapper.Post(playerId, "/Server/GetUserReadOnlyData", request, callback);
		}
	
	}
}
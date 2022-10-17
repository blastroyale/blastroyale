using System.Collections.Generic;
using System.Linq;
using System.Text;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules;
using Photon.Hive.Plugin;
using PlayFab;
using PlayFab.CloudScriptModels;
using PlayFab.ServerModels;
using Quantum;

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
			if(photonConfig.TryGetValue("LocalLogicServer", out var localLogicServer) && localLogicServer=="true")
			{
				HttpWrapper.ServerAddress = "http://localhost:7274";
			}
		}

		/// <summary>
		/// Sends a server command trought playfab.
		/// Requires a userId and a token to prove this was originated from an authenticated user.
		/// The command will impersonate the given player.
		/// </summary>
		public void SendServerCommand(string userId, string token, IQuantumCommand command, bool async = true)
		{
			Log.Info($"Sending command {command.GetType()} to {userId}");
;			var data = new Dictionary<string, string>();
			data[CommandFields.Command] = ModelSerializer.Serialize(command).Value;
			data["SecretKey"] = PlayFabSettings.staticSettings.DeveloperSecretKey;
			var request = new ExecuteFunctionRequest()
			{
				FunctionName = "ExecuteCommand",
				FunctionParameter = new LogicRequest()
				{
					Command = command.GetType().FullName, 
					Data = data
				},
				AuthenticationContext = new PlayFabAuthenticationContext()
				{
					PlayFabId = userId,
					EntityToken = token,
				}
			};
			HttpWrapper.Post(userId, "/CloudScript/ExecuteFunction", request, OnPlayfabCommand, new Dictionary<string, string>()
			{
				{ "X-EntityToken", token }
			}, async);
		}

		/// <summary>
		/// Obtains a user readonly data.
		/// </summary>
		public void GetProfileReadOnlyData(string playerId, HttpRequestCallback callback)
		{
			var request = new GetUserDataRequest()
			{
				PlayFabId = playerId,
				Keys = new string[] { typeof(EquipmentData).FullName }.ToList()
			};
			HttpWrapper.Post(playerId, "/Server/GetUserReadOnlyData", request, callback);
		}
		
		private void OnPlayfabCommand(IHttpResponse response, object userId)
		{
			if(response.HttpCode >= 400)
			{
				var dataString = response.ResponseData?.Length > 0 ? Encoding.UTF8.GetString(response.ResponseData) : "";
				Log.Error($"Invalid PlayFab response to url {response.Request.Url} status {response.Status} data {dataString} text {response.ResponseText}");
			} else
			{
				Log.Debug($"Request from {userId} OK");
			}
		}
	}
}
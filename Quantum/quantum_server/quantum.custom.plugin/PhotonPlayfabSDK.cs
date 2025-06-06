using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Logic.RPC;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.Commands;
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
			if (photonConfig.TryGetValue("LocalLogicServer", out var localLogicServer) && localLogicServer == "true")
			{
				Log.Info("Using local gamelogic server!");
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
			var data = new Dictionary<string, string>();
			data[CommandFields.CommandData] = ModelSerializer.Serialize(command).Value;
			data[CommandFields.CommandType] = command.GetType().FullName;
			data["SecretKey"] = PlayFabSettings.staticSettings.DeveloperSecretKey;
			var request = new ExecuteFunctionRequest()
			{
				FunctionName = "Generic",
				FunctionParameter = new LogicRequest()
				{
					Command = CommandNames.EXECUTE_LOGIC,
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
				Keys = new []
				{
					typeof(EquipmentData).FullName,
					typeof(CollectionData).FullName,
				}.ToList()
			};
			HttpWrapper.Post(playerId, "/Server/GetUserReadOnlyData", request, callback);
		}

		private void OnPlayfabCommand(IHttpResponse response, object userId)
		{
			try
			{
				if (response.HttpCode >= 400)
				{
					var dataString = response.ResponseData?.Length > 0
						? Encoding.UTF8.GetString(response.ResponseData)
						: "";
					Log.Error(
						$"Invalid PlayFab response to url {response.Request.Url} status {response.Status} data {dataString} text {response.ResponseText}");
				}
				else
				{
					if (FlgConfig.DebugMode)
					{
						Log.Debug($"Request from {userId} OK");
					}
				}
			}
			catch (Exception e)
			{
				Log.Error(
					$"Invalid PlayFab response to url {response.Request.Url} status {response.Status} text {response.ResponseText}");
			}
		}
	}
}
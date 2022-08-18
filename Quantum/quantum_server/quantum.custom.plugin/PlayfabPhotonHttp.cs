using Photon.Hive.Plugin;
using PlayFab;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using PlayFab.Json;
using PlayFab.Internal;

namespace quantum.custom.plugin
{
	/// <summary>
	/// Format playfab responds to raw http calls.
	/// Case is not our standards but its the case playfab uses.
	/// </summary>
	public class HttpResponseObject
	{
		public int code;
		public string status;
		public object data;
	}

	/// <summary>
	/// Minimal implementation of photon http utilities.Main reason we need this is due to:
	/// - Using a single host thread to handle requests
	/// - Ensuring non-blocking calls on the main thread
	/// - This was a recommended practice from Photon.
	/// </summary>
	public class PlayfabPhotonHttp
	{
		private readonly IPluginHost _host;
		private readonly ISerializerPlugin _playfabSerializer;
		public string ServerAddress = null;

		public PlayfabPhotonHttp(IPluginHost host)
		{
			_host = host;
			_playfabSerializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		}

		/// <summary>
		/// Deserializes playfab responses using playfab formatts.
		/// </summary>
		public T DeserializePlayFabResponse<T>(IHttpResponse response) where T : PlayFabResultCommon
		{
			var serializedJson = Encoding.UTF8.GetString(response.ResponseData);
			var httpResponse = _playfabSerializer.DeserializeObject<HttpResponseObject>(serializedJson);
			var playfabJson = httpResponse.data as JsonObject;
			var playfabResponse = _playfabSerializer.DeserializeObject<T>(playfabJson.ToString());
			return playfabResponse;
		}

		/// <summary>
		/// Implementation of a Post request using playfab bitstream formatting.
		/// </summary>
		public void Post(string playerId, string service, object requestObject, HttpRequestCallback callback, Dictionary<string, string> headers=null)
		{
			var url = PlayFabSettings.staticSettings.GetFullUrl(service);
			if(ServerAddress != null)
			{
				url = ServerAddress + service;
			}
			var payload = _playfabSerializer.SerializeObject(requestObject);
			var bytes = Encoding.UTF8.GetBytes(payload);
			var request = new HttpRequest()
			{
				Method = "POST",
				Callback = callback,
				Url = url,
				Async = true,
				Accept = "*/*",
				DataStream = new MemoryStream(bytes, 0, bytes.Length),
				CustomHeaders = headers ?? new Dictionary<string, string>()
				{
					{ "X-SecretKey", PlayFabSettings.staticSettings.DeveloperSecretKey }
				},
				Headers = new Dictionary<HttpRequestHeader, string>()
				{
					{ HttpRequestHeader.KeepAlive, "true" }, // TODO: Verify with sniffer
					{ HttpRequestHeader.ContentType, "application/json" },
				},
				UserState = playerId
			};
			_host.HttpRequest(request);
		}
		
	}
}

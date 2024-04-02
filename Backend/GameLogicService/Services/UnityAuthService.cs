using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FirstLight.Game.Logic;
using FirstLight.Server.SDK.Services;
using GameLogicService.Game;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;

namespace GameLogicService.Services;

public class UnityAuthService
{
	private const string URL_TOKEN_EXCHANGE = "https://services.api.unity.com/auth/v1/token-exchange?projectId={0}&environmentId={1}";
	private const string URL_CUSTOM_ID = "https://player-auth.services.api.unity.com/v1/projects/{0}/authentication/server/custom-id";
	private const string PROJECT_ID = "***REMOVED***";

	private HttpClient _tokenExchangeClient;
	private HttpClient _customIDClient;
	private IBaseServiceConfiguration _config;
	private ILogger _log;

	private string _unityAccessToken = null;
	private DateTime _unityAccessTokenExpiration;

	public UnityAuthService(IHttpClientFactory clientFactory, IBaseServiceConfiguration config, ILogger log)
	{
		_config = config;
		_log = log;
		
		_tokenExchangeClient = clientFactory.CreateClient("UnityTokenExchange");
		_tokenExchangeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _config.UnityCloudAuthToken);
		
		_customIDClient = clientFactory.CreateClient("UnityCustomID");
		_customIDClient.DefaultRequestHeaders.Add("UnityEnvironment", _config.UnityCloudEnvironmentName);
	}

	public async Task<PlayFabResult<BackendLogicResult>> Authenticate(string playerId)
	{
		if (string.IsNullOrEmpty(_unityAccessToken) || _unityAccessTokenExpiration < DateTime.Now)
		{
			var tokenExchangeResponse = await _tokenExchangeClient.PostAsync(string.Format(URL_TOKEN_EXCHANGE, PROJECT_ID, _config.UnityCloudEnvironmentID), null);
			var tokenExchangeResponseStr = await tokenExchangeResponse.Content.ReadAsStringAsync();
			if (tokenExchangeResponse.StatusCode != HttpStatusCode.Created)
			{
				_log.LogError($"Unity Token Exchange API call failed with: {tokenExchangeResponse.StatusCode} - {tokenExchangeResponseStr}");
				throw new Exception($"Unity Token Exchange API call failed with: {tokenExchangeResponse.StatusCode} - {tokenExchangeResponseStr}");
			}

			_unityAccessToken = JsonConvert.DeserializeObject<TokenExchangeResponse>(tokenExchangeResponseStr)!.accessToken;
			_unityAccessTokenExpiration = DateTime.Now.AddMinutes(45);
			_customIDClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _unityAccessToken);
		}

		var customIdResponse = await _customIDClient.PostAsync(string.Format(URL_CUSTOM_ID, PROJECT_ID), JsonContent.Create(new {externalId = playerId, signInOnly = false}));
		var customIdResponseStr = await customIdResponse.Content.ReadAsStringAsync();

		if (customIdResponse.StatusCode != HttpStatusCode.OK)
		{
			_log.LogError($"Unity Custom ID API call failed with: {customIdResponse.StatusCode} - {customIdResponseStr}");
			throw new Exception($"Unity Custom ID API call failed with: {customIdResponse.StatusCode}");
		}

		var customID = JsonConvert.DeserializeObject<CustomIDResponse>(customIdResponseStr)!;
		return Playfab.Result(playerId, new Dictionary<string, string>
		{
			{
				"idToken", customID.idToken
			},
			{
				"sessionToken", customID.sessionToken
			},
		});
	}

	private class TokenExchangeResponse
	{
		public string accessToken { get; set; }
	}

	private class CustomIDResponse
	{
		public string idToken { get; set; }
		public string sessionToken { get; set; }
	}
}

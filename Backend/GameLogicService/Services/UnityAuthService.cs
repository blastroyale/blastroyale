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
using Newtonsoft.Json;
using PlayFab;

namespace GameLogicService.Services;

public class UnityAuthService
{
	private const string URL_TOKEN_EXCHANGE = "https://services.api.unity.com/auth/v1/token-exchange?projectId={0}&environmentId={1}";
	private const string URL_CUSTOM_ID = "https://player-auth.services.api.unity.com/v1/projects/{0}/authentication/server/custom-id";
	private const string PROJECT_ID = "***REMOVED***";

	private HttpClient _client;
	private IBaseServiceConfiguration _config;

	private string _unityAccessToken = null;
	private DateTime _unityAccessTokenExpiration;

	public UnityAuthService(IHttpClientFactory clientFactory, IBaseServiceConfiguration config)
	{
		_config = config;
		_client = clientFactory.CreateClient();
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _config.UnityCloudAuthToken);
		_client.DefaultRequestHeaders.Add("UnityEnvironment", _config.UnityCloudEnvironmentName);
	}

	public async Task<PlayFabResult<BackendLogicResult>> Authenticate(string playerId)
	{
		if (string.IsNullOrEmpty(_unityAccessToken) || _unityAccessTokenExpiration < DateTime.Now)
		{
			var tokenExchangeResponse =
				await _client.PostAsync(string.Format(URL_TOKEN_EXCHANGE, PROJECT_ID, _config.UnityCloudEnvironmentID), null);
			if (tokenExchangeResponse.StatusCode != HttpStatusCode.Created)
			{
				throw new Exception("Unity Token Exchange API call failed.");
			}

			var tokenExchangeStr = await tokenExchangeResponse.Content.ReadAsStringAsync();

			_unityAccessToken = JsonConvert.DeserializeObject<TokenExchangeResponse>(tokenExchangeStr)!.accessToken;
			_unityAccessTokenExpiration = DateTime.Now.AddMinutes(45);
			_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _unityAccessToken);
		}

		var customIdResponse = await _client.PostAsync(string.Format(URL_CUSTOM_ID, PROJECT_ID),
			JsonContent.Create(new {externalId = playerId, signInOnly = false}));

		if (customIdResponse.StatusCode != HttpStatusCode.OK)
		{
			throw new Exception("Unity Custom ID API call failed.");
		}

		var customID =
			JsonConvert.DeserializeObject<CustomIDResponse>(await customIdResponse.Content.ReadAsStringAsync())!;
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
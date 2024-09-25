using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using FirstLight.Game.Logic;
using FirstLight.Server.SDK.Services;
using GameLogicService.Game;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.AuthenticationModels;

namespace GameLogicService.Services;

public class UnityAuthService
{
	public const string PROJECT_ID = "***REMOVED***";
	private const string URL_TOKEN_EXCHANGE = "https://services.api.unity.com/auth/v1/token-exchange?projectId={0}&environmentId={1}";
	private const string URL_CUSTOM_ID = "https://player-auth.services.api.unity.com/v1/projects/{0}/authentication/server/custom-id";

	private readonly HttpClient _authenticatedClient;
	private HttpClient _customIDClient;
	private IBaseServiceConfiguration _config;
	private ILogger _log;

	private string _unityAccessToken = null;
	private DateTime _unityAccessTokenExpiration;
	private Dictionary<string, string> _playfabIdToUnityCache = new();

	public UnityAuthService(IHttpClientFactory clientFactory, IBaseServiceConfiguration config, ILogger log)
	{
		_config = config;
		_log = log;

		_authenticatedClient = clientFactory.CreateClient("UnityTokenExchange");
		_authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", config.UnityCloudAuthToken);
		_authenticatedClient.DefaultRequestHeaders.Add("ProjectId", PROJECT_ID);

		_customIDClient = clientFactory.CreateClient("UnityCustomID");
		_customIDClient.DefaultRequestHeaders.Add("UnityEnvironment", _config.UnityCloudEnvironmentName);
	}

	public async Task<string> GetUnityId(string playfabId)
	{
		if (_playfabIdToUnityCache.TryGetValue(playfabId, out var unityId))
		{
			return unityId;
		}

		var data = await AuthenticateCustomIdRequest(playfabId);
		_playfabIdToUnityCache[playfabId] = data.user.id;
		return data.user.id;
	}


	private async Task FetchUnityToken()
	{
		if (string.IsNullOrEmpty(_unityAccessToken) || _unityAccessTokenExpiration < DateTime.Now)
		{
			var tokenExchangeResponse = await _authenticatedClient.PostAsync(string.Format(URL_TOKEN_EXCHANGE, PROJECT_ID, _config.UnityCloudEnvironmentID), null);
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
	}

	public async Task<CustomIDResponse> AuthenticateCustomIdRequest(string playfabId)
	{
		await FetchUnityToken();
		var customIdResponse = await _customIDClient.PostAsync(string.Format(URL_CUSTOM_ID, PROJECT_ID), JsonContent.Create(new { externalId = playfabId, signInOnly = false }));
		var customIdResponseStr = await customIdResponse.Content.ReadAsStringAsync();

		if (customIdResponse.StatusCode != HttpStatusCode.OK)
		{
			_log.LogError($"Unity Custom ID API call failed with: {customIdResponse.StatusCode} - {customIdResponseStr}");
			throw new Exception($"Unity Custom ID API call failed with: {customIdResponse.StatusCode}");
		}

		return JsonConvert.DeserializeObject<CustomIDResponse>(customIdResponseStr)!;
	}


	public HttpClient GetServerAuthenticatedClient()
	{
		return _authenticatedClient;
	}


	private class TokenExchangeResponse
	{
		public string accessToken { get; set; }
	}

	public class CustomIDResponse
	{
		public string idToken { get; set; }
		public string sessionToken { get; set; }

		public UserResponse user { get; set; }
	}

	public class UserResponse
	{
		public string id { get; set; }
	}
}
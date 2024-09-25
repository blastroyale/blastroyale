using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FirstLight.Server.SDK.Services;
using Microsoft.Extensions.Logging;
using PlayFab;
using PlayFab.AdminModels;

namespace GameLogicService.Services;

public class UnityCloudService
{
	public IBaseServiceConfiguration config;
	public UnityAuthService unityAuthService;
	private ILogger _logger;

	public UnityCloudService(IBaseServiceConfiguration config, UnityAuthService unityAuthService, ILogger logger)
	{
		this._logger = logger;
		this.config = config;
		this.unityAuthService = unityAuthService;
	}

	private class UserMetadata
	{
		public string username;
	}

	private class GetPlayerResponse
	{
		public UserMetadata usernamePasswordLoginMetadata;
	}

	public async Task<string> GetPlayerName(string playerId)
	{
		var response = await unityAuthService.GetServerAuthenticatedClient().GetAsync(
			$"https://services.api.unity.com/player-identity/v1/projects/{UnityAuthService.PROJECT_ID}/users/{playerId}");


		if (response.StatusCode == HttpStatusCode.OK)
		{
			var parsed = await response.Content.ReadFromJsonAsync<GetPlayerResponse>();
			return parsed.usernamePasswordLoginMetadata?.username;
		}

		return null;
	}

	public async Task SetGameData(string key, string propertyName, string propertyValue)
	{
		var url = $"https://services.api.unity.com/cloud-save/v1/data/projects/{UnityAuthService.PROJECT_ID}/environments/{config.UnityCloudEnvironmentID}/custom/{key}/items";

		var res = await unityAuthService.GetServerAuthenticatedClient()
			.PostAsJsonAsync(url, new
			{
				key = propertyName,
				value = propertyValue,
			});
		if (res.StatusCode != HttpStatusCode.OK)
		{
			var body = await res.Content.ReadAsStringAsync();
			throw new HttpRequestException(res.StatusCode + " " + body);
		}

		_logger.LogDebug("Setting " + key + " " + propertyName + "=" + propertyValue);
	}

	public async Task SyncName(string playfabId)
	{
		// This is an unnatural aberration and should not exist, the SHITTY unity friend system
		// doesn't support adding friends by name those suckers, so we are storing the player's name
		// in a queryable value inside CloudSave, then we get the player ID and add it
		// This shit should not be used directly like I'm doing, but it's not worth refactoring all the system now,
		// its like putting a cherry on the top of a huge pile of shit
		// we need to migrate this friends system to a proper solution
		var playerName = await PlayFabAdminAPI.GetPlayerProfileAsync(new GetPlayerProfileRequest()
		{
			PlayFabId = playfabId,
			ProfileConstraints = new PlayerProfileViewConstraints()
			{
				ShowDisplayName = true
			}
		});
		if (playerName.Error != null)
		{
			_logger.LogError(playerName.Error.GenerateErrorReport());
			return;
		}

		var name = playerName.Result.PlayerProfile.DisplayName;
		var unityId = await unityAuthService.GetUnityId(playfabId);
		await SetGameData("read-only-" + unityId, "player_name", name);
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using PlayFab;
using PlayFab.ServerModels;

/// <summary>
/// Minimal playfab api's consumed by this plugin.
/// </summary>
public class PlayfabService
{
	public PlayfabService(Dictionary<String, String> photonConfig)
	{
		PlayFabSettings.staticSettings.TitleId = photonConfig["PlayfabTitle"];
		PlayFabSettings.staticSettings.DeveloperSecretKey = photonConfig["PlayfabKey"];
	}

	/// <summary>
	/// Reads user readonly data. 
	/// </summary>
	public Dictionary<string, string> GetProfileReadOnlyData(string id)
	{
		var fabResponse = PlayFabServerAPI.GetUserReadOnlyDataAsync(new GetUserDataRequest()
		{
			PlayFabId = id
		});
		fabResponse.Wait();
		return fabResponse.Result.Result.Data.ToDictionary(
			entry => entry.Key,
			entry => entry.Value.Value);
	}
}
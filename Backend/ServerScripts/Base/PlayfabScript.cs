using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;

/// <summary>
/// Playfab configuration for a single environment
/// </summary>
public class PlayfabConfiguration
{
	public string TitleId;
	public string SecretKey;
	public string AllPlayersSegmentId;
}

/// <summary>
/// Represents our stacks
/// </summary>
public enum PlayfabEnvironment
{
	DEV, STAGING, PROD
}

/// <summary>
/// Will setup playfab before script run
/// </summary>
public abstract class PlayfabScript : IScript
{
	private Dictionary<PlayfabEnvironment, PlayfabConfiguration> _envSetups = new ()
	{
		{ PlayfabEnvironment.DEV, new PlayfabConfiguration()
		{
			TitleId = "***REMOVED***",
			SecretKey = "***REMOVED***",
			AllPlayersSegmentId = "97EC6C2DE051B678"
		}},
		
		{ PlayfabEnvironment.STAGING, new PlayfabConfiguration()
		{
			TitleId = "***REMOVED***",
			SecretKey = "***REMOVED***",
			AllPlayersSegmentId = "1ECB17662366E940"
		}},
		
		{ PlayfabEnvironment.PROD, new PlayfabConfiguration()
		{
			TitleId = "302CF",
			SecretKey = Environment.GetEnvironmentVariable("PLAYFAB_PROD_SECRET_KEY"),
			AllPlayersSegmentId = "4C470D5AF0430D65"
		}},
	};
	
	public abstract PlayfabEnvironment GetEnvironment();

	protected PlayfabConfiguration GetPlayfabConfiguration()
	{
		return _envSetups[GetEnvironment()];
	}

	protected async Task<List<PlayerProfile>> GetAllPlayers()
	{
		Console.WriteLine(GetPlayfabConfiguration().AllPlayersSegmentId);
		var segmentResult = await PlayFabServerAPI.GetPlayersInSegmentAsync(new GetPlayersInSegmentRequest()
		{
			SegmentId = GetPlayfabConfiguration().AllPlayersSegmentId,
			MaxBatchSize = 10000
		});
		HandleError(segmentResult.Error);
		Console.WriteLine($"Processing {segmentResult.Result.PlayerProfiles.Count} Players");
		return segmentResult.Result.PlayerProfiles;
	}
	
	public abstract void Execute(ScriptParameters args);

	public void HandleError(PlayFabError? error)
	{
		if (error == null)
			return;
		throw new Exception($"Playfab Error {error.ErrorMessage}:{JsonConvert.SerializeObject(error.ErrorDetails)}");
	}
	
	public PlayfabScript()
	{
		var playfabSetup = GetPlayfabConfiguration();
		PlayFabSettings.staticSettings.TitleId = playfabSetup.TitleId;
		PlayFabSettings.staticSettings.DeveloperSecretKey = playfabSetup.SecretKey;
		Console.WriteLine($"Using Playfab Title {PlayFabSettings.staticSettings.TitleId}");
	}
}
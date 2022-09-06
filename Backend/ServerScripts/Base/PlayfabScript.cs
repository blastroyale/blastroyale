using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Backend.Game.Services;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Services;
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
	
	public PlayfabScript()
	{
		var playfabSetup = GetPlayfabConfiguration();
		PlayFabSettings.staticSettings.TitleId = playfabSetup.TitleId;
		PlayFabSettings.staticSettings.DeveloperSecretKey = playfabSetup.SecretKey;
		Console.WriteLine($"Using Playfab Title {PlayFabSettings.staticSettings.TitleId}");
	}
	
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
	
	/// <summary>
	/// Reads user state from playfab
	/// </summary>
	protected async Task<ServerState> ReadUserState(string playfabId)
	{
		var userDataResult = await PlayFabServerAPI.GetUserReadOnlyDataAsync(new GetUserDataRequest()
		{
			PlayFabId = playfabId
		});
		if (userDataResult.Error != null)
		{
			Console.WriteLine($"Error finding user {playfabId}");
			return null;
		}
		var userDataJson = userDataResult.Result.Data.ToDictionary(
		                                                           entry => entry.Key,
		                                                           entry => entry.Value.Value);
		return new ServerState(userDataJson);
	}
	
	/// <summary>
	/// Updates the given server state on playfab for a given user.
	/// </summary>
	protected async Task SetUserState(string playerId, ServerState state)
	{
		var result = await PlayFabServerAPI.UpdateUserReadOnlyDataAsync(new UpdateUserDataRequest()
		{
			PlayFabId = playerId,
			Data = state
		});
		if (result.Error != null)
		{
			Console.WriteLine($"Error updating user {playerId}");
		}
		else
		{
			Console.WriteLine($"User {playerId} updated");
		}
	}
	
	/// <summary>
	/// Deletes specific keys from playfab user readonly data
	/// </summary>
	protected async Task DeleteStateKey(string playerId, params string [] keys)
	{
		var result = await PlayFabServerAPI.UpdateUserReadOnlyDataAsync(new UpdateUserDataRequest()
		{
			PlayFabId = playerId,
			KeysToRemove = keys.ToList()
		});
		if (result.Error != null)
		{
			Console.WriteLine($"Error deleteing key for user user {playerId}");
		}
		else
		{
			Console.WriteLine($"User {playerId} got keys deleted");
		}
	}
	
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Backend.Game.Services;
using CsvHelper;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Services;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.AdminModels;
using PlayFab.Internal;
using PlayFab.ServerModels;
using AddPlayerTagRequest = PlayFab.AdminModels.AddPlayerTagRequest;
using GetAllSegmentsRequest = PlayFab.ServerModels.GetAllSegmentsRequest;
using GetPlayerProfileRequest = PlayFab.ServerModels.GetPlayerProfileRequest;
using GetPlayersInSegmentRequest = PlayFab.ServerModels.GetPlayersInSegmentRequest;
using GetUserDataRequest = PlayFab.ServerModels.GetUserDataRequest;
using LoginIdentityProvider = PlayFab.ServerModels.LoginIdentityProvider;
using PlayerProfile = PlayFab.ServerModels.PlayerProfile;
using PlayerProfileModel = PlayFab.ServerModels.PlayerProfileModel;
using RemovePlayerTagRequest = PlayFab.AdminModels.RemovePlayerTagRequest;
using UpdateUserDataRequest = PlayFab.ServerModels.UpdateUserDataRequest;

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
	DEV,
	STAGING,
	PROD,
	TESTNET,
}

/// <summary>
/// Will setup playfab before script run
/// </summary>
public abstract class PlayfabScript : IScript
{
	private static string ReadFromFile = "read_from_file";

	private Dictionary<PlayfabEnvironment, PlayfabConfiguration> _envSetups = new()
	{
		{
			PlayfabEnvironment.DEV, new PlayfabConfiguration()
			{
				TitleId = "***REMOVED***",
				SecretKey = "***REMOVED***",
				AllPlayersSegmentId = "97EC6C2DE051B678"
			}
		},

		{
			PlayfabEnvironment.STAGING, new PlayfabConfiguration()
			{
				TitleId = "***REMOVED***",
				SecretKey = "***REMOVED***",
				AllPlayersSegmentId = "1ECB17662366E940"
			}
		},
		{
			PlayfabEnvironment.TESTNET, new PlayfabConfiguration()
			{
				TitleId = "***REMOVED***",
				SecretKey = "***REMOVED***",
				AllPlayersSegmentId = "	4F3220F8011EE630"
			}
		},

		{
			PlayfabEnvironment.PROD, new PlayfabConfiguration()
			{
				TitleId = "***REMOVED***",
				SecretKey = ReadFromFile,
				AllPlayersSegmentId = "98523D5E0EF3941"
			}
		},
	};

	public async Task SetPlayerTag(string playfabId, string tag, bool enabled)
	{
		if (enabled)
		{
			var result = await PlayFabAdminAPI.AddPlayerTagAsync(new AddPlayerTagRequest()
			{
				PlayFabId = playfabId,
				TagName = tag,
			});
			HandleError(result.Error);
		}
		else
		{
			var result = await PlayFabAdminAPI.RemovePlayerTagAsync(new RemovePlayerTagRequest()
			{
				PlayFabId = playfabId,
				TagName = tag,
			});
			HandleError(result.Error);
		}
	}


	public void ToCsv(string path, List<List<string>> data)
	{
		using (var writer = new StreamWriter(path))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
			{
				var headings = new List<string>(data.First());
				foreach (var heading in headings)
				{
					csv.WriteField(heading);
				}


				csv.NextRecord();
				for (var x = 1; x < data.Count; x++)
				{
					foreach (var column in data)
					{
						csv.WriteField(column);
					}

					csv.NextRecord();
				}
			}
	}

	public PlayfabScript()
	{
		SetEnvironment(GetEnvironment());
	}

	protected void SetEnvironment(PlayfabEnvironment environment)
	{
		var playfabSetup = _envSetups[environment];
		PlayFabSettings.staticSettings.TitleId = playfabSetup.TitleId;
		var key = playfabSetup.SecretKey;
		if (key == ReadFromFile)
		{
			var keyPath = Path.Combine(Environment.CurrentDirectory, "playfab_key.txt");
			key = File.ReadAllText(keyPath);
		}

		PlayFabSettings.staticSettings.DeveloperSecretKey = key;
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
		return await GetPlayerSegment(GetPlayfabConfiguration().AllPlayersSegmentId);
	}

	protected async Task<string?> GetSegmentID(string segmentName)
	{
		var segments = await PlayFabServerAPI.GetAllSegmentsAsync(new GetAllSegmentsRequest());
		HandleError(segments.Error);
		return segments.Result.Segments.Where(s => s.Name == segmentName).Select(s => s.Id).FirstOrDefault();
	}

	protected async Task<List<PlayerProfile>> GetPlayerSegment(string segment)
	{
		var segmentResult = await PlayFabServerAPI.GetPlayersInSegmentAsync(new GetPlayersInSegmentRequest()
		{
			SegmentId = segment,
			MaxBatchSize = 10000
		});
		HandleError(segmentResult.Error);
		Console.WriteLine($"Processing {segmentResult.Result.PlayerProfiles.Count} Players");
		return segmentResult.Result.PlayerProfiles;
	}

	protected async Task<List<PlayerProfile>> GetPlayerSegmentByName(string segmentName)
	{
		var segmentId = await GetSegmentID(segmentName);
		if (segmentId == null)
		{
			throw new Exception($"Segment {segmentName} not found!");
		}

		return await GetPlayerSegment(segmentId);
	}

	protected async Task<string?> GetPlayfabID(string email)
	{
		var info = await PlayFabAdminAPI.GetUserAccountInfoAsync(new LookupUserAccountInfoRequest()
		{
			Email = email
		});


		if (info.Error == null)
		{
			return info.Result.UserInfo.PlayFabId;
		}

		if (info.Error.Error == PlayFabErrorCode.TitleNotActivated)
		{
			return null;
		}

		if (info.Error.Error == PlayFabErrorCode.AccountNotFound)
		{
			return null;
		}

		HandleError(info.Error);
		return null;
	}

	public async Task CreateSumMetric(string name)
	{
		await PlayFabAdminAPI.CreatePlayerStatisticDefinitionAsync(new()
		{
			AggregationMethod = StatisticAggregationMethod.Sum,
			StatisticName = name
		});
	}

	public async Task SumStatistic(string user, params StatisticUpdate[] updates)
	{
		var r = await PlayFabServerAPI.UpdatePlayerStatisticsAsync(new UpdatePlayerStatisticsRequest()
		{
			PlayFabId = user,
			Statistics = updates.ToList()
		});
		HandleError(r.Error);
	}

	public T HandleError<T>(PlayFabResult<T> result) where T : PlayFabResultCommon
	{
		if (result.Error == null) return result.Result;
		throw new Exception($"Playfab Error {result.Error.ErrorMessage}:{result.Error.GenerateErrorReport()}");
	}

	public abstract void Execute(ScriptParameters args);

	public async Task<List<PlayerLeaderboardEntry>> GetLeaderboard(int offset, int count, string name)
	{
		var result = await PlayFabServerAPI.GetLeaderboardAsync(new GetLeaderboardRequest()
		{
			StatisticName = name,
			MaxResultsCount = count,
			StartPosition = offset
		});
		HandleError(result.Error);
		return result.Result.Leaderboard;
	}

	public void HandleError(PlayFabError? error)
	{
		if (error == null)
			return;
		throw new Exception($"Playfab Error {error.ErrorMessage}:{error.GenerateErrorReport()}");
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

	protected async Task<ServerState> ReadUserState(PlayerProfile profile)
	{
		var userDataResult = await PlayFabServerAPI.GetUserReadOnlyDataAsync(new GetUserDataRequest()
		{
			PlayFabId = profile.PlayerId
		});
		if (userDataResult.Error != null)
		{
			var linked = profile.LinkedAccounts.FirstOrDefault(a => a.Platform == LoginIdentityProvider.PlayFab);
			if (linked != null)
			{
				profile.LinkedAccounts.Remove(linked);
				profile.PlayerId = linked.PlatformUserId;
				return await ReadUserState(profile);
			}

			Console.WriteLine($"Error finding user {profile.PlayerId}");
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
	protected async Task DeleteStateKey(string playerId, params string[] keys)
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
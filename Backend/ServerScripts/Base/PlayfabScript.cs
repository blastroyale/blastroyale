using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using PlayFab;
using PlayFab.AdminModels;
using PlayFab.AuthenticationModels;
using PlayFab.Internal;
using PlayFab.ServerModels;
using Scripts.Base;
using ShellProgressBar;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Apis.Admin;
using AddPlayerTagRequest = PlayFab.AdminModels.AddPlayerTagRequest;
using GetAllSegmentsRequest = PlayFab.ServerModels.GetAllSegmentsRequest;
using GetPlayersInSegmentRequest = PlayFab.ServerModels.GetPlayersInSegmentRequest;
using GetUserDataRequest = PlayFab.ServerModels.GetUserDataRequest;
using LoginIdentityProvider = PlayFab.ServerModels.LoginIdentityProvider;
using PlayerProfile = PlayFab.ServerModels.PlayerProfile;
using RemovePlayerTagRequest = PlayFab.AdminModels.RemovePlayerTagRequest;
using UpdateUserDataRequest = PlayFab.ServerModels.UpdateUserDataRequest;

/// <summary>
/// Playfab configuration for a single environment
/// </summary>
public class EnvironmentConfiguration
{
	public string TitleId;
	public string AllPlayersSegmentId;
	public string ServerBaseEndpoint;
	public string UnityEnvironmentId;
	public string UnityProjectId;
	[AllowNull] public EnvironmentSecrets Secrets;
}

/// <summary>
/// Represents our stacks
/// </summary>
public enum Environment
{
	DEV,
	STAGING,
	PROD,
	TESTNET,
}

public class EnvironmentSecrets
{
	public string SecretKey;
	public string ServerSecretKey;
	public string UnityServiceAccountKeyId;
	public string UnityServiceAccountSecretKey;
	public string UnityHeader;
}

/// <summary>
/// Will setup playfab before script run
/// </summary>
public abstract class PlayfabScript : IScript
{
	private static string EnvsFile = "environments.json";
	public Environment CurrentEnvironment { private set; get; }

	public IAdminApiClient GetUnityAdmin()
	{
		return AdminApiClient.Create();
	}

	public UnityScriptExecutionContext GetUnityContext()
	{
		var config = GetPlayfabConfiguration();
		return new UnityScriptExecutionContext()
		{
			ProjectId = config.UnityProjectId,
			EnvironmentId = config.UnityEnvironmentId,
			ServiceAccountSecretKey = config.Secrets.UnityServiceAccountSecretKey,
			ServiceAccountKeyId = config.Secrets.UnityServiceAccountKeyId,
		};
	}


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

	public EnvironmentConfiguration GetPlayfabConfiguration()
	{
		return ReadEnvironmentConfiguration(CurrentEnvironment.ToString());
	}

	protected void SetEnvironment(Environment environment)
	{
		var playfabSetup = ReadEnvironmentConfiguration(environment.ToString());
		if (playfabSetup == null) return;
		CurrentEnvironment = environment;
		var secrets = playfabSetup.Secrets;
		PlayFabSettings.staticSettings.TitleId = playfabSetup.TitleId;

		// ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract

		playfabSetup.Secrets = secrets;
		PlayFabSettings.staticSettings.DeveloperSecretKey = secrets.SecretKey;
		ConfigRegistry.Set("ServerSecretKey", secrets.ServerSecretKey);
		ConfigRegistry.Set("ServerBaseEndpoint", playfabSetup.ServerBaseEndpoint);
		Console.WriteLine($"Using Playfab Title {PlayFabSettings.staticSettings.TitleId}");
	}

	private EnvironmentConfiguration ReadEnvironmentConfiguration(string env)
	{
		try
		{
			var keyPath = Path.Combine(System.Environment.CurrentDirectory, EnvsFile);
			var envs =
				ModelSerializer.Deserialize<Dictionary<string, EnvironmentConfiguration>>(File.ReadAllText(keyPath));
			if (envs.TryGetValue(env, out var config))
			{
				return config;
			}
		}
		catch (IOException ex)
		{
			Console.Error.WriteLine($"Error reading file {EnvsFile}: {ex.Message}");
		}

		Console.Error.WriteLine(
			$"Could not get env file {EnvsFile}!, please create it! PS: You can get it configured from 1password");
		return null;
	}

	protected async Task AuthenticateServer()
	{
		await PlayFabAuthenticationAPI.GetEntityTokenAsync(new GetEntityTokenRequest()
		{
			AuthenticationContext = new PlayFabAuthenticationContext()
			{
				PlayFabId = GetPlayfabConfiguration().TitleId
			}
		}).HandleError();
	}

	public abstract Environment GetEnvironment();


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


	public async Task<int> GetPlayerAmountInSegment(
		string segmentId)
	{
		var segmentResult = await PlayFabServerAPI.GetPlayersInSegmentAsync(new GetPlayersInSegmentRequest()
		{
			SegmentId = segmentId,
			MaxBatchSize = 0,
		}).HandleError();
		return segmentResult.ProfilesInSegment;
	}

	public async IAsyncEnumerable<List<PlayerProfile>> GetPlayersEnumerable(
		string segmentId, uint? maxBatchSize = 2000, int maxRetries = 8, int retryDelay = 7)
	{
		string continuationToken = null;

		int batch = 0;
		Console.WriteLine($"Fetching all players in batches of " + maxBatchSize + " players");
		int totalPlayers = 0;
		int retries = 0;
		do
		{
			var segmentResult = await PlayFabServerAPI.GetPlayersInSegmentAsync(new GetPlayersInSegmentRequest()
			{
				SegmentId = segmentId,
				MaxBatchSize = maxBatchSize,
				ContinuationToken = continuationToken,
				SecondsToLive = 5300,
			});
			if (segmentResult.Error != null && retries < maxRetries)
			{
				var waitForRetry = retryDelay * Math.Pow(2, retries);
				Console.WriteLine(segmentResult.Error.GenerateErrorReport());
				Console.WriteLine("Failed fetching users attempt " + retries + ", attempting again in " + waitForRetry +
					" seconds!");
				await Task.Delay(TimeSpan.FromSeconds(waitForRetry));
				retries++;
				continue;
			}

			retries = 0;
			HandleError(segmentResult.Error);

			bool sentLog = false;
			if (segmentResult.Result.PlayerProfiles != null)
			{
				yield return segmentResult.Result.PlayerProfiles;
			}

			batch++;
			continuationToken = segmentResult.Result.ContinuationToken;
		} while (!string.IsNullOrEmpty(continuationToken));

		Console.WriteLine(
			$"Completed retrieving players for segment!");
	}

	protected async Task<List<PlayerProfile>> GetPlayerSegmentByName(string segmentName)
	{
		const uint MAX_BATCH_SIZE = 10000;

		var allPlayerProfiles = new List<PlayerProfile>();
		string continuationToken = null;

		Console.WriteLine($"Starting to retrieve players for segment: {segmentName}");

		do
		{
			var segmentResult = await PlayFabServerAPI.GetPlayersInSegmentAsync(new GetPlayersInSegmentRequest()
			{
				SegmentId = await GetSegmentID(segmentName),
				MaxBatchSize = MAX_BATCH_SIZE,
				ContinuationToken = continuationToken
			});

			HandleError(segmentResult.Error);

			if (segmentResult.Result.PlayerProfiles != null)
			{
				allPlayerProfiles.AddRange(segmentResult.Result.PlayerProfiles);
			}

			continuationToken = segmentResult.Result.ContinuationToken;

			if (!string.IsNullOrEmpty(continuationToken))
			{
				Console.WriteLine(
					$"Continuation token present on Playfab Response. Proceeding to next batch of {MAX_BATCH_SIZE} players...");
			}
			else
			{
				Console.WriteLine(
					$"No more continuation token. All players have been retrieved for Segment {segmentName}.");
			}
		} while (!string.IsNullOrEmpty(continuationToken));

		Console.WriteLine(
			$"Completed retrieving players for segment: {segmentName}. Total players: {allPlayerProfiles.Count}");

		return allPlayerProfiles;
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

	protected async Task TagPlayer(PlayerProfile profile, string tagName)
	{
		await PlayFabAdminAPI.AddPlayerTagAsync(new AddPlayerTagRequest()
		{
			PlayFabId = profile.PlayerId,
			TagName = tagName
		});
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
			Console.WriteLine($"Error finding user {playfabId} " + userDataResult.Error.GenerateErrorReport());
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
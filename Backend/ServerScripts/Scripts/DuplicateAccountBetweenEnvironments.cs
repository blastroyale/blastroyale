using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Server.SDK.Modules;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.ServerModels;
using AddUserVirtualCurrencyRequest = PlayFab.ServerModels.AddUserVirtualCurrencyRequest;
using BanRequest = PlayFab.AdminModels.BanRequest;
using BanUsersRequest = PlayFab.AdminModels.BanUsersRequest;
using GetPlayerCombinedInfoRequestParams = PlayFab.ClientModels.GetPlayerCombinedInfoRequestParams;
using GetPlayerStatisticsRequest = PlayFab.ServerModels.GetPlayerStatisticsRequest;
using SendAccountRecoveryEmailRequest = PlayFab.AdminModels.SendAccountRecoveryEmailRequest;
using StatisticUpdate = PlayFab.ServerModels.StatisticUpdate;
using UpdatePlayerStatisticsRequest = PlayFab.ServerModels.UpdatePlayerStatisticsRequest;


public class DuplicateAccountBetweenEnvironments : PlayfabScript
{
	private GetPlayerCombinedInfoRequestParams StandardLoginInfoRequestParams =>
		new()
		{
			GetPlayerProfile = true,
			GetUserAccountInfo = true,
			GetTitleData = true,
			GetPlayerStatistics = true
		};

	public override PlayfabEnvironment GetEnvironment() => PlayfabEnvironment.PROD;

	public override void Execute(ScriptParameters parameters)
	{

		RunAsync().Wait();
	}

	public async Task RunAsync()
	{
		var availableEnvironments = string.Join(", ", Enum.GetNames<PlayfabEnvironment>());

		// Source
		Console.WriteLine();
		Console.WriteLine("Available Environments: " + availableEnvironments);
		Console.WriteLine("Input the environment to copy from: ");
		var targetString = Console.ReadLine();
		if (!Enum.TryParse<PlayfabEnvironment>(targetString?.Trim().ToUpperInvariant(), out var copyFrom))
		{
			Console.WriteLine("Invalid environment, available ones: " + availableEnvironments);
			return;
		}
		
		// Target
		Console.WriteLine();
		Console.WriteLine("Input the environment to copy to: ");
		targetString = Console.ReadLine();
		if (!Enum.TryParse<PlayfabEnvironment>(targetString?.Trim().ToUpperInvariant(), out var copyTo))
		{
			Console.WriteLine("Invalid environment, available ones: " + availableEnvironments);
			return;
		}
		Console.WriteLine("Input the account id");
		var baseAccount = Console.ReadLine();
		
		SetEnvironment(copyFrom);

		// Get User State
		var state = await ReadUserState(baseAccount);
		// Get User Statistics
		var getStatistics = HandleError(await PlayFabServerAPI.GetPlayerStatisticsAsync(new GetPlayerStatisticsRequest()
		{
			PlayFabId = baseAccount
		}));
		// Get User inventory
		var inventoryResult = HandleError(await PlayFabServerAPI.GetUserInventoryAsync(new()
		{
			PlayFabId = baseAccount,
		}));

		// Start copying to new accoutn
		SetEnvironment(copyTo);
		var newTestAccount = "clone-" + Guid.NewGuid();
		// Create new account
		var newAccount = HandleError(await PlayFabClientAPI.LoginWithCustomIDAsync(new LoginWithCustomIDRequest()
		{
			CustomId = newTestAccount,
			InfoRequestParameters = StandardLoginInfoRequestParams,
			CreateAccount = true
		}));

		var d = DateTime.Now;
		var changeName = HandleError(await PlayFabClientAPI.UpdateUserTitleDisplayNameAsync(new UpdateUserTitleDisplayNameRequest()
		{
			DisplayName = "Clone" + DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH'-'mm'-'ss")
		}));
		var newPlayFabId = newAccount.PlayFabId;

		await SetUserState(newAccount.PlayFabId, state);

		foreach (var chunk in getStatistics.Statistics.Chunk(25))
		{
			HandleError(await PlayFabServerAPI.UpdatePlayerStatisticsAsync(new UpdatePlayerStatisticsRequest()
			{
				PlayFabId = newPlayFabId,
				Statistics = chunk.Select((s) => new StatisticUpdate()
				{
					StatisticName = s.StatisticName,
					Value = s.Value
				}).ToList()
			}));
		}


		if (inventoryResult.Inventory.Count > 0)
		{
			HandleError(await PlayFabServerAPI.GrantItemsToUserAsync(new GrantItemsToUserRequest()
			{
				PlayFabId = newAccount.PlayFabId,
				ItemIds = inventoryResult.Inventory.Select(a => a.ItemId).ToList()
			}));
		}

		foreach (var kv in inventoryResult.VirtualCurrency)
		{
			if (kv.Value == 0) continue;
			HandleError(await PlayFabServerAPI.AddUserVirtualCurrencyAsync(new AddUserVirtualCurrencyRequest()
			{
				PlayFabId = newAccount.PlayFabId,
				VirtualCurrency = kv.Key,
				Amount = kv.Value
			}));
		}

		Console.WriteLine("Copied account to id " + newAccount.PlayFabId + " with customId " + newTestAccount);
		Console.WriteLine($"Link: https://developer.playfab.com/en-us/r/t/{PlayFabSettings.staticSettings.TitleId}/players/{newPlayFabId}/data");
	}
}
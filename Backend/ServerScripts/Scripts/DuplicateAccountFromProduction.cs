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


public class DuplicateAccountFromProduction : PlayfabScript
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
		Console.WriteLine("Input the account id");
		var account = Console.ReadLine();
		RunAsync(account).Wait();
	}

	public async Task RunAsync(string baseAccount)
	{
		var copyFrom = PlayfabEnvironment.PROD;
		var copyTo = PlayfabEnvironment.STAGING;

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
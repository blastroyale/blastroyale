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
using GetPlayerStatisticsRequest = PlayFab.ServerModels.GetPlayerStatisticsRequest;
using SendAccountRecoveryEmailRequest = PlayFab.AdminModels.SendAccountRecoveryEmailRequest;
using StatisticUpdate = PlayFab.ServerModels.StatisticUpdate;
using UpdatePlayerStatisticsRequest = PlayFab.ServerModels.UpdatePlayerStatisticsRequest;


public class DuplicateAccount : PlayfabScript
{
	public override PlayfabEnvironment GetEnvironment() => PlayfabEnvironment.PROD;

	public override void Execute(ScriptParameters parameters)
	{
		Console.WriteLine("Input the account id");
		var account = Console.ReadLine();

		Console.WriteLine("Email");
		var email = Console.ReadLine();
		var task = RunAsync(account, email);
		task.Wait();
	}

	public async Task RunAsync(string baseAccount, string email)
	{
		// Create new account
		var newAccount = HandleError(await PlayFabClientAPI.RegisterPlayFabUserAsync(new RegisterPlayFabUserRequest()
		{
			TitleId = GetPlayfabConfiguration().TitleId,
			DisplayName = $"RecoveredAccount{Random.Shared.Next(3000)}",
			Email = email,
			Password = "genericpa$sw@rd2",
			RequireBothUsernameAndEmail = false,
		}));

		// Copy user state
		var state = await ReadUserState(baseAccount);
		await SetUserState(newAccount.PlayFabId, state);


		// Copy user statistics
		var getStatistics = HandleError(await PlayFabServerAPI.GetPlayerStatisticsAsync(new GetPlayerStatisticsRequest()
		{
			PlayFabId = baseAccount
		}));
		HandleError(await PlayFabServerAPI.UpdatePlayerStatisticsAsync(new UpdatePlayerStatisticsRequest()
		{
			PlayFabId = newAccount.PlayFabId,
			Statistics = getStatistics.Statistics.Select((s) => new StatisticUpdate()
			{
				StatisticName = s.StatisticName,
				Value = s.Value
			}).ToList()
		}));
		var inventoryResult = HandleError(await PlayFabServerAPI.GetUserInventoryAsync(new()
		{
			PlayFabId = baseAccount,
		}));


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

		// Ban user so we can track which account he was migrated to
		HandleError(await PlayFabAdminAPI.BanUsersAsync(new BanUsersRequest()
		{
			Bans = new List<BanRequest>()
			{
				new()
				{
					DurationInHours = 2628000,
					PlayFabId = baseAccount,
					Reason = "Migrated account to " + newAccount.PlayFabId
				}
			}
		}));

		await UnlinkCustomIds(baseAccount);
		Console.WriteLine("Copied account to id " + newAccount.PlayFabId);
		HandleError(await PlayFabAdminAPI.SendAccountRecoveryEmailAsync(new SendAccountRecoveryEmailRequest()
		{
			Email = email
		}));
		Console.WriteLine("Sent recover email to " + email);
	}

	private async Task UnlinkCustomIds(string baseAccount)
	{
		var getServerCustomIds = HandleError(await PlayFabServerAPI.GetServerCustomIDsFromPlayFabIDsAsync(new GetServerCustomIDsFromPlayFabIDsRequest()
		{
			PlayFabIDs = new List<string>() { baseAccount }
		}));
		foreach (var pair in getServerCustomIds.Data)
		{
			if (string.IsNullOrEmpty(pair.ServerCustomId)) continue;
			Console.WriteLine("Trying to unlink " + pair.PlayFabId + " - " + pair.ServerCustomId);
			HandleError(await PlayFabServerAPI.UnlinkServerCustomIdAsync(new UnlinkServerCustomIdRequest()
			{
				PlayFabId = pair.PlayFabId,
				ServerCustomId = pair.ServerCustomId
			}));
		}
	}
}
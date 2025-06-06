using System;
using System.Linq;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.ServerModels;
using AddUserVirtualCurrencyRequest = PlayFab.ServerModels.AddUserVirtualCurrencyRequest;
using GetPlayerCombinedInfoRequestParams = PlayFab.ClientModels.GetPlayerCombinedInfoRequestParams;
using GetPlayerStatisticsRequest = PlayFab.ServerModels.GetPlayerStatisticsRequest;
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

	public override Environment GetEnvironment() => Environment.PROD;

	public override void Execute(ScriptParameters parameters)
	{
		RunAsync().Wait();
	}

	public async Task RunAsync()
	{
		var availableEnvironments = string.Join(", ", Enum.GetNames<Environment>());

		// Source
		Console.WriteLine();
		Console.WriteLine("Available Environments: " + availableEnvironments);
		Console.WriteLine("Input the environment to copy from: ");
		var targetString = Console.ReadLine();
		if (!Enum.TryParse<Environment>(targetString?.Trim().ToUpperInvariant(), out var copyFrom))
		{
			Console.WriteLine("Invalid environment, available ones: " + availableEnvironments);
			return;
		}

		// Target
		Console.WriteLine();
		Console.WriteLine("Input the environment to copy to: ");
		targetString = Console.ReadLine();
		if (!Enum.TryParse<Environment>(targetString?.Trim().ToUpperInvariant(), out var copyTo))
		{
			Console.WriteLine("Invalid environment, available ones: " + availableEnvironments);
			return;
		}

		Console.WriteLine("Input the source account id");
		var baseAccount = Console.ReadLine();


		Console.WriteLine("Input the target account id (you can type NEW) to create a new one");
		var targetAccount = Console.ReadLine();

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

		var newPlayFabId = targetAccount;
		var customId = "";
		if (targetAccount.ToLowerInvariant().Equals("new"))
		{
			var newTestAccount = "clone-" + Guid.NewGuid();
			// Create new account
			var newAccount = HandleError(await PlayFabClientAPI.LoginWithCustomIDAsync(new LoginWithCustomIDRequest()
			{
				CustomId = newTestAccount,
				InfoRequestParameters = StandardLoginInfoRequestParams,
				CreateAccount = true
			}));
			customId = newTestAccount;
			var changeName = HandleError(await PlayFabClientAPI.UpdateUserTitleDisplayNameAsync(
				new UpdateUserTitleDisplayNameRequest()
				{
					DisplayName = "Clone" + DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH'-'mm'-'ss")
				}));
			newPlayFabId = newAccount.PlayFabId;
		}

		var d = DateTime.Now;


		await SetUserState(newPlayFabId, state);

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
				PlayFabId = targetAccount,
				ItemIds = inventoryResult.Inventory.Select(a => a.ItemId).ToList()
			}));
		}

		foreach (var kv in inventoryResult.VirtualCurrency)
		{
			if (kv.Value == 0) continue;
			HandleError(await PlayFabServerAPI.AddUserVirtualCurrencyAsync(new AddUserVirtualCurrencyRequest()
			{
				PlayFabId = targetAccount,
				VirtualCurrency = kv.Key,
				Amount = kv.Value
			}));
		}

		Console.WriteLine("Copied account to id " + targetAccount + " with customId " + customId);
		Console.WriteLine(
			$"Link: https://developer.playfab.com/en-us/r/t/{PlayFabSettings.staticSettings.TitleId}/players/{newPlayFabId}/data");
	}
}
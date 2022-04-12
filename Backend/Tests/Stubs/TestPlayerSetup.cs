using System.Linq;
using Backend.Game;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.ServerModels;

namespace Tests.Stubs;

public static class TestPlayerSetup
{

	private static string LoginAndGetId(PlayfabServerSettings? server)
	{
		var res = PlayFabClientAPI.LoginWithEmailAddressAsync(new LoginWithEmailAddressRequest()
		{
			Email = "test@test.com",
			Password = "test123",
			TitleId = server.TitleId
		});
		res.Wait();
		var loginResult = res.Result;
		ResetPlayerData(loginResult.Result.PlayFabId, server);
		return loginResult.Result.PlayFabId;
	}

	public static void ResetPlayerData(string playfabId, PlayfabServerSettings? server)
	{
		var res = PlayFabServerAPI.GetUserReadOnlyDataAsync(new PlayFab.ServerModels.GetUserDataRequest()
		{
			PlayFabId = playfabId
		});
		PlayFabServerAPI.UpdateUserDataAsync(new PlayFab.ServerModels.UpdateUserDataRequest()
		{
			PlayFabId = playfabId,
			KeysToRemove = res.Result.Result.Data.Keys.ToList()
		});
	}
	
	public static string GetTestPlayerId(PlayfabServerSettings? server)
	{
		var response = PlayFabClientAPI.RegisterPlayFabUserAsync(new RegisterPlayFabUserRequest()
		{
			TitleId = server.TitleId,
			PlayerSecret = server.SecretKey,
			Username = "test",
			Password = "test123",
			Email = "test@test.com",
			DisplayName = "test"
		});
		response.Wait();
		var result = response.Result;
		if (result.Error != null && result.Error.ErrorMessage.Contains("The display name entered is not available"))
		{
			return LoginAndGetId(server);
		}
		return result.Result.PlayFabId;
	}
}
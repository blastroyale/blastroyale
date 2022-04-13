using System;
using System.Collections.Generic;
using System.Linq;
using Backend.Game;
using Backend.Game.Services;
using Microsoft.Extensions.DependencyInjection;
using PlayFab;
using PlayFab.ClientModels;

namespace Tests.Stubs;

public interface ITestPlayerSetup
{
	string GetTestPlayerId();
}

public class TestPlayerSetup: ITestPlayerSetup
{
	private IServiceProvider? _services;
	private PlayfabServerSettings? _server;
	
	public TestPlayerSetup(IServiceProvider services)
	{
		_services = services;
		_server = _services.GetService<IPlayfabServer>() as PlayfabServerSettings;
	}
	
	private string LoginAndGetId()
	{
		var res = PlayFabClientAPI.LoginWithEmailAddressAsync(new LoginWithEmailAddressRequest()
		{
			Email = "test@test.com",
			Password = "test123",
			TitleId = _server?.TitleId
		});
		res.Wait();
		var loginResult = res.Result;
		ResetPlayerData(loginResult.Result.PlayFabId);
		return loginResult.Result.PlayFabId;
	}

	private  void ResetPlayerData(string playfabId)
	{
		var res = PlayFabServerAPI.GetUserReadOnlyDataAsync(new PlayFab.ServerModels.GetUserDataRequest()
		{
			PlayFabId = playfabId
		});
		var removeTask = PlayFabServerAPI.UpdateUserDataAsync(new PlayFab.ServerModels.UpdateUserDataRequest()
		{
			PlayFabId = playfabId,
			KeysToRemove = res.Result.Result.Data.Keys.ToList(),
			Data = new Dictionary<string, string>()
		});
		removeTask.Wait();
		var result = removeTask.Result;
		var asd = 123;
	}
	
	public string GetTestPlayerId()
	{
		var response = PlayFabClientAPI.RegisterPlayFabUserAsync(new RegisterPlayFabUserRequest()
		{
			TitleId = _server.TitleId,
			PlayerSecret = _server.SecretKey,
			Username = "test",
			Password = "test123",
			Email = "test@test.com",
			DisplayName = "test"
		});
		response.Wait();
		var result = response.Result;
		string playerId = null!;
		if (result.Error != null && result.Error.ErrorMessage.Contains("The display name entered is not available"))
		{
			playerId = LoginAndGetId();
		}
		else
		{
			playerId = result.Result.PlayFabId;
		}

		var initialState = _services.GetService<IPlayerSetupService>().GetInitialState(playerId);
		_services.GetService<IServerStateService>().UpdatePlayerState(playerId, initialState);
		return playerId;
	}
}


public class InMemoryTestSetup : ITestPlayerSetup
{
	private IServiceProvider _services;

	public InMemoryTestSetup(IServiceProvider services)
	{
		_services = services;
	}
	public string GetTestPlayerId()
	{
		var id = Guid.NewGuid().ToString();
		var initialState = _services.GetService<IPlayerSetupService>().GetInitialState(id);
		_services.GetService<IServerStateService>().UpdatePlayerState(id, initialState);
		return id;
	}
}
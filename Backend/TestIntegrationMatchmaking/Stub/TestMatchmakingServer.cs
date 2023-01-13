using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Backend;
using Backend.Game;
using ServerCommon.Cloudscript;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PlayFab;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Services;

/// <summary>
/// Represents what is needed to run a isolated server testing environment.
/// </summary>
public class TestMatchmakingServer: TestService<Program>
{
	private PlayFabAuthenticationContext _auth;

	public TestMatchmakingServer()
	{
		SetupTestPlayer().Wait();
	}
	
	private async Task SetupTestPlayer()
	{
		var result = await PlayFabClientAPI.LoginWithCustomIDAsync(new()
		{
			CreateAccount = true, CustomId = "smoketest"
		});
		_auth = result.Result.AuthenticationContext;
	}
	
	public CloudscriptRequest BuildUserRequest()
	{
		var request = new CloudscriptRequest()
		{
			CallerEntityProfile = new PlayfabEntityProfile()
			{
				Entity = new PlayfabEntity()
				{
					Id = _auth.EntityId,
				},
				Lineage = new PlayfabLineage()
				{
					MasterPlayerAccountId = _auth.PlayFabId,
					TitlePlayerAccountId = _auth.EntityId
				}
			},
			FunctionArgument = new LogicRequest()
			{
				Data = new Dictionary<string, string>() 
			}
		};
		ModelSerializer.SerializeToData(request.FunctionArgument.Data, _auth);
		return request;
	}
	
}

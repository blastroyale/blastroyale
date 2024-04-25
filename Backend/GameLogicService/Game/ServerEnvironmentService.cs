using System;
using System.Collections.Generic;
using FirstLight.Game.Services;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Services;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using EnvironmentList = FirstLight.Game.Services.Environment;

namespace GameLogicService.Game
{
	public class ServerEnvironmentService : IEnvironmentService
	{
		public ServerEnvironmentService(IBaseServiceConfiguration configuration)
		{
			// TODO: Change testnet-prod to community
			var mapping = new Dictionary<string, EnvironmentList>() { { "dev", EnvironmentList.DEV }, { "staging", EnvironmentList.STAGING }, { "testnet-prod", EnvironmentList.COMMUNITY }, { "mainnet-prod", EnvironmentList.PROD } };

			if (!mapping.TryGetValue(configuration.ApplicationEnvironment, out var environment))
			{
				throw new Exception("Not found environment " + configuration.ApplicationEnvironment);
			}

			Environment = environment;
		}


		public EnvironmentList? Environment { get; }
	}
}


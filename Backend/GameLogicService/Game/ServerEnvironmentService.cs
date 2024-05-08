using System;
using System.Collections.Generic;
using FirstLight.Server.SDK.Services;

namespace GameLogicService.Game
{
	public class ServerEnvironmentService
	{
		public ServerEnvironmentService(IBaseServiceConfiguration configuration)
		{
			var mapping = new Dictionary<string, string>()
			{
				{"dev", "development"},
				{"staging", "staging"},
				{"testnet-prod", "community"}, 
				{"mainnet-prod", "production"}
			};
			
			if (!mapping.TryGetValue(configuration.ApplicationEnvironment, out var environment))
			{
				throw new Exception("Not found environment " + configuration.ApplicationEnvironment);
			}
			
			Environment = environment;
		}
		
		
		public string Environment { get; }
	}
}
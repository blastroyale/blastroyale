using System;
using FirstLight.Server.SDK.Services;
using Tests.Stubs;

namespace IntegrationTests.Setups
{
	public class IntegrationSetup
	{
		public static IServerConfiguration GetIntegrationConfiguration()
		{
			return new StubConfiguration()
			{
				DbConnectionString = "Server=localhost;Database=localDatabase;Port=5432;User Id=postgres;Password=localPassword;Ssl Mode=Allow;",
				PlayfabTitle = "***REMOVED***",
				PlayfabSecretKey = "***REMOVED***",
				DevelopmentMode = false,
				NftSync = false,
				MinClientVersion = new Version("0.1.0"),
				RemoteGameConfiguration = true,
			};
		}
	}
}


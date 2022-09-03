using System;
using FirstLight.Server.SDK.Services;

namespace Backend.Game.Services
{
	public class EnvironmentVariablesConfigurationService : IServerConfiguration
	{
		public string PlayfabSecretKey => FromEnv("PLAYFAB_DEV_SECRET_KEY");
		public string PlayfabTitle => FromEnv("PLAYFAB_TITLE");
		public bool NftSync => FromEnv("NFT_SYNC") != "false";
		public string DbConnectionString => FromEnv("SqlConnectionString");
		public string? TelemetryConnectionString => FromEnv("APPLICATIONINSIGHTS_CONNECTION_STRING");
		public Version MinClientVersion => new Version("0.4.0");
		public bool DevelopmentMode => FromEnv("DEV_MODE") == "true";
		private static string FromEnv(string name, string defaultValue = null)
		{
			return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process) ??
			       defaultValue;
		}
	}
}


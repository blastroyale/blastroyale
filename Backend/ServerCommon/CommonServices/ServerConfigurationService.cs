using System;
using FirstLight.Server.SDK.Services;

namespace Backend.Game.Services
{
	public class EnvironmentVariablesConfigurationService : IBaseServiceConfiguration
	{
		private string _path;

		public EnvironmentVariablesConfigurationService(string appPath)
		{
			_path = appPath;
		}

		public string AppPath => _path;
		public string PlayfabSecretKey => FromEnv("PLAYFAB_DEV_SECRET_KEY");
		public string PlayfabTitle => FromEnv("PLAYFAB_TITLE");
		public bool NftSync => FromEnv("NFT_SYNC", "true")?.ToLower() != "false";
		public string DbConnectionString => FromEnv("CONNECTION_STRING", FromEnv("SqlConnectionString", ""));
		public string? TelemetryConnectionString => FromEnv("APPLICATIONINSIGHTS_CONNECTION_STRING", "");
		public Version MinClientVersion => new Version(FromEnv("MIN_CLIENT_VERSION", "0.10.0"));
		public bool DevelopmentMode => FromEnv("DEV_MODE", "false")?.ToLower() == "true";
		public bool RemoteGameConfiguration => FromEnv("REMOTE_CONFIGURATION", "false")?.ToLower() == "true";
		private static string FromEnv(string name, string? defaultValue = null)
		{
			var envValue = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
			if (envValue == null && defaultValue == null)
			{
				throw new Exception($"Missing environment variable: {name}");
			}
			return envValue ?? defaultValue;
		}
	}
}


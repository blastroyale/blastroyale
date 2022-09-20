using System.Threading.Tasks;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Server.SDK.Services;
using Microsoft.Extensions.Logging;

namespace Backend.Game.Services
{
	/// <summary>
	/// Container for game configurations.
	/// Should handle any internal configuration management like updates or necessary mangling.
	/// </summary>
	public interface IGameConfigurationService
	{
		/// <summary>
		/// Should always return the most up-to-date game configuration.
		/// </summary>
		Task<IConfigsProvider> GetGameConfigs();
	}

	/// <summary>
	/// Standard server container for configurations. Does the bridge between 
	/// IConfigBackendService who owns the repository of configs and the actual configuration
	/// in memory on server.
	/// </summary>
	public class GameConfigurationService : IGameConfigurationService
	{
		private IConfigsAdder _configs;
		private IConfigBackendService _configBackend;
		private IServerConfiguration _serverConfig;
		private ILogger _log;

		public GameConfigurationService(ILogger log, IConfigsProvider configs, IConfigBackendService configBackend, IServerConfiguration serverConfig)
		{
			_configs = configs as IConfigsAdder;
			_log = log;
			_configBackend = configBackend;
			_serverConfig = serverConfig;
		}

		public async Task<IConfigsProvider> GetGameConfigs()
		{
			if (_serverConfig.RemoteGameConfiguration)
			{
				await CheckForUpdates();
			}
			return _configs;
		}

		private async Task<bool> CheckForUpdates()
		{
			var remoteVersion = await _configBackend.GetRemoteVersion();
			if (remoteVersion > _configs.Version)
			{
				_log.LogInformation($"Updating configuration from V {_configs.Version} to {remoteVersion}");
				var remoteConfigs = await _configBackend.FetchRemoteConfiguration(remoteVersion);
				_configs.UpdateTo(remoteVersion, remoteConfigs.GetAllConfigs());
				return true;
			}
			return false;
		}
	}
}


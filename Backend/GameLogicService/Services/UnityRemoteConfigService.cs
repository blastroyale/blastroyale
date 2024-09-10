using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLight.Server.SDK.Services;
using FirstLightServerSDK.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.String;

namespace GameLogicService.Services;


/// <summary>
/// Responsible for creating a remote config in the server context
/// </summary>
public interface IRemoteConfigService
{
	Task<IRemoteConfigProvider> FetchConfig(int clientVersion);
}

public class UnityRemoteConfig
{
	public UnityConfigValue[] value;
}

public class UnityConfigValue
{
	public string key;
	public string type;
	public object value;
}

public class UnityRemoteConfigResponse
{
	public UnityRemoteConfig[] configs;
	public int ConfigVersion;

	public T GetConfig<T>(string key) where T : class
	{
		return configs.First().value.Where(v => v.key == key && v.type == "json")
			.Select(a => ((JObject) a.value).ToObject<T>()).FirstOrDefault();
	}
}

public class UnityRemoteConfigService : IRemoteConfigService
{
	private const string GET_CONFIG_URL = "https://services.api.unity.com/remote-config/v1/projects/{0}/environments/{1}/configs";
	private UnityAuthService _unityAuthService;
	private IBaseServiceConfiguration _serviceConfiguration;
	private ILogger _logger;


	private UnityRemoteConfigResponse _cachedUnityRemoteConfig;
	private SemaphoreSlim _configSemaphore = new(1);


	public UnityRemoteConfigService(UnityAuthService unityAuthService, IBaseServiceConfiguration serviceConfiguration, ILogger logger)
	{
		_unityAuthService = unityAuthService;
		_serviceConfiguration = serviceConfiguration;
		_logger = logger;
	}


	private async Task<UnityRemoteConfigResponse> GetConfigs()
	{
		var rawJson = await _unityAuthService.GetServerAuthenticatedClient()
			.GetStringAsync(Format(GET_CONFIG_URL, UnityAuthService.PROJECT_ID, _serviceConfiguration.UnityCloudEnvironmentID));
		var cfg = ModelSerializer.Deserialize<UnityRemoteConfigResponse>(rawJson);
		cfg.ConfigVersion = cfg.configs.SelectMany(a => a.value).Where(a => a.key == CommandFields.ServerConfigurationVersion).Select(a => (int) (long) a.value).FirstOrDefault();
		return cfg;
	}


	public async Task<IRemoteConfigProvider> FetchConfig(int clientVersion)
	{
		await _configSemaphore.WaitAsync();
		try
		{
			if (_cachedUnityRemoteConfig == null || clientVersion > _cachedUnityRemoteConfig.ConfigVersion)
			{
				if (_cachedUnityRemoteConfig == null)
				{
					_logger.LogInformation("Downloading remote configs for the first time!");
				}
				else
				{
					_logger.LogInformation($"Client has newer version {clientVersion} of the config, fetching again!");
				}

				_cachedUnityRemoteConfig = await GetConfigs();
				_logger.LogInformation($"Downloaded config version {_cachedUnityRemoteConfig.ConfigVersion} from unity!");
			}

			return new UnityServerRemoteConfigProvider(_cachedUnityRemoteConfig);
		}
		finally
		{
			_configSemaphore.Release();
		}
	}
}
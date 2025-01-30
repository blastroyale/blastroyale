using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLightServerSDK.Services;
using GameLogicService.Services;


class InMemoryRemoteConfigProvider : IRemoteConfigProvider
{
	private InMemoryRemoteConfigService _service;

	public InMemoryRemoteConfigProvider(InMemoryRemoteConfigService service)
	{
		_service = service;
	}

	public T GetConfig<T>() where T : class
	{
		return (T) _service._configs[typeof(T)];
	}

	public bool ValidateConfig(Type type)
	{
		return true;
	}

	public int GetConfigVersion()
	{
		return _service._version;
	}
}

public class InMemoryRemoteConfigService : IRemoteConfigService
{
	internal Dictionary<Type, object> _configs = new();
	internal int _version;

	public void SetConfig<T>(T value)
	{
		_configs[typeof(T)] = value;
	}

	public void SetVersion(int version)
	{
		_version = version;
	}

	public async Task<IRemoteConfigProvider> FetchConfig(int clientVersion)
	{
		return new InMemoryRemoteConfigProvider(this);
	}
}
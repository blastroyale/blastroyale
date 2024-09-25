using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FirstLight.Game.Logic.RPC;
using FirstLight.Server.SDK.Modules;
using FirstLightServerSDK.Services;

namespace GameLogicService.Services;

public class UnityServerRemoteConfigProvider : IRemoteConfigProvider
{
	private Dictionary<Type, object> _configs = new Dictionary<Type, object>();
	private UnityRemoteConfigResponse _unityRemoteConfigResponse;


	public UnityServerRemoteConfigProvider(UnityRemoteConfigResponse response)
	{
		_unityRemoteConfigResponse = response;
	}


	public T GetConfig<T>() where T : class
	{
		if (_configs.TryGetValue(typeof(T), out var cachedValue))
		{
			return cachedValue as T;
		}

		var value = _unityRemoteConfigResponse.GetConfig<T>(typeof(T).Name);
		if (value == null)
		{
			throw new Exception("No server config found!");
		}

		_configs[typeof(T)] = value;
		return value;
	}

	public int GetConfigVersion()
	{
		return _unityRemoteConfigResponse.ConfigVersion;
	}
}
using System;
using System.Collections.Generic;
using Circuit;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLightServerSDK.Services;
using Unity.Services.RemoteConfig;

namespace FirstLight.Game.Services
{
	public class UnityRemoteConfigProvider : IRemoteConfigProvider
	{
		private static Dictionary<Type, object> _cachedConfigs = new ();

		public T GetConfig<T>() where T : class
		{
			if (_cachedConfigs.TryGetValue(typeof(T), out var config))
			{
				return config as T;
			}

			var rawJson = RemoteConfigs.RuntimeConfig.GetJson(typeof(T).Name);
			var deserialized = ModelSerializer.Deserialize<T>(rawJson);
			_cachedConfigs[typeof(T)] = deserialized;
			return deserialized;
		}

		public int GetConfigVersion()
		{
			return RemoteConfigs.RuntimeConfig.GetInt(CommandFields.ServerConfigurationVersion);
		}
	}
}
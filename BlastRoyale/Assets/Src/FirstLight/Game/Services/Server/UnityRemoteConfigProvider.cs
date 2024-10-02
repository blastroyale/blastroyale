using System;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLightServerSDK.Services;

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

		public bool ValidateConfig(Type type)
		{
			try
			{
				var rawJson = RemoteConfigs.RuntimeConfig.GetJson(type.Name);
				ModelSerializer.Deserialize(type, rawJson);
			}
			catch (Exception ex)
			{
				FLog.Error("Failed to parse config " + type.Name, ex);
				return false;
			}

			return true;
		}

		public int GetConfigVersion()
		{
			return RemoteConfigs.RuntimeConfig.GetInt(CommandFields.ServerConfigurationVersion);
		}
	}
}
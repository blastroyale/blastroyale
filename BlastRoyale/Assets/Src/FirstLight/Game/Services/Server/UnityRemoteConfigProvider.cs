using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLightServerSDK.Services;
using Newtonsoft.Json;
using Unity.Services.Authentication;
using Unity.Services.RemoteConfig;
using UnityEngine.PlayerLoop;

namespace FirstLight.Game.Services
{
	public interface IGameRemoteConfigProvider : IRemoteConfigProvider
	{
		public UniTask Init();

		///<summary>Only updates the config from remote if the remote version number increased, otherwise skips to save bandwidth</summary>
		/// <param name="lifespan">The timespan is reasonable for the config to be cached</param>
		/// <param name="types">The config types it will be fetched, if null or empty, then it is all of them</param>
		public UniTask<bool> UpdateConfigWhenExpired(TimeSpan lifespan, params Type[] types);
	}

	public class UnityRemoteConfigProvider : IGameRemoteConfigProvider
	{
		/// <summary>
		/// This variable is incrementally updated during the game session, unity remote config stores it in a cache
		/// When reassigning it every update it is actually getting the same ref from the cache, assignment is there just for safety
		/// </summary>
		private RuntimeConfig _runtimeConfig;

		private Dictionary<Type, object> _cachedConfigs = new ();
		private DateTime _lastUpdatedTime;

		public T GetConfig<T>() where T : class
		{
			if (_cachedConfigs.TryGetValue(typeof(T), out var config))
			{
				return config as T;
			}

			var rawJson = _runtimeConfig.GetJson(typeof(T).Name);
			var deserialized = ModelSerializer.Deserialize<T>(rawJson);
			_cachedConfigs[typeof(T)] = deserialized;
			return deserialized;
		}

		public bool ValidateConfig(Type type)
		{
			try
			{
				var rawJson = _runtimeConfig.GetJson(type.Name);
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
			return _runtimeConfig.GetInt(CommandFields.ServerConfigurationVersion);
		}

		public async UniTask Init()
		{
			RemoteConfigService.Instance.SetCustomUserID(AuthenticationService.Instance.PlayerId);

			// Fetch regular configs
			await UpdateConfig();
		}

		public async UniTask UpdateRemoteVersion()
		{
			var filter = new FilterAttributes();
			filter.key = new[] {CommandFields.ServerConfigurationVersion};
			_runtimeConfig = await RemoteConfigService.Instance.FetchConfigsAsync(new UserAttributes(), new AppAttributes(), filter);
			_lastUpdatedTime = DateTime.Now;
		}

		public async UniTask UpdateConfig(params Type[] types)
		{
			var filter = new FilterAttributes();
			if (types.Length != 0)
			{
				filter.key = types.Select(type => type.Name)
					.Append(CommandFields.ServerConfigurationVersion)
					.ToArray();
				foreach (var type in types)
				{
					_cachedConfigs.Remove(type);
				}
			}
			else
			{
				_cachedConfigs.Clear();
			}

			// Fetch regular configs
			_runtimeConfig = await RemoteConfigService.Instance.FetchConfigsAsync(new UserAttributes(), new AppAttributes(), filter);
			_lastUpdatedTime = DateTime.Now;
		}

		public async UniTask<bool> UpdateConfigWhenExpired(TimeSpan ttl, params Type[] types)
		{
			if (_lastUpdatedTime != default && _lastUpdatedTime + ttl > DateTime.Now)
			{
				return false;
			}

			var currentVersion = GetConfigVersion();
			await UpdateRemoteVersion();
			var newVersion = GetConfigVersion();
			// If there is no increment in version number no need to upgrade it
			if (currentVersion == newVersion)
			{
				FLog.Info("Skipping config update, same version!");
				return false;
			}

			FLog.Info("Updating config types: " + string.Join(",", types.Select(t => t.Name)));
			await UpdateConfig(types);
			return true;
		}

		/// <summary>
		/// Additional user attributes for JEXL expressions.
		/// </summary>
		private struct UserAttributes
		{
		}

		/// <summary>
		/// Additional user attributes for JEXL expressions.
		/// </summary>
		private struct AppAttributes
		{
		}

		private struct FilterAttributes
		{
			public string[] key;
		}
	}
}
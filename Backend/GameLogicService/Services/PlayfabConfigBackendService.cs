using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using PlayFab;
using PlayFab.AdminModels;

namespace Backend.Game.Services
{
	// TODO: move to client. Need to sync playfab lib versions.
	public class PlayfabConfigurationBackendService : IConfigBackendService 
	{
		/// <summary>
		/// Sets internal title data key value pair.
		/// </summary>
		private static async Task SetTitleData(string key, string data)
		{
			var result = await PlayFabAdminAPI.SetTitleDataAsync(new SetTitleDataRequest()
			{
				Key = key,
				Value = data,
			});
			if (result.Error != null)
			{
				throw new Exception(result.Error.ErrorMessage);
			}
		}

		/// <summary>
		/// Gets an specific internal title key data
		/// </summary>
		private static async Task<string> GetTitleData(string key)
		{
			var result = await PlayFabAdminAPI.GetTitleDataAsync(new GetTitleDataRequest()
			{
				Keys = new List<string>() { key }
			});
			if (result.Error != null)
			{
				throw new Exception(result.Error.ErrorMessage);
			}
			if (!result.Result.Data.TryGetValue(key, out var data))
			{
				throw new Exception($"Key {key} did not exist on title data");
			}
			return data;
		}
		
		public async Task<ulong> GetRemoteVersion()
		{
			return ulong.Parse(await GetTitleData(PlayfabConfigurationProvider.ConfigVersion));
		}

		public async Task<IConfigsProvider> FetchRemoteConfiguration(ulong version)
		{
			var gameConfig = await GetTitleData(PlayfabConfigurationProvider.ConfigName);
			var serializer = new ConfigsSerializer();
			var config = serializer.Deserialize<ServerConfigsProvider>(gameConfig);
			config.SetVersion(version);
			return config;
		}
	}
}
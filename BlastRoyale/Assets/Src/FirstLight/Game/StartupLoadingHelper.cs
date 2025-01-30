using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Analytics;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using UnityEngine;

namespace FirstLight.Game
{
	/// <summary>
	/// TODO: REFAC THIS CLASS
	/// </summary>
	public static class StartupLoadingHelper
	{
		private static IGameServices _services;
		private static IAssetAdderService _assetService;
		private static IConfigsAdder _configsAdder;
		private static IConfigsLoader _configsLoader;

		public static async UniTask LoadConfigs(IGameServices services, IAssetAdderService assetService, IConfigsAdder configsAdder)
		{
			_services = services;
			_assetService = assetService;
			_configsAdder = configsAdder;
			_configsLoader = new GameConfigsLoader(_assetService, _configsAdder);

			var tasks = new List<UniTask>();
			tasks.AddRange(_configsLoader.LoadConfigTasks());
			tasks.AddRange(_configsLoader.LoadAssetConfigTasks(_assetService));
			await UniTask.WhenAll(tasks);

			var audioTasks = new List<UniTask>
			{
				_services.AudioFxService.LoadAudioMixers(_services.ConfigsProvider.GetConfig<AudioMixerConfigs>().ConfigsDictionary),
				_services.AudioFxService.LoadAudioClips(_services.ConfigsProvider.GetConfig<AudioSharedAssetConfigs>().ConfigsDictionary)
			};

			await UniTask.WhenAll(audioTasks);
		}
		
	}
}
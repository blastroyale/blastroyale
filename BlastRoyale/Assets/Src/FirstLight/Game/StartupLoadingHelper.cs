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
			_configsLoader = new GameConfigsLoader(_assetService);

			var tasks = new List<UniTask>();
			tasks.AddRange(_configsLoader.LoadConfigTasks(_configsAdder));
			tasks.AddRange(LoadAssetConfigs());
			await UniTask.WhenAll(tasks);

			var audioTasks = new List<UniTask>
			{
				_services.AudioFxService.LoadAudioMixers(_services.ConfigsProvider.GetConfig<AudioMixerConfigs>().ConfigsDictionary),
				_services.AudioFxService.LoadAudioClips(_services.ConfigsProvider.GetConfig<AudioSharedAssetConfigs>().ConfigsDictionary)
			};

			await UniTask.WhenAll(audioTasks);

			LoadVfx(); // This is not awaited
		}

		private static IEnumerable<UniTask> LoadAssetConfigs()
		{
			return new List<UniTask>
			{
				_configsLoader.LoadConfig<AudioMixerConfigs>(AddressableId.Configs_AudioMixerConfigs,
					asset => _configsAdder.AddSingletonConfig(asset)),
				_configsLoader.LoadConfig<AudioMatchAssetConfigs>(AddressableId.Configs_AudioMatchAssetConfigs,
					asset => _configsAdder.AddSingletonConfig(asset)),
				_configsLoader.LoadConfig<AudioMainMenuAssetConfigs>(AddressableId.Configs_AudioMainMenuAssetConfigs,
					asset => _configsAdder.AddSingletonConfig(asset)),
				_configsLoader.LoadConfig<AudioSharedAssetConfigs>(AddressableId.Configs_AudioSharedAssetConfigs,
					asset => _configsAdder.AddSingletonConfig(asset)),
				_configsLoader.LoadConfig<MatchAssetConfigs>(AddressableId.Configs_AdventureAssetConfigs,
					asset => _configsAdder.AddSingletonConfig(asset)),
				_configsLoader.LoadConfig<MainMenuAssetConfigs>(AddressableId.Configs_MainMenuAssetConfigs,
					asset => _configsAdder.AddSingletonConfig(asset)),
				_configsLoader.LoadConfig<DummyAssetConfigs>(AddressableId.Configs_DummyAssetConfigs,
					asset => _configsAdder.AddSingletonConfig(asset)),
				_configsLoader.LoadConfig<SpriteAssetConfigs>(AddressableId.Configs_SpriteAssetConfigs, asset => _assetService.AddConfigs(asset)),
				_configsLoader.LoadConfig<SpecialMoveAssetConfigs>(AddressableId.Configs_SpecialMoveAssetConfigs,
					asset => _assetService.AddConfigs(asset)),
				_configsLoader.LoadConfig<VfxAssetConfigs>(AddressableId.Configs_VfxConfigs, asset => _assetService.AddConfigs(asset)),
				_configsLoader.LoadConfig<MaterialVfxConfigs>(AddressableId.Configs_MaterialVfxConfigs, asset => _assetService.AddConfigs(asset)),
				_configsLoader.LoadConfig<IndicatorVfxAssetConfigs>(AddressableId.Configs_IndicatorVfxAssetConfigs,
					asset => _assetService.AddConfigs(asset)),
				_configsLoader.LoadConfig<EquipmentRarityAssetConfigs>(AddressableId.Configs_EquipmentRarityAssetConfigs,
					asset => _assetService.AddConfigs(asset)),
				_configsLoader.LoadConfig<VideoAssetConfigs>(AddressableId.Configs_VideoAssetConfigs, asset => _assetService.AddConfigs(asset)),
			};
		}

		private static void LoadVfx()
		{
			foreach (var vfx in Enum.GetValues(typeof(VfxId)).Cast<VfxId>())
			{
				_assetService.RequestAsset<VfxId, GameObject>(vfx, true, false, VfxLoaded).AsUniTask().Forget();
			}

			void VfxLoaded(VfxId id, GameObject vfxAsset, bool instantiate)
			{
				var reference = vfxAsset.GetComponent<Vfx<VfxId>>();
				if (reference == null) return;
				_services.VfxService.AddPool(reference);
			}
		}
	}
}
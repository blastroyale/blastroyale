using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Services;
using FirstLight.Statechart;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for Initial Loading of the game in the <seealso cref="GameStateMachine"/>
	/// </summary>
	internal class InitialLoadingState
	{
		private readonly IGameServices _services;
		private readonly IGameUiServiceInit _uiService;
		private readonly IAssetAdderService _assetService;
		private readonly IConfigsAdder _configsAdder;
		private readonly IVfxInternalService<VfxId> _vfxService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly IConfigsLoader _configsLoader;
		
		public InitialLoadingState(IGameServices services, IGameUiServiceInit uiService, 
		                           IAssetAdderService assetService, IConfigsAdder configsAdder, 
		                           IVfxInternalService<VfxId> vfxService, Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_uiService = uiService;
			_assetService = assetService;
			_configsAdder = configsAdder;
			_vfxService = vfxService;
			_statechartTrigger = statechartTrigger;
			_configsLoader = new GameConfigsLoader(_assetService);
		}

		/// <summary>
		/// Setups the Initial Loading state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var downloadData = stateFactory.TaskWait("Download game content");
			var assetLoading = stateFactory.TaskWait("Asset loading");

			initial.Transition().Target(downloadData);
			initial.OnExit(GameLoadStartAnalyticsEvent);
			initial.OnExit(SubscribeEvents);
			
			downloadData.WaitingFor(DownloadData).Target(assetLoading);

			assetLoading.WaitingFor(LoadInitialAssets).Target(final);
			
			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			// Subscribe to events
		}

		private void GameLoadStartAnalyticsEvent()
		{
			_services?.AnalyticsService.SessionCalls.GameLoadStart();
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		private async Task DownloadData()
		{
			//await Addressables.DownloadDependenciesAsync(AddressableLabel.Label_Quantum.ToLabelString(), true).Task;
			await Addressables.InitializeAsync().Task;
			
			Resources.UnloadUnusedAssets();
		}

		private async Task LoadInitialAssets()
		{
			var configProvider = _services.ConfigsProvider;
			
			var tasks = new List<Task>();

			tasks.Add(LoadErrorAssets());
			tasks.AddRange(_configsLoader.LoadConfigTasks(_configsAdder));
			tasks.AddRange(LoadAssetConfigs());
			
			await Task.WhenAll(tasks);

			await _services.AudioFxService
			               .LoadAudioClips(configProvider.GetConfig<AudioSharedAssetConfigs>().ConfigsDictionary);
			
			LoadVfx();
		}

		private async Task LoadErrorAssets()
		{
			await _configsLoader.LoadConfig<CustomAssetConfigs>(AddressableId.Configs_CustomAssetConfigs, asset => _configsAdder.AddSingletonConfig(asset));
			
			var customConfigs = _configsAdder.GetConfig<CustomAssetConfigs>();
			var errorSprite = customConfigs.ErrorSprite.LoadAssetAsync();
			var errorCube = customConfigs.ErrorCube.LoadAssetAsync();
			var errorMaterial = customConfigs.ErrorMaterial.LoadAssetAsync();
			var errorClip = customConfigs.ErrorClip.LoadAssetAsync();

			await Task.WhenAll(errorSprite.Task, errorCube.Task, errorMaterial.Task, errorClip.Task);
			
			_assetService.AddDebugConfigs(errorSprite.Result, errorCube.Result, errorMaterial.Result, errorClip.Result);
			_services.RemoteTextureService.SetErrorTexture(errorSprite.Result.texture);
		}

		private IEnumerable<Task> LoadAssetConfigs()
		{
			return new List<Task>
			{
				_configsLoader.LoadConfig<AudioMixerConfigs>(AddressableId.Configs_AudioMixerConfigs, asset => _configsAdder.AddSingletonConfig(asset)),
				_configsLoader.LoadConfig<AudioMatchAssetConfigs>(AddressableId.Configs_AudioMatchAssetConfigs, asset => _configsAdder.AddSingletonConfig(asset)),
				_configsLoader.LoadConfig<AudioMainMenuAssetConfigs>(AddressableId.Configs_AudioMainMenuAssetConfigs, asset => _configsAdder.AddSingletonConfig(asset)),
				_configsLoader.LoadConfig<AudioSharedAssetConfigs>(AddressableId.Configs_AudioSharedAssetConfigs, asset => _configsAdder.AddSingletonConfig(asset)),
				_configsLoader.LoadConfig<AdventureAssetConfigs>(AddressableId.Configs_AdventureAssetConfigs, asset => _configsAdder.AddSingletonConfig(asset)),
				_configsLoader.LoadConfig<EquipmentRarityAssetConfigs>(AddressableId.Configs_EquipmentRarityAssetConfigs, asset => _configsAdder.AddSingletonConfig(asset)),
				_configsLoader.LoadConfig<MainMenuAssetConfigs>(AddressableId.Configs_MainMenuAssetConfigs, asset => _configsAdder.AddSingletonConfig(asset)),
				_configsLoader.LoadConfig<DummyAssetConfigs>(AddressableId.Configs_DummyAssetConfigs, asset => _configsAdder.AddSingletonConfig(asset)),
				_configsLoader.LoadConfig<SceneAssetConfigs>(AddressableId.Configs_SceneAssetConfigs, asset => _assetService.AddConfigs(asset)),
				_configsLoader.LoadConfig<SpriteAssetConfigs>(AddressableId.Configs_SpriteAssetConfigs, asset => _assetService.AddConfigs(asset)),
				_configsLoader.LoadConfig<SpecialMoveAssetConfigs>(AddressableId.Configs_SpecialMoveAssetConfigs, asset => _assetService.AddConfigs(asset)),
				_configsLoader.LoadConfig<VfxAssetConfigs>(AddressableId.Configs_VfxConfigs, asset => _assetService.AddConfigs(asset)),
				_configsLoader.LoadConfig<MaterialVfxConfigs>(AddressableId.Configs_MaterialVfxConfigs, asset => _assetService.AddConfigs(asset)),
				_configsLoader.LoadConfig<IndicatorVfxAssetConfigs>(AddressableId.Configs_IndicatorVfxAssetConfigs, asset => _assetService.AddConfigs(asset)),
				_configsLoader.LoadConfig<VideoAssetConfigs>(AddressableId.Configs_VideoAssetConfigs, asset => _assetService.AddConfigs(asset)),
				_configsLoader.LoadConfig<PlayerRankAssetConfigs>(AddressableId.Configs_PlayerRankAssetConfigs, asset => _assetService.AddConfigs(asset)),
			};
		}

		private void LoadVfx()
		{
			for (var i = 0; i < (int) VfxId.TOTAL; i++)
			{
				_assetService.RequestAsset<VfxId, GameObject>((VfxId) i, true, false, VfxLoaded);
			}

			void VfxLoaded(VfxId id, GameObject vfxAsset, bool instantiate)
			{
				_vfxService.AddPool(vfxAsset.GetComponent<Vfx<VfxId>>());
			}
		}

	}
}
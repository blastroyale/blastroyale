using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.AssetImporter;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.Services;
using FirstLight.Statechart;
using FirstLight.UiService;
using I2.Loc;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using SRDebugger;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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
			initial.OnExit(SubscribeEvents);
			
			downloadData.WaitingFor(DownloadData).Target(assetLoading);

			assetLoading.WaitingFor(LoadInitialAssets).Target(final);
			
			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			// Subscribe to events
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
			var tasks = new List<Task>();

			tasks.Add(LoadErrorAssets());
			tasks.AddRange(LoadConfigs());
			tasks.AddRange(LoadAssetConfigs());
			
			await Task.WhenAll(tasks);

			LoadVfx();
		}

		private async Task LoadErrorAssets()
		{
			await LoadConfig<CustomAssetConfigs>(AddressableId.Configs_CustomAssetConfigs, asset => _configsAdder.AddSingletonConfig(asset));
			
			var customConfigs = _configsAdder.GetConfig<CustomAssetConfigs>();
			var errorSprite = customConfigs.ErrorSprite.LoadAssetAsync();
			var errorCube = customConfigs.ErrorCube.LoadAssetAsync();
			var errorMaterial = customConfigs.ErrorMaterial.LoadAssetAsync();
			var errorClip = customConfigs.ErrorClip.LoadAssetAsync();

			await Task.WhenAll(errorSprite.Task, errorCube.Task, errorMaterial.Task, errorClip.Task);
			
			_assetService.AddDebugConfigs(errorSprite.Result, errorCube.Result, errorMaterial.Result, errorClip.Result);
		}

		private IEnumerable<Task> LoadAssetConfigs()
		{
			return new List<Task>
			{
				LoadConfig<AudioAdventureAssetConfigs>(AddressableId.Configs_AudioAdventureAssetConfigs, asset => _configsAdder.AddSingletonConfig(asset)),
				LoadConfig<AudioMainMenuAssetConfigs>(AddressableId.Configs_AudioMainMenuAssetConfigs, asset => _configsAdder.AddSingletonConfig(asset)),
				LoadConfig<AdventureAssetConfigs>(AddressableId.Configs_AdventureAssetConfigs, asset => _configsAdder.AddSingletonConfig(asset)),
				LoadConfig<MainMenuAssetConfigs>(AddressableId.Configs_MainMenuAssetConfigs, asset => _configsAdder.AddSingletonConfig(asset)),
				LoadConfig<DummyAssetConfigs>(AddressableId.Configs_DummyAssetConfigs, asset => _configsAdder.AddSingletonConfig(asset)),
				LoadConfig<SceneAssetConfigs>(AddressableId.Configs_SceneAssetConfigs, asset => _assetService.AddConfigs(asset)),
				LoadConfig<SpriteAssetConfigs>(AddressableId.Configs_SpriteAssetConfigs, asset => _assetService.AddConfigs(asset)),
				LoadConfig<SpecialMoveAssetConfigs>(AddressableId.Configs_SpecialMoveAssetConfigs, asset => _assetService.AddConfigs(asset)),
				LoadConfig<AudioSharedAssetConfigs>(AddressableId.Configs_AudioSharedAssetConfigs, asset => _assetService.AddConfigs(asset)),
				LoadConfig<VfxAssetConfigs>(AddressableId.Configs_VfxConfigs, asset => _assetService.AddConfigs(asset)),
				LoadConfig<MaterialVfxConfigs>(AddressableId.Configs_MaterialVfxConfigs, asset => _assetService.AddConfigs(asset)),
				LoadConfig<IndicatorVfxAssetConfigs>(AddressableId.Configs_IndicatorVfxAssetConfigs, asset => _assetService.AddConfigs(asset)),
				LoadConfig<VideoAssetConfigs>(AddressableId.Configs_VideoAssetConfigs, asset => _assetService.AddConfigs(asset)),
				LoadConfig<PlayerRankAssetConfigs>(AddressableId.Configs_PlayerRankAssetConfigs, asset => _assetService.AddConfigs(asset)),
			};
		}

		private IEnumerable<Task> LoadConfigs()
		{
			return new List<Task>
			{
				LoadConfig<GameConfigs>(AddressableId.Configs_GameConfigs, asset => _configsAdder.AddSingletonConfig(asset.Config)),
				LoadConfig<MapGridConfigs>(AddressableId.Configs_MapGridConfigs, asset => _configsAdder.AddSingletonConfig(asset)),
				LoadConfig<MapConfigs>(AddressableId.Configs_MapConfigs, asset => _configsAdder.AddConfigs(data => data.Id, asset.Configs)),
				LoadConfig<WeaponConfigs>(AddressableId.Configs_WeaponConfigs, asset => _configsAdder.AddConfigs(data => (int) data.Id, asset.Configs)),
				LoadConfig<GearConfigs>(AddressableId.Configs_GearConfigs, asset => _configsAdder.AddConfigs(data => (int) data.Id, asset.Configs)),
				LoadConfig<PlayerLevelConfigs>(AddressableId.Configs_PlayerLevelConfigs, asset => _configsAdder.AddConfigs(data => (int) data.Level, asset.Configs)),
				LoadConfig<SpecialConfigs>(AddressableId.Configs_SpecialConfigs, asset => _configsAdder.AddConfigs(data => (int) data.Id, asset.Configs)),
				LoadConfig<ConsumableConfigs>(AddressableId.Configs_ConsumableConfigs, asset => _configsAdder.AddConfigs(data => (int) data.Id, asset.Configs)),
				LoadConfig<DestructibleConfigs>(AddressableId.Configs_DestructibleConfigs, asset => _configsAdder.AddConfigs(data => (int) data.Id, asset.Configs)),
				LoadConfig<ShrinkingCircleConfigs>(AddressableId.Configs_ShrinkingCircleConfigs, asset => _configsAdder.AddConfigs(data => data.Step, asset.Configs)),
				LoadConfig<ResourcePoolConfigs>(AddressableId.Configs_ResourcePoolConfigs, asset => _configsAdder.AddConfigs(data => (int) data.Id, asset.Configs))
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
		
		private async Task LoadConfig<TContainer>(AddressableId id, Action<TContainer> onLoadComplete)
		{
			var asset = await _assetService.LoadAssetAsync<TContainer>(id.GetConfig().Address);

			onLoadComplete(asset);

			_assetService.UnloadAsset(asset);
		}
	}
}
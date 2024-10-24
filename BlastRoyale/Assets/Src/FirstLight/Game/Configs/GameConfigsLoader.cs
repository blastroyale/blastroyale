using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs.Collection;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;
using UnityEngine;
using Cysharp.Threading.Tasks;
using FirstLight.AssetImporter;
using FirstLight.Game.Configs.AssetConfigs;
using UnityEngine.Video;
using static FirstLight.Game.Ids.AddressableId;
using Assert = UnityEngine.Assertions.Assert;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Interface responsible for loading game-specific configuration files from a given asset resolver.
	/// </summary>
	public interface IConfigsLoader
	{
		/// <summary>
		/// Using the given asset resolver, loads and fills the IConfigsAdder object.
		/// </summary>
		IEnumerable<UniTask> LoadConfigTasks();

		/// <summary>
		/// Fills the IConfigs with the configs needed for assets (like VFX, map assets and etc)
		/// </summary>
		/// <param name="assetService"></param>
		public IEnumerable<UniTask> LoadAssetConfigTasks(IAssetAdderService assetService);

		/// <summary>
		/// Loads a specific config using the given asset resolver. 
		/// </summary>
		UniTask LoadConfig<TContainer>(AddressableId id, Action<TContainer> onLoadComplete) where TContainer : ScriptableObject;
	}

	public static class IConfigLoaderExtensions
	{
	}

	/// <summary>
	/// Configuration loader specific for our game.
	/// </summary>
	public class GameConfigsLoader : IConfigsLoader
	{
		private IAssetAdderService _assetLoader;
		private readonly IConfigsAdder _configsAdder;

		public GameConfigsLoader(IAssetAdderService assetLoader, IConfigsAdder configsAdder)
		{
			_assetLoader = assetLoader;
			_configsAdder = configsAdder;
		}

		// TODO: load via addressable bundle in a single go
		private IEnumerable<IConfigLoadHandler> GetLoadHandlersForConfigs()
		{
			return new List<IConfigLoadHandler>
			{
				// last type argument is the one added to the container
				// TODO: Remove this pile of shit 
				AddStringKeyValue<GameModeConfigs, QuantumGameModeConfig>(Configs_GameModeConfigs, v => v.Id),
				AddStringKeyValue<MapConfigs, QuantumMapConfig>(Configs_QuantumMapConfigs, v => v.Map.ToString()),
				AddKeyValue<LiveopsFeatureFlagConfigs, LiveopsFeatureFlagConfig>(Configs_LiveopsFeatureFlagConfigs,
					v => v.UniqueIdentifier()),
				AddKeyValue<WeaponConfigs, QuantumWeaponConfig>(Configs_WeaponConfigs, v => (int) v.Id),
				AddKeyValue<PlayerLevelConfigs, PlayerLevelConfig>(Configs_PlayerLevelConfigs, v => (int) v.LevelStart),
				AddKeyValue<SpecialConfigs, QuantumSpecialConfig>(Configs_SpecialConfigs, v => (int) v.Id),
				AddKeyValue<ConsumableConfigs, QuantumConsumableConfig>(Configs_ConsumableConfigs, v => (int) v.Id),
				AddKeyValue<DestructibleConfigs, QuantumDestructibleConfig>(Configs_DestructibleConfigs, v => (int) v.Id),
				AddKeyValue<ShrinkingCircleConfigs, QuantumShrinkingCircleConfig>(Configs_ShrinkingCircleConfigs, v => v.Key),
				AddKeyValue<ResourcePoolConfigs, ResourcePoolConfig>(Configs_ResourcePoolConfigs, v => (int) v.Id),
				AddKeyValue<AudioWeaponConfigs, AudioWeaponConfig>(Configs_AudioWeaponConfigs, v => (int) v.GameId),
				AddKeyValue<MatchRewardConfigs, MatchRewardConfig>(Configs_MatchRewardConfigs, v => v.MatchRewardId),
				AddKeyValue<TrophyRewardConfigs, TrophyRewardConfig>(Configs_TrophyRewardConfigs, v => v.MatchRewardId),
				AddKeyValue<ChestConfigs, QuantumChestConfig>(Configs_ChestConfigs, v => (int) v.Id),
				AddKeyValue<EquipmentStatConfigs, QuantumEquipmentStatConfig>(Configs_EquipmentStatConfigs, v => v.GetKey()),
				AddKeyValue<EquipmentMaterialStatConfigs, QuantumEquipmentMaterialStatConfig>(Configs_EquipmentMaterialStatConfigs,
					v => v.GetKey()),
				AddKeyValue<BaseEquipmentStatConfigs, QuantumBaseEquipmentStatConfig>(Configs_BaseEquipmentStatConfigs,
					v => (int) v.Id),
				AddKeyValue<StatConfigs, QuantumStatConfig>(Configs_StatConfigs, v => (int) v.StatType),
				AddKeyValue<EquipmentRewardConfigs, EquipmentRewardConfig>(Configs_EquipmentRewardConfigs, v => v.Id),
				AddKeyValue<RarityDataConfigs, RarityDataConfig>(Configs_RarityDataConfigs, v => (int) v.Rarity), // REMOVE
				AddKeyValue<FuseConfigs, FuseConfig>(Configs_FuseConfigs, v => (int) v.Rarity), // REMOVE
				AddKeyValue<AdjectiveDataConfigs, AdjectiveDataConfig>(Configs_AdjectiveDataConfigs, v => (int) v.Adjective), // REMOVE
				AddKeyValue<GradeDataConfigs, GradeDataConfig>(Configs_GradeDataConfigs, v => (int) v.Grade), // REMOVE
				AddKeyValue<ScrapConfigs, ScrapConfig>(Configs_ScrapConfigs, v => (int) v.Rarity), // REMOVE
				AddKeyValue<UpgradeDataConfigs, UpgradeDataConfig>(Configs_UpgradeDataConfigs, v => (int) v.Level), // REMOVE
				AddKeyValue<RepairDataConfigs, RepairDataConfig>(Configs_RepairDataConfigs, v => (int) v.ResourceType), // REMOVE
				AddKeyValue<LiveopsSegmentActionConfigs, LiveopsSegmentActionConfig>(Configs_LiveopsSegmentActionConfigs,
					v => v.ActionIdentifier),
				AddSingleton<BotDifficultyConfigs>(Configs_BotDifficultyConfigs),
				AddSingleton<QuantumRunnerConfigs>(Configs_Settings_QuantumRunnerConfigs),
				AddWrappedSingleton<BuffConfigs, QuantumBuffConfigs>(Configs_BuffConfigs),
				AddWrappedSingleton<TutorialConfigs, TutorialConfig>(Configs_TutorialConfigs),
				// This one is needed on the server so we can set the url in playfab profile
				AddWrappedSingleton<AvatarCollectableConfigs, AvatarCollectableConfig>(Collections_ProfilePicture_AvatarCollectableConfigs),
				AddWrappedSingleton<WeaponSkinsConfigContainer, WeaponSkinsConfig>(Collections_WeaponSkins_Config),
				AddWrappedSingleton<MatchmakingAndRoomConfigs, MatchmakingAndRoomConfig>(Configs_MatchmakingAndRoomConfigs),
				AddWrappedSingleton<GameConfigs, QuantumGameConfig>(Configs_GameConfigs),
				AddWrappedSingleton<BattlePassConfigs, BattlePassConfig>(Configs_BattlePassConfigs),
				AddWrappedSingleton<ReviveConfigs, QuantumReviveConfigs>(Configs_ReviveConfigs),
			};
		}

		private IEnumerable<IConfigLoadHandler> GetLoadHandlersForAssets(IAssetAdderService _assetService)
		{
			return new List<IConfigLoadHandler>
			{
				// Collections
				AddWrappedSingleton<FlagSkinConfigs, FlagSkinConfig>(Collections_Flags_FlagSkinConfigs),
				AddWrappedSingleton<CharacterSkinConfigs, CharacterSkinsConfig>(Collections_CharacterSkins_Config),
				// Others
				AddWrappedSingleton<CurrencySpriteConfigs, CurrencySpriteConfig>(Configs_CurrencySpriteConfigs),
				AddSingleton<MapAreaConfigs>(Configs_MapAreaConfigs),
				AddSingleton<MapAssetConfigs>(Configs_MapAssetConfigs),
				AddSingleton<AudioMixerConfigs>(Configs_AudioMixerConfigs),
				AddSingleton<AudioMatchAssetConfigs>(Configs_AudioMatchAssetConfigs),
				AddSingleton<AudioMainMenuAssetConfigs>(Configs_AudioMainMenuAssetConfigs),
				AddSingleton<AudioSharedAssetConfigs>(Configs_AudioSharedAssetConfigs),
				AddSingleton<MatchAssetConfigs>(Configs_AdventureAssetConfigs),
				AddSingleton<MainMenuAssetConfigs>(Configs_MainMenuAssetConfigs),
				AddSingleton<DummyAssetConfigs>(Configs_DummyAssetConfigs),

				AddSingletonToAssetService<MaterialVfxConfigs, MaterialVfxId, Material>(Configs_MaterialVfxConfigs),
				AddSingletonToAssetService<SpriteAssetConfigs, GameId, Sprite>(Configs_SpriteAssetConfigs),
				AddSingletonToAssetService<SpecialMoveAssetConfigs, SpecialType, Sprite>(Configs_SpecialMoveAssetConfigs),
				AddSingletonToAssetService<IndicatorVfxAssetConfigs, IndicatorVfxId, GameObject>(Configs_IndicatorVfxAssetConfigs),
				AddSingletonToAssetService<VideoAssetConfigs, GameId, VideoClip>(Configs_VideoAssetConfigs),

				AddSingleton<PooledVFXConfigs>(VFX_PooledVFXConfigs),
			};

			ConfigLoadDefinition<T> AddSingletonToAssetService<T, I, A>(AddressableId id)
				where I : struct
				where T : AssetConfigsScriptableObject<I, A>
			{
				return new ConfigLoadDefinition<T>(id, cfg => _assetLoader.AddConfigs<I, A>(cfg));
			}
		}

		private ConfigLoadDefinition<W> AddStringKeyValue<W, T>(AddressableId id, Func<T, string> convertToKey)
			where W : ScriptableObject, IConfigsContainer<T>
		{
			return new ConfigLoadDefinition<W>(id, asset => _configsAdder.AddConfigs(convertToKey, asset.Configs));
		}

		private ConfigLoadDefinition<T> AddSingleton<T>(AddressableId id) where T : ScriptableObject
		{
			return new ConfigLoadDefinition<T>(id, _configsAdder.AddSingletonConfig);
		}

		private ConfigLoadDefinition<W> AddWrappedSingleton<W, T>(AddressableId id)
			where W : ScriptableObject, ISingleConfigContainer<T>
		{
			return new ConfigLoadDefinition<W>(id, asset => _configsAdder.AddSingletonConfig(asset.Config));
		}

		private ConfigLoadDefinition<W> AddKeyValue<W, T>(AddressableId id, Func<T, int> convertToKey)
			where W : ScriptableObject, IConfigsContainer<T>
		{
			return new ConfigLoadDefinition<W>(id, asset => _configsAdder.AddConfigs(convertToKey, asset.Configs));
		}

		private bool ConfigComesFromServer<TContainer>()
		{
			return typeof(TContainer).CustomAttributes.Any(c => c.AttributeType == typeof(IgnoreServerSerialization));
		}

		public async UniTask LoadConfig<TContainer>(AddressableId id, Action<TContainer> onLoadComplete) where TContainer : ScriptableObject
		{
			var asset = await _assetLoader.LoadAssetAsync<TContainer>(id.GetConfig().Address);
			onLoadComplete(asset);
			_assetLoader.UnloadAsset(asset);
		}

		public IEnumerable<UniTask> LoadConfigTasks()
		{
			return GetLoadHandlersForConfigs()
				.Select(cld => cld.LoadConfigRuntime(_assetLoader));
		}

		public IEnumerable<UniTask> LoadAssetConfigTasks(IAssetAdderService assetService)
		{
			return GetLoadHandlersForAssets(assetService)
				.Select(cld => cld.LoadConfigRuntime(_assetLoader));
		}

		public void LoadConfigEditor()
		{
			foreach (var clh in GetLoadHandlersForConfigs())
			{
				clh.LoadConfigEditor();
			}
		}

		public interface IConfigLoadHandler
		{
			public UniTask LoadConfigRuntime(IAssetAdderService assetLoader);

			public void LoadConfigEditor();
		}

		private class ConfigLoadDefinition<TContainer> : IConfigLoadHandler
			where TContainer : ScriptableObject
		{
			private readonly AddressableId _id;
			private readonly Action<TContainer> _onLoadComplete;
			private readonly bool _unloadConfig;

			public ConfigLoadDefinition(AddressableId id, Action<TContainer> onLoadComplete, bool unloadConfig = true)
			{
				_id = id;
				_onLoadComplete = onLoadComplete;
				_unloadConfig = unloadConfig;
			}

			public async UniTask LoadConfigRuntime(IAssetAdderService assetLoader)
			{
				var asset = await assetLoader.LoadAssetAsync<TContainer>(_id.GetConfig().Address);
				_onLoadComplete(asset);
				if (_unloadConfig)
				{
					assetLoader.UnloadAsset(asset);
				}
			}

			public void LoadConfigEditor()
			{
#if UNITY_EDITOR
				var editorCfg = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(TContainer)}");
				Assert.AreEqual(1, editorCfg.Length, $"Found more than one config of type {typeof(TContainer)}");
				var cfgPath = UnityEditor.AssetDatabase.GUIDToAssetPath(editorCfg[0]);
				var asset = (TContainer) UnityEditor.AssetDatabase.LoadAssetAtPath(cfgPath, typeof(TContainer));
				_onLoadComplete(asset);
#else
				throw new NotSupportedException("You're not allowed to call this at runtime");
#endif
			}
		}

#if UNITY_EDITOR
		public class EditorConfigProvider
		{
			private static ConfigsProvider _provider;

			public static ConfigsProvider GetProvider()
			{
				if (_provider == null)
				{
					_provider = new ConfigsProvider();
					var configsLoader = new GameConfigsLoader(new AssetResolverService(), _provider);
					configsLoader.LoadConfigEditor();
				}

				return _provider;
			}

			public static T LoadFromAddressable<T>() where T : ScriptableObject
			{
				var editorCfg = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T)}");
				Assert.AreEqual(1, editorCfg.Length, $"Found more than one config of type {typeof(T)}");
				var cfgPath = UnityEditor.AssetDatabase.GUIDToAssetPath(editorCfg[0]);
				var asset = (T) UnityEditor.AssetDatabase.LoadAssetAtPath(cfgPath, typeof(T));
				return asset;
			}
		}
#endif
	}
}
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
		IEnumerable<UniTask> LoadConfigTasks(IConfigsAdder cfg);

		/// <summary>
		/// Loads a specific config using the given asset resolver. 
		/// </summary>
		UniTask LoadConfig<TContainer>(AddressableId id, Action<TContainer> onLoadComplete) where TContainer : ScriptableObject;
	}

	/// <summary>
	/// Configuration loader specific for our game.
	/// </summary>
	public class GameConfigsLoader : IConfigsLoader
	{
		private IAssetAdderService _assetLoader;

		public GameConfigsLoader(IAssetAdderService assetLoader)
		{
			_assetLoader = assetLoader;
		}

		public IEnumerable<UniTask> LoadConfigTasks(IConfigsAdder configsAdder)
		{
			return GetLoadHandlers(configsAdder).Select(cld => cld.LoadConfigRuntime());
		}

		public void LoadConfigEditor(IConfigsAdder configsAdder)
		{
			foreach (var clh in GetLoadHandlers(configsAdder))
			{
				clh.LoadConfigEditor();
			}
		}
		
		public IEnumerable<IConfigLoadHandler> GetLoadHandlers(IConfigsAdder configsAdder)
		{
			return new List<IConfigLoadHandler>
			{
				new ConfigLoadDefinition<GameConfigs>(_assetLoader, AddressableId.Configs_GameConfigs, asset => configsAdder.AddSingletonConfig(asset.Config)),
				new ConfigLoadDefinition<MapAreaConfigs>(_assetLoader, AddressableId.Configs_MapAreaConfigs, configsAdder.AddSingletonConfig, false),
				new ConfigLoadDefinition<MapConfigs>(_assetLoader, AddressableId.Configs_QuantumMapConfigs, asset => configsAdder.AddConfigs(data => (int) data.Map, asset.Configs)),
				new ConfigLoadDefinition<MapAssetConfigs>(_assetLoader, AddressableId.Configs_MapAssetConfigs, configsAdder.AddSingletonConfig),
				new ConfigLoadDefinition<WeaponConfigs>(_assetLoader, AddressableId.Configs_WeaponConfigs, asset => configsAdder.AddConfigs(data => (int) data.Id, asset.Configs)),
				new ConfigLoadDefinition<PlayerLevelConfigs>(_assetLoader, AddressableId.Configs_PlayerLevelConfigs, asset => configsAdder.AddConfigs(data => (int) data.LevelStart, asset.Configs)),
				new ConfigLoadDefinition<SpecialConfigs>(_assetLoader, AddressableId.Configs_SpecialConfigs, asset => configsAdder.AddConfigs(data => (int) data.Id, asset.Configs)),
				new ConfigLoadDefinition<ConsumableConfigs>(_assetLoader, AddressableId.Configs_ConsumableConfigs, asset => configsAdder.AddConfigs(data => (int) data.Id, asset.Configs)),
				new ConfigLoadDefinition<DestructibleConfigs>(_assetLoader, AddressableId.Configs_DestructibleConfigs, asset => configsAdder.AddConfigs(data => (int) data.Id, asset.Configs)),
				new ConfigLoadDefinition<ShrinkingCircleConfigs>(_assetLoader, AddressableId.Configs_ShrinkingCircleConfigs, asset => configsAdder.AddConfigs(data => data.Key, asset.Configs)),
				new ConfigLoadDefinition<ResourcePoolConfigs>(_assetLoader, AddressableId.Configs_ResourcePoolConfigs, asset => configsAdder.AddConfigs(data => (int) data.Id, asset.Configs)),
				new ConfigLoadDefinition<AudioWeaponConfigs>(_assetLoader, AddressableId.Configs_AudioWeaponConfigs, asset => configsAdder.AddConfigs(data => (int) data.GameId, asset.Configs)),
				new ConfigLoadDefinition<MatchRewardConfigs>(_assetLoader, AddressableId.Configs_MatchRewardConfigs, asset => configsAdder.AddConfigs(data => data.MatchRewardId, asset.Configs)),
				new ConfigLoadDefinition<TrophyRewardConfigs>(_assetLoader, AddressableId.Configs_TrophyRewardConfigs, asset => configsAdder.AddConfigs(data => data.MatchRewardId, asset.Configs)),
				new ConfigLoadDefinition<ChestConfigs>(_assetLoader, AddressableId.Configs_ChestConfigs, asset => configsAdder.AddConfigs(data => (int) data.Id, asset.Configs)),
				new ConfigLoadDefinition<EquipmentStatConfigs>(_assetLoader, AddressableId.Configs_EquipmentStatConfigs, asset => configsAdder.AddConfigs(data => data.GetKey(), asset.Configs)),
				new ConfigLoadDefinition<EquipmentMaterialStatConfigs>(_assetLoader, AddressableId.Configs_EquipmentMaterialStatConfigs, asset => configsAdder.AddConfigs(data => data.GetKey(), asset.Configs)),
				new ConfigLoadDefinition<BaseEquipmentStatConfigs>(_assetLoader, AddressableId.Configs_BaseEquipmentStatConfigs, asset => configsAdder.AddConfigs(data => (int) data.Id, asset.Configs)),
				new ConfigLoadDefinition<StatConfigs>(_assetLoader, AddressableId.Configs_StatConfigs, asset => configsAdder.AddConfigs(data => (int) data.StatType, asset.Configs)),
				new ConfigLoadDefinition<GraphicsConfig>(_assetLoader, AddressableId.Configs_GraphicsConfig, asset => configsAdder.AddSingletonConfig(asset)),
				new ConfigLoadDefinition<BattlePassConfigs>(_assetLoader, AddressableId.Configs_BattlePassConfigs, asset => configsAdder.AddSingletonConfig(asset.Config)),
				new ConfigLoadDefinition<EquipmentRewardConfigs>(_assetLoader, AddressableId.Configs_EquipmentRewardConfigs, asset => configsAdder.AddConfigs(data => data.Id, asset.Configs)),
				new ConfigLoadDefinition<RarityDataConfigs>(_assetLoader, AddressableId.Configs_RarityDataConfigs, asset => configsAdder.AddConfigs(data => (int) data.Rarity, asset.Configs)),
				new ConfigLoadDefinition<FuseConfigs>(_assetLoader, AddressableId.Configs_FuseConfigs, asset => configsAdder.AddConfigs(data => (int) data.Rarity, asset.Configs)),
				new ConfigLoadDefinition<AdjectiveDataConfigs>(_assetLoader, AddressableId.Configs_AdjectiveDataConfigs, asset => configsAdder.AddConfigs(data => (int) data.Adjective, asset.Configs)),
				new ConfigLoadDefinition<GradeDataConfigs>(_assetLoader, AddressableId.Configs_GradeDataConfigs, asset => configsAdder.AddConfigs(data => (int) data.Grade, asset.Configs)),
				new ConfigLoadDefinition<GameModeConfigs>(_assetLoader, AddressableId.Configs_GameModeConfigs, asset => configsAdder.AddConfigs(data => data.Id, asset.Configs)),
				new ConfigLoadDefinition<ReviveConfigs>(_assetLoader, AddressableId.Configs_ReviveConfigs, asset => configsAdder.AddSingletonConfig(asset.Config)),
				new ConfigLoadDefinition<GameModeRotationConfigs>(_assetLoader, AddressableId.Configs_GameModeRotationConfigs, asset => configsAdder.AddSingletonConfig(asset.Config)),
				new ConfigLoadDefinition<MutatorConfigs>(_assetLoader, AddressableId.Configs_MutatorConfigs, asset => configsAdder.AddConfigs(data => data.Id.GetHashCode(), asset.Configs)),
				new ConfigLoadDefinition<ScrapConfigs>(_assetLoader, AddressableId.Configs_ScrapConfigs, asset => configsAdder.AddConfigs(data => (int) data.Rarity, asset.Configs)),
				new ConfigLoadDefinition<UpgradeDataConfigs>(_assetLoader, AddressableId.Configs_UpgradeDataConfigs, asset => configsAdder.AddConfigs(data => (int) data.Level, asset.Configs)),
				new ConfigLoadDefinition<RepairDataConfigs>(_assetLoader, AddressableId.Configs_RepairDataConfigs, asset => configsAdder.AddConfigs(data => (int) data.ResourceType, asset.Configs)),
				new ConfigLoadDefinition<LiveopsSegmentActionConfigs>(_assetLoader, AddressableId.Configs_LiveopsSegmentActionConfigs, asset => configsAdder.AddConfigs(data => data.ActionIdentifier, asset.Configs)),
				new ConfigLoadDefinition<TutorialRewardConfigs>(_assetLoader, AddressableId.Configs_TutorialRewardConfigs, asset => configsAdder.AddConfigs(data => (int) data.Section, asset.Configs)),
				new ConfigLoadDefinition<BotDifficultyConfigs>(_assetLoader, AddressableId.Configs_BotDifficultyConfigs, configsAdder.AddSingletonConfig),
				new ConfigLoadDefinition<MatchmakingAndRoomConfigs>(_assetLoader, AddressableId.Configs_MatchmakingAndRoomConfigs, asset => configsAdder.AddSingletonConfig(asset.Config)),
				new ConfigLoadDefinition<CharacterSkinConfigs>(_assetLoader, AddressableId.Collections_CharacterSkins_Config, asset => configsAdder.AddSingletonConfig(asset.Config)),
				new ConfigLoadDefinition<WeaponSkinsConfigContainer>(_assetLoader, AddressableId.Collections_WeaponSkins_Config, asset => configsAdder.AddSingletonConfig(asset.Config)),
				new ConfigLoadDefinition<AvatarCollectableConfigs>(_assetLoader, AddressableId.Collections_ProfilePicture_AvatarCollectableConfigs, asset => configsAdder.AddSingletonConfig(asset.Config)),
				new ConfigLoadDefinition<CurrencySpriteConfigs>(_assetLoader, AddressableId.Configs_CurrencySpriteConfigs, cfg => configsAdder.AddSingletonConfig(cfg.Config)),
			};
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

		public interface IConfigLoadHandler
		{
			public UniTask LoadConfigRuntime();

			public void LoadConfigEditor();
		}

		private class ConfigLoadDefinition<TContainer> : IConfigLoadHandler
			where TContainer : ScriptableObject
		{
			private readonly IAssetAdderService _assetLoader;

			private readonly AddressableId _id;
			private readonly Action<TContainer> _onLoadComplete;
			private readonly bool _unloadConfig;

			public ConfigLoadDefinition(IAssetAdderService assetLoader, AddressableId id, Action<TContainer> onLoadComplete, bool unloadConfig = true)
			{
				_assetLoader = assetLoader;
				_id = id;
				_onLoadComplete = onLoadComplete;
				_unloadConfig = unloadConfig;
			}

			public async UniTask LoadConfigRuntime()
			{
				var asset = await _assetLoader.LoadAssetAsync<TContainer>(_id.GetConfig().Address);
				_onLoadComplete(asset);
				if (_unloadConfig)
				{
					_assetLoader.UnloadAsset(asset);
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
	}
}
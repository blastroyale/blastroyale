/* AUTO GENERATED CODE */

using System.Collections.Generic;
using System.Collections.ObjectModel;
using FirstLight.AddressablesExtensions;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace

namespace FirstLight.Game.Ids
{
	public enum AddressableId
	{
		Configs_AdjectiveDataConfigs,
		Configs_AdventureAssetConfigs,
		Configs_AudioMainMenuAssetConfigs,
		Configs_AudioMatchAssetConfigs,
		Configs_AudioMixerConfigs,
		Configs_AudioSharedAssetConfigs,
		Configs_AudioWeaponConfigs,
		Configs_BaseEquipmentStatConfigs,
		Configs_BattlePassConfigs,
		Configs_BotConfigs,
		Configs_ChestConfigs,
		Configs_ConsumableConfigs,
		Configs_CustomAssetConfigs,
		Configs_DestructibleConfigs,
		Configs_DummyAssetConfigs,
		Configs_EditionDataConfigs,
		Configs_EquipmentMaterialStatConfigs,
		Configs_EquipmentRarityAssetConfigs,
		Configs_EquipmentRewardConfigs,
		Configs_EquipmentStatConfigs,
		Configs_FactionDataConfigs,
		Configs_GameConfigs,
		Configs_GameModeConfigs,
		Configs_GameModeRotationConfigs,
		Configs_GradeDataConfigs,
		Configs_GraphicsConfig,
		Configs_IndicatorVfxAssetConfigs,
		Configs_MainMenuAssetConfigs,
		Configs_ManufacturerDataConfigs,
		Configs_MapConfigs,
		Configs_MapGridConfigs,
		Configs_MatchRewardConfigs,
		Configs_MaterialDataConfigs,
		Configs_MaterialVfxConfigs,
		Configs_MutatorConfigs,
		Configs_PlayerLevelConfigs,
		Configs_PlayerRankAssetConfigs,
		Configs_QuantumPrototypeAssetConfigs,
		Configs_RarityDataConfigs,
		Configs_ResourcePoolConfigs,
		Configs_SceneAssetConfigs,
		Configs_ScrapConfigs,
		Configs_ShrinkingCircleConfigs,
		Configs_SpecialConfigs,
		Configs_SpecialMoveAssetConfigs,
		Configs_SpriteAssetConfigs,
		Configs_StatConfigs,
		Configs_UpgradeDataConfigs,
		Configs_VfxConfigs,
		Configs_VideoAssetConfigs,
		Configs_WeaponConfigs,
		Configs_Settings_AssetResources,
		Configs_Settings_Deterministic_Config,
		Configs_Settings_PhotonServerSettings,
		Configs_Settings_QuantumRunnerConfigs,
		Configs_Settings_SimulationConfig,
		Configs_Settings_UiConfigs,
		Timeline_FtueTimeline
	}

	public enum AddressableLabel
	{
		Label_GenerateIds
	}

	public static class AddressablePathLookup
	{
		public static readonly string Timeline = "Timeline";
		public static readonly string ConfigsSettings = "Configs/Settings";
		public static readonly string Configs = "Configs";
	}

	public static class AddressableConfigLookup
	{
		public static IList<AddressableConfig> Configs => _addressableConfigs;
		public static IList<string> Labels => _addressableLabels;

		public static AddressableConfig GetConfig(this AddressableId addressable)
		{
			return _addressableConfigs[(int) addressable];
		}

		public static IList<AddressableConfig> GetConfigs(this AddressableLabel label)
		{
			return _addressableLabelMap[_addressableLabels[(int) label]];
		}

		public static IList<AddressableConfig> GetConfigs(string label)
		{
			return _addressableLabelMap[label];
		}

		public static string ToLabelString(this AddressableLabel label)
		{
			return _addressableLabels[(int) label];
		}

		private static readonly IList<string> _addressableLabels = new List<string>
		{
			"GenerateIds"
		}.AsReadOnly();

		private static readonly IReadOnlyDictionary<string, IList<AddressableConfig>> _addressableLabelMap = new ReadOnlyDictionary<string, IList<AddressableConfig>>(new Dictionary<string, IList<AddressableConfig>>
		{
			{"GenerateIds", new List<AddressableConfig>
				{
					new AddressableConfig(0, "Timeline/FtueTimeline.prefab", "Assets/AddressableResources/Timeline/FtueTimeline.prefab", typeof(UnityEngine.GameObject), new [] {"GenerateIds"}),
					new AddressableConfig(1, "Configs/Settings/UiConfigs.asset", "Assets/AddressableResources/Configs/Settings/UiConfigs.asset", typeof(FirstLight.UiService.UiConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(2, "Configs/Settings/SimulationConfig.asset", "Assets/AddressableResources/Configs/Settings/SimulationConfig.asset", typeof(SimulationConfigAsset), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(3, "Configs/Settings/QuantumRunnerConfigs.asset", "Assets/AddressableResources/Configs/Settings/QuantumRunnerConfigs.asset", typeof(FirstLight.Game.Configs.QuantumRunnerConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(4, "Configs/Settings/PhotonServerSettings.asset", "Assets/AddressableResources/Configs/Settings/PhotonServerSettings.asset", typeof(PhotonServerSettings), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(5, "Configs/Settings/Deterministic Config.asset", "Assets/AddressableResources/Configs/Settings/Deterministic Config.asset", typeof(DeterministicSessionConfigAsset), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(6, "Configs/Settings/AssetResources.asset", "Assets/AddressableResources/Configs/Settings/AssetResources.asset", typeof(Quantum.AssetResourceContainer), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(7, "Configs/WeaponConfigs.asset", "Assets/AddressableResources/Configs/WeaponConfigs.asset", typeof(FirstLight.Game.Configs.WeaponConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(8, "Configs/VideoAssetConfigs.asset", "Assets/AddressableResources/Configs/VideoAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.VideoAssetConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(9, "Configs/VfxConfigs.asset", "Assets/AddressableResources/Configs/VfxConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.VfxAssetConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(10, "Configs/UpgradeDataConfigs.asset", "Assets/AddressableResources/Configs/UpgradeDataConfigs.asset", typeof(FirstLight.Game.Configs.UpgradeDataConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(11, "Configs/StatConfigs.asset", "Assets/AddressableResources/Configs/StatConfigs.asset", typeof(FirstLight.Game.Configs.StatConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(12, "Configs/SpriteAssetConfigs.asset", "Assets/AddressableResources/Configs/SpriteAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.SpriteAssetConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(13, "Configs/SpecialMoveAssetConfigs.asset", "Assets/AddressableResources/Configs/SpecialMoveAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.SpecialMoveAssetConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(14, "Configs/SpecialConfigs.asset", "Assets/AddressableResources/Configs/SpecialConfigs.asset", typeof(FirstLight.Game.Configs.SpecialConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(15, "Configs/ShrinkingCircleConfigs.asset", "Assets/AddressableResources/Configs/ShrinkingCircleConfigs.asset", typeof(FirstLight.Game.Configs.ShrinkingCircleConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(16, "Configs/ScrapConfigs.asset", "Assets/AddressableResources/Configs/ScrapConfigs.asset", typeof(FirstLight.Game.Configs.ScrapConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(17, "Configs/SceneAssetConfigs.asset", "Assets/AddressableResources/Configs/SceneAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.SceneAssetConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(18, "Configs/ResourcePoolConfigs.asset", "Assets/AddressableResources/Configs/ResourcePoolConfigs.asset", typeof(FirstLight.Game.Configs.ResourcePoolConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(19, "Configs/RarityDataConfigs.asset", "Assets/AddressableResources/Configs/RarityDataConfigs.asset", typeof(FirstLight.Game.Configs.RarityDataConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(20, "Configs/QuantumPrototypeAssetConfigs.asset", "Assets/AddressableResources/Configs/QuantumPrototypeAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.QuantumPrototypeAssetConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(21, "Configs/PlayerRankAssetConfigs.asset", "Assets/AddressableResources/Configs/PlayerRankAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.PlayerRankAssetConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(22, "Configs/PlayerLevelConfigs.asset", "Assets/AddressableResources/Configs/PlayerLevelConfigs.asset", typeof(FirstLight.Game.Configs.PlayerLevelConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(23, "Configs/MutatorConfigs.asset", "Assets/AddressableResources/Configs/MutatorConfigs.asset", typeof(FirstLight.Game.Configs.MutatorConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(24, "Configs/MaterialVfxConfigs.asset", "Assets/AddressableResources/Configs/MaterialVfxConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.MaterialVfxConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(25, "Configs/MaterialDataConfigs.asset", "Assets/AddressableResources/Configs/MaterialDataConfigs.asset", typeof(FirstLight.Game.Configs.MaterialDataConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(26, "Configs/MatchRewardConfigs.asset", "Assets/AddressableResources/Configs/MatchRewardConfigs.asset", typeof(FirstLight.Game.Configs.MatchRewardConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(27, "Configs/MapGridConfigs.asset", "Assets/AddressableResources/Configs/MapGridConfigs.asset", typeof(FirstLight.Game.Configs.MapGridConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(28, "Configs/MapConfigs.asset", "Assets/AddressableResources/Configs/MapConfigs.asset", typeof(FirstLight.Game.Configs.MapConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(29, "Configs/ManufacturerDataConfigs.asset", "Assets/AddressableResources/Configs/ManufacturerDataConfigs.asset", typeof(FirstLight.Game.Configs.ManufacturerDataConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(30, "Configs/MainMenuAssetConfigs.asset", "Assets/AddressableResources/Configs/MainMenuAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.MainMenuAssetConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(31, "Configs/IndicatorVfxAssetConfigs.asset", "Assets/AddressableResources/Configs/IndicatorVfxAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.IndicatorVfxAssetConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(32, "Configs/GraphicsConfig.asset", "Assets/AddressableResources/Configs/GraphicsConfig.asset", typeof(FirstLight.Game.Configs.GraphicsConfig), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(33, "Configs/GradeDataConfigs.asset", "Assets/AddressableResources/Configs/GradeDataConfigs.asset", typeof(FirstLight.Game.Configs.GradeDataConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(34, "Configs/GameModeRotationConfigs.asset", "Assets/AddressableResources/Configs/GameModeRotationConfigs.asset", typeof(FirstLight.Game.Configs.GameModeRotationConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(35, "Configs/GameModeConfigs.asset", "Assets/AddressableResources/Configs/GameModeConfigs.asset", typeof(FirstLight.Game.Configs.GameModeConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(36, "Configs/GameConfigs.asset", "Assets/AddressableResources/Configs/GameConfigs.asset", typeof(FirstLight.Game.Configs.GameConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(37, "Configs/FactionDataConfigs.asset", "Assets/AddressableResources/Configs/FactionDataConfigs.asset", typeof(FirstLight.Game.Configs.FactionDataConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(38, "Configs/EquipmentStatConfigs.asset", "Assets/AddressableResources/Configs/EquipmentStatConfigs.asset", typeof(FirstLight.Game.Configs.EquipmentStatConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(39, "Configs/EquipmentRewardConfigs.asset", "Assets/AddressableResources/Configs/EquipmentRewardConfigs.asset", typeof(FirstLight.Game.Configs.EquipmentRewardConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(40, "Configs/EquipmentRarityAssetConfigs.asset", "Assets/AddressableResources/Configs/EquipmentRarityAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.EquipmentRarityAssetConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(41, "Configs/EquipmentMaterialStatConfigs.asset", "Assets/AddressableResources/Configs/EquipmentMaterialStatConfigs.asset", typeof(FirstLight.Game.Configs.EquipmentMaterialStatConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(42, "Configs/EditionDataConfigs.asset", "Assets/AddressableResources/Configs/EditionDataConfigs.asset", typeof(FirstLight.Game.Configs.EditionDataConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(43, "Configs/DummyAssetConfigs.asset", "Assets/AddressableResources/Configs/DummyAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.DummyAssetConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(44, "Configs/DestructibleConfigs.asset", "Assets/AddressableResources/Configs/DestructibleConfigs.asset", typeof(FirstLight.Game.Configs.DestructibleConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(45, "Configs/CustomAssetConfigs.asset", "Assets/AddressableResources/Configs/CustomAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.CustomAssetConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(46, "Configs/ConsumableConfigs.asset", "Assets/AddressableResources/Configs/ConsumableConfigs.asset", typeof(FirstLight.Game.Configs.ConsumableConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(47, "Configs/ChestConfigs.asset", "Assets/AddressableResources/Configs/ChestConfigs.asset", typeof(FirstLight.Game.Configs.ChestConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(48, "Configs/BotConfigs.asset", "Assets/AddressableResources/Configs/BotConfigs.asset", typeof(FirstLight.Game.Configs.BotConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(49, "Configs/BattlePassConfigs.asset", "Assets/AddressableResources/Configs/BattlePassConfigs.asset", typeof(FirstLight.Game.Configs.BattlePassConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(50, "Configs/BaseEquipmentStatConfigs.asset", "Assets/AddressableResources/Configs/BaseEquipmentStatConfigs.asset", typeof(FirstLight.Game.Configs.BaseEquipmentStatConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(51, "Configs/AudioWeaponConfigs.asset", "Assets/AddressableResources/Configs/AudioWeaponConfigs.asset", typeof(FirstLight.Game.Configs.AudioWeaponConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(52, "Configs/AudioSharedAssetConfigs.asset", "Assets/AddressableResources/Configs/AudioSharedAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AudioSharedAssetConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(53, "Configs/AudioMixerConfigs.asset", "Assets/AddressableResources/Configs/AudioMixerConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AudioMixerConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(54, "Configs/AudioMatchAssetConfigs.asset", "Assets/AddressableResources/Configs/AudioMatchAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AudioMatchAssetConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(55, "Configs/AudioMainMenuAssetConfigs.asset", "Assets/AddressableResources/Configs/AudioMainMenuAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AudioMainMenuAssetConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(56, "Configs/AdventureAssetConfigs.asset", "Assets/AddressableResources/Configs/AdventureAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.MatchAssetConfigs), new [] {"Quantum","GenerateIds"}),
					new AddressableConfig(57, "Configs/AdjectiveDataConfigs.asset", "Assets/AddressableResources/Configs/AdjectiveDataConfigs.asset", typeof(FirstLight.Game.Configs.AdjectiveDataConfigs), new [] {"Quantum","GenerateIds"}),
				}.AsReadOnly()}
		});

		private static readonly IList<AddressableConfig> _addressableConfigs = new List<AddressableConfig>
		{
			new AddressableConfig(0, "Configs/AdjectiveDataConfigs.asset", "Assets/AddressableResources/Configs/AdjectiveDataConfigs.asset", typeof(FirstLight.Game.Configs.AdjectiveDataConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(1, "Configs/AdventureAssetConfigs.asset", "Assets/AddressableResources/Configs/AdventureAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.MatchAssetConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(2, "Configs/AudioMainMenuAssetConfigs.asset", "Assets/AddressableResources/Configs/AudioMainMenuAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AudioMainMenuAssetConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(3, "Configs/AudioMatchAssetConfigs.asset", "Assets/AddressableResources/Configs/AudioMatchAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AudioMatchAssetConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(4, "Configs/AudioMixerConfigs.asset", "Assets/AddressableResources/Configs/AudioMixerConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AudioMixerConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(5, "Configs/AudioSharedAssetConfigs.asset", "Assets/AddressableResources/Configs/AudioSharedAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AudioSharedAssetConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(6, "Configs/AudioWeaponConfigs.asset", "Assets/AddressableResources/Configs/AudioWeaponConfigs.asset", typeof(FirstLight.Game.Configs.AudioWeaponConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(7, "Configs/BaseEquipmentStatConfigs.asset", "Assets/AddressableResources/Configs/BaseEquipmentStatConfigs.asset", typeof(FirstLight.Game.Configs.BaseEquipmentStatConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(8, "Configs/BattlePassConfigs.asset", "Assets/AddressableResources/Configs/BattlePassConfigs.asset", typeof(FirstLight.Game.Configs.BattlePassConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(9, "Configs/BotConfigs.asset", "Assets/AddressableResources/Configs/BotConfigs.asset", typeof(FirstLight.Game.Configs.BotConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(10, "Configs/ChestConfigs.asset", "Assets/AddressableResources/Configs/ChestConfigs.asset", typeof(FirstLight.Game.Configs.ChestConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(11, "Configs/ConsumableConfigs.asset", "Assets/AddressableResources/Configs/ConsumableConfigs.asset", typeof(FirstLight.Game.Configs.ConsumableConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(12, "Configs/CustomAssetConfigs.asset", "Assets/AddressableResources/Configs/CustomAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.CustomAssetConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(13, "Configs/DestructibleConfigs.asset", "Assets/AddressableResources/Configs/DestructibleConfigs.asset", typeof(FirstLight.Game.Configs.DestructibleConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(14, "Configs/DummyAssetConfigs.asset", "Assets/AddressableResources/Configs/DummyAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.DummyAssetConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(15, "Configs/EditionDataConfigs.asset", "Assets/AddressableResources/Configs/EditionDataConfigs.asset", typeof(FirstLight.Game.Configs.EditionDataConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(16, "Configs/EquipmentMaterialStatConfigs.asset", "Assets/AddressableResources/Configs/EquipmentMaterialStatConfigs.asset", typeof(FirstLight.Game.Configs.EquipmentMaterialStatConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(17, "Configs/EquipmentRarityAssetConfigs.asset", "Assets/AddressableResources/Configs/EquipmentRarityAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.EquipmentRarityAssetConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(18, "Configs/EquipmentRewardConfigs.asset", "Assets/AddressableResources/Configs/EquipmentRewardConfigs.asset", typeof(FirstLight.Game.Configs.EquipmentRewardConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(19, "Configs/EquipmentStatConfigs.asset", "Assets/AddressableResources/Configs/EquipmentStatConfigs.asset", typeof(FirstLight.Game.Configs.EquipmentStatConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(20, "Configs/FactionDataConfigs.asset", "Assets/AddressableResources/Configs/FactionDataConfigs.asset", typeof(FirstLight.Game.Configs.FactionDataConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(21, "Configs/GameConfigs.asset", "Assets/AddressableResources/Configs/GameConfigs.asset", typeof(FirstLight.Game.Configs.GameConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(22, "Configs/GameModeConfigs.asset", "Assets/AddressableResources/Configs/GameModeConfigs.asset", typeof(FirstLight.Game.Configs.GameModeConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(23, "Configs/GameModeRotationConfigs.asset", "Assets/AddressableResources/Configs/GameModeRotationConfigs.asset", typeof(FirstLight.Game.Configs.GameModeRotationConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(24, "Configs/GradeDataConfigs.asset", "Assets/AddressableResources/Configs/GradeDataConfigs.asset", typeof(FirstLight.Game.Configs.GradeDataConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(25, "Configs/GraphicsConfig.asset", "Assets/AddressableResources/Configs/GraphicsConfig.asset", typeof(FirstLight.Game.Configs.GraphicsConfig), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(26, "Configs/IndicatorVfxAssetConfigs.asset", "Assets/AddressableResources/Configs/IndicatorVfxAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.IndicatorVfxAssetConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(27, "Configs/MainMenuAssetConfigs.asset", "Assets/AddressableResources/Configs/MainMenuAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.MainMenuAssetConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(28, "Configs/ManufacturerDataConfigs.asset", "Assets/AddressableResources/Configs/ManufacturerDataConfigs.asset", typeof(FirstLight.Game.Configs.ManufacturerDataConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(29, "Configs/MapConfigs.asset", "Assets/AddressableResources/Configs/MapConfigs.asset", typeof(FirstLight.Game.Configs.MapConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(30, "Configs/MapGridConfigs.asset", "Assets/AddressableResources/Configs/MapGridConfigs.asset", typeof(FirstLight.Game.Configs.MapGridConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(31, "Configs/MatchRewardConfigs.asset", "Assets/AddressableResources/Configs/MatchRewardConfigs.asset", typeof(FirstLight.Game.Configs.MatchRewardConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(32, "Configs/MaterialDataConfigs.asset", "Assets/AddressableResources/Configs/MaterialDataConfigs.asset", typeof(FirstLight.Game.Configs.MaterialDataConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(33, "Configs/MaterialVfxConfigs.asset", "Assets/AddressableResources/Configs/MaterialVfxConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.MaterialVfxConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(34, "Configs/MutatorConfigs.asset", "Assets/AddressableResources/Configs/MutatorConfigs.asset", typeof(FirstLight.Game.Configs.MutatorConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(35, "Configs/PlayerLevelConfigs.asset", "Assets/AddressableResources/Configs/PlayerLevelConfigs.asset", typeof(FirstLight.Game.Configs.PlayerLevelConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(36, "Configs/PlayerRankAssetConfigs.asset", "Assets/AddressableResources/Configs/PlayerRankAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.PlayerRankAssetConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(37, "Configs/QuantumPrototypeAssetConfigs.asset", "Assets/AddressableResources/Configs/QuantumPrototypeAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.QuantumPrototypeAssetConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(38, "Configs/RarityDataConfigs.asset", "Assets/AddressableResources/Configs/RarityDataConfigs.asset", typeof(FirstLight.Game.Configs.RarityDataConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(39, "Configs/ResourcePoolConfigs.asset", "Assets/AddressableResources/Configs/ResourcePoolConfigs.asset", typeof(FirstLight.Game.Configs.ResourcePoolConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(40, "Configs/SceneAssetConfigs.asset", "Assets/AddressableResources/Configs/SceneAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.SceneAssetConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(41, "Configs/ScrapConfigs.asset", "Assets/AddressableResources/Configs/ScrapConfigs.asset", typeof(FirstLight.Game.Configs.ScrapConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(42, "Configs/ShrinkingCircleConfigs.asset", "Assets/AddressableResources/Configs/ShrinkingCircleConfigs.asset", typeof(FirstLight.Game.Configs.ShrinkingCircleConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(43, "Configs/SpecialConfigs.asset", "Assets/AddressableResources/Configs/SpecialConfigs.asset", typeof(FirstLight.Game.Configs.SpecialConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(44, "Configs/SpecialMoveAssetConfigs.asset", "Assets/AddressableResources/Configs/SpecialMoveAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.SpecialMoveAssetConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(45, "Configs/SpriteAssetConfigs.asset", "Assets/AddressableResources/Configs/SpriteAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.SpriteAssetConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(46, "Configs/StatConfigs.asset", "Assets/AddressableResources/Configs/StatConfigs.asset", typeof(FirstLight.Game.Configs.StatConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(47, "Configs/UpgradeDataConfigs.asset", "Assets/AddressableResources/Configs/UpgradeDataConfigs.asset", typeof(FirstLight.Game.Configs.UpgradeDataConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(48, "Configs/VfxConfigs.asset", "Assets/AddressableResources/Configs/VfxConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.VfxAssetConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(49, "Configs/VideoAssetConfigs.asset", "Assets/AddressableResources/Configs/VideoAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.VideoAssetConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(50, "Configs/WeaponConfigs.asset", "Assets/AddressableResources/Configs/WeaponConfigs.asset", typeof(FirstLight.Game.Configs.WeaponConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(51, "Configs/Settings/AssetResources.asset", "Assets/AddressableResources/Configs/Settings/AssetResources.asset", typeof(Quantum.AssetResourceContainer), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(52, "Configs/Settings/Deterministic Config.asset", "Assets/AddressableResources/Configs/Settings/Deterministic Config.asset", typeof(DeterministicSessionConfigAsset), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(53, "Configs/Settings/PhotonServerSettings.asset", "Assets/AddressableResources/Configs/Settings/PhotonServerSettings.asset", typeof(PhotonServerSettings), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(54, "Configs/Settings/QuantumRunnerConfigs.asset", "Assets/AddressableResources/Configs/Settings/QuantumRunnerConfigs.asset", typeof(FirstLight.Game.Configs.QuantumRunnerConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(55, "Configs/Settings/SimulationConfig.asset", "Assets/AddressableResources/Configs/Settings/SimulationConfig.asset", typeof(SimulationConfigAsset), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(56, "Configs/Settings/UiConfigs.asset", "Assets/AddressableResources/Configs/Settings/UiConfigs.asset", typeof(FirstLight.UiService.UiConfigs), new [] {"Quantum","GenerateIds"}),
			new AddressableConfig(57, "Timeline/FtueTimeline.prefab", "Assets/AddressableResources/Timeline/FtueTimeline.prefab", typeof(UnityEngine.GameObject), new [] {"GenerateIds"})
		}.AsReadOnly();
	}
}

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
		Collections_Flags_FlagSkinConfigs,
		Collections_ProfilePicture_AvatarCollectableConfigs,
		Collections_WeaponSkins_Config,
		VFX_PooledVFXConfigs,
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
		Configs_BotDifficultyConfigs,
		Configs_BuffConfigs,
		Configs_ChestConfigs,
		Configs_ConsumableConfigs,
		Configs_CurrencySpriteConfigs,
		Configs_CustomAssetConfigs,
		Configs_DestructibleConfigs,
		Configs_DummyAssetConfigs,
		Configs_EditionDataConfigs,
		Configs_EquipmentMaterialStatConfigs,
		Configs_EquipmentRewardConfigs,
		Configs_EquipmentStatConfigs,
		Configs_FactionDataConfigs,
		Configs_FuseConfigs,
		Configs_GameConfigs,
		Configs_GameModeConfigs,
		Configs_GradeDataConfigs,
		Configs_IndicatorVfxAssetConfigs,
		Configs_LiveopsFeatureFlagConfigs,
		Configs_LiveopsSegmentActionConfigs,
		Configs_MainMenuAssetConfigs,
		Configs_ManufacturerDataConfigs,
		Configs_MatchRewardConfigs,
		Configs_MatchmakingAndRoomConfigs,
		Configs_MaterialDataConfigs,
		Configs_MaterialVfxConfigs,
		Configs_PlayerLevelConfigs,
		Configs_QuantumMapConfigs,
		Configs_QuantumPrototypeAssetConfigs,
		Configs_RarityDataConfigs,
		Configs_RepairDataConfigs,
		Configs_ResourcePoolConfigs,
		Configs_ReviveConfigs,
		Configs_ScrapConfigs,
		Configs_ShrinkingCircleConfigs,
		Configs_SpecialConfigs,
		Configs_SpecialMoveAssetConfigs,
		Configs_SpriteAssetConfigs,
		Configs_StatConfigs,
		Configs_TrophyRewardConfigs,
		Configs_TutorialConfigs,
		Configs_UpgradeDataConfigs,
		Configs_VideoAssetConfigs,
		Configs_WeaponConfigs,
		Configs_Settings_AssetResources,
		Configs_Settings_Deterministic_Config,
		Configs_Settings_PhotonServerSettings,
		Configs_Settings_QuantumRunnerConfigs,
		Configs_Settings_SimulationConfig,
		Configs_MapAssetConfigs,
		Collections_CharacterSkins_Config
	}

	public enum AddressableLabel
	{
		Label_GenerateIds
	}

	public static class AddressablePathLookup
	{
		public static readonly string CollectionsCharacterSkins = "Collections/CharacterSkins";
		public static readonly string Configs = "Configs";
		public static readonly string ConfigsSettings = "Configs/Settings";
		public static readonly string VFX = "VFX";
		public static readonly string CollectionsWeaponSkins = "Collections/WeaponSkins";
		public static readonly string CollectionsProfilePicture = "Collections/ProfilePicture";
		public static readonly string CollectionsFlags = "Collections/Flags";
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
					new AddressableConfig(0, "Collections/CharacterSkins/Config.asset", "Assets/AddressableResources/Collections/CharacterSkins/Config.asset", typeof(FirstLight.Game.Configs.CharacterSkinConfigs), new [] {"GenerateIds","ScriptableObjectsOnly"}),
					new AddressableConfig(1, "Configs/MapAssetConfigs.asset", "Assets/AddressableResources/Maps/MapAssetsConfig.asset", typeof(FirstLight.Game.Configs.MapAssetConfigIndex), new [] {"GenerateIds","ScriptableObjectsOnly"}),
					new AddressableConfig(2, "Configs/Settings/SimulationConfig.asset", "Assets/AddressableResources/Configs/Settings/SimulationConfig.asset", typeof(SimulationConfigAsset), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(3, "Configs/Settings/QuantumRunnerConfigs.asset", "Assets/AddressableResources/Configs/Settings/QuantumRunnerConfigs.asset", typeof(FirstLight.Game.Configs.QuantumRunnerConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(4, "Configs/Settings/PhotonServerSettings.asset", "Assets/AddressableResources/Configs/Settings/PhotonServerSettings.asset", typeof(PhotonServerSettings), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(5, "Configs/Settings/Deterministic Config.asset", "Assets/AddressableResources/Configs/Settings/Deterministic Config.asset", typeof(DeterministicSessionConfigAsset), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(6, "Configs/Settings/AssetResources.asset", "Assets/AddressableResources/Configs/Settings/AssetResources.asset", typeof(Quantum.AssetResourceContainer), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(7, "Configs/WeaponConfigs.asset", "Assets/AddressableResources/Configs/WeaponConfigs.asset", typeof(FirstLight.Game.Configs.WeaponConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(8, "Configs/VideoAssetConfigs.asset", "Assets/AddressableResources/Configs/VideoAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.VideoAssetConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(9, "Configs/UpgradeDataConfigs.asset", "Assets/AddressableResources/Configs/UpgradeDataConfigs.asset", typeof(FirstLight.Game.Configs.UpgradeDataConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(10, "Configs/TutorialConfigs.asset", "Assets/AddressableResources/Configs/TutorialConfigs.asset", typeof(FirstLight.Game.Configs.TutorialConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(11, "Configs/TrophyRewardConfigs.asset", "Assets/AddressableResources/Configs/TrophyRewardConfigs.asset", typeof(FirstLight.Game.Configs.TrophyRewardConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(12, "Configs/StatConfigs.asset", "Assets/AddressableResources/Configs/StatConfigs.asset", typeof(FirstLight.Game.Configs.StatConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(13, "Configs/SpriteAssetConfigs.asset", "Assets/AddressableResources/Configs/SpriteAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.SpriteAssetConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(14, "Configs/SpecialMoveAssetConfigs.asset", "Assets/AddressableResources/Configs/SpecialMoveAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.SpecialMoveAssetConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(15, "Configs/SpecialConfigs.asset", "Assets/AddressableResources/Configs/SpecialConfigs.asset", typeof(FirstLight.Game.Configs.SpecialConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(16, "Configs/ShrinkingCircleConfigs.asset", "Assets/AddressableResources/Configs/ShrinkingCircleConfigs.asset", typeof(FirstLight.Game.Configs.ShrinkingCircleConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(17, "Configs/ScrapConfigs.asset", "Assets/AddressableResources/Configs/ScrapConfigs.asset", typeof(FirstLight.Game.Configs.ScrapConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(18, "Configs/ReviveConfigs.asset", "Assets/AddressableResources/Configs/ReviveConfigs.asset", typeof(FirstLight.Game.Configs.ReviveConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(19, "Configs/ResourcePoolConfigs.asset", "Assets/AddressableResources/Configs/ResourcePoolConfigs.asset", typeof(FirstLight.Game.Configs.ResourcePoolConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(20, "Configs/RepairDataConfigs.asset", "Assets/AddressableResources/Configs/RepairDataConfigs.asset", typeof(FirstLight.Game.Configs.RepairDataConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(21, "Configs/RarityDataConfigs.asset", "Assets/AddressableResources/Configs/RarityDataConfigs.asset", typeof(FirstLight.Game.Configs.RarityDataConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(22, "Configs/QuantumPrototypeAssetConfigs.asset", "Assets/AddressableResources/Configs/QuantumPrototypeAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.QuantumPrototypeAssetConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(23, "Configs/QuantumMapConfigs.asset", "Assets/AddressableResources/Configs/QuantumMapConfigs.asset", typeof(FirstLight.Game.Configs.MapConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(24, "Configs/PlayerLevelConfigs.asset", "Assets/AddressableResources/Configs/PlayerLevelConfigs.asset", typeof(FirstLight.Game.Configs.PlayerLevelConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(25, "Configs/MaterialVfxConfigs.asset", "Assets/AddressableResources/Configs/MaterialVfxConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.MaterialVfxConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(26, "Configs/MaterialDataConfigs.asset", "Assets/AddressableResources/Configs/MaterialDataConfigs.asset", typeof(FirstLight.Game.Configs.MaterialDataConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(27, "Configs/MatchmakingAndRoomConfigs.asset", "Assets/AddressableResources/Configs/MatchmakingAndRoomConfigs.asset", typeof(FirstLight.Game.Configs.MatchmakingAndRoomConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(28, "Configs/MatchRewardConfigs.asset", "Assets/AddressableResources/Configs/MatchRewardConfigs.asset", typeof(FirstLight.Game.Configs.MatchRewardConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(29, "Configs/ManufacturerDataConfigs.asset", "Assets/AddressableResources/Configs/ManufacturerDataConfigs.asset", typeof(FirstLight.Game.Configs.ManufacturerDataConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(30, "Configs/MainMenuAssetConfigs.asset", "Assets/AddressableResources/Configs/MainMenuAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.MainMenuAssetConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(31, "Configs/LiveopsSegmentActionConfigs.asset", "Assets/AddressableResources/Configs/LiveopsSegmentActionConfigs.asset", typeof(FirstLight.Game.Configs.LiveopsSegmentActionConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(32, "Configs/LiveopsFeatureFlagConfigs.asset", "Assets/AddressableResources/Configs/LiveopsFeatureFlagConfigs.asset", typeof(FirstLight.Game.Configs.LiveopsFeatureFlagConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(33, "Configs/IndicatorVfxAssetConfigs.asset", "Assets/AddressableResources/Configs/IndicatorVfxAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.IndicatorVfxAssetConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(34, "Configs/GradeDataConfigs.asset", "Assets/AddressableResources/Configs/GradeDataConfigs.asset", typeof(FirstLight.Game.Configs.GradeDataConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(35, "Configs/GameModeConfigs.asset", "Assets/AddressableResources/Configs/GameModeConfigs.asset", typeof(FirstLight.Game.Configs.GameModeConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(36, "Configs/GameConfigs.asset", "Assets/AddressableResources/Configs/GameConfigs.asset", typeof(FirstLight.Game.Configs.GameConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(37, "Configs/FuseConfigs.asset", "Assets/AddressableResources/Configs/FuseConfigs.asset", typeof(FirstLight.Game.Configs.FuseConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(38, "Configs/FactionDataConfigs.asset", "Assets/AddressableResources/Configs/FactionDataConfigs.asset", typeof(FirstLight.Game.Configs.FactionDataConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(39, "Configs/EquipmentStatConfigs.asset", "Assets/AddressableResources/Configs/EquipmentStatConfigs.asset", typeof(FirstLight.Game.Configs.EquipmentStatConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(40, "Configs/EquipmentRewardConfigs.asset", "Assets/AddressableResources/Configs/EquipmentRewardConfigs.asset", typeof(FirstLight.Game.Configs.EquipmentRewardConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(41, "Configs/EquipmentMaterialStatConfigs.asset", "Assets/AddressableResources/Configs/EquipmentMaterialStatConfigs.asset", typeof(FirstLight.Game.Configs.EquipmentMaterialStatConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(42, "Configs/EditionDataConfigs.asset", "Assets/AddressableResources/Configs/EditionDataConfigs.asset", typeof(FirstLight.Game.Configs.EditionDataConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(43, "Configs/DummyAssetConfigs.asset", "Assets/AddressableResources/Configs/DummyAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.DummyAssetConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(44, "Configs/DestructibleConfigs.asset", "Assets/AddressableResources/Configs/DestructibleConfigs.asset", typeof(FirstLight.Game.Configs.DestructibleConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(45, "Configs/CustomAssetConfigs.asset", "Assets/AddressableResources/Configs/CustomAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.CustomAssetConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(46, "Configs/CurrencySpriteConfigs.asset", "Assets/AddressableResources/Configs/CurrencySpriteConfigs.asset", typeof(FirstLight.Game.Configs.CurrencySpriteConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(47, "Configs/ConsumableConfigs.asset", "Assets/AddressableResources/Configs/ConsumableConfigs.asset", typeof(FirstLight.Game.Configs.ConsumableConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(48, "Configs/ChestConfigs.asset", "Assets/AddressableResources/Configs/ChestConfigs.asset", typeof(FirstLight.Game.Configs.ChestConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(49, "Configs/BuffConfigs.asset", "Assets/AddressableResources/Configs/BuffConfigs.asset", typeof(FirstLight.Game.Configs.BuffConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(50, "Configs/BotDifficultyConfigs.asset", "Assets/AddressableResources/Configs/BotDifficultyConfigs.asset", typeof(FirstLight.Game.Configs.BotDifficultyConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(51, "Configs/BotConfigs.asset", "Assets/AddressableResources/Configs/BotConfigs.asset", typeof(FirstLight.Game.Configs.BotConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(52, "Configs/BattlePassConfigs.asset", "Assets/AddressableResources/Configs/BattlePassConfigs.asset", typeof(FirstLight.Game.Configs.BattlePassConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(53, "Configs/BaseEquipmentStatConfigs.asset", "Assets/AddressableResources/Configs/BaseEquipmentStatConfigs.asset", typeof(FirstLight.Game.Configs.BaseEquipmentStatConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(54, "Configs/AudioWeaponConfigs.asset", "Assets/AddressableResources/Configs/AudioWeaponConfigs.asset", typeof(FirstLight.Game.Configs.AudioWeaponConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(55, "Configs/AudioSharedAssetConfigs.asset", "Assets/AddressableResources/Configs/AudioSharedAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AudioSharedAssetConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(56, "Configs/AudioMixerConfigs.asset", "Assets/AddressableResources/Configs/AudioMixerConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AudioMixerConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(57, "Configs/AudioMatchAssetConfigs.asset", "Assets/AddressableResources/Configs/AudioMatchAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AudioMatchAssetConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(58, "Configs/AudioMainMenuAssetConfigs.asset", "Assets/AddressableResources/Configs/AudioMainMenuAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AudioMainMenuAssetConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(59, "Configs/AdventureAssetConfigs.asset", "Assets/AddressableResources/Configs/AdventureAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.MatchAssetConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(60, "Configs/AdjectiveDataConfigs.asset", "Assets/AddressableResources/Configs/AdjectiveDataConfigs.asset", typeof(FirstLight.Game.Configs.AdjectiveDataConfigs), new [] {"GenerateIds","Quantum"}),
					new AddressableConfig(61, "VFX/PooledVFXConfigs.asset", "Assets/AddressableResources/VFX/PooledVFXConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.PooledVFXConfigs), new [] {"GenerateIds","ScriptableObjectsOnly"}),
					new AddressableConfig(62, "Collections/WeaponSkins/Config.asset", "Assets/AddressableResources/Collections/WeaponSkins/Config.asset", typeof(FirstLight.Game.Configs.Collection.WeaponSkinsConfigContainer), new [] {"GenerateIds","ScriptableObjectsOnly"}),
					new AddressableConfig(63, "Collections/ProfilePicture/AvatarCollectableConfigs.asset", "Assets/AddressableResources/Collections/ProfilePicture/AvatarCollectableConfigs.asset", typeof(FirstLight.Game.Configs.AvatarCollectableConfigs), new [] {"GenerateIds","ScriptableObjectsOnly"}),
					new AddressableConfig(64, "Collections/Flags/FlagSkinConfigs.asset", "Assets/AddressableResources/Collections/Flags/FlagSkinConfigs.asset", typeof(FirstLight.Game.Configs.FlagSkinConfigs), new [] {"GenerateIds","ScriptableObjectsOnly"}),
				}.AsReadOnly()}
		});

		private static readonly IList<AddressableConfig> _addressableConfigs = new List<AddressableConfig>
		{
			new AddressableConfig(0, "Collections/Flags/FlagSkinConfigs.asset", "Assets/AddressableResources/Collections/Flags/FlagSkinConfigs.asset", typeof(FirstLight.Game.Configs.FlagSkinConfigs), new [] {"GenerateIds","ScriptableObjectsOnly"}),
			new AddressableConfig(1, "Collections/ProfilePicture/AvatarCollectableConfigs.asset", "Assets/AddressableResources/Collections/ProfilePicture/AvatarCollectableConfigs.asset", typeof(FirstLight.Game.Configs.AvatarCollectableConfigs), new [] {"GenerateIds","ScriptableObjectsOnly"}),
			new AddressableConfig(2, "Collections/WeaponSkins/Config.asset", "Assets/AddressableResources/Collections/WeaponSkins/Config.asset", typeof(FirstLight.Game.Configs.Collection.WeaponSkinsConfigContainer), new [] {"GenerateIds","ScriptableObjectsOnly"}),
			new AddressableConfig(3, "VFX/PooledVFXConfigs.asset", "Assets/AddressableResources/VFX/PooledVFXConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.PooledVFXConfigs), new [] {"GenerateIds","ScriptableObjectsOnly"}),
			new AddressableConfig(4, "Configs/AdjectiveDataConfigs.asset", "Assets/AddressableResources/Configs/AdjectiveDataConfigs.asset", typeof(FirstLight.Game.Configs.AdjectiveDataConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(5, "Configs/AdventureAssetConfigs.asset", "Assets/AddressableResources/Configs/AdventureAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.MatchAssetConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(6, "Configs/AudioMainMenuAssetConfigs.asset", "Assets/AddressableResources/Configs/AudioMainMenuAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AudioMainMenuAssetConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(7, "Configs/AudioMatchAssetConfigs.asset", "Assets/AddressableResources/Configs/AudioMatchAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AudioMatchAssetConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(8, "Configs/AudioMixerConfigs.asset", "Assets/AddressableResources/Configs/AudioMixerConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AudioMixerConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(9, "Configs/AudioSharedAssetConfigs.asset", "Assets/AddressableResources/Configs/AudioSharedAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AudioSharedAssetConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(10, "Configs/AudioWeaponConfigs.asset", "Assets/AddressableResources/Configs/AudioWeaponConfigs.asset", typeof(FirstLight.Game.Configs.AudioWeaponConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(11, "Configs/BaseEquipmentStatConfigs.asset", "Assets/AddressableResources/Configs/BaseEquipmentStatConfigs.asset", typeof(FirstLight.Game.Configs.BaseEquipmentStatConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(12, "Configs/BattlePassConfigs.asset", "Assets/AddressableResources/Configs/BattlePassConfigs.asset", typeof(FirstLight.Game.Configs.BattlePassConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(13, "Configs/BotConfigs.asset", "Assets/AddressableResources/Configs/BotConfigs.asset", typeof(FirstLight.Game.Configs.BotConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(14, "Configs/BotDifficultyConfigs.asset", "Assets/AddressableResources/Configs/BotDifficultyConfigs.asset", typeof(FirstLight.Game.Configs.BotDifficultyConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(15, "Configs/BuffConfigs.asset", "Assets/AddressableResources/Configs/BuffConfigs.asset", typeof(FirstLight.Game.Configs.BuffConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(16, "Configs/ChestConfigs.asset", "Assets/AddressableResources/Configs/ChestConfigs.asset", typeof(FirstLight.Game.Configs.ChestConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(17, "Configs/ConsumableConfigs.asset", "Assets/AddressableResources/Configs/ConsumableConfigs.asset", typeof(FirstLight.Game.Configs.ConsumableConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(18, "Configs/CurrencySpriteConfigs.asset", "Assets/AddressableResources/Configs/CurrencySpriteConfigs.asset", typeof(FirstLight.Game.Configs.CurrencySpriteConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(19, "Configs/CustomAssetConfigs.asset", "Assets/AddressableResources/Configs/CustomAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.CustomAssetConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(20, "Configs/DestructibleConfigs.asset", "Assets/AddressableResources/Configs/DestructibleConfigs.asset", typeof(FirstLight.Game.Configs.DestructibleConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(21, "Configs/DummyAssetConfigs.asset", "Assets/AddressableResources/Configs/DummyAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.DummyAssetConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(22, "Configs/EditionDataConfigs.asset", "Assets/AddressableResources/Configs/EditionDataConfigs.asset", typeof(FirstLight.Game.Configs.EditionDataConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(23, "Configs/EquipmentMaterialStatConfigs.asset", "Assets/AddressableResources/Configs/EquipmentMaterialStatConfigs.asset", typeof(FirstLight.Game.Configs.EquipmentMaterialStatConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(24, "Configs/EquipmentRewardConfigs.asset", "Assets/AddressableResources/Configs/EquipmentRewardConfigs.asset", typeof(FirstLight.Game.Configs.EquipmentRewardConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(25, "Configs/EquipmentStatConfigs.asset", "Assets/AddressableResources/Configs/EquipmentStatConfigs.asset", typeof(FirstLight.Game.Configs.EquipmentStatConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(26, "Configs/FactionDataConfigs.asset", "Assets/AddressableResources/Configs/FactionDataConfigs.asset", typeof(FirstLight.Game.Configs.FactionDataConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(27, "Configs/FuseConfigs.asset", "Assets/AddressableResources/Configs/FuseConfigs.asset", typeof(FirstLight.Game.Configs.FuseConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(28, "Configs/GameConfigs.asset", "Assets/AddressableResources/Configs/GameConfigs.asset", typeof(FirstLight.Game.Configs.GameConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(29, "Configs/GameModeConfigs.asset", "Assets/AddressableResources/Configs/GameModeConfigs.asset", typeof(FirstLight.Game.Configs.GameModeConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(30, "Configs/GradeDataConfigs.asset", "Assets/AddressableResources/Configs/GradeDataConfigs.asset", typeof(FirstLight.Game.Configs.GradeDataConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(31, "Configs/IndicatorVfxAssetConfigs.asset", "Assets/AddressableResources/Configs/IndicatorVfxAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.IndicatorVfxAssetConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(32, "Configs/LiveopsFeatureFlagConfigs.asset", "Assets/AddressableResources/Configs/LiveopsFeatureFlagConfigs.asset", typeof(FirstLight.Game.Configs.LiveopsFeatureFlagConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(33, "Configs/LiveopsSegmentActionConfigs.asset", "Assets/AddressableResources/Configs/LiveopsSegmentActionConfigs.asset", typeof(FirstLight.Game.Configs.LiveopsSegmentActionConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(34, "Configs/MainMenuAssetConfigs.asset", "Assets/AddressableResources/Configs/MainMenuAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.MainMenuAssetConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(35, "Configs/ManufacturerDataConfigs.asset", "Assets/AddressableResources/Configs/ManufacturerDataConfigs.asset", typeof(FirstLight.Game.Configs.ManufacturerDataConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(36, "Configs/MatchRewardConfigs.asset", "Assets/AddressableResources/Configs/MatchRewardConfigs.asset", typeof(FirstLight.Game.Configs.MatchRewardConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(37, "Configs/MatchmakingAndRoomConfigs.asset", "Assets/AddressableResources/Configs/MatchmakingAndRoomConfigs.asset", typeof(FirstLight.Game.Configs.MatchmakingAndRoomConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(38, "Configs/MaterialDataConfigs.asset", "Assets/AddressableResources/Configs/MaterialDataConfigs.asset", typeof(FirstLight.Game.Configs.MaterialDataConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(39, "Configs/MaterialVfxConfigs.asset", "Assets/AddressableResources/Configs/MaterialVfxConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.MaterialVfxConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(40, "Configs/PlayerLevelConfigs.asset", "Assets/AddressableResources/Configs/PlayerLevelConfigs.asset", typeof(FirstLight.Game.Configs.PlayerLevelConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(41, "Configs/QuantumMapConfigs.asset", "Assets/AddressableResources/Configs/QuantumMapConfigs.asset", typeof(FirstLight.Game.Configs.MapConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(42, "Configs/QuantumPrototypeAssetConfigs.asset", "Assets/AddressableResources/Configs/QuantumPrototypeAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.QuantumPrototypeAssetConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(43, "Configs/RarityDataConfigs.asset", "Assets/AddressableResources/Configs/RarityDataConfigs.asset", typeof(FirstLight.Game.Configs.RarityDataConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(44, "Configs/RepairDataConfigs.asset", "Assets/AddressableResources/Configs/RepairDataConfigs.asset", typeof(FirstLight.Game.Configs.RepairDataConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(45, "Configs/ResourcePoolConfigs.asset", "Assets/AddressableResources/Configs/ResourcePoolConfigs.asset", typeof(FirstLight.Game.Configs.ResourcePoolConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(46, "Configs/ReviveConfigs.asset", "Assets/AddressableResources/Configs/ReviveConfigs.asset", typeof(FirstLight.Game.Configs.ReviveConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(47, "Configs/ScrapConfigs.asset", "Assets/AddressableResources/Configs/ScrapConfigs.asset", typeof(FirstLight.Game.Configs.ScrapConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(48, "Configs/ShrinkingCircleConfigs.asset", "Assets/AddressableResources/Configs/ShrinkingCircleConfigs.asset", typeof(FirstLight.Game.Configs.ShrinkingCircleConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(49, "Configs/SpecialConfigs.asset", "Assets/AddressableResources/Configs/SpecialConfigs.asset", typeof(FirstLight.Game.Configs.SpecialConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(50, "Configs/SpecialMoveAssetConfigs.asset", "Assets/AddressableResources/Configs/SpecialMoveAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.SpecialMoveAssetConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(51, "Configs/SpriteAssetConfigs.asset", "Assets/AddressableResources/Configs/SpriteAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.SpriteAssetConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(52, "Configs/StatConfigs.asset", "Assets/AddressableResources/Configs/StatConfigs.asset", typeof(FirstLight.Game.Configs.StatConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(53, "Configs/TrophyRewardConfigs.asset", "Assets/AddressableResources/Configs/TrophyRewardConfigs.asset", typeof(FirstLight.Game.Configs.TrophyRewardConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(54, "Configs/TutorialConfigs.asset", "Assets/AddressableResources/Configs/TutorialConfigs.asset", typeof(FirstLight.Game.Configs.TutorialConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(55, "Configs/UpgradeDataConfigs.asset", "Assets/AddressableResources/Configs/UpgradeDataConfigs.asset", typeof(FirstLight.Game.Configs.UpgradeDataConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(56, "Configs/VideoAssetConfigs.asset", "Assets/AddressableResources/Configs/VideoAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.VideoAssetConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(57, "Configs/WeaponConfigs.asset", "Assets/AddressableResources/Configs/WeaponConfigs.asset", typeof(FirstLight.Game.Configs.WeaponConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(58, "Configs/Settings/AssetResources.asset", "Assets/AddressableResources/Configs/Settings/AssetResources.asset", typeof(Quantum.AssetResourceContainer), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(59, "Configs/Settings/Deterministic Config.asset", "Assets/AddressableResources/Configs/Settings/Deterministic Config.asset", typeof(DeterministicSessionConfigAsset), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(60, "Configs/Settings/PhotonServerSettings.asset", "Assets/AddressableResources/Configs/Settings/PhotonServerSettings.asset", typeof(PhotonServerSettings), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(61, "Configs/Settings/QuantumRunnerConfigs.asset", "Assets/AddressableResources/Configs/Settings/QuantumRunnerConfigs.asset", typeof(FirstLight.Game.Configs.QuantumRunnerConfigs), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(62, "Configs/Settings/SimulationConfig.asset", "Assets/AddressableResources/Configs/Settings/SimulationConfig.asset", typeof(SimulationConfigAsset), new [] {"GenerateIds","Quantum"}),
			new AddressableConfig(63, "Configs/MapAssetConfigs.asset", "Assets/AddressableResources/Maps/MapAssetsConfig.asset", typeof(FirstLight.Game.Configs.MapAssetConfigIndex), new [] {"GenerateIds","ScriptableObjectsOnly"}),
			new AddressableConfig(64, "Collections/CharacterSkins/Config.asset", "Assets/AddressableResources/Collections/CharacterSkins/Config.asset", typeof(FirstLight.Game.Configs.CharacterSkinConfigs), new [] {"GenerateIds","ScriptableObjectsOnly"})
		}.AsReadOnly();
	}
}

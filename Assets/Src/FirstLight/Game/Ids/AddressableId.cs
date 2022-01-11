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
		Configs_AdventureAssetConfigs,
		Configs_AudioAdventureAssetConfigs,
		Configs_AudioMainMenuAssetConfigs,
		Configs_AudioSharedAssetConfigs,
		Configs_BotConfigs,
		Configs_CardRarityAssetConfigs,
		Configs_ConsumableConfigs,
		Configs_CustomAssetConfigs,
		Configs_DestructibleConfigs,
		Configs_DummyAssetConfigs,
		Configs_GameConfigs,
		Configs_GearConfigs,
		Configs_HazardConfigs,
		Configs_IndicatorVfxAssetConfigs,
		Configs_LootBoxConfigs,
		Configs_MainMenuAssetConfigs,
		Configs_MapConfigs,
		Configs_MaterialVfxConfigs,
		Configs_PlayerLevelConfigs,
		Configs_PlayerRankAssetConfigs,
		Configs_PlayerSkinConfigs,
		Configs_ProjectileAssetConfigs,
		Configs_QuantumPrototypeAssetConfigs,
		Configs_RarityConfigs,
		Configs_SceneAssetConfigs,
		Configs_ShrinkingCircleConfigs,
		Configs_SpecialConfigs,
		Configs_SpecialMoveAssetConfigs,
		Configs_SpriteAssetConfigs,
		Configs_VfxConfigs,
		Configs_VideoAssetConfigs,
		Configs_WeaponConfigs,
		Configs_Settings_AssetResources,
		Configs_Settings_Deterministic_Config,
		Configs_Settings_PhotonServerSettings,
		Configs_Settings_QuantumRunnerConfigs,
		Configs_Settings_SimulationConfig,
		Configs_Settings_UiConfigs,
		Video_AssaultRifle,
		Video_Intro,
		Video_SniperRifle,
		Video_SpecialAggroBeaconGrenade,
		Video_SpecialAimingAirstrike,
		Video_SpecialAimingStunGrenade,
		Video_SpecialAirstrikeSimple,
		Video_SpecialHealingField,
		Video_SpecialHealingMode,
		Video_SpecialInvisibilitySelf,
		Video_SpecialRageSelf,
		Video_SpecialShieldedCharge,
		Video_SpecialShieldSelf,
		Video_SpecialSkyLaserBeam,
		Timeline_FtueTimeline
	}

	public enum AddressableLabel
	{
		Label_Quantum
	}

	public static class AddressablePathLookup
	{
		public static readonly string Timeline = "Timeline";
		public static readonly string Video = "Video";
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
			"Quantum"
		}.AsReadOnly();

		private static readonly IReadOnlyDictionary<string, IList<AddressableConfig>> _addressableLabelMap = new ReadOnlyDictionary<string, IList<AddressableConfig>>(new Dictionary<string, IList<AddressableConfig>>
		{
			{"Quantum", new List<AddressableConfig>
				{
					new AddressableConfig(0, "QuantumAssets/Spawners/WeaponPlatformSpawner.prefab", "Assets/AddressableResources/QuantumAssets/Spawners/WeaponPlatformSpawner.prefab", typeof(UnityEngine.GameObject), new [] {"Quantum","Ignore"}),
					new AddressableConfig(1, "QuantumAssets/Spawners/ConsumablePlatformSpawner.prefab", "Assets/AddressableResources/QuantumAssets/Spawners/ConsumablePlatformSpawner.prefab", typeof(UnityEngine.GameObject), new [] {"Quantum","Ignore"}),
					new AddressableConfig(2, "QuantumAssets/Projectiles/PlayerBullet.prefab", "Assets/AddressableResources/QuantumAssets/Projectiles/PlayerBullet.prefab", typeof(UnityEngine.GameObject), new [] {"Quantum","Ignore"}),
					new AddressableConfig(3, "QuantumAssets/Physics Config Assets/Player CharacterController.asset", "Assets/AddressableResources/QuantumAssets/Physics Config Assets/Player CharacterController.asset", typeof(CharacterController3DConfigAsset), new [] {"Quantum","Ignore"}),
					new AddressableConfig(4, "QuantumAssets/Physics Config Assets/Bot NavMeshAgentConfig.asset", "Assets/AddressableResources/QuantumAssets/Physics Config Assets/Bot NavMeshAgentConfig.asset", typeof(NavMeshAgentConfigAsset), new [] {"Quantum","Ignore"}),
					new AddressableConfig(5, "QuantumAssets/Hazards/Hazard.prefab", "Assets/AddressableResources/QuantumAssets/Hazards/Hazard.prefab", typeof(UnityEngine.GameObject), new [] {"Quantum","Ignore"}),
					new AddressableConfig(6, "QuantumAssets/Destructibles/Barrel.prefab", "Assets/AddressableResources/QuantumAssets/Destructibles/Barrel.prefab", typeof(UnityEngine.GameObject), new [] {"Quantum","Ignore"}),
					new AddressableConfig(7, "QuantumAssets/Default Config Assets/Default PhysicsMaterialAsset.asset", "Assets/AddressableResources/QuantumAssets/Default Config Assets/Default PhysicsMaterialAsset.asset", typeof(PhysicsMaterialAsset), new [] {"Quantum","Ignore"}),
					new AddressableConfig(8, "QuantumAssets/Default Config Assets/Default NavMeshAgentConfigAsset.asset", "Assets/AddressableResources/QuantumAssets/Default Config Assets/Default NavMeshAgentConfigAsset.asset", typeof(NavMeshAgentConfigAsset), new [] {"Quantum","Ignore"}),
					new AddressableConfig(9, "QuantumAssets/Default Config Assets/Default CharacterController3DConfigAsset.asset", "Assets/AddressableResources/QuantumAssets/Default Config Assets/Default CharacterController3DConfigAsset.asset", typeof(CharacterController3DConfigAsset), new [] {"Quantum","Ignore"}),
					new AddressableConfig(10, "QuantumAssets/Default Config Assets/Default CharacterController2DConfigAsset.asset", "Assets/AddressableResources/QuantumAssets/Default Config Assets/Default CharacterController2DConfigAsset.asset", typeof(CharacterController2DConfigAsset), new [] {"Quantum","Ignore"}),
					new AddressableConfig(11, "QuantumAssets/Collectables/WeaponPickup.prefab", "Assets/AddressableResources/QuantumAssets/Collectables/WeaponPickup.prefab", typeof(UnityEngine.GameObject), new [] {"Quantum","Ignore"}),
					new AddressableConfig(12, "QuantumAssets/Collectables/ConsumablePickup.prefab", "Assets/AddressableResources/QuantumAssets/Collectables/ConsumablePickup.prefab", typeof(UnityEngine.GameObject), new [] {"Quantum","Ignore"}),
					new AddressableConfig(13, "QuantumAssets/CircuitExport/HFSM_Assets/Player.asset", "Assets/AddressableResources/QuantumAssets/CircuitExport/HFSM_Assets/Player.asset", typeof(HFSMRootAsset), new [] {"Quantum","Ignore"}),
					new AddressableConfig(14, "QuantumAssets/CircuitExport/HFSM_Assets/Player Shapes.asset", "Assets/AddressableResources/QuantumAssets/CircuitExport/HFSM_Assets/Player Shapes.asset", typeof(HFSMRootAsset), new [] {"Quantum","Ignore"}),
					new AddressableConfig(15, "QuantumAssets/CircuitExport/Blackboard_Assets/PlayerBlackboardInitializer.asset", "Assets/AddressableResources/QuantumAssets/CircuitExport/Blackboard_Assets/PlayerBlackboardInitializer.asset", typeof(AIBlackboardInitializerAsset), new [] {"Quantum","Ignore"}),
					new AddressableConfig(16, "QuantumAssets/CircuitExport/Blackboard_Assets/PlayerBlackboard.asset", "Assets/AddressableResources/QuantumAssets/CircuitExport/Blackboard_Assets/PlayerBlackboard.asset", typeof(AIBlackboardAsset), new [] {"Quantum","Ignore"}),
					new AddressableConfig(17, "QuantumAssets/CircuitExport/Blackboard_Assets/Player ShapesBlackboardInitializer.asset", "Assets/AddressableResources/QuantumAssets/CircuitExport/Blackboard_Assets/Player ShapesBlackboardInitializer.asset", typeof(AIBlackboardInitializerAsset), new [] {"Quantum","Ignore"}),
					new AddressableConfig(18, "QuantumAssets/CircuitExport/Blackboard_Assets/Player ShapesBlackboard.asset", "Assets/AddressableResources/QuantumAssets/CircuitExport/Blackboard_Assets/Player ShapesBlackboard.asset", typeof(AIBlackboardAsset), new [] {"Quantum","Ignore"}),
					new AddressableConfig(19, "QuantumAssets/Characters/PlayerCharacter.prefab", "Assets/AddressableResources/QuantumAssets/Characters/PlayerCharacter.prefab", typeof(UnityEngine.GameObject), new [] {"Quantum","Ignore"}),
					new AddressableConfig(20, "QuantumAssets/Characters/DummyCharacter.prefab", "Assets/AddressableResources/QuantumAssets/Characters/DummyCharacter.prefab", typeof(UnityEngine.GameObject), new [] {"Quantum","Ignore"}),
					new AddressableConfig(21, "Configs/Settings/UiConfigs.asset", "Assets/AddressableResources/Configs/Settings/UiConfigs.asset", typeof(FirstLight.UiService.UiConfigs), new [] {"Quantum"}),
					new AddressableConfig(22, "Configs/Settings/SimulationConfig.asset", "Assets/AddressableResources/Configs/Settings/SimulationConfig.asset", typeof(SimulationConfigAsset), new [] {"Quantum"}),
					new AddressableConfig(23, "Configs/Settings/QuantumRunnerConfigs.asset", "Assets/AddressableResources/Configs/Settings/QuantumRunnerConfigs.asset", typeof(FirstLight.Game.Configs.QuantumRunnerConfigs), new [] {"Quantum"}),
					new AddressableConfig(24, "Configs/Settings/PhotonServerSettings.asset", "Assets/AddressableResources/Configs/Settings/PhotonServerSettings.asset", typeof(PhotonServerSettings), new [] {"Quantum"}),
					new AddressableConfig(25, "Configs/Settings/Deterministic Config.asset", "Assets/AddressableResources/Configs/Settings/Deterministic Config.asset", typeof(DeterministicSessionConfigAsset), new [] {"Quantum"}),
					new AddressableConfig(26, "Configs/Settings/AssetResources.asset", "Assets/AddressableResources/Configs/Settings/AssetResources.asset", typeof(Quantum.AssetResourceContainer), new [] {"Quantum"}),
					new AddressableConfig(27, "Configs/WeaponConfigs.asset", "Assets/AddressableResources/Configs/WeaponConfigs.asset", typeof(FirstLight.Game.Configs.WeaponConfigs), new [] {"Quantum"}),
					new AddressableConfig(28, "Configs/VideoAssetConfigs.asset", "Assets/AddressableResources/Configs/VideoAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.VideoAssetConfigs), new [] {"Quantum"}),
					new AddressableConfig(29, "Configs/VfxConfigs.asset", "Assets/AddressableResources/Configs/VfxConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.VfxAssetConfigs), new [] {"Quantum"}),
					new AddressableConfig(30, "Configs/SpriteAssetConfigs.asset", "Assets/AddressableResources/Configs/SpriteAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.SpriteAssetConfigs), new [] {"Quantum"}),
					new AddressableConfig(31, "Configs/SpecialMoveAssetConfigs.asset", "Assets/AddressableResources/Configs/SpecialMoveAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.SpecialMoveAssetConfigs), new [] {"Quantum"}),
					new AddressableConfig(32, "Configs/SpecialConfigs.asset", "Assets/AddressableResources/Configs/SpecialConfigs.asset", typeof(FirstLight.Game.Configs.SpecialConfigs), new [] {"Quantum"}),
					new AddressableConfig(33, "Configs/ShrinkingCircleConfigs.asset", "Assets/AddressableResources/Configs/ShrinkingCircleConfigs.asset", typeof(FirstLight.Game.Configs.ShrinkingCircleConfigs), new [] {"Quantum"}),
					new AddressableConfig(34, "Configs/SceneAssetConfigs.asset", "Assets/AddressableResources/Configs/SceneAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.SceneAssetConfigs), new [] {"Quantum"}),
					new AddressableConfig(35, "Configs/RarityConfigs.asset", "Assets/AddressableResources/Configs/RarityConfigs.asset", typeof(FirstLight.Game.Configs.RarityConfigs), new [] {"Quantum"}),
					new AddressableConfig(36, "Configs/QuantumPrototypeAssetConfigs.asset", "Assets/AddressableResources/Configs/QuantumPrototypeAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.QuantumPrototypeAssetConfigs), new [] {"Quantum"}),
					new AddressableConfig(37, "Configs/ProjectileAssetConfigs.asset", "Assets/AddressableResources/Configs/ProjectileAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.ProjectileAssetConfigs), new [] {"Quantum"}),
					new AddressableConfig(38, "Configs/PlayerSkinConfigs.asset", "Assets/AddressableResources/Configs/PlayerSkinConfigs.asset", typeof(FirstLight.Game.Configs.PlayerSkinConfigs), new [] {"Quantum"}),
					new AddressableConfig(39, "Configs/PlayerRankAssetConfigs.asset", "Assets/AddressableResources/Configs/PlayerRankAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.PlayerRankAssetConfigs), new [] {"Quantum"}),
					new AddressableConfig(40, "Configs/PlayerLevelConfigs.asset", "Assets/AddressableResources/Configs/PlayerLevelConfigs.asset", typeof(FirstLight.Game.Configs.PlayerLevelConfigs), new [] {"Quantum"}),
					new AddressableConfig(41, "Configs/MaterialVfxConfigs.asset", "Assets/AddressableResources/Configs/MaterialVfxConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.MaterialVfxConfigs), new [] {"Quantum"}),
					new AddressableConfig(42, "Configs/MapConfigs.asset", "Assets/AddressableResources/Configs/MapConfigs.asset", typeof(FirstLight.Game.Configs.MapConfigs), new [] {"Quantum"}),
					new AddressableConfig(43, "Configs/MainMenuAssetConfigs.asset", "Assets/AddressableResources/Configs/MainMenuAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.MainMenuAssetConfigs), new [] {"Quantum"}),
					new AddressableConfig(44, "Configs/LootBoxConfigs.asset", "Assets/AddressableResources/Configs/LootBoxConfigs.asset", typeof(FirstLight.Game.Configs.LootBoxConfigs), new [] {"Quantum"}),
					new AddressableConfig(45, "Configs/IndicatorVfxAssetConfigs.asset", "Assets/AddressableResources/Configs/IndicatorVfxAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.IndicatorVfxAssetConfigs), new [] {"Quantum"}),
					new AddressableConfig(46, "Configs/HazardConfigs.asset", "Assets/AddressableResources/Configs/HazardConfigs.asset", typeof(FirstLight.Game.Configs.HazardConfigs), new [] {"Quantum"}),
					new AddressableConfig(47, "Configs/GearConfigs.asset", "Assets/AddressableResources/Configs/GearConfigs.asset", typeof(FirstLight.Game.Configs.GearConfigs), new [] {"Quantum"}),
					new AddressableConfig(48, "Configs/GameConfigs.asset", "Assets/AddressableResources/Configs/GameConfigs.asset", typeof(FirstLight.Game.Configs.GameConfigs), new [] {"Quantum"}),
					new AddressableConfig(49, "Configs/DummyAssetConfigs.asset", "Assets/AddressableResources/Configs/DummyAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.DummyAssetConfigs), new [] {"Quantum"}),
					new AddressableConfig(50, "Configs/DestructibleConfigs.asset", "Assets/AddressableResources/Configs/DestructibleConfigs.asset", typeof(FirstLight.Game.Configs.DestructibleConfigs), new [] {"Quantum"}),
					new AddressableConfig(51, "Configs/CustomAssetConfigs.asset", "Assets/AddressableResources/Configs/CustomAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.CustomAssetConfigs), new [] {"Quantum"}),
					new AddressableConfig(52, "Configs/ConsumableConfigs.asset", "Assets/AddressableResources/Configs/ConsumableConfigs.asset", typeof(FirstLight.Game.Configs.ConsumableConfigs), new [] {"Quantum"}),
					new AddressableConfig(53, "Configs/CardRarityAssetConfigs.asset", "Assets/AddressableResources/Configs/CardRarityAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.CardRarityAssetConfigs), new [] {"Quantum"}),
					new AddressableConfig(54, "Configs/BotConfigs.asset", "Assets/AddressableResources/Configs/BotConfigs.asset", typeof(FirstLight.Game.Configs.BotConfigs), new [] {"Quantum"}),
					new AddressableConfig(55, "Configs/AudioSharedAssetConfigs.asset", "Assets/AddressableResources/Configs/AudioSharedAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AudioSharedAssetConfigs), new [] {"Quantum"}),
					new AddressableConfig(56, "Configs/AudioMainMenuAssetConfigs.asset", "Assets/AddressableResources/Configs/AudioMainMenuAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AudioMainMenuAssetConfigs), new [] {"Quantum"}),
					new AddressableConfig(57, "Configs/AudioAdventureAssetConfigs.asset", "Assets/AddressableResources/Configs/AudioAdventureAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AudioAdventureAssetConfigs), new [] {"Quantum"}),
					new AddressableConfig(58, "Configs/AdventureAssetConfigs.asset", "Assets/AddressableResources/Configs/AdventureAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AdventureAssetConfigs), new [] {"Quantum"}),
				}.AsReadOnly()}
		});

		private static readonly IList<AddressableConfig> _addressableConfigs = new List<AddressableConfig>
		{
			new AddressableConfig(0, "Configs/AdventureAssetConfigs.asset", "Assets/AddressableResources/Configs/AdventureAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AdventureAssetConfigs), new [] {"Quantum"}),
			new AddressableConfig(1, "Configs/AudioAdventureAssetConfigs.asset", "Assets/AddressableResources/Configs/AudioAdventureAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AudioAdventureAssetConfigs), new [] {"Quantum"}),
			new AddressableConfig(2, "Configs/AudioMainMenuAssetConfigs.asset", "Assets/AddressableResources/Configs/AudioMainMenuAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AudioMainMenuAssetConfigs), new [] {"Quantum"}),
			new AddressableConfig(3, "Configs/AudioSharedAssetConfigs.asset", "Assets/AddressableResources/Configs/AudioSharedAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.AudioSharedAssetConfigs), new [] {"Quantum"}),
			new AddressableConfig(4, "Configs/BotConfigs.asset", "Assets/AddressableResources/Configs/BotConfigs.asset", typeof(FirstLight.Game.Configs.BotConfigs), new [] {"Quantum"}),
			new AddressableConfig(5, "Configs/CardRarityAssetConfigs.asset", "Assets/AddressableResources/Configs/CardRarityAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.CardRarityAssetConfigs), new [] {"Quantum"}),
			new AddressableConfig(6, "Configs/ConsumableConfigs.asset", "Assets/AddressableResources/Configs/ConsumableConfigs.asset", typeof(FirstLight.Game.Configs.ConsumableConfigs), new [] {"Quantum"}),
			new AddressableConfig(7, "Configs/CustomAssetConfigs.asset", "Assets/AddressableResources/Configs/CustomAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.CustomAssetConfigs), new [] {"Quantum"}),
			new AddressableConfig(8, "Configs/DestructibleConfigs.asset", "Assets/AddressableResources/Configs/DestructibleConfigs.asset", typeof(FirstLight.Game.Configs.DestructibleConfigs), new [] {"Quantum"}),
			new AddressableConfig(9, "Configs/DummyAssetConfigs.asset", "Assets/AddressableResources/Configs/DummyAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.DummyAssetConfigs), new [] {"Quantum"}),
			new AddressableConfig(10, "Configs/GameConfigs.asset", "Assets/AddressableResources/Configs/GameConfigs.asset", typeof(FirstLight.Game.Configs.GameConfigs), new [] {"Quantum"}),
			new AddressableConfig(11, "Configs/GearConfigs.asset", "Assets/AddressableResources/Configs/GearConfigs.asset", typeof(FirstLight.Game.Configs.GearConfigs), new [] {"Quantum"}),
			new AddressableConfig(12, "Configs/HazardConfigs.asset", "Assets/AddressableResources/Configs/HazardConfigs.asset", typeof(FirstLight.Game.Configs.HazardConfigs), new [] {"Quantum"}),
			new AddressableConfig(13, "Configs/IndicatorVfxAssetConfigs.asset", "Assets/AddressableResources/Configs/IndicatorVfxAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.IndicatorVfxAssetConfigs), new [] {"Quantum"}),
			new AddressableConfig(14, "Configs/LootBoxConfigs.asset", "Assets/AddressableResources/Configs/LootBoxConfigs.asset", typeof(FirstLight.Game.Configs.LootBoxConfigs), new [] {"Quantum"}),
			new AddressableConfig(15, "Configs/MainMenuAssetConfigs.asset", "Assets/AddressableResources/Configs/MainMenuAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.MainMenuAssetConfigs), new [] {"Quantum"}),
			new AddressableConfig(16, "Configs/MapConfigs.asset", "Assets/AddressableResources/Configs/MapConfigs.asset", typeof(FirstLight.Game.Configs.MapConfigs), new [] {"Quantum"}),
			new AddressableConfig(17, "Configs/MaterialVfxConfigs.asset", "Assets/AddressableResources/Configs/MaterialVfxConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.MaterialVfxConfigs), new [] {"Quantum"}),
			new AddressableConfig(18, "Configs/PlayerLevelConfigs.asset", "Assets/AddressableResources/Configs/PlayerLevelConfigs.asset", typeof(FirstLight.Game.Configs.PlayerLevelConfigs), new [] {"Quantum"}),
			new AddressableConfig(19, "Configs/PlayerRankAssetConfigs.asset", "Assets/AddressableResources/Configs/PlayerRankAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.PlayerRankAssetConfigs), new [] {"Quantum"}),
			new AddressableConfig(20, "Configs/PlayerSkinConfigs.asset", "Assets/AddressableResources/Configs/PlayerSkinConfigs.asset", typeof(FirstLight.Game.Configs.PlayerSkinConfigs), new [] {"Quantum"}),
			new AddressableConfig(21, "Configs/ProjectileAssetConfigs.asset", "Assets/AddressableResources/Configs/ProjectileAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.ProjectileAssetConfigs), new [] {"Quantum"}),
			new AddressableConfig(22, "Configs/QuantumPrototypeAssetConfigs.asset", "Assets/AddressableResources/Configs/QuantumPrototypeAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.QuantumPrototypeAssetConfigs), new [] {"Quantum"}),
			new AddressableConfig(23, "Configs/RarityConfigs.asset", "Assets/AddressableResources/Configs/RarityConfigs.asset", typeof(FirstLight.Game.Configs.RarityConfigs), new [] {"Quantum"}),
			new AddressableConfig(24, "Configs/SceneAssetConfigs.asset", "Assets/AddressableResources/Configs/SceneAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.SceneAssetConfigs), new [] {"Quantum"}),
			new AddressableConfig(25, "Configs/ShrinkingCircleConfigs.asset", "Assets/AddressableResources/Configs/ShrinkingCircleConfigs.asset", typeof(FirstLight.Game.Configs.ShrinkingCircleConfigs), new [] {"Quantum"}),
			new AddressableConfig(26, "Configs/SpecialConfigs.asset", "Assets/AddressableResources/Configs/SpecialConfigs.asset", typeof(FirstLight.Game.Configs.SpecialConfigs), new [] {"Quantum"}),
			new AddressableConfig(27, "Configs/SpecialMoveAssetConfigs.asset", "Assets/AddressableResources/Configs/SpecialMoveAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.SpecialMoveAssetConfigs), new [] {"Quantum"}),
			new AddressableConfig(28, "Configs/SpriteAssetConfigs.asset", "Assets/AddressableResources/Configs/SpriteAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.SpriteAssetConfigs), new [] {"Quantum"}),
			new AddressableConfig(29, "Configs/VfxConfigs.asset", "Assets/AddressableResources/Configs/VfxConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.VfxAssetConfigs), new [] {"Quantum"}),
			new AddressableConfig(30, "Configs/VideoAssetConfigs.asset", "Assets/AddressableResources/Configs/VideoAssetConfigs.asset", typeof(FirstLight.Game.Configs.AssetConfigs.VideoAssetConfigs), new [] {"Quantum"}),
			new AddressableConfig(31, "Configs/WeaponConfigs.asset", "Assets/AddressableResources/Configs/WeaponConfigs.asset", typeof(FirstLight.Game.Configs.WeaponConfigs), new [] {"Quantum"}),
			new AddressableConfig(32, "Configs/Settings/AssetResources.asset", "Assets/AddressableResources/Configs/Settings/AssetResources.asset", typeof(Quantum.AssetResourceContainer), new [] {"Quantum"}),
			new AddressableConfig(33, "Configs/Settings/Deterministic Config.asset", "Assets/AddressableResources/Configs/Settings/Deterministic Config.asset", typeof(DeterministicSessionConfigAsset), new [] {"Quantum"}),
			new AddressableConfig(34, "Configs/Settings/PhotonServerSettings.asset", "Assets/AddressableResources/Configs/Settings/PhotonServerSettings.asset", typeof(PhotonServerSettings), new [] {"Quantum"}),
			new AddressableConfig(35, "Configs/Settings/QuantumRunnerConfigs.asset", "Assets/AddressableResources/Configs/Settings/QuantumRunnerConfigs.asset", typeof(FirstLight.Game.Configs.QuantumRunnerConfigs), new [] {"Quantum"}),
			new AddressableConfig(36, "Configs/Settings/SimulationConfig.asset", "Assets/AddressableResources/Configs/Settings/SimulationConfig.asset", typeof(SimulationConfigAsset), new [] {"Quantum"}),
			new AddressableConfig(37, "Configs/Settings/UiConfigs.asset", "Assets/AddressableResources/Configs/Settings/UiConfigs.asset", typeof(FirstLight.UiService.UiConfigs), new [] {"Quantum"}),
			new AddressableConfig(38, "Video/AssaultRifle.mp4", "Assets/AddressableResources/Video/AssaultRifle.mp4", typeof(UnityEngine.Video.VideoClip), new [] {""}),
			new AddressableConfig(39, "Video/Intro.mp4", "Assets/AddressableResources/Video/Intro.mp4", typeof(UnityEngine.Video.VideoClip), new [] {""}),
			new AddressableConfig(40, "Video/SniperRifle.mp4", "Assets/AddressableResources/Video/SniperRifle.mp4", typeof(UnityEngine.Video.VideoClip), new [] {""}),
			new AddressableConfig(41, "Video/SpecialAggroBeaconGrenade.mp4", "Assets/AddressableResources/Video/SpecialAggroBeaconGrenade.mp4", typeof(UnityEngine.Video.VideoClip), new [] {""}),
			new AddressableConfig(42, "Video/SpecialAimingAirstrike.mp4", "Assets/AddressableResources/Video/SpecialAimingAirstrike.mp4", typeof(UnityEngine.Video.VideoClip), new [] {""}),
			new AddressableConfig(43, "Video/SpecialAimingStunGrenade.mp4", "Assets/AddressableResources/Video/SpecialAimingStunGrenade.mp4", typeof(UnityEngine.Video.VideoClip), new [] {""}),
			new AddressableConfig(44, "Video/SpecialAirstrikeSimple.mp4", "Assets/AddressableResources/Video/SpecialAirstrikeSimple.mp4", typeof(UnityEngine.Video.VideoClip), new [] {""}),
			new AddressableConfig(45, "Video/SpecialHealingField.mp4", "Assets/AddressableResources/Video/SpecialHealingField.mp4", typeof(UnityEngine.Video.VideoClip), new [] {""}),
			new AddressableConfig(46, "Video/SpecialHealingMode.mp4", "Assets/AddressableResources/Video/SpecialHealingMode.mp4", typeof(UnityEngine.Video.VideoClip), new [] {""}),
			new AddressableConfig(47, "Video/SpecialInvisibilitySelf.mp4", "Assets/AddressableResources/Video/SpecialInvisibilitySelf.mp4", typeof(UnityEngine.Video.VideoClip), new [] {""}),
			new AddressableConfig(48, "Video/SpecialRageSelf.mp4", "Assets/AddressableResources/Video/SpecialRageSelf.mp4", typeof(UnityEngine.Video.VideoClip), new [] {""}),
			new AddressableConfig(49, "Video/SpecialShieldedCharge.mp4", "Assets/AddressableResources/Video/SpecialShieldedCharge.mp4", typeof(UnityEngine.Video.VideoClip), new [] {""}),
			new AddressableConfig(50, "Video/SpecialShieldSelf.mp4", "Assets/AddressableResources/Video/SpecialShieldSelf.mp4", typeof(UnityEngine.Video.VideoClip), new [] {""}),
			new AddressableConfig(51, "Video/SpecialSkyLaserBeam.mp4", "Assets/AddressableResources/Video/SpecialSkyLaserBeam.mp4", typeof(UnityEngine.Video.VideoClip), new [] {""}),
			new AddressableConfig(52, "Timeline/FtueTimeline.prefab", "Assets/AddressableResources/Timeline/FtueTimeline.prefab", typeof(UnityEngine.GameObject), new [] {""})
		}.AsReadOnly();
	}
}

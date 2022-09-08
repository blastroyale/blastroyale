using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;

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
		IEnumerable<Task> LoadConfigTasks(IConfigsAdder cfg);
		
		/// <summary>
		/// Loads a specific config using the given asset resolver. 
		/// </summary>
		Task LoadConfig<TContainer>(AddressableId id, Action<TContainer> onLoadComplete);
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
		
		public IEnumerable<Task> LoadConfigTasks(IConfigsAdder configsAdder)
		{
			return new List<Task>
			{
				LoadConfig<GameConfigs>(AddressableId.Configs_GameConfigs, asset => configsAdder.AddSingletonConfig(asset.Config)),
				LoadConfig<MapGridConfigs>(AddressableId.Configs_MapGridConfigs, asset => configsAdder.AddSingletonConfig(asset)),
				LoadConfig<MapConfigs>(AddressableId.Configs_MapConfigs, asset => configsAdder.AddConfigs(data => (int) data.Map, asset.Configs)),
				LoadConfig<WeaponConfigs>(AddressableId.Configs_WeaponConfigs, asset => configsAdder.AddConfigs(data => (int) data.Id, asset.Configs)),
				LoadConfig<PlayerLevelConfigs>(AddressableId.Configs_PlayerLevelConfigs, asset => configsAdder.AddConfigs(data => (int) data.Level, asset.Configs)),
				LoadConfig<SpecialConfigs>(AddressableId.Configs_SpecialConfigs, asset => configsAdder.AddConfigs(data => (int) data.Id, asset.Configs)),
				LoadConfig<ConsumableConfigs>(AddressableId.Configs_ConsumableConfigs, asset => configsAdder.AddConfigs(data => (int) data.Id, asset.Configs)),
				LoadConfig<DestructibleConfigs>(AddressableId.Configs_DestructibleConfigs, asset => configsAdder.AddConfigs(data => (int) data.Id, asset.Configs)),
				LoadConfig<ShrinkingCircleConfigs>(AddressableId.Configs_ShrinkingCircleConfigs, asset => configsAdder.AddConfigs(data => data.Step, asset.Configs)),
				LoadConfig<ResourcePoolConfigs>(AddressableId.Configs_ResourcePoolConfigs, asset => configsAdder.AddConfigs(data => (int)data.Id, asset.Configs)),
				LoadConfig<AudioWeaponConfigs>(AddressableId.Configs_AudioWeaponConfigs, asset => configsAdder.AddConfigs(data => (int)data.GameId, asset.Configs)),
				LoadConfig<MatchRewardConfigs>(AddressableId.Configs_MatchRewardConfigs, asset => configsAdder.AddConfigs(data => data.Placement, asset.Configs)),
				LoadConfig<ChestConfigs>(AddressableId.Configs_ChestConfigs, asset => configsAdder.AddConfigs(data => (int) data.Id, asset.Configs)),
				LoadConfig<EquipmentStatConfigs>(AddressableId.Configs_EquipmentStatConfigs, asset => configsAdder.AddConfigs(data => data.GetKey(), asset.Configs)),
				LoadConfig<EquipmentMaterialStatConfigs>(AddressableId.Configs_EquipmentMaterialStatConfigs, asset => configsAdder.AddConfigs(data => data.GetKey(), asset.Configs)),
				LoadConfig<BaseEquipmentStatConfigs>(AddressableId.Configs_BaseEquipmentStatConfigs, asset => configsAdder.AddConfigs(data => (int) data.Id, asset.Configs)),
				LoadConfig<StatConfigs>(AddressableId.Configs_StatConfigs, asset => configsAdder.AddConfigs(data => (int) data.StatType, asset.Configs)),
				LoadConfig<GraphicsConfig>(AddressableId.Configs_GraphicsConfig, asset => configsAdder.AddSingletonConfig(asset)),
				LoadConfig<RarityDataConfigs>(AddressableId.Configs_RarityDataConfigs, asset => configsAdder.AddConfigs(data => (int)data.Rarity, asset.Configs)),
				LoadConfig<AdjectiveDataConfigs>(AddressableId.Configs_AdjectiveDataConfigs, asset => configsAdder.AddConfigs(data => (int)data.Adjective, asset.Configs)),
				LoadConfig<GradeDataConfigs>(AddressableId.Configs_GradeDataConfigs, asset => configsAdder.AddConfigs(data => (int)data.Grade, asset.Configs)),
				LoadConfig<GameModeConfigs>(AddressableId.Configs_GameModeConfigs, asset => configsAdder.AddConfigs(data => data.Id.GetHashCode(), asset.Configs)),
				LoadConfig<GameModeRotationConfigs>(AddressableId.Configs_GameModeRotationConfigs, asset => configsAdder.AddSingletonConfig(asset.Config)),
			};
		}
	
		public async Task LoadConfig<TContainer>(AddressableId id, Action<TContainer> onLoadComplete)
		{
			var asset = await _assetLoader.LoadAssetAsync<TContainer>(id.GetConfig().Address);
			onLoadComplete(asset);
			_assetLoader.UnloadAsset(asset);
		}
	}
}

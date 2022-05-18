using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;

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
		
		public IEnumerable<Task> LoadConfigTasks(IConfigsAdder _configsAdder)
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
				LoadConfig<ResourcePoolConfigs>(AddressableId.Configs_ResourcePoolConfigs, asset => _configsAdder.AddConfigs(data => (int)data.Id, asset.Configs)),
				LoadConfig<MatchRewardConfigs>(AddressableId.Configs_MatchRewardConfigs, asset => _configsAdder.AddConfigs(data => (int)data.Id, asset.Configs))
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
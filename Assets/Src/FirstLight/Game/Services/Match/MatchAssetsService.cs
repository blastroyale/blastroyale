using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Game.Services.RoomService;
using FirstLight.Game.Utils;
using Photon.Realtime;
using Quantum;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Responsible for asset loading before the match
	/// </summary>
	public interface IMatchAssetsService
	{
		/// <summary>
		/// Loads assets that are mandatory to the game to boot.
		/// The simulation can only start after this assets are ready.
		/// This can be loaded in background during matchmaking, just need to ensure
		/// all players loaded before starting the game
		/// </summary>
		void StartMandatoryAssetLoad();

		/// <summary>
		/// This method will start loading the optional assets, and hopefully the game starts when all are loaded.
		/// Not loading all optional assets is just fine, and starting the game while one asset is in the middle
		/// of the load is also ok.
		/// In case they are not loaded the game will run just fine and assets will be loaded during the game.
		/// </summary>
		void StartOptionalAssetLoad();

		/// <summary>
		/// Unloads all loaded assets
		/// </summary>
		Task UnloadAllMatchAssets();

		/// <summary>
		/// Waits for all mandatory assets to have completed loading
		/// </summary>
		Task WaitMandatoryComplete();
	}

	public class MatchAssetsService : IMatchAssetsService, MatchServices.IMatchService
	{
		private readonly IGameServices _services;
		private readonly IAssetAdderService _assetAdderService;
		private readonly IGameDataProvider _data;
		private readonly AsyncTaskTracker _mandatoryAssets;
		private readonly AsyncTaskTracker _optionalAssets;

		public MatchAssetsService()
		{
			_services = MainInstaller.ResolveServices();
			_assetAdderService = _services.AssetResolverService as IAssetAdderService;
			_data = MainInstaller.Resolve<IGameDataProvider>();
			_mandatoryAssets = new AsyncTaskTracker();
			_optionalAssets = new AsyncTaskTracker();
			_services.RoomService.OnPlayersChange += OnRoomPlayersChange;
		}


		public void StartOptionalAssetLoad()
		{
			var localPlayerLoadout = _services.RoomService.CurrentRoom.LocalPlayerProperties.Loadout.Value;
			if (localPlayerLoadout != null)
			{
				LoadGameIds(localPlayerLoadout);
			}

			_optionalAssets.Add(_assetAdderService.LoadAllAssets<MaterialVfxId, GameObject>());
			_optionalAssets.Add(_assetAdderService.LoadAllAssets<IndicatorVfxId, GameObject>());
			_optionalAssets.Add(_assetAdderService.LoadAllAssets<EquipmentRarity, GameObject>());
			_optionalAssets.Add(_services.AssetResolverService.RequestAsset<GameId, GameObject>(GameId.Hammer, true, false));
			LoadOptionalGroup(GameIdGroup.Consumable);
			LoadOptionalGroup(GameIdGroup.BotItem);
			LoadOptionalGroup(GameIdGroup.Chest);
		}

		public void StartMandatoryAssetLoad()
		{
			if (!_services.NetworkService.InRoom) return;
			var time = Time.realtimeSinceStartup;
			var map = _services.RoomService.CurrentRoom.Properties.MapId.Value;
			_assetAdderService.AddConfigs(_services.ConfigsProvider.GetConfig<MatchAssetConfigs>());
			_services.RoomService.CurrentRoom.SetRuntimeConfig();
			LoadQuantumAssets(map);
			_mandatoryAssets.Add(LoadScene(map));
			_mandatoryAssets.Add(
				_services.AudioFxService.LoadAudioClips(_services.ConfigsProvider.GetConfig<AudioMatchAssetConfigs>().ConfigsDictionary));
			_mandatoryAssets.Add(_services.GameUiService.LoadUiAsync<HUDScreenPresenter>());
			_mandatoryAssets.OnCompleteAll(() =>
			{
				var dic = new Dictionary<string, object>
				{
					{"client_version", VersionUtils.VersionInternal},
					{"total_time", Time.realtimeSinceStartup - time},
					{"vendor_id", SystemInfo.deviceUniqueIdentifier},
					{"playfab_player_id", _data.AppDataProvider.PlayerId}
				};
				_services.AnalyticsService.LogEvent(AnalyticsEvents.LoadMatchAssetsComplete, dic);
				FLog.Verbose("Completed loading all core assets");

				if (_services.RoomService.InRoom)
				{
					_services.RoomService.CurrentRoom.LocalPlayerProperties.CoreLoaded.Value = true;
				}
			});
		}


		public async Task UnloadAllMatchAssets()
		{
			var scene = SceneManager.GetActiveScene();
			var configProvider = _services.ConfigsProvider;

			_services.GameUiService.UnloadUiSet((int) UiSetId.MatchUi);
			_services.AudioFxService.DetachAudioListener();

			await _services.AssetResolverService.UnloadSceneAsync(scene);

			_services.VfxService.DespawnAll();
			_services.AudioFxService.UnloadAudioClips(configProvider.GetConfig<AudioMatchAssetConfigs>().ConfigsDictionary);
			_services.AssetResolverService.UnloadAssets<EquipmentRarity, GameObject>(false);
			_services.AssetResolverService.UnloadAssets<IndicatorVfxId, GameObject>(false);
			_services.AssetResolverService.UnloadAssets(true, configProvider.GetConfig<MatchAssetConfigs>());

			Resources.UnloadUnusedAssets();
		}


		private void LoadOptionalGroup(GameIdGroup group)
		{
			foreach (var id in group.GetIds())
				_optionalAssets.Add(_services.AssetResolverService.RequestAsset<GameId, GameObject>(id, true, false));
		}

		public async Task WaitMandatoryComplete()
		{
			await _mandatoryAssets.WaitForCompletion();
		}


		private async Task LoadScene(GameId map)
		{
			if (!_services.ConfigsProvider.GetConfig<MapAssetConfigs>().TryGetConfigForMap(map, out var config))
			{
				throw new Exception("Asset map config not found for map " + map);
			}

			var sceneTask = _assetAdderService.LoadSceneAsync(config.Scene, LoadSceneMode.Additive);
			SceneManager.SetActiveScene(await sceneTask);
		}

		private void OnRoomPlayersChange(Player player, PlayerChangeReason reason)
		{
			if (reason == PlayerChangeReason.Join)
			{
				LoadGameIds(_services.RoomService.CurrentRoom.GetPlayerProperties(player).Loadout.Value);
			}
		}

		private void LoadGameIds(List<GameId> ids)
		{
			foreach (var id in ids)
			{
				_optionalAssets.Add(_services.CollectionService.LoadCollectionItem3DModel(ItemFactory.Collection(id), false, false));
			}
		}

		private void LoadQuantumAssets(GameId map)
		{
			if (_services.ConfigsProvider.GetConfig<MapAssetConfigs>().TryGetConfigForMap(map, out var config))
			{
				_optionalAssets.Add(_assetAdderService.LoadAssetAsync<AssetBase>(config.QuantumMap));
			}

			var assets = UnityDB.CollectAddressableAssets();
			foreach (var asset in assets)
			{
				if (asset.Item1.Contains("Settings") || (asset.Item1.StartsWith("Maps/") && !asset.Item1.Contains(map.ToString())) ||
					asset.Item1.Contains("CircuitExport"))
				{
					continue;
				}

				FLog.Verbose("Preloading Quantum Asset " + asset.Item1);
				_optionalAssets.Add(_assetAdderService.LoadAssetAsync<AssetBase>(asset.Item1));
			}
		}

		public void Dispose()
		{
		}

		public void OnMatchStarted(QuantumGame game, bool isReconnect)
		{
		}

		public void OnMatchEnded(QuantumGame game, bool isDisconnected)
		{
		}
	}
}
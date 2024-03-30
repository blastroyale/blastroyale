using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
using Object = UnityEngine.Object;

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
		UniTask LoadMandatoryAssets();

		/// <summary>
		/// This method will start loading the optional assets, and hopefully the game starts when all are loaded.
		/// Not loading all optional assets is just fine, and starting the game while one asset is in the middle
		/// of the load is also ok.
		/// In case they are not loaded the game will run just fine and assets will be loaded during the game.
		/// </summary>
		UniTask LoadOptionalAssets();

		/// <summary>
		/// Unloads all loaded assets
		/// </summary>
		UniTask UnloadAllMatchAssets();

		/// <summary>
		/// Waits for all mandatory assets to have completed loading
		/// </summary>
		UniTask WaitMandatoryComplete();
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


		public async UniTask LoadOptionalAssets()
		{
			FLog.Verbose("Starting optional match asset load");
			var localPlayerLoadout = _services.RoomService.CurrentRoom.LocalPlayerProperties.Loadout.Value;
			if (localPlayerLoadout != null)
			{
				await LoadGameIds(localPlayerLoadout);
			}

			await _assetAdderService.LoadAllAssets<MaterialVfxId, Material>();
			await _assetAdderService.LoadAllAssets<IndicatorVfxId, GameObject>();
			await _assetAdderService.LoadAllAssets<EquipmentRarity, GameObject>();
			await _services.AssetResolverService.RequestAsset<GameId, GameObject>(GameId.Hammer, true, false);

			await LoadOptionalGroup<GameObject>(GameIdGroup.Collectable);
			await LoadOptionalGroup<GameObject>(GameIdGroup.Weapon);
			await LoadOptionalGroup<Sprite>(GameIdGroup.Weapon);
			await LoadOptionalGroup<GameObject>(GameIdGroup.BotItem);
		}

		public async UniTask LoadMandatoryAssets()
		{
			if (!_services.NetworkService.InRoom) return;

			FLog.Verbose("Starting mandatory asset load");
			var time = Time.realtimeSinceStartup;
			var map = _services.RoomService.CurrentRoom.Properties.MapId.Value;
			_assetAdderService.AddConfigs(_services.ConfigsProvider.GetConfig<MatchAssetConfigs>());
			_services.RoomService.CurrentRoom.SetRuntimeConfig();
			await LoadQuantumAssets(map);
			await LoadScene(map);
			await _services.AudioFxService.LoadAudioClips(_services.ConfigsProvider.GetConfig<AudioMatchAssetConfigs>().ConfigsDictionary);

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
		}


		public async UniTask UnloadAllMatchAssets()
		{
			var start = DateTime.UtcNow;
			FLog.Info("Unloading Match Assets");
			var configProvider = _services.ConfigsProvider;

			_services.AudioFxService.DetachAudioListener();

			var sceneCount = SceneManager.sceneCount;
			for (var i = 0; i < sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				if (!scene.path.Contains("Maps")) continue;
				await _services.AssetResolverService.UnloadSceneAsync(scene);
				break;
			}

			_services.VfxService.DespawnAll();
			_services.AudioFxService.UnloadAudioClips(configProvider.GetConfig<AudioMatchAssetConfigs>().ConfigsDictionary);
			_services.AssetResolverService.UnloadAssets<EquipmentRarity, GameObject>(false);
			_services.AssetResolverService.UnloadAssets<IndicatorVfxId, GameObject>(false);
			_services.AssetResolverService.UnloadAssets<MaterialVfxId, Material>(false);
			_services.AssetResolverService.UnloadAssets(true, configProvider.GetConfig<MatchAssetConfigs>());

			await Resources.UnloadUnusedAssets().ToUniTask();
			FLog.Verbose($"Unloading match assets took {(DateTime.UtcNow - start).TotalMilliseconds} ms");
		}

		private async UniTask LoadOptionalGroup<T>(GameIdGroup group) where T : Object
		{
			foreach (var id in group.GetIds())
			{
				if (id.IsInGroup(GameIdGroup.Deprecated)) continue;

				if (id.IsInGroup(GameIdGroup.Collection))
				{
					await _services.CollectionService.LoadCollectionItem3DModel(ItemFactory.Collection(id), false, false);
				}
				else
				{
					await _services.AssetResolverService.RequestAsset<GameId, T>(id, true, false);
				}
			}
		}

		public async UniTask WaitMandatoryComplete()
		{
			await _mandatoryAssets.WaitForCompletion();
		}


		private async UniTask LoadScene(GameId map)
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
				LoadGameIds(_services.RoomService.CurrentRoom.GetPlayerProperties(player).Loadout.Value).Forget();
			}
		}

		private async UniTask LoadGameIds(List<GameId> ids)
		{
			foreach (var id in ids)
			{
				await _services.CollectionService.LoadCollectionItem3DModel(ItemFactory.Collection(id), false, false);
			}
		}

		private async UniTask LoadQuantumAssets(GameId map)
		{
			if (_services.ConfigsProvider.GetConfig<MapAssetConfigs>().TryGetConfigForMap(map, out var config))
			{
				await _assetAdderService.LoadAssetAsync<AssetBase>(config.QuantumMap);
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
				await _assetAdderService.LoadAssetAsync<AssetBase>(asset.Item1);
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
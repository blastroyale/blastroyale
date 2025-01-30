using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services.RoomService;
using FirstLight.Game.Utils;
using Photon.Realtime;
using PlayFab;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Responsible for asset loading before the match
	/// </summary>
	public interface IMatchAssetsService : IMatchServiceAssetLoader
	{
	}

	public class MatchAssetsService : IMatchAssetsService, IMatchService
	{
		private readonly IGameServices _services;
		private readonly IAssetAdderService _assetAdderService;
		private readonly IGameDataProvider _data;
		private LinkedList<AssetReference> _loadedAssets = new ();
		private MapAssetConfig _mapAssetConfig;

		public MatchAssetsService()
		{
			_services = MainInstaller.ResolveServices();
			_assetAdderService = _services.AssetResolverService as IAssetAdderService;
			_data = MainInstaller.Resolve<IGameDataProvider>();
			_services.RoomService.OnPlayersChange += OnRoomPlayersChange;
		}

		private UniTask<T> LoadAutoClean<T>(AssetReferenceT<T> @ref) where T : Object
		{
			// Cloning because when loading scriptable objects they share the same assetreferences, and we can only load once for each reference
			var newAssetRef = @ref.Clone();
			_loadedAssets.AddLast(newAssetRef);
			return newAssetRef.LoadAssetAsync().ToUniTask();
		}

		private UniTask<Object> LoadAutoClean(string address)
		{
			return LoadAutoClean(new AssetReferenceT<Object>(address));
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
			await _services.AssetResolverService.RequestAsset<GameId, GameObject>(GameId.Hammer, true, false);

			await LoadOptionalGroup<GameObject>(GameIdGroup.Collectable);
			await LoadOptionalGroup<GameObject>(GameIdGroup.Weapon);
			await LoadOptionalGroup<Sprite>(GameIdGroup.Weapon);
			
			
			//await LoadOptionalGroup<GameObject>(GameIdGroup.BotItem);
			_services.MessageBrokerService.Publish(new BenchmarkLoadedOptionalMatchAssets());
		}

		public async UniTask LoadMandatoryAssets()
		{
			if (!_services.NetworkService.InRoom) return;

			FLog.Verbose("Starting mandatory asset load");
			var time = Time.realtimeSinceStartup;
			var map = Enum.Parse<GameId>(_services.RoomService.CurrentRoom.Properties.SimulationMatchConfig.Value.MapId);
			_services.MessageBrokerService.Publish(new BenchmarkStartedLoadingMatchAssets
			{
				Map = map.ToString()
			});
			_assetAdderService.AddConfigs(_services.ConfigsProvider.GetConfig<MatchAssetConfigs>());

			if (!_services.ConfigsProvider.GetConfig<MapAssetConfigIndex>().TryGetConfigForMap(map, out var mapAssetConfigRef))
			{
				throw new Exception("Failed to find map asset config for " + map);
			}

			_mapAssetConfig = await LoadAutoClean(mapAssetConfigRef);
			var quantumMapAsset = await LoadAutoClean(_mapAssetConfig.QuantumMap);
			_services.RoomService.CurrentRoom.SetRuntimeConfig(quantumMapAsset);
			await LoadQuantumAssets(_mapAssetConfig);
			await LoadScene(_mapAssetConfig);
			await _services.AudioFxService.LoadAudioClips(_services.ConfigsProvider.GetConfig<AudioMatchAssetConfigs>().ConfigsDictionary);

			var dic = new Dictionary<string, object>
			{
				{"client_version", VersionUtils.VersionInternal},
				{"total_time", Time.realtimeSinceStartup - time},
				{"vendor_id", SystemInfo.deviceUniqueIdentifier},
				{"playfab_player_id", PlayFabSettings.staticPlayer.PlayFabId}
			};
			FLog.Verbose("Completed loading all core assets");
			_services.MessageBrokerService.Publish(new BenchmarkLoadedMandatoryMatchAssets());

			if (_services.RoomService.InRoom)
			{
				_services.RoomService.CurrentRoom.LocalPlayerProperties.CoreLoaded.Value = true;
			}
		}

		public async UniTask UnloadAssets()
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

			_services.AudioFxService.UnloadAudioClips(configProvider.GetConfig<AudioMatchAssetConfigs>().ConfigsDictionary);
			_services.AssetResolverService.UnloadAssets<IndicatorVfxId, GameObject>(false);
			_services.AssetResolverService.UnloadAssets<MaterialVfxId, Material>(false);
			_services.AssetResolverService.UnloadAssets(true, configProvider.GetConfig<MatchAssetConfigs>());

			foreach (var assetReference in _loadedAssets)
			{
				assetReference.ReleaseAsset();
			}

			UnityDB.Dispose();
			await Resources.UnloadUnusedAssets().ToUniTask();
			FLog.Verbose($"Unloading match assets took {(DateTime.UtcNow - start).TotalMilliseconds} ms");
		}

		private async UniTask LoadOptionalGroup<T>(GameIdGroup group) where T : Object
		{
			var loadTasks = new List<UniTask>();

			foreach (var id in group.GetIds())
			{
				if (id.IsInGroup(GameIdGroup.Deprecated)) continue;

				if (id.IsInGroup(GameIdGroup.Collection))
				{
					loadTasks.Add(_services.CollectionService.LoadCollectionItem3DModel(ItemFactory.Collection(id), false, false));
				}
				else
				{
					loadTasks.Add(_services.AssetResolverService.RequestAsset<GameId, T>(id, true, false));
				}
			}

			await UniTask.WhenAll(loadTasks);
		}

		private async UniTask LoadScene(MapAssetConfig map)
		{
			var sceneTask = _assetAdderService.LoadSceneAsync(map.Scene, LoadSceneMode.Additive);
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
			var loadTasks = new List<UniTask>();

			foreach (var id in ids)
			{
				loadTasks.Add(_services.CollectionService.LoadCollectionItem3DModel(ItemFactory.Collection(id), false, false));
			}

			await UniTask.WhenAll(loadTasks);
		}

		private async UniTask LoadQuantumAssets(MapAssetConfig config)
		{
			var loadTasks = new List<UniTask>();
			var assets = UnityDB.CollectAddressableAssets();
			foreach (var asset in assets)
			{
				if (asset.Item1.Contains("Settings") || (asset.Item1.StartsWith("Maps/")) ||
					asset.Item1.Contains("CircuitExport"))
				{
					continue;
				}

				FLog.Verbose("Preloading Quantum Asset " + asset.Item1);
				loadTasks.Add(LoadAutoClean(asset.Item1));
			}

			await UniTask.WhenAll(loadTasks);
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
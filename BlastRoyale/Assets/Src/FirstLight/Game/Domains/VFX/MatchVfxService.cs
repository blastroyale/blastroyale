using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Domains.VFX;
using FirstLight.Game.Ids;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Services.Match
{
	public interface IMatchVfxService
	{
		public AbstractVfx<VfxId> Spawn(VfxId id, bool pool = true);

		public UniTask<AbstractVfx<VfxId>> SpawnAsync(VfxId id, bool pool = true);

		public void Despawn(AbstractVfx<VfxId> id);
	}

	public class MatchVfxService : IMatchVfxService, IMatchService, IMatchServiceAssetLoader
	{
		private VfxService<VfxId> _vfxService;
		private IGameServices _gameServices;
		private Dictionary<VfxId, GameObject> _references;

		public MatchVfxService(IGameServices gameServices)
		{
			_gameServices = gameServices;
			_vfxService = new VfxService<VfxId>("Match Vfx Container");
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

		public AbstractVfx<VfxId> Spawn(VfxId id, bool pool = true)
		{
			return _vfxService.Spawn(id, pool);
		}

		public UniTask<AbstractVfx<VfxId>> SpawnAsync(VfxId id, bool pool = true)
		{
			return _vfxService.SpawnAsync(id, pool);
		}

		public void Despawn(AbstractVfx<VfxId> vfx)
		{
			_vfxService.Despawn(vfx);
		}

		public async UniTask InitializeVfxConfig(PooledVFXConfigs.VfxConfigEntry entry)
		{
			var prefab = await entry.AssetRef.LoadAssetAsync<GameObject>().ToUniTask();
			if (prefab.TryGetComponent<AbstractVfx<VfxId>>(out var component))
			{
				if (component.Id != entry.Id)
				{
					throw new Exception("Vfx id " + entry.Id + " from config doesn't match component one " + component.Id);
				}

				await _vfxService.AddPoolAsync(component, entry.InitialPoolCount);
				return;
			}

			throw new Exception("Asset " + entry.Id + " does not include VfxMonoBehaviour!");
		}

		public UniTask LoadMandatoryAssets()
		{
			var configs = _gameServices.ConfigsProvider.GetConfig<PooledVFXConfigs>().Configs;
			return UniTask.WhenAll(configs.Select(InitializeVfxConfig));
		}

		public UniTask LoadOptionalAssets()
		{
			return UniTask.CompletedTask;
		}

		public UniTask UnloadAssets()
		{
			_vfxService.Dispose();
			var configs = _gameServices.ConfigsProvider.GetConfig<PooledVFXConfigs>().Configs;
			foreach (var cfg in configs)
			{
				cfg.AssetRef.ReleaseAsset();
			}

			return UniTask.CompletedTask;
		}
	}
}
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Domains.Flags.View;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using Quantum;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FirstLight.Game.Domains.Flags
{
	public interface IMatchFlagService
	{
		public DeathFlagView Spawn(GameId id);

		public void Despawn(DeathFlagView id);
	}

	public class MatchFlagService : IMatchFlagService, IMatchService, IMatchServiceAssetLoader
	{
		private const int POOL_SIZE = 50;
		private Dictionary<GameId, Mesh> _meshes;
		private Stack<DeathFlagView> _pool;
		private IGameServices _gameServices;
		private Dictionary<VfxId, GameObject> _references;
		private FlagSkinConfig _flagConfig;
		private GameObject _container;

		public MatchFlagService(IGameServices gameServices)
		{
			_container = new GameObject("Flags");
			_gameServices = gameServices;
			_flagConfig = _gameServices.ConfigsProvider.GetConfig<FlagSkinConfig>();
			_meshes = new Dictionary<GameId, Mesh>(_flagConfig.Skins.Count);
			_pool = new Stack<DeathFlagView>(POOL_SIZE);
			for (var i = 0; i < POOL_SIZE; i++)
			{
				var prefabInstance = Object.Instantiate(_flagConfig.FlagPrefab);
				var view = prefabInstance.GetComponent<DeathFlagView>();
				view.transform.SetParent(_container.transform, false);
				view.Dispose();
				_pool.Push(view);
			}
		}

		public void Dispose()
		{
			while (_pool.Count > 0)
			{
				var view = _pool.Pop();
				view.Dispose();
				Object.Destroy(view);
			}

			foreach (var meshEntry in _meshes)
			{
				Object.Destroy(meshEntry.Value);
			}
			_meshes.Clear();
			if(_container != null) Object.Destroy(_container);
			_container = null;
		}

		public void OnMatchStarted(QuantumGame game, bool isReconnect)
		{
		}

		public void OnMatchEnded(QuantumGame game, bool isDisconnected)
		{
		}

		public DeathFlagView Spawn(GameId id)
		{
			if (_meshes.TryGetValue(id, out var flagMesh))
			{
				var view = _pool.Pop();
				view.Initialise(flagMesh);
				return view;
			}
			FLog.Error($"No mesh found for flag with gameid: {id}");
			return null;
		}

		public void Despawn(DeathFlagView view)
		{
			view.Dispose();
			_pool.Push(view);
		}

		private async UniTask InitializeFlagConfig(FlagConfigEntry entry)
		{
			var mesh = await entry.Mesh.LoadAssetAsync<Mesh>().ToUniTask();
			_meshes.Add(entry.GameId, mesh);
		}

		public UniTask LoadMandatoryAssets()
		{
			return UniTask.WhenAll(_flagConfig.Skins.Select(InitializeFlagConfig));
		}

		public UniTask LoadOptionalAssets()
		{
			return UniTask.CompletedTask;
		}

		public UniTask UnloadAssets()
		{
			Dispose();
			var configs = _gameServices.ConfigsProvider.GetConfig<FlagSkinConfigs>().Config.Skins;
			foreach (var cfg in configs)
			{
				cfg.Mesh.ReleaseAsset();
			}

			return UniTask.CompletedTask;
		}
	}
}
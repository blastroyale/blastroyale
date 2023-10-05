using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Utils;
using FirstLight.Services;
using Quantum;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// This Service allows to access <see cref="EntityView"/> from any point in the game.
	/// Also overrides the Quantum's <see cref="EntityViewUpdaterService"/> by extending the possibility to only delete
	/// GameObjects on late update to allow other components to access it on the same frame if marked to be manual desposable.
	/// </summary>
	public interface IEntityViewUpdaterService
	{
		/// <summary>
		/// Requests the <see cref="EntityView"/> existing in the game world that represents the given <paramref name="entityRef"/>.
		/// Returns true if the entity exists in the active or manual entities to destroy.
		/// </summary>
		/// <remarks>
		/// Use this call instead of <see cref="EntityViewUpdater.GetView"/> in potential cases where the entity is
		/// marked to be manual destroyed
		/// </remarks>
		bool TryGetView(EntityRef entityRef, out EntityView view);

		/// <summary>
		/// Requests the <see cref="EntityView"/> existing in the game world that represents the given <paramref name="entityRef"/>.
		/// </summary>
		/// <remarks>
		/// Use this call instead of <see cref="EntityViewUpdater.GetView"/> in potential cases where the entity is
		/// marked to be manual destroyed
		/// </remarks>
		EntityView GetManualView(EntityRef entityRef);

		/// <summary>
		/// Set the parents of the view in the scene, only used to organize objects in runtime
		/// </summary>
		void SetParents(EntityView view, string group = null);
	}

	/// <inheritdoc cref="IEntityViewUpdaterService" />
	public class EntityViewUpdaterService : EntityViewUpdater, IEntityViewUpdaterService
	{
		private readonly IDictionary<EntityRef, EntityView>
			_viewsToDestroy = new Dictionary<EntityRef, EntityView>(256);

		private readonly List<EntityView> _viewsListToDestroy = new List<EntityView>(256);

		private IGameServices _gameServices;

		private Dictionary<long, List<EntityView>> _inactiveBullets = new();

		private Dictionary<string, GameObject> _entityViewGroups = new();

		private GameObject _viewParent;

		private void PollBullet(EntityView view)
		{
			view.gameObject.SetActive(false);
			if (!_inactiveBullets.TryGetValue(view.AssetGuid.Value, out var list))
			{
				list = new List<EntityView>();
				_inactiveBullets[view.AssetGuid.Value] = list;
			}

			list.Add(view);
		}

		/// <summary>
		/// Override to poll bullets so we dont need to re-create the views for every new bullet.
		/// This saves us some CPU time and makes memory usage more stable.
		/// </summary>
		protected override EntityView CreateEntityViewInstance(EntityViewAsset asset, Vector3? position = null,
															   Quaternion? rotation = null)
		{
			if (_inactiveBullets.TryGetValue(asset.AssetObject.Guid.Value, out var inactiveList) && inactiveList.Count > 0)
			{
				var bullet = inactiveList[0];
				if (position.HasValue) bullet.transform.position = position.Value;
				if (rotation.HasValue) bullet.transform.rotation = rotation.Value;
				inactiveList.RemoveAt(0);
				bullet.gameObject.SetActive(true);
				return bullet;
			}

			return base.CreateEntityViewInstance(asset, position, rotation);
		}

		public void SetParents(EntityView view, string group = null)
		{
			GameObject parent;
			// Need to move to the parent, because when creating it will use the same scene as the current MonoBehaviour.
			// And this is a service running on the main scene, with this line it will move to the same scene as the view
			SceneManager.MoveGameObjectToScene(_viewParent, view.gameObject.scene);
			if (group == null)
			{
				parent = _viewParent;
			}
			else if (!_entityViewGroups.TryGetValue(group, out parent))
			{
				parent = new GameObject($"{group}");
				parent.transform.SetParent(_viewParent.transform);
				_entityViewGroups[group] = parent;
			}
			view.transform.SetParent(parent.transform);
		}

		private new void Awake()
		{
			base.Awake();

			_viewParent = new GameObject("Views");
			_gameServices = MainInstaller.Resolve<IGameServices>();

			_gameServices.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStartedMessage);
		}

		private void OnMatchStartedMessage(MatchStartedMessage msg)
		{
			if (!msg.IsResync) return;

			var f = msg.Game.Frames.Verified;

			if (!f.Context.GameModeConfig.DeathMarker) return;

			var data = f.GetSingleton<GameContainer>().PlayersData;
			for (var i = 0; i < data.Length; i++)
			{
				var playerData = data[i];

				if (playerData.DeathCount > 0)
				{

					var cosmetics = PlayerLoadout.GetCosmetics(f, playerData.Player);
					var deathMarker = _gameServices.CollectionService.GetCosmeticForGroup(cosmetics,GameIdGroup.DeathMarker);
					SpawnDeathMarker(deathMarker, playerData.LastDeathPosition.ToUnityVector3());
				}
			}
		}


		private void LateUpdate()
		{
			foreach (var view in _viewsListToDestroy)
			{
				view.OnEntityDestroyed.Invoke(ObservedGame);

				if (view.AssetGuid.IsValid)
				{
					if (view.gameObject.CompareTag("Bullet"))
					{
						PollBullet(view);
					}
					else
					{
						DestroyEntityViewInstance(view);
					}
				}
				else
				{
					DisableMapEntityInstance(view);
				}
			}

			_viewsListToDestroy.Clear();
			_viewsToDestroy.Clear();
		}

		/// <inheritdoc />
		public bool TryGetView(EntityRef entityRef, out EntityView view)
		{
			return ActiveViews.TryGetValue(entityRef, out view) || _viewsToDestroy.TryGetValue(entityRef, out view);
		}

		/// <inheritdoc />
		public EntityView GetManualView(EntityRef entityRef)
		{
			if (!TryGetView(entityRef, out var view))
			{
				throw new KeyNotFoundException($"There is no {nameof(EntityView)} for the given entity {entityRef}.");
			}

			return view;
		}

		protected override void DestroyEntityView(QuantumGame game, EntityView view)
		{
			// Checks if the simulation is running
			if (game?.Frames?.Predicted == null)
			{
				return;
			}

			if (view.ManualDisposal)
			{
				_viewsListToDestroy.Add(view);
				_viewsToDestroy.Add(view.EntityRef, view);

				return;
			}

			base.DestroyEntityView(game, view);
		}

		protected override void DisableMapEntityInstance(EntityView instance)
		{
			if (!instance.IsDestroyed())
			{
				base.DisableMapEntityInstance(instance);
			}
		}

		private async void SpawnDeathMarker(GameId marker, Vector3 position)
		{
			var obj = await _gameServices.AssetResolverService.RequestAsset<GameId, GameObject>(marker);

			obj.transform.position = position;
		}
	}
}
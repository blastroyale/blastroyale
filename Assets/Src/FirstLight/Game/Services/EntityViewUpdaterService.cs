using System;
using System.Collections.Generic;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

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
	}

	/// <inheritdoc cref="IEntityViewUpdaterService" />
	public class EntityViewUpdaterService : EntityViewUpdater, IEntityViewUpdaterService
	{
		private readonly IDictionary<EntityRef, EntityView>
			_viewsToDestroy = new Dictionary<EntityRef, EntityView>(256);

		private readonly List<EntityView> _viewsListToDestroy = new List<EntityView>(256);

		private IGameServices _gameServices;

		private void Start()
		{
			_gameServices = MainInstaller.Resolve<IGameServices>();

			QuantumCallback.Subscribe<CallbackGameResynced>(this, OnGameResynced);
		}

		private void LateUpdate()
		{
			foreach (var view in _viewsListToDestroy)
			{
				view.OnEntityDestroyed.Invoke(ObservedGame);

				if (view.AssetGuid.IsValid)
				{
					DestroyEntityViewInstance(view);
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
			if (game.Frames.Predicted == null)
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

		private void OnGameResynced(CallbackGameResynced callback)
		{
			var f = callback.Game.Frames.Verified;

			if (!f.Context.GameModeConfig.DeathMarker) return;

			var data = f.GetSingleton<GameContainer>().PlayersData;
			for (var i = 0; i < data.Length; i++)
			{
				var playerData = data[i];

				if (playerData.DeathCount > 0)
				{
					SpawnDeathMarker(playerData.PlayerDeathMarker, playerData.LastDeathPosition.ToUnityVector3());
				}
			}
		}

		private async void SpawnDeathMarker(GameId marker, Vector3 position)
		{
			var obj = await _gameServices.AssetResolverService.RequestAsset<GameId, GameObject>(marker);

			obj.transform.position = position;
		}
	}
}
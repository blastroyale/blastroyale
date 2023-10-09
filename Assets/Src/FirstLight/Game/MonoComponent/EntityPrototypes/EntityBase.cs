using System;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// Abstract contract for the controllers existing in the 3D world.
	/// Inherit this class if you plan to add extra functionality between the Game World and Quantum.
	/// This controller base does not provide the pointer to the <see cref="IComponent"/>, only a copy of the data itself.
	/// </summary>
	[RequireComponent(typeof(EntityView))]
	public abstract class EntityBase : MonoBehaviour
	{
		public EntityView EntityView;

		private IGameServices _services;

		public GameObject Instance { get; private set; }

		public bool HasRenderedView()
		{
			return Instance != null;
		}

		protected IGameServices Services => _services ??= MainInstaller.Resolve<IGameServices>();

		private void OnValidate()
		{
			EntityView = EntityView ? EntityView : GetComponent<EntityView>();
		}

		private void Awake()
		{
			EntityView.OnEntityInstantiated.AddListener(OnEntityInstantiatedInternal);
			EntityView.OnEntityDestroyed.AddListener(OnEntityDestroyed);

			OnAwake();
		}


		protected void RemoveListeners()
		{
			QuantumCallback.UnsubscribeListener(this);
			QuantumEvent.UnsubscribeListener(this);
			Services.MessageBrokerService.UnsubscribeAll(this);
		}

		void OnDestroy()
		{
			RemoveListeners();
		}

		protected T GetComponentData<T>(QuantumGame game) where T : unmanaged, IComponent
		{
			return EntityView.BindBehaviour == EntityViewBindBehaviour.Verified
				? GetComponentVerifiedData<T>(game)
				: GetComponentPredictedData<T>(game);
		}

		protected T GetComponentPredictedData<T>(QuantumGame game) where T : unmanaged, IComponent
		{
			return game.Frames.Predicted.Get<T>(EntityView.EntityRef);
		}

		protected T GetComponentVerifiedData<T>(QuantumGame game) where T : unmanaged, IComponent
		{
			return game.Frames.Verified.Get<T>(EntityView.EntityRef);
		}

		protected Frame GetFrame(QuantumGame game)
		{
			return EntityView.BindBehaviour == EntityViewBindBehaviour.Verified
				? game.Frames.Verified
				: game.Frames.Predicted;
		}

		protected bool TryGetComponentData<T>(QuantumGame game, out T component) where T : unmanaged, IComponent
		{
			return EntityView.BindBehaviour == EntityViewBindBehaviour.Verified
				? TryGetComponentVerifiedData(game, out component)
				: TryGetComponentPredictedData(game, out component);
		}

		protected bool TryGetComponentPredictedData<T>(QuantumGame game, out T component) where T : unmanaged, IComponent
		{
			return game.Frames.Predicted.TryGet(EntityView.EntityRef, out component);
		}

		protected bool TryGetComponentVerifiedData<T>(QuantumGame game, out T component) where T : unmanaged, IComponent
		{
			return game.Frames.Verified.TryGet(EntityView.EntityRef, out component);
		}

		protected void OnLoaded(GameId id, GameObject instance, bool instantiated)
		{
			var runner = QuantumRunner.Default;

			if (this.IsDestroyed() || runner == null)
			{
				Destroy(instance);
				return;
			}

			var cacheTransform = instance.transform;
			Instance = instance;
			if (instance.TryGetComponent<EntityMainViewBase>(out var mainViewBase))
			{
				mainViewBase.SetEntityView(QuantumRunner.Default.Game, EntityView);
			}

			cacheTransform.SetParent(transform);

			cacheTransform.localPosition = Vector3.zero;
			cacheTransform.localRotation = Quaternion.identity;

			_services.MessageBrokerService.Publish(new EntityViewLoaded()
			{
				Entity = this, View = EntityView
			});
		}
		
		/// <summary>
		/// Get the name of the game object for the entity
		/// </summary>
		protected virtual string GetName(QuantumGame game)
		{
			return EntityView.EntityRef.ToString();
		}

		/// <summary>
		/// Get the group name in the game object hierarchy
		/// </summary>
		protected virtual string GetGroup(QuantumGame game)
		{
			return GetType().Name.Replace("MonoComponent", "");
		}

		private void OnEntityInstantiatedInternal(QuantumGame game)
		{
			OnEntityInstantiated(game);
#if UNITY_EDITOR
			gameObject.name = GetName(game);
			MainInstaller.ResolveMatchServices().EntityViewUpdaterService.SetParents(EntityView, GetGroup(game));
#endif
		}

		protected virtual void OnAwake()
		{
		}

		protected virtual void OnEntityInstantiated(QuantumGame game)
		{
		}

		protected virtual void OnEntityDestroyed(QuantumGame game)
		{
		}
	}
}
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
		
		protected GameObject Instance;
		protected IGameServices Services => _services ??= MainInstaller.Resolve<IGameServices>();

		private void OnValidate()
		{
			EntityView = EntityView ? EntityView : GetComponent<EntityView>();
			
			OnEditorValidate();
		}

		private void Awake()
		{
			EntityView.OnEntityInstantiated.AddListener(OnEntityInstantiated);
			EntityView.OnEntityDestroyed.AddListener(OnEntityDestroyed);
			
			OnAwake();
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

		protected void OnLoaded(GameId id, GameObject instance, bool instantiated)
		{
			if (this.IsDestroyed())
			{
				Destroy(instance);
				return;
			}
			
			Instance = instance;
			
			var cacheTransform = instance.transform;

			if(instance.TryGetComponent<EntityMainViewBase>(out var mainViewBase))
			{
				mainViewBase.SetEntityView(EntityView);
			}
			
			cacheTransform.SetParent(transform);
			
			cacheTransform.localPosition = Vector3.zero;
			cacheTransform.localRotation = Quaternion.identity;
		}

		protected virtual void OnEditorValidate() {}
		protected virtual void OnAwake() {}
		protected virtual void OnEntityInstantiated(QuantumGame game) {}
		protected virtual void OnEntityDestroyed(QuantumGame game) {}
	}
}
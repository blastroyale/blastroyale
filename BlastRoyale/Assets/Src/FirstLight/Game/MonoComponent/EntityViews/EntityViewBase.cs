using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	
	/// <summary>
	/// Inherit from this class if you need a reference to a parent <see cref="EntityView"/>.
	/// All <see cref="EntityView"/> live in the same prefab where <see cref="Quantum.EntityPrototype"/> live
	/// </summary>
	public abstract class EntityViewBase : MonoBehaviour
	{
		protected IGameServices Services;
		protected IMatchServices MatchServices;
		
		/// <summary>
		/// Requests the <see cref="EntityView"/> representing this view execution base
		/// </summary>
		public EntityView EntityView { get; private set; }

		/// <summary>
		/// Requests the <see cref="EntityRef"/> that this entity view is referencing to
		/// </summary>
		public EntityRef EntityRef { get; private set; }

		protected virtual void Awake()
		{
			// This is necessary because Views are loaded with Tasks and the player might already left the match when this is called
			if (this.IsDestroyed() || !MainInstaller.TryResolve<IMatchServices>(out var matchServices))
			{
				FLog.Error($"This '{this}' object is already destroyed");
				return;
			}

			Services = MainInstaller.Resolve<IGameServices>();
			MatchServices = matchServices;

			OnAwake();
		}

		/// <summary>
		/// Sets this entity view reference to be the same of the parent <see cref="Quantum.EntityPrototype"/>
		/// </summary>
		public void SetEntityView(QuantumGame game, EntityView entityView)
		{
			if (EntityRef.IsValid)
			{
				return;
			}
			EntityView = entityView;
			EntityRef = EntityView.EntityRef;
			EntityView.ManualDisposal = true;
			OnInit(game);
		}

		protected virtual void OnAwake() { }

		protected virtual void OnInit(QuantumGame game) { }
	}
	
	/// <inheritdoc />
	/// <remarks>
	/// Inherit this view base for the main view representing the defined <see cref="EntityView"/>
	/// </remarks>
	[RequireComponent(typeof(RenderersContainerMonoComponent))]
	[RequireComponent(typeof(RenderersContainerProxyMonoComponent))]
	public abstract class EntityMainViewBase : EntityViewBase
	{
		private bool _culled = false;
		private bool _visible = false;
		protected bool Visible
		{
			get => _visible;
		}
		
		public bool Culled
		{
			get => _culled; 
		}

		[SerializeField] protected RenderersContainerProxyMonoComponent RenderersContainerProxy;

		protected override void Awake()
		{
			if (RenderersContainerProxy == null)
			{
				RenderersContainerProxy = GetComponent<RenderersContainerProxyMonoComponent>();
			}
			base.Awake();
			QuantumCallback.Subscribe<CallbackUpdateView>(this, OnUpdateView, onlyIfActiveAndEnabled: true);
		}

		public void OnUpdateView(CallbackUpdateView callback)
		{
			if (callback.Game.IsGameOver()) return;
			if (callback.Game.Frames.Predicted.IsCulled(EntityRef))
			{
				if (!_culled)
				{
					SetCulled(true);
				}
			}
			else
			{
				if (_culled)
				{
					SetCulled(false);
				}
			}
		}

		/// <summary>
		/// Sets if a given entity view gets culled or not by the simulation prediction
		/// </summary>
		/// <param name="culled"></param>
		public virtual void SetCulled(bool culled)
		{
			_culled = culled;
		}

		/// <summary>
		/// Sets the root rendering container object active or inactive
		/// </summary>
		public virtual void SetRenderContainerVisible(bool active)
		{
			RenderersContainerProxy.SetEnabled(active);
		}
	}
}
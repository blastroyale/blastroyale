using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FirstLight.FLogger;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.Rendering;

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
			EntityView = entityView;
			EntityRef = EntityView.EntityRef;
			
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
		private static readonly int  _dissolveProperty = Shader.PropertyToID("dissolve_amount");
		
		[SerializeField] protected RenderersContainerProxyMonoComponent RenderersContainerProxy;

		protected override void Awake()
		{
			base.Awake();
			QuantumCallback.Subscribe<CallbackGameDestroyed>(this, HandleGameDestroyed);
		}

		/// <summary>
		/// Sets the root rendering container object active or inactive
		/// </summary>
		public virtual void SetRenderContainerVisible(bool active)
		{
			RenderersContainerProxy.SetRendererState(active);
		}
		
		protected void Dissolve(bool destroyGameObject, float startValue, float endValue, float delay, float duration)
		{
			StartCoroutine(DissolveCoroutine(destroyGameObject, startValue, endValue, delay, duration));
		}

		private IEnumerator DissolveCoroutine(bool destroyGameObject, float startValue, float endValue, float delay, float duration)
		{
			var task = Services.AssetResolverService.RequestAsset<MaterialVfxId, Material>(MaterialVfxId.Dissolve, true, false);
			
			if (delay > 0)
			{
				yield return new WaitForSeconds(delay);
			}

			while (!task.IsCompleted)
			{
				yield return null;
			}
			
			RenderersContainerProxy.SetMaterial(SetMaterial, ShadowCastingMode.On, true);
			RenderersContainerProxy.DisableParticles();
			
			yield return new WaitForSeconds(duration);

			if (destroyGameObject)
			{
				Destroy(gameObject);
			}

			Material SetMaterial(int index)
			{
				var newMat = new Material(task.Result);
				
				newMat.SetFloat(_dissolveProperty, startValue);
				
				if (duration > 0f)
				{
					newMat.DOFloat(endValue, _dissolveProperty, duration).SetAutoKill(true);
				}
				else
				{
					newMat.SetFloat(_dissolveProperty, endValue);
				}
				
				return newMat;
			}
		}

		private void HandleGameDestroyed(CallbackGameDestroyed callback)
		{
			Destroy(gameObject);
		}
	}
}
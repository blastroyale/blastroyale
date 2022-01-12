using System;
using System.Collections;
using FirstLight.Services;
using FirstLight.Game.Ids;
using FirstLight.Game.MonoComponent.Vfx;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <summary>
	/// Polls and listens th Actor state to update this avatar animations accordingly.
	/// Sets animation parameters for contexts shared between players and enemies.
	/// </summary>
	[RequireComponent(typeof(Animator), typeof(RenderersContainerProxyMonoComponent))]
	public abstract class AvatarViewBase : EntityMainViewBase
	{
		private static readonly int _mainText = Shader.PropertyToID("_MainTex");
		private static readonly int  _hitProperty = Shader.PropertyToID("_Hit");
		
		/// <summary>
		/// Animation booleans to play in the avatar
		/// </summary>
		public static class Bools
		{
			public static readonly AnimatorWrapper.Bool Move = new AnimatorWrapper.Bool("move");
			public static readonly AnimatorWrapper.Bool Aim = new AnimatorWrapper.Bool("aim");
			public static readonly AnimatorWrapper.Bool Stun = new AnimatorWrapper.Bool("stun");
			public static readonly AnimatorWrapper.Bool Pickup = new AnimatorWrapper.Bool("pickup");
			public static readonly AnimatorWrapper.Bool Furious = new AnimatorWrapper.Bool("furious");
		}

		/// <summary>
		/// Animation triggers to play in the avatar
		/// </summary>
		public static class Triggers
		{
			public static readonly AnimatorWrapper.Trigger Shoot = new AnimatorWrapper.Trigger("shoot");
			public static readonly AnimatorWrapper.Trigger Die = new AnimatorWrapper.Trigger("die");
			public static readonly AnimatorWrapper.Trigger Hit = new AnimatorWrapper.Trigger("hit");
			public static readonly AnimatorWrapper.Trigger Victory = new AnimatorWrapper.Trigger("victory");
			public static readonly AnimatorWrapper.Trigger Spawn = new AnimatorWrapper.Trigger("spawn");
			public static readonly AnimatorWrapper.Trigger Revive = new AnimatorWrapper.Trigger("revive");
			public static readonly AnimatorWrapper.Trigger Special = new AnimatorWrapper.Trigger("special");
			public static readonly AnimatorWrapper.Trigger Charge = new AnimatorWrapper.Trigger("charge");
			public static readonly AnimatorWrapper.Trigger Jump = new AnimatorWrapper.Trigger("jump");
			public static readonly AnimatorWrapper.Trigger Melee = new AnimatorWrapper.Trigger("melee");
		}

		/// <summary>
		/// Animation states to play in the avatar
		/// </summary>
		public static class States
		{
			public static readonly AnimatorWrapper.State Dissolve = new AnimatorWrapper.State("dissolve");
			public static readonly AnimatorWrapper.State Aim = new AnimatorWrapper.State("aim_gun");
		}

		public UnityEvent FootprintRightEvent;
		public UnityEvent FootprintLeftEvent;

		[FormerlySerializedAs("_rigidbodyContainerMonoComponent")] [SerializeField] protected RigidbodyContainerMonoComponent RigidbodyContainerMonoComponent;
		
		[SerializeField] private Animator _animator;
		[SerializeField] private VfxId _projectileHitVfx;
		[SerializeField] private Vector3 _vfxLocalScale = Vector3.one;
		
		private AnimatorWrapper _animatorWrapper;
		private Coroutine _stunCoroutine;
		private Coroutine _materialsCoroutine;
		private Coroutine _starCoroutine;
		private Vfx<VfxId> _statusVfx;

		/// <summary>
		/// The readonly <see cref="AnimatorWrapper"/> to play the avatar animations
		/// </summary>
		public AnimatorWrapper AnimatorWrapper => _animatorWrapper;

		private void OnDestroy()
		{
			CleanUp();
		}

		protected override void OnAwake()
		{
			_animatorWrapper = new AnimatorWrapper(_animator);
			
			QuantumEvent.Subscribe<EventOnHealthChanged>(this, HandleOnHealthChanged);
			QuantumEvent.Subscribe<EventOnHealthIsZero>(this, HandleOnHealthIsZero);
			QuantumEvent.Subscribe<EventOnProjectileFired>(this, HandleOnProjectileFired);
			QuantumEvent.Subscribe<EventOnStatusModifierSet>(this, HandleOnStatusModifierSet);
			QuantumEvent.Subscribe<EventOnStatusModifierCancelled>(this, HandleOnStatusModifierCancelled);
			QuantumEvent.Subscribe<EventOnStatusModifierFinished>(this, HandleOnStatusModifierFinished);
			QuantumEvent.Subscribe<EventOnLocalSpecialUsed>(this, HandleOnLocalSpecialUsed);
		}

		protected override void OnInit()
		{
			RigidbodyContainerMonoComponent.SetState(false);
			
			EntityView.OnEntityDestroyed.AddListener(HandleOnEntityDestroyed);
		}

		/// <summary>
		/// Sets the modifier effect for the player
		/// </summary>
		public void SetStatusModifierEffect(StatusModifierType statusType, float duration)
		{
			if (statusType == StatusModifierType.Stun)
			{
				// Set "stun" bool to false in advance to allow stun outro animation to play
				duration -= _animator.GetFloat(GameConstants.STUN_OUTRO_TIME_ANIMATOR_PARAM);
				
				_stunCoroutine = Services.CoroutineService.StartCoroutine(StunCoroutine(duration));
			}
			
			if (statusType == StatusModifierType.Star)
			{
				// Scale UP the character if it has a Star status
				_starCoroutine = Services.CoroutineService.StartCoroutine(StarCoroutine(duration));
			}
			
			if (statusType.TryGetVfx(out MaterialVfxId materialVfx))
			{
				RenderersContainerProxy.SetMaterial(materialVfx, ShadowCastingMode.On, true);
				_materialsCoroutine = Services.CoroutineService.StartCoroutine(ResetMaterials(duration));
			}
			else if (statusType.TryGetVfx(out VfxId vfx))
			{
				if (_statusVfx != null)
				{
					_statusVfx.Despawn();
					_statusVfx = null;
				}
				
				_statusVfx = Services.VfxService.Spawn(vfx);

				var cachedTransform = _statusVfx.transform;
				
				_statusVfx.GetComponent<MutableTimeVfxMonoComponent>().StartDespawnTimer(duration);
				cachedTransform.SetParent(transform);
				
				cachedTransform.localPosition = Vector3.zero;
				cachedTransform.localRotation = Quaternion.identity;
				cachedTransform.localScale = _vfxLocalScale;
			}
		}

		private void HandleOnHealthIsZero(EventOnHealthIsZero callback)
		{
			if (callback.Entity != EntityView.EntityRef)
			{
				return;
			}
			
			AnimatorWrapper.Enabled = false;
			
			RigidbodyContainerMonoComponent.SetState(true);
			
			var direction = callback.DamageSourceDirection.ToUnityVector3().normalized;
				
			if (direction.sqrMagnitude > Mathf.Epsilon)
			{
				direction *= Mathf.Clamp(GameConstants.PLAYER_RAGDOLL_FORCE_SCALAR * Mathf.Sqrt(callback.DamageAmount), 0, 
				                         GameConstants.PLAYER_RAGDOLL_FORCE_MAX);

				RigidbodyContainerMonoComponent.AddForce(direction, ForceMode.Impulse);
			}
		}
		
		private void HandleOnEntityDestroyed(QuantumGame game)
		{
			transform.parent = null;

			Services.AudioFxService.PlayClip3D(AudioId.ActorDeath01, transform.position);
			Dissolve(true);
					
			QuantumEvent.UnsubscribeListener(this);
			QuantumCallback.UnsubscribeListener<CallbackUpdateView>(this);
		}

		protected virtual void HandleOnHealthChanged(EventOnHealthChanged evnt)
		{
			if (evnt.Entity != EntityView.EntityRef || evnt.PreviousHealth <= evnt.CurrentHealth)
			{
				return;
			}
			
			var cacheTransform = transform;
			
			Services.VfxService.Spawn(_projectileHitVfx).transform.SetPositionAndRotation(cacheTransform.position, cacheTransform.rotation);
			_animatorWrapper.SetTrigger(Triggers.Hit);
			Services.AudioFxService.PlayClip3D(AudioId.ActorHit01, transform.position);
			RenderersContainerProxy.SetMaterialPropertyValue(_hitProperty, 0, 1, GameConstants.HitDuration);
		}
		
		private void HandleOnProjectileFired(EventOnProjectileFired evnt)
		{
			if (evnt.ProjectileData.Attacker != EntityRef)
			{
				return;
			}
			
			Services.AudioFxService.PlayClip3D(AudioId.ProjectileFired01, transform.position);
			_animatorWrapper.SetTrigger(Triggers.Shoot);
		}
		
		private void HandleOnStatusModifierSet(EventOnStatusModifierSet evnt)
		{
			if (evnt.Entity != EntityView.EntityRef)
			{
				return;
			}
			
			SetStatusModifierEffect(evnt.Type, evnt.Duration.AsFloat);
		}

		private void HandleOnStatusModifierFinished(EventOnStatusModifierFinished evnt)
		{
			if (evnt.Entity != EntityView.EntityRef)
			{
				return;
			}
			
			CleanUp();
		}

		private void HandleOnStatusModifierCancelled(EventOnStatusModifierCancelled evnt)
		{
			if (evnt.Entity != EntityView.EntityRef)
			{
				return;
			}
			
			CleanUp();
		}

		private void HandleOnLocalSpecialUsed(EventOnLocalSpecialUsed evnt)
		{
			if (evnt.Entity != EntityView.EntityRef)
			{
				return;
			}
			
			_animatorWrapper.SetTrigger(Triggers.Special);
		}

		private void CleanUp()
		{
			if (_statusVfx != null)
			{
				_statusVfx.Despawn();
				_statusVfx = null;
			}
			
			if (_materialsCoroutine != null)
			{
				Services.CoroutineService.StopCoroutine(_materialsCoroutine);
				RenderersContainerProxy.ResetToOriginalMaterials();
				_materialsCoroutine = null;
			}
			
			if (_stunCoroutine != null)
			{
				Services.CoroutineService.StopCoroutine(_stunCoroutine);
				FinishStun();
			}
			
			if (_starCoroutine != null)
			{
				Services.CoroutineService.StopCoroutine(_starCoroutine);
				FinishStar();
			}
		}
		
		/// <summary>
		/// This method is invoked by this avatar <see cref="Animation"/> event
		/// </summary>
		private void FootprintR()
		{
			FootprintRightEvent?.Invoke();
		}

		/// <summary>
		/// This method is invoked by this avatar <see cref="Animation"/> event
		/// </summary>
		private void FootprintL()
		{
			FootprintLeftEvent?.Invoke();
		}

		private void FinishStun()
		{
			_stunCoroutine = null;
			_animatorWrapper.SetBool(Bools.Stun, false);
		}

		private IEnumerator StunCoroutine(float time)
		{
			_animatorWrapper.SetBool(Bools.Aim, false);
			_animatorWrapper.SetBool(Bools.Stun, true);

			yield return new WaitForSeconds(time);

			FinishStun();
		}
		
		private IEnumerator ResetMaterials(float time)
		{
			yield return new WaitForSeconds(time);
			
			RenderersContainerProxy.ResetToOriginalMaterials();
		}
		
		private IEnumerator StarCoroutine(float time)
		{
			transform.localScale *= GameConstants.STAR_STATUS_CHARACTER_SCALE_MULTIPLIER;
			
			yield return new WaitForSeconds(time);
			
			FinishStar();
		}
		
		private void FinishStar()
		{
			_starCoroutine = null;
			transform.localScale /= GameConstants.STAR_STATUS_CHARACTER_SCALE_MULTIPLIER;
		}
	}
}
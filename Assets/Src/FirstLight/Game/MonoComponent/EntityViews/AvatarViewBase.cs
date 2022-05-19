using System.Collections;
using FirstLight.Services;
using FirstLight.Game.Ids;
using FirstLight.Game.MonoComponent.Vfx;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
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
		private static readonly int _hitProperty = Shader.PropertyToID("_Hit");

		/// <summary>
		/// Animation booleans to play in the avatar
		/// </summary>
		protected static class Bools
		{
			public static readonly AnimatorWrapper.Bool Move = new("move");
			public static readonly AnimatorWrapper.Bool Aim = new("aim");
			public static readonly AnimatorWrapper.Bool Stun = new("stun");
			public static readonly AnimatorWrapper.Bool Pickup = new("pickup");
			public static readonly AnimatorWrapper.Bool Furious = new("furious");
			public static readonly AnimatorWrapper.Bool Flying = new("flying");
		}

		/// <summary>
		/// Animation triggers to play in the avatar
		/// </summary>
		protected static class Triggers
		{
			public static readonly AnimatorWrapper.Trigger Shoot = new("shoot");
			public static readonly AnimatorWrapper.Trigger Die = new("die");
			public static readonly AnimatorWrapper.Trigger Hit = new("hit");
			public static readonly AnimatorWrapper.Trigger Victory = new("victory");
			public static readonly AnimatorWrapper.Trigger Spawn = new("spawn");
			public static readonly AnimatorWrapper.Trigger Revive = new("revive");
			public static readonly AnimatorWrapper.Trigger Special = new("special");
			public static readonly AnimatorWrapper.Trigger Charge = new("charge");
			public static readonly AnimatorWrapper.Trigger Jump = new("jump");
			public static readonly AnimatorWrapper.Trigger Melee = new("melee");
			public static readonly AnimatorWrapper.Trigger PLF = new("plf");
		}

		/// <summary>
		/// Animation states to play in the avatar
		/// </summary>
		public static class States
		{
			public static readonly AnimatorWrapper.State Dissolve = new("dissolve");
			public static readonly AnimatorWrapper.State Aim = new("aim_gun");
		}

		public UnityEvent FootprintRightEvent;
		public UnityEvent FootprintLeftEvent;

		[FormerlySerializedAs("_rigidbodyContainerMonoComponent")] [SerializeField]
		protected RigidbodyContainerMonoComponent RigidbodyContainerMonoComponent;

		[SerializeField, Required] private Animator _animator;
		[SerializeField] private EnumSelector<VfxId> _projectileHitVfx;
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

			QuantumEvent.Subscribe<EventOnPlayerAlive>(this, HandleEventOnPlayerAlive);
			QuantumEvent.Subscribe<EventOnHealthChanged>(this, HandleOnHealthChanged);
			QuantumEvent.Subscribe<EventOnHealthIsZero>(this, HandleOnHealthIsZero);
			QuantumEvent.Subscribe<EventOnStatusModifierSet>(this, HandleOnStatusModifierSet);
			QuantumEvent.Subscribe<EventOnStatusModifierCancelled>(this, HandleOnStatusModifierCancelled);
			QuantumEvent.Subscribe<EventOnStatusModifierFinished>(this, HandleOnStatusModifierFinished);
		}

		protected override void OnInit(QuantumGame game)
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
				duration -= _animator.GetFloat(GameConstants.Visuals.STUN_OUTRO_TIME_ANIMATOR_PARAM);

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

		protected virtual void OnAvatarEliminated(QuantumGame game)
		{
			var frame = game.Frames.Verified;
			var isBattleRoyale = frame.RuntimeConfig.GameMode == GameMode.BattleRoyale;
			
			AnimatorWrapper.SetBool(Bools.Stun, false);
			AnimatorWrapper.SetBool(Bools.Pickup, false);
			Dissolve(isBattleRoyale, 0, GameConstants.Visuals.DISSOLVE_END_ALPHA_CLIP_VALUE, GameConstants.Visuals.DISSOLVE_DELAY,
			         GameConstants.Visuals.DISSOLVE_DURATION);
		}

		private void HandleOnEntityDestroyed(QuantumGame game)
		{
			transform.parent = null;

			QuantumEvent.UnsubscribeListener(this);
			QuantumCallback.UnsubscribeListener(this);
		}

		private void HandleOnHealthIsZero(EventOnHealthIsZero callback)
		{
			if (callback.Entity != EntityView.EntityRef)
			{
				return;
			}

			var direction = Vector3.zero;

			if (EntityViewUpdaterService.TryGetView(callback.Attacker, out var attackerView))
			{
				direction = (transform.position - attackerView.transform.position).normalized;
			}

			AnimatorWrapper.Enabled = false;
			direction = direction.sqrMagnitude > Mathf.Epsilon ? direction : transform.rotation.eulerAngles.normalized;
			direction *= Mathf.Lerp(GameConstants.Visuals.PLAYER_RAGDOLL_FORCE_MIN, GameConstants.Visuals.PLAYER_RAGDOLL_FORCE_MAX,
			                        (float) callback.DamageAmount / callback.MaxHealth);

			RigidbodyContainerMonoComponent.SetState(true);
			OnAvatarEliminated(callback.Game);

			RigidbodyContainerMonoComponent.AddForce(direction, ForceMode.VelocityChange);
		}

		private void HandleEventOnPlayerAlive(EventOnPlayerAlive evnt)
		{
			if (evnt.Entity != EntityView.EntityRef)
			{
				return;
			}
			
			Dissolve(false, 0,0,0, 0);
		}
		
		private void HandleOnHealthChanged(EventOnHealthChanged evnt)
		{
			if (evnt.Entity != EntityView.EntityRef || evnt.PreviousHealth <= evnt.CurrentHealth)
			{
				return;
			}

			var cacheTransform = transform;

			Services.VfxService.Spawn(_projectileHitVfx).transform
			        .SetPositionAndRotation(cacheTransform.position, cacheTransform.rotation);
			_animatorWrapper.SetTrigger(Triggers.Hit);
			Services.AudioFxService.PlayClip3D(AudioId.ActorHit01, transform.position);
			RenderersContainerProxy.SetMaterialPropertyValue(_hitProperty, 0, 1, GameConstants.Visuals.HIT_DURATION);
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
			transform.localScale *= GameConstants.Visuals.STAR_STATUS_CHARACTER_SCALE_MULTIPLIER;

			yield return new WaitForSeconds(time);

			FinishStar();
		}

		private void FinishStar()
		{
			_starCoroutine = null;
			transform.localScale /= GameConstants.Visuals.STAR_STATUS_CHARACTER_SCALE_MULTIPLIER;
		}
	}
}
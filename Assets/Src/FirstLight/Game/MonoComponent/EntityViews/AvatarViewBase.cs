using System.Collections;
using FirstLight.Services;
using FirstLight.Game.Ids;
using FirstLight.Game.MonoComponent.Collections;
using FirstLight.Game.MonoComponent.Vfx;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

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
		private IGameServices _services;
		
		// /// <summary>
		// /// Animation booleans to play in the avatar
		// /// </summary>
		// protected static class Bools
		// {
		// 	public static readonly AnimatorWrapper.Bool Move = new("move");
		// 	public static readonly AnimatorWrapper.Bool Aim = new("aim");
		// 	public static readonly AnimatorWrapper.Bool Stun = new("stun");
		// 	public static readonly AnimatorWrapper.Bool Pickup = new("pickup");
		// 	public static readonly AnimatorWrapper.Bool Furious = new("furious");
		// 	public static readonly AnimatorWrapper.Bool Flying = new("flying");
		// }
		//
		// /// <summary>
		// /// Animation triggers to play in the avatar
		// /// </summary>
		// public static class Triggers
		// {
		// 	public static readonly AnimatorWrapper.Trigger Shoot = new("shoot");
		// 	public static readonly AnimatorWrapper.Trigger Die = new("die");
		// 	public static readonly AnimatorWrapper.Trigger Hit = new("hit");
		// 	public static readonly AnimatorWrapper.Trigger Victory = new("victory");
		// 	public static readonly AnimatorWrapper.Trigger Spawn = new("spawn");
		// 	public static readonly AnimatorWrapper.Trigger Revive = new("revive");
		// 	public static readonly AnimatorWrapper.Trigger Special = new("special");
		// 	public static readonly AnimatorWrapper.Trigger Charge = new("charge");
		// 	public static readonly AnimatorWrapper.Trigger Jump = new("jump");
		// 	public static readonly AnimatorWrapper.Trigger Melee = new("melee");
		// 	public static readonly AnimatorWrapper.Trigger PLF = new("plf");
		// 	public static readonly AnimatorWrapper.Trigger KnockedOut = new("knockedout");
		// 	public static readonly AnimatorWrapper.Trigger Revived = new("revived");
		// }
		
		[SerializeField, Required] private Animator _animator;
		[SerializeField] private Vector3 _vfxLocalScale = Vector3.one;

		protected CharacterSkinMonoComponent _skin;
		private Coroutine _stunCoroutine;
		private Coroutine _materialsCoroutine;
		private Coroutine _starCoroutine;
		private Vfx<VfxId> _statusVfx;

		/// <summary>
		/// The readonly <see cref="AnimatorWrapper"/> to play the avatar animations
		/// </summary>
		public CharacterSkinMonoComponent CharacterSkin => _skin;

		private void OnDestroy()
		{
			CleanUp();
		}

		protected override void OnAwake()
		{
			_skin = GetComponent<CharacterSkinMonoComponent>();
			_services = MainInstaller.ResolveServices();
			
			QuantumEvent.Subscribe<EventOnHealthIsZeroFromAttacker>(this, HandleOnHealthIsZeroFromAttacker);
			QuantumEvent.Subscribe<EventOnStatusModifierSet>(this, HandleOnStatusModifierSet);
			QuantumEvent.Subscribe<EventOnStatusModifierCancelled>(this, HandleOnStatusModifierCancelled);
			QuantumEvent.Subscribe<EventOnStatusModifierFinished>(this, HandleOnStatusModifierFinished);
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

				_stunCoroutine = StartCoroutine(StunCoroutine(duration));
			}

			if (statusType == StatusModifierType.Star)
			{
				// Scale UP the character if it has a Star status
				_starCoroutine = StartCoroutine(StarCoroutine(duration));
			}

			if (statusType.TryGetVfx(out MaterialVfxId materialVfx))
			{
				RenderersContainerProxy.SetMaterial(materialVfx, true);
				_materialsCoroutine = StartCoroutine(ResetMaterials(duration));
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
			if (!Culled)
			{
				_services.VfxService.Spawn(VfxId.DeathEffect).transform.position = transform.position + Vector3.up;
			}
			
			_skin.TriggerDie();
		}

		private void HandleOnHealthIsZeroFromAttacker(EventOnHealthIsZeroFromAttacker callback)
		{
			if (callback.Entity != EntityView.EntityRef)
			{
				return;
			}
			
			SetCulled(false);
			
			OnAvatarEliminated(callback.Game);
		}

		
		/// <summary>www
		/// Updates the color of the given character for the duration
		/// </summary>
		public void UpdateAdditiveColor(Color color, float duration)
		{
			RenderersContainerProxy.SetAdditiveColor(color);
			StartCoroutine(EndBlink(duration));
		}
		
		private IEnumerator EndBlink(float duration)
		{
			yield return new WaitForSeconds(duration);
			RenderersContainerProxy.SetAdditiveColor(Color.black);
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
				StopCoroutine(_materialsCoroutine);
				RenderersContainerProxy.ResetMaterials();
				_materialsCoroutine = null;
			}

			if (_stunCoroutine != null)
			{
				StopCoroutine(_stunCoroutine);
				FinishStun();
			}

			if (_starCoroutine != null)
			{
				StopCoroutine(_starCoroutine);
				FinishStar();
			}
		}
		
		private void FinishStun()
		{
			_stunCoroutine = null;
			_skin.TriggerRestore();
		}

		private IEnumerator StunCoroutine(float time)
		{
			_skin.TriggerStun();

			yield return new WaitForSeconds(time);

			FinishStun();
		}

		private IEnumerator ResetMaterials(float time)
		{
			yield return new WaitForSeconds(time);

			// If game disconnects and unloads assets, this async method still runs (CoroutineService
			// and this script can become null.
			if (!this.IsDestroyed())
			{
				RenderersContainerProxy.ResetMaterials();
			}
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
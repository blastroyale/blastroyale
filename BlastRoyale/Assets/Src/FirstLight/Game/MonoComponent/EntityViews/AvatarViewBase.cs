using System;
using System.Collections;
using FirstLight.Game.Domains.VFX;
using FirstLight.Services;
using FirstLight.Game.Ids;
using FirstLight.Game.MonoComponent.Collections;
using FirstLight.Game.MonoComponent.Vfx;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <summary>
	/// Polls and listens th Actor state to update this avatar animations accordingly.
	/// Sets animation parameters for contexts shared between players and enemies.
	/// </summary>
	[RequireComponent(typeof(RenderersContainerProxyMonoComponent))]
	public abstract class AvatarViewBase : EntityMainViewBase
	{
		[SerializeField] private Vector3 _vfxLocalScale = Vector3.one;

		protected CharacterSkinMonoComponent _skin;
		private IGameServices _services;
		private IMatchServices _matchServices;
		private Coroutine _stunCoroutine;
		private Coroutine _materialsCoroutine;
		private AbstractVfx<VfxId> _statusAbstractVfx;

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
			_matchServices = MainInstaller.ResolveMatchServices();

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
				_stunCoroutine = StartCoroutine(StunCoroutine(duration));
			}

			if (statusType.TryGetVfx(out MaterialVfxId materialVfx))
			{
				RenderersContainerProxy.SetMaterial(materialVfx, true);
				_materialsCoroutine = StartCoroutine(ResetMaterials(duration));
			}
			else if (statusType.TryGetVfx(out VfxId vfx))
			{
				if (_statusAbstractVfx != null)
				{
					_statusAbstractVfx.Despawn();
					_statusAbstractVfx = null;
				}

				_statusAbstractVfx = _matchServices.VfxService.Spawn(vfx);

				var cachedTransform = _statusAbstractVfx.transform;

				_statusAbstractVfx.StartDespawnTimer(duration);
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
				_matchServices.VfxService.Spawn(VfxId.DeathEffect).transform.position = transform.position + Vector3.up;
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

			if (!this.IsDestroyed())
			{
				RenderersContainerProxy.ResetAdditiveColor();
			}
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
			if (_statusAbstractVfx != null)
			{
				_statusAbstractVfx.Despawn();
				_statusAbstractVfx = null;
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
	}
}
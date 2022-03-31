using System.Collections;
using System.Numerics;
using Quantum;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <summary>
	/// Shows any visual feedback for the projectiles created in the scene via quantum.
	/// This object is only responsible for the view.
	/// </summary>
	public class ProjectileViewMonoComponent : EntityViewBase
	{
		[SerializeField] private ParticleSystem _hitEffect;
		[SerializeField] private ParticleSystem _failedHitEffect;
		
		private Coroutine _recoverEffectWhenEndedCoroutine;

		protected override void OnAwake()
		{
			QuantumEvent.Subscribe<EventOnProjectileSuccessHit>(this, OnEventOnProjectileHit);
			QuantumEvent.Subscribe<EventOnProjectileFailedHit>(this, OnProjectileFailedHit);
		}

		private void OnProjectileFailedHit(EventOnProjectileFailedHit callback)
		{
			if (callback.Projectile != EntityRef)
			{
				return;
			}
			
			PlayEffect(_failedHitEffect, callback.LastPosition.ToUnityVector3());
		}

		private void OnEventOnProjectileHit(EventOnProjectileSuccessHit callback)
		{
			if (callback.Projectile != EntityRef)
			{
				return;
			}
			
			PlayEffect(_hitEffect, callback.HitPosition.ToUnityVector3());
		}

		private void PlayEffect(ParticleSystem effect, Vector3 position)
		{
			if (_recoverEffectWhenEndedCoroutine != null)
			{
				Services.CoroutineService.StopCoroutine(_recoverEffectWhenEndedCoroutine);
			}
			
			var effectTransform = effect.transform;
			
			_recoverEffectWhenEndedCoroutine = Services.CoroutineService.StartCoroutine(RecoverEffectWhenEnded(effect, transform));

			effectTransform.SetParent(null);
			effectTransform.position = position;
			effect.Play();
		}

		private IEnumerator RecoverEffectWhenEnded(ParticleSystem effect, Transform transform)
		{
			yield return new WaitForSeconds(effect.main.duration);
			
			effect.transform.SetParent(transform);

			_recoverEffectWhenEndedCoroutine = null;
		}
	}
}
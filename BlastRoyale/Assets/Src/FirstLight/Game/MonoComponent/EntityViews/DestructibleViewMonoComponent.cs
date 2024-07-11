using System.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <summary>
	/// Shows any visual feedback explosive things, like Barrel.
	/// </summary>
	public class DestructibleViewMonoComponent : EntityMainViewBase
	{
		private static readonly int _dieHash = Animator.StringToHash("die");
		private static readonly int _onfireHash = Animator.StringToHash("onfire");
		private static readonly int _hitHash = Animator.StringToHash("hit");

		public ParticleSystem DestroyEffect;
		public AudioId DestroyAudio;
		
		[SerializeField, Required] private Animator _animator;

		protected override void OnAwake()
		{
			QuantumEvent.Subscribe<EventOnDestructibleScheduled>(this, HandleDestructionScheduled);
			QuantumEvent.Subscribe<EventOnPlayerAttackHit>(this, HandleProjectileHit);
		}
		
		protected override void OnInit(QuantumGame game)
		{
			var frame = game.Frames.Verified;

			if (frame.TryGet<Destructible>(EntityView.EntityRef, out var destructible) && destructible.IsDestructing)
			{
				var lifetime = destructible.TimeToDestroy - frame.Time;
				StartDestruction(lifetime.AsFloat, destructible.SplashRadius.AsFloat);
			}
			
			EntityView.OnEntityDestroyed.AddListener(OnEntityDestroyed);
		}

		private void OnEntityDestroyed(QuantumGame game)
		{
			transform.parent = null;
		}

		private void HandleProjectileHit(EventOnPlayerAttackHit callback)
		{
			if (callback.HitEntity != EntityRef)
			{
				return;
			}
			
			_animator.SetTrigger(_hitHash);
		}

		private void HandleDestructionScheduled(EventOnDestructibleScheduled callback)
		{
			if (callback.Entity != EntityView.EntityRef)
			{
				return;
			}
			
			var lifetime = callback.Destructible.TimeToDestroy - callback.Game.Frames.Verified.Time;

			StartDestruction(Mathf.Max(0f, lifetime.AsFloat), callback.Destructible.SplashRadius.AsFloat);
		}

		private async void StartDestruction(float lifetime, float radius)
		{
			_animator.SetTrigger(_onfireHash);
			QuantumEvent.UnsubscribeListener(this);

			if (DestroyEffect != null)
			{
				DestroyEffect.Play();
			}

			if (DestroyAudio != AudioId.None)
			{
				MainInstaller.ResolveServices().AudioFxService.PlayClip2D(DestroyAudio);
			}
			
			await Task.Delay((int) (lifetime * 1000));

			if (this.IsDestroyed())
			{
				return;
			}

			_animator.SetTrigger(_dieHash);
		}
	}
}
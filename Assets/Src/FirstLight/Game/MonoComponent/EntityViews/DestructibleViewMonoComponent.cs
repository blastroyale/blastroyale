using System.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Game.MonoComponent.Vfx;
using FirstLight.Game.Utils;
using MoreMountains.NiceVibrations;
using Quantum;
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
		
		[SerializeField] private Animator _animator;
		[SerializeField] private float _dissolveDelay = 1f;
		
		protected override void OnInit()
		{
			var frame = QuantumRunner.Default.Game.Frames.Verified;

			if (frame.TryGet<Destructible>(EntityView.EntityRef, out var destructible) && destructible.IsDestructing)
			{
				var lifetime = destructible.TimeToDestroy - frame.Time;

				StartDestruction(lifetime.AsFloat, destructible.SplashRadius.AsFloat);
			}
			
			EntityView.OnEntityDestroyed.AddListener(OnEntityDestroyed);
			QuantumEvent.Subscribe<EventOnDestructibleScheduled>(this, HandleDestructionScheduled);
			QuantumEvent.Subscribe<EventOnProjectileHit>(this, HandleProjectileHit);
		}

		private void OnEntityDestroyed(QuantumGame game)
		{
			transform.parent = null;
		}

		private void HandleProjectileHit(EventOnProjectileHit callback)
		{
			if (callback.HitData.TargetHit != EntityRef)
			{
				return;
			}
			
			_animator.SetTrigger(_hitHash);
		}

		private void HandleDestructionScheduled(EventOnDestructibleScheduled callback)
		{
			if (callback.ProjectileData.Attacker != EntityView.EntityRef)
			{
				return;
			}
			
			var lifetime = callback.Destructible.TimeToDestroy - callback.Game.Frames.Verified.Time;

			StartDestruction(Mathf.Max(0f, lifetime.AsFloat), callback.Destructible.SplashRadius.AsFloat);
		}

		private async void StartDestruction(float lifetime, float radius)
		{
			var vfx = Services.VfxService.Spawn(VfxId.DangerIndicator) as DangerIndicatorVfxMonoComponent;
			
			_animator.SetTrigger(_onfireHash);
			vfx.Init(transform.position, lifetime, radius);
			QuantumEvent.UnsubscribeListener(this);
			
			await Task.Delay((int) (lifetime * 1000));

			if (this.IsDestroyed())
			{
				return;
			}

			_animator.SetTrigger(_dieHash);
			MMVibrationManager.Haptic(HapticTypes.HeavyImpact);
			
			this.LateCall(_dissolveDelay, () =>
			{
				Dissolve(false);
			});
		}
	}
}
using System.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Game.MonoComponent.Vfx;
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
		
		[SerializeField, Required] private Animator _animator;
		[SerializeField] private float _dissolveDelay = 1f;

		protected override void OnAwake()
		{
			QuantumEvent.Subscribe<EventOnDestructibleScheduled>(this, HandleDestructionScheduled);
			QuantumEvent.Subscribe<EventOnProjectileTargetableHit>(this, HandleProjectileHit);
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

		private void HandleProjectileHit(EventOnProjectileTargetableHit callback)
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
			
			await Task.Delay((int) (lifetime * 1000));

			if (this.IsDestroyed())
			{
				return;
			}

			_animator.SetTrigger(_dieHash);
			
			this.LateCall(_dissolveDelay, () =>
			{
				Dissolve(false, 0, GameConstants.Visuals.DISSOLVE_END_ALPHA_CLIP_VALUE, GameConstants.Visuals.DISSOLVE_DELAY,
				         GameConstants.Visuals.DISSOLVE_DURATION);
			});
		}
	}
}
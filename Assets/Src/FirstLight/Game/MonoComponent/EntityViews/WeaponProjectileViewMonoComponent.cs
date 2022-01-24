using FirstLight.Game.MonoComponent.Vfx;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <summary>
	/// This Mono component handles spawning vfx when a weapon receives a quantum projectile fire and player attack event.
	/// </summary>
	public class WeaponProjectileViewMonoComponent : EntityViewBase
	{
		[SerializeField] private ParticleSystem _particleSystem;
		[SerializeField] private AdventureVfxSpawnerMonoComponent _spawnVfx;
		
		protected override void OnInit()
		{
			QuantumEvent.Subscribe<EventOnPlayerAttacked>(this, OnEventOnPlayerAttacked);
			QuantumEvent.Subscribe<EventOnProjectileFired>(this, OnEventOnProjectileFired);
		}
		
		private void OnEventOnProjectileFired(EventOnProjectileFired callback)
		{
			if (_spawnVfx && callback.ProjectileData.Attacker == EntityRef)
			{
				_spawnVfx.Spawn();
			}
		}
		
		private void OnEventOnPlayerAttacked(EventOnPlayerAttacked callback)
		{
			if (_particleSystem)
			{
				if (callback.PlayerEntity == EntityRef)
				{
					_particleSystem.Simulate(0.0f, true, true);
					_particleSystem.Play();
				}
			}
		}
	}
}
using Quantum;
using UnityEngine;


namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <summary>
	/// This Mono component handles particle system playback when player aims.
	/// </summary>
	public class WeaponViewMonoComponent : EntityViewBase
	{
		[SerializeField] private ParticleSystem _particleSystem;
		
		protected override void OnInit()
		{
			QuantumEvent.Subscribe<EventOnPlayerAttack>(this, OnEventOnPlayerAttack);
			QuantumEvent.Subscribe<EventOnPlayerStopAttack>(this, OnEventOnPlayerStopAttack);
			QuantumEvent.Subscribe<EventOnGameEnded>(this, OnEventOnGameEnded);
		}

		private void OnEventOnPlayerAttack(EventOnPlayerAttack callback)
		{
			if (EntityRef != callback.PlayerEntity)
			{
				return;
			}

			if (!_particleSystem.isPlaying)
			{
				var config = Services.ConfigsProvider.GetConfig<QuantumWeaponConfig>((int) callback.WeaponConfigId);

				if (config.AttackHitTime.AsFloat > 0)
				{
					// Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.
					var main = _particleSystem.main;
					main.startLifetime = config.AttackHitTime.AsFloat;
					main.startSpeed = config.AttackRange.AsFloat / config.AttackHitTime.AsFloat;
					main.startDelay = 0;
					main.loop = false;
					main.maxParticles = 10;
					var emission = _particleSystem.emission;
					emission.rateOverTime = 0.1f;
				}
			}
			
			_particleSystem.Stop();
			_particleSystem.time = 0;
			_particleSystem.Play();
		}

		private void OnEventOnPlayerStopAttack(EventOnPlayerStopAttack callback)
		{
			if (EntityRef != callback.PlayerEntity)
			{
				return;
			}

			_particleSystem.Stop();
		}

		private void OnEventOnGameEnded(EventOnGameEnded callback)
		{
			_particleSystem.Stop();
		}
	}
}
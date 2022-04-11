using Photon.Deterministic;
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

		protected override void OnAwake()
		{
			QuantumEvent.Subscribe<EventOnPlayerWeaponChanged>(this, OnWeaponChanged);
			QuantumEvent.Subscribe<EventOnPlayerAttack>(this, OnEventOnPlayerAttack);
			QuantumEvent.Subscribe<EventOnPlayerStopAttack>(this, OnEventOnPlayerStopAttack);
			QuantumEvent.Subscribe<EventOnGameEnded>(this, OnEventOnGameEnded);
		}
		
		protected override void OnInit(QuantumGame game)
		{
			var f = game.Frames.Verified;
			var playerCharacter = f.Get<PlayerCharacter>(EntityRef);
			
			UpdateParticleSystem((int)playerCharacter.CurrentWeapon.GameId);
		}

		private void OnWeaponChanged(EventOnPlayerWeaponChanged callback)
		{
			if (EntityRef != callback.Entity)
			{
				return;
			}
			
			UpdateParticleSystem((int)callback.Weapon.GameId);
		}

		private void OnEventOnPlayerAttack(EventOnPlayerAttack callback)
		{
			if (EntityRef != callback.PlayerEntity)
			{
				return;
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

		private void UpdateParticleSystem(int weaponId)
		{
			var config = Services.ConfigsProvider.GetConfig<QuantumWeaponConfig>(weaponId);
			var main = _particleSystem.main;
			var emission = _particleSystem.emission;
			var speed = config.AttackHitSpeed.AsFloat;
			
			if (speed < float.Epsilon)
			{
				return;
			}
			
			// Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.
			main.startLifetime = speed * config.AttackRange.AsFloat;
			main.startSpeed = speed;
			main.startDelay = 0;
			main.loop = false;
			main.maxParticles = 10;
			emission.rateOverTime = 0.1f;
		}
	}
}
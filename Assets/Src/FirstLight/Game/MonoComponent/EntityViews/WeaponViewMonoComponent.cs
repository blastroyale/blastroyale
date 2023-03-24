using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;


namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <summary>
	/// This Mono component handles particle system playback when player aims.
	/// </summary>
	public class WeaponViewMonoComponent : EntityViewBase
	{
		[SerializeField, Required] private ParticleSystem _particleSystem;

		protected override void OnAwake()
		{
			QuantumEvent.Subscribe<EventOnPlayerAttack>(this, OnEventOnPlayerAttack);
			QuantumEvent.Subscribe<EventOnPlayerStopAttack>(this, OnEventOnPlayerStopAttack);
			QuantumEvent.Subscribe<EventOnGameEnded>(this, OnEventOnGameEnded);
			QuantumEvent.Subscribe<EventOnPlayerWeaponChanged>(this, OnPlayerWeaponChanged);
		}

		protected override void OnInit(QuantumGame game)
		{
			var f = game.Frames.Verified;
			var playerCharacter = f.Get<PlayerCharacter>(EntityRef);

			UpdateParticleSystem(playerCharacter.CurrentWeapon.GameId);
		}

		private void OnPlayerWeaponChanged(EventOnPlayerWeaponChanged callback)
		{	
			if (EntityRef != callback.Entity)
			{
				return;
			}
			
			UpdateParticleSystem(callback.Weapon.GameId);
		}

		private void OnEventOnPlayerAttack(EventOnPlayerAttack callback)
		{
			if (EntityRef != callback.PlayerEntity)
			{
				return;
			}
			
			if (callback.Game.Frames.Predicted.IsCulled(EntityRef))
			{
				return;
			}

			_particleSystem.Stop();
			_particleSystem.time = 0;
			_particleSystem.Play();
			var config = Services.ConfigsProvider.GetConfig<QuantumWeaponConfig>((int)callback.Weapon.GameId);


			if (config.IsProjectile)
			{
				return;
			}

			var main = _particleSystem.main;
			var shape = _particleSystem.shape;
			var arc = 0;
			var rotation = -(90f + callback.ShotDir.AsFloat);

			if (config.NumberOfShots > 1)
			{
				arc = (int)callback.AttackAngle;
				rotation = -(90 - (shape.arc / 2));
			}
			
			shape.arc = arc;
			shape.arcMode = ParticleSystemShapeMultiModeValue.BurstSpread;
			shape.rotation = new Vector3(90, rotation, 0);
			main.startLifetime = callback.AttackRange.AsFloat / config.AttackHitSpeed.AsFloat;

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

		private void UpdateParticleSystem(GameId weaponId)
		{
			var config = Services.ConfigsProvider.GetConfig<QuantumWeaponConfig>((int) weaponId);
			var main = _particleSystem.main;
			var emission = _particleSystem.emission;
			var speed = config.AttackHitSpeed.AsFloat;

			if (speed < float.Epsilon || config.IsProjectile)
			{
				return;
			}

			if (_particleSystem.isPlaying)
			{
				_particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			}

			// Particle System modules do not need to be reassigned back to the system; they are interfaces and not independent objects.
			main.duration = config.AttackCooldown.AsFloat;
			main.startDelay = 0;
			main.maxParticles = 100;
			emission.rateOverTime = 0;
			main.loop = false;
			main.startSpeed = config.NumberOfShots > 1 ? new ParticleSystem.MinMaxCurve(speed, speed * 1.2f) : speed;

			emission.burstCount = 1;
			var burst = emission.GetBurst(0);
			burst.count = config.NumberOfShots;
			burst.repeatInterval = 1 / config.AttackCooldown.AsFloat;
			emission.SetBurst(0, burst);
		}

	}
}
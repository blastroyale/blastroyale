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
			QuantumEvent.Subscribe<EventOnPlayerEquipmentStatsChanged>(this, OnStatsChanged);
		}
		
		protected override void OnInit(QuantumGame game)
		{
			var f = game.Frames.Verified;
			var playerCharacter = f.Get<PlayerCharacter>(EntityRef);
			var stats = f.Get<Stats>(EntityRef);

			UpdateParticleSystem((int)playerCharacter.CurrentWeapon.GameId, stats);
		}

		private void OnStatsChanged(EventOnPlayerEquipmentStatsChanged callback)
		{
			if (EntityRef != callback.Entity)
			{
				return;
			}
			var f = callback.Game.Frames.Verified;
			var weaponGameId = f.Get<PlayerCharacter>(EntityRef).CurrentWeapon.GameId;
			var stats = f.Get<Stats>(EntityRef);
			UpdateParticleSystem((int)weaponGameId, stats);
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

			var config = Services.ConfigsProvider.GetConfig<QuantumWeaponConfig>((int)callback.Weapon.GameId);

			if (config.IsProjectile)
			{
				return;
			}

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

		private void UpdateParticleSystem(int weaponId, Stats stats)
		{
			var config = Services.ConfigsProvider.GetConfig<QuantumWeaponConfig>(weaponId);
			
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
			main.maxParticles = 50;
			emission.rateOverTime = 0;
			main.loop = false;
			main.startSpeed = speed;
			main.startLifetime = stats.GetStatData(StatType.AttackRange).StatValue.AsFloat / speed ;

			emission.burstCount = 1;
			var burst = emission.GetBurst(0);
			burst.count = config.NumberOfShots;
			burst.repeatInterval = 1 / config.AttackCooldown.AsFloat;
			emission.SetBurst(0, burst);
		}

	}
}
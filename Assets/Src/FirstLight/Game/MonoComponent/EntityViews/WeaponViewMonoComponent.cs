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
			if (_particleSystem.isPlaying || EntityRef != callback.PlayerEntity)
			{
				return;
			}

			_particleSystem.Simulate(0.0f, true, true);
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
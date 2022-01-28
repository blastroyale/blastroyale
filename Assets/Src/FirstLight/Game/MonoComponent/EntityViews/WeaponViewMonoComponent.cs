using FirstLight.Game.Input;
using FirstLight.Game.MonoComponent.Vfx;
using Quantum;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <summary>
	/// This Mono component handles particle system playback when player aims.
	/// </summary>
	public class WeaponViewMonoComponent : EntityViewBase
	{
		[SerializeField] private ParticleSystem _particleSystem;
		
		private LocalInput _localInput;
		
		protected override void OnInit()
		{
			_localInput = new LocalInput();

			_localInput.Gameplay.AimButton.canceled += _ => _particleSystem.Stop();
			
			_localInput.Enable();
			
			QuantumEvent.Subscribe<EventOnPlayerAttack>(this, OnEventOnPlayerAttack);
			QuantumEvent.Subscribe<EventOnPlayerStopAttack>(this, OnEventOnPlayerStopAttack);
			EntityView.OnEntityDestroyed.AddListener(OnEntityDestroyed);
		}

		private void OnEntityDestroyed(QuantumGame game)
		{
			_localInput?.Dispose();
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
	}
}
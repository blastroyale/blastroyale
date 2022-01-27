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
			
			_localInput.Gameplay.AimButton.canceled += context =>
			{
				if (_particleSystem && _particleSystem.isPlaying)
				{
					_particleSystem.Stop();
				}
			};
			
			_localInput.Enable();
			
			QuantumEvent.Subscribe<EventOnPlayerAttack>(this, OnEventOnPlayerAttack);
			EntityView.OnEntityDestroyed.AddListener(OnEntityDestroyed);
		}

		private void OnEntityDestroyed(QuantumGame game)
		{
			_localInput?.Dispose();
		}

		private void OnEventOnPlayerAttack(EventOnPlayerAttack callback)
		{
			if (!_particleSystem || EntityRef != callback.PlayerEntity)
			{
				return;
			}

			if (_particleSystem.isPlaying)
			{
				return;
			}
			
			_particleSystem.Simulate(0.0f, true, true);
			_particleSystem.Play();
		}
	}
}
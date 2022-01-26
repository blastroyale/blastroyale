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
			
			_localInput.Gameplay.Aim.performed += OnAim;
			_localInput.Gameplay.Aim.canceled += OnAim;
			
			_localInput.Enable();
			
			EntityView.OnEntityDestroyed.AddListener(OnEntityDestroyed);
		}

		private void OnEntityDestroyed(QuantumGame game)
		{
			_localInput?.Dispose();
		}
		
		private void OnAim(InputAction.CallbackContext context)
		{
			Debug.Log("OnAim");
			
			if (_particleSystem == null)
			{
				return;
			}

			var game = QuantumRunner.Default;
			var frame = game == null ? null : game.Game?.Frames?.Verified;
			var isEmptied = frame != null && frame.TryGet<Weapon>(EntityView.EntityRef, out var weapon) && weapon.IsEmpty;
			var direction = context.ReadValue<Vector2>();
			var isShooting = direction.sqrMagnitude > 0;
			var isPlaying = _particleSystem.isPlaying;
			
			if (!isPlaying && !isEmptied && isShooting)
			{
				_particleSystem.Simulate(0.0f, true, true);
				_particleSystem.Play();
			}
			else if (isPlaying && (!isShooting || isEmptied))
			{
				_particleSystem.Stop();
			}
		}
	}
}
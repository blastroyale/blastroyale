using System;
using FirstLight.Game.Input;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MatchHudViews;
using Quantum;
using Sirenix.OdinInspector.Editor.Licensing;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Service that hooks into the input system to render the player match indicators.
	/// Will hook when 
	/// </summary>
	public interface IPlayerIndicatorService : IDisposable
	{
		/// <summary>
		/// Registers the indicators input listeners
		/// </summary>
		void RegisterListeners();
	}

	public class PlayerIndicatorsService : IPlayerIndicatorService, MatchServices.IMatchService
	{
		private IGameServices _services;
		private IMatchServices _matchServices;
		private LocalInput.GameplayActions _inputs;
		private bool _shooting;
		private int _specialPressed = -1;
		private bool _disposed = false;

		private LocalPlayerIndicatorContainerView _indicatorContainerView;

		public PlayerIndicatorsService()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerDead>(this, OnPlayerDied);
		}

		public void OnMatchStarted(QuantumGame game, bool isReconnect)
		{
			RegisterListeners();
			if (isReconnect)
			{
				InitializeLocalPlayer(game);
			}
		}

		public void OnMatchEnded(QuantumGame game, bool isDisconnected)
		{
			Dispose();
		}

		private void OnPlayerDied(EventOnLocalPlayerDead ev)
		{
			Dispose();
		}

		public void RegisterListeners()
		{
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_indicatorContainerView = new LocalPlayerIndicatorContainerView();
			_inputs = _matchServices.PlayerInputService.Input.Gameplay;
			_inputs.Move.performed += OnMove;
			_inputs.AimButton.performed += OnShooting;
			_inputs.AimButton.canceled += OnShooting;
			_inputs.SpecialButton0.started += OnSpecial0;
			_inputs.SpecialButton0.performed += OnSpecial0;
			_inputs.SpecialButton0.canceled += OnSpecial0;
			_inputs.SpecialButton1.started += OnSpecial1;
			_inputs.SpecialButton1.performed += OnSpecial1;
			_inputs.SpecialButton1.canceled += OnSpecial1;
			_inputs.SpecialAim.performed += OnSpecialAim;
		}

		public void Dispose()
		{
			if (_disposed) return;
			QuantumEvent.UnsubscribeListener(this);
			_services?.TickService.Unsubscribe(OnUpdate);
			_indicatorContainerView?.Dispose();
			if (_inputs.Get() != null)
			{
				_inputs.Move.performed -= OnMove;
				_inputs.AimButton.performed -= OnShooting;
				_inputs.AimButton.canceled -= OnShooting;
				_inputs.SpecialButton0.started -= OnSpecial0;
				_inputs.SpecialButton0.performed -= OnSpecial0;
				_inputs.SpecialButton0.canceled -= OnSpecial0;
				_inputs.SpecialButton1.started -= OnSpecial1;
				_inputs.SpecialButton1.performed -= OnSpecial1;
				_inputs.SpecialButton1.canceled -= OnSpecial1;
				_inputs.SpecialAim.performed -= OnSpecialAim;
			}
			_disposed = true;
		}

		private bool CanListen() => QuantumRunner.Default.IsDefinedAndRunning();

		private void OnUpdate(float timeDelta)
		{
			if (!CanListen()) return;
			_indicatorContainerView?.OnUpdateAim(
				QuantumRunner.Default.Game.Frames.Predicted,
				_inputs.Aim.ReadValue<Vector2>().ToFPVector2(),
				_shooting);
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			InitializeLocalPlayer(callback.Game);
		}

		private void OnSpecial0(InputAction.CallbackContext c)
		{
			if (!CanListen()) return;
			OnSpecialSetupIndicator(c, 0);
		}

		private void OnSpecial1(InputAction.CallbackContext c)
		{
			if (!CanListen()) return;
			OnSpecialSetupIndicator(c, 1);
		}

		private void OnSpecialAim(InputAction.CallbackContext c)
		{
			if (!CanListen()) return;
			if (_specialPressed != -1)
			{
				_indicatorContainerView.GetSpecialIndicator(_specialPressed)
					.SetTransformState(_inputs.SpecialAim.ReadValue<Vector2>());
			}
		}

		private void OnShooting(InputAction.CallbackContext c)
		{
			_shooting = c.ReadValueAsButton();
		}

		private void OnSpecialSetupIndicator(InputAction.CallbackContext context, int specialIndex)
		{
			var indicator = _indicatorContainerView.GetSpecialIndicator(specialIndex);
			if (!context.canceled)
			{
				_specialPressed = specialIndex;
				indicator.SetVisualState(true);
				indicator.SetTransformState(Vector2.zero);
				if (FeatureFlags.SPECIAL_RADIUS)
				{
					var radiusIndicator = _indicatorContainerView.GetSpecialRadiusIndicator(specialIndex);
					radiusIndicator.SetVisualState(true);
					radiusIndicator.SetTransformState(Vector2.zero);
				}

				return;
			}

			_specialPressed = -1;
			indicator.SetVisualState(false);
			if (FeatureFlags.SPECIAL_RADIUS)
			{
				var radiusIndicator = _indicatorContainerView.GetSpecialRadiusIndicator(specialIndex);
				radiusIndicator.SetVisualState(false);
			}
		}

		private void OnMove(InputAction.CallbackContext context)
		{
			if (!CanListen()) return;
			var direction = context.ReadValue<Vector2>();
			_indicatorContainerView.OnMoveUpdate(direction, direction != Vector2.zero);
		}

		private unsafe void InitializeLocalPlayer(QuantumGame game)
		{
			if (!CanListen()) return;
			var localPlayer = game.GetLocalPlayerData(false, out var f);
			if (!localPlayer.IsValid || !localPlayer.Entity.IsValid || !localPlayer.Entity.IsAlive(f))
			{
				return;
			}

			if (!_matchServices.EntityViewUpdaterService.TryGetView(localPlayer.Entity, out var playerView))
			{
				return;
			}
			var playerCharacter = f.Get<PlayerCharacter>(localPlayer.Entity);
			_indicatorContainerView.InstantiateAllIndicators();
			_indicatorContainerView.Init(playerView);
			_indicatorContainerView.SetupWeaponInfo(f, playerCharacter.CurrentWeapon.GameId);
			_indicatorContainerView.SetupWeaponSpecials(*playerCharacter.WeaponSlot);
			_services.TickService.SubscribeOnUpdate(OnUpdate);
		}
	}
}
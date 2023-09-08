using System;
using FirstLight.Game.Input;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MatchHudViews;
using Quantum;
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
	}

	public class PlayerIndicatorsService : IPlayerIndicatorService, MatchServices.IMatchService
	{
		private readonly IGameServices _services;
		private readonly IMatchServices _matchServices;

		private readonly LocalPlayerIndicatorContainerView _indicatorContainerView;

		private LocalInput.GameplayActions _inputs;
		private bool _shooting;
		private int _specialPressed = -1;
		private bool _inCancel = false;
		private bool _registered;

		public PlayerIndicatorsService(IMatchServices matchServices, IGameServices gameServices)
		{
			_matchServices = matchServices;
			_services = gameServices;
			_indicatorContainerView = new LocalPlayerIndicatorContainerView();

			QuantumEvent.SubscribeManual<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerDead>(this, OnLocalPlayerDied);
		}

		public void OnMatchStarted(QuantumGame game, bool isReconnect)
		{
			_inputs = _matchServices.PlayerInputService.Input.Gameplay;
			_indicatorContainerView.InstantiateAllIndicators();
			RegisterListeners();
			if (_services.NetworkService.LocalPlayer.IsSpectator() || !isReconnect)
			{
				return;
			}
			InitializeLocalPlayer(game);
		}

		public void OnMatchEnded(QuantumGame game, bool isDisconnected)
		{
			UnregisterListeners();
		}

		private void OnLocalPlayerDied(EventOnLocalPlayerDead ev)
		{
			UnregisterListeners();
			_indicatorContainerView?.Dispose();
		}

		private void RegisterListeners()
		{
			_inputs.Move.AddListener(OnMove);
			_inputs.Aim.AddListener(OnAim);
			_inputs.AimButton.AddListener(OnShooting);
			_inputs.SpecialButton0.AddListener(OnSpecial0);
			_inputs.SpecialButton1.AddListener(OnSpecial1);
			_inputs.SpecialAim.AddListener(OnSpecialAim);
			_inputs.CancelButton.AddListener(OnSpecialCancel);
			_registered = true;
		}

		private void UnregisterListeners()
		{
			if (_registered)
			{
				_inputs.Move.RemoveListener(OnMove);
				_inputs.Aim.RemoveListener(OnAim);
				_inputs.AimButton.RemoveListener(OnShooting);
				_inputs.SpecialButton0.RemoveListener(OnSpecial0);
				_inputs.SpecialButton1.RemoveListener(OnSpecial1);
				_inputs.SpecialAim.RemoveListener(OnSpecialAim);
				_inputs.CancelButton.RemoveListener(OnSpecialCancel);
			}
			QuantumEvent.UnsubscribeListener(this);
		}

		public void Dispose()
		{
			UnregisterListeners();
			_indicatorContainerView?.Dispose();
		}

		private bool CanListen() => QuantumRunner.Default.IsDefinedAndRunning() &&
			_indicatorContainerView != null && _indicatorContainerView.IsInitialized();

		private void OnAim(InputAction.CallbackContext c)
		{
			if (!CanListen()) return;
			_indicatorContainerView?.OnUpdateAim(
				QuantumRunner.Default.Game.Frames.Predicted,
				c.ReadValue<Vector2>().ToFPVector2(),
				_shooting);
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			if (callback.HasRespawned)
			{
				return;
			}
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

		private void OnSpecialCancel(InputAction.CallbackContext c)
		{
			if (!CanListen()) return;
			if (_specialPressed == -1) return;
			
			var buttonValue = c.ReadValue<float>();
			var cancelPressed = c.ReadValueAsButton();
			var radius = _indicatorContainerView.GetSpecialRadiusIndicator(_specialPressed);
			var specialIndicator = _indicatorContainerView.GetSpecialIndicator(_specialPressed);

			if (cancelPressed)
			{
				specialIndicator.ResetColor();
				RemoveSpecialIndicators(_specialPressed);
			}
			else
			{
				_inCancel = buttonValue > 0 && buttonValue < 1;
				if (_inCancel)
				{
					radius.SetColor(Color.red);
					specialIndicator.SetColor(Color.red);
				}
				else
				{
					radius.ResetColor();
					specialIndicator.ResetColor();
				}
			}
		}

		private void OnShooting(InputAction.CallbackContext c)
		{
			if (!QuantumRunner.Default.IsDefinedAndRunning()) return;
			var newValue = c.ReadValueAsButton();
			if (newValue != _shooting && _shooting)
			{
				_indicatorContainerView?.OnUpdateAim(
					QuantumRunner.Default.Game.Frames.Predicted,
					_inputs.Aim.ReadValue<Vector2>().ToFPVector2(),
					newValue);
			}
			_shooting = newValue;
		}

		private void OnSpecialSetupIndicator(InputAction.CallbackContext context, int specialIndex)
		{
			if (!context.canceled) AddSpecialIndicator(specialIndex);
			else RemoveSpecialIndicators(specialIndex);
		}

		private void AddSpecialIndicator(int specialIndex)
		{
			var indicator = _indicatorContainerView.GetSpecialIndicator(specialIndex);
			_specialPressed = specialIndex;
			_inCancel = false;
			indicator.SetVisualState(true);
			indicator.SetTransformState(Vector2.zero);
			var radiusIndicator = _indicatorContainerView.GetSpecialRadiusIndicator(specialIndex);
			radiusIndicator.SetVisualState(true);
			radiusIndicator.SetTransformState(Vector2.zero);
		}

		private void RemoveSpecialIndicators(int specialIndex)
		{
			_specialPressed = -1;
			_inCancel = false;
			_indicatorContainerView.GetSpecialIndicator(specialIndex).SetVisualState(false);
			var radius = _indicatorContainerView.GetSpecialRadiusIndicator(specialIndex);
			radius.SetVisualState(false);
			radius.ResetColor();
		}

		private void OnMove(InputAction.CallbackContext context)
		{
			if (!CanListen()) return;
			var direction = context.ReadValue<Vector2>();
			_indicatorContainerView.OnMoveUpdate(direction, direction != Vector2.zero);
		}

		private unsafe void InitializeLocalPlayer(QuantumGame game)
		{
			if (!QuantumRunner.Default.IsDefinedAndRunning()) return;
			var localPlayer = game.GetLocalPlayerData(true, out var f);
			if (!localPlayer.IsValid || !localPlayer.Entity.IsValid || !localPlayer.Entity.IsAlive(f))
			{
				return;
			}

			if (!_matchServices.EntityViewUpdaterService.TryGetView(localPlayer.Entity, out var playerView))
			{
				return;
			}

			var playerCharacter = f.Get<PlayerCharacter>(localPlayer.Entity);
			_indicatorContainerView.Init(playerView);
			_indicatorContainerView.SetupWeaponInfo(f, playerCharacter.CurrentWeapon.GameId);
			_indicatorContainerView.SetupWeaponSpecials(*playerCharacter.WeaponSlot);
		}
	}
}
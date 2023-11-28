using System;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Input;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using Quantum.Commands;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// This service handles Player Input general interaction for spell controls  
	/// </summary>
	public interface IPlayerInputService
	{
		/// <summary>
		/// Enable accessor for the player spell control input 
		/// </summary>
		LocalInput Input { get; }

		public delegate void SetQuantumInput(CallbackPollInput callback, ref Quantum.Input input);

		/// <summary>
		/// Allows to overwrite the quantum input polling, useful for tests
		/// </summary>
		SetQuantumInput OverwriteCallbackInput { set; }

		public delegate void QuantumInputSent(Quantum.Input input);

		/// <summary>
		/// Called when we set a input in quantum poll function
		/// </summary>
		QuantumInputSent OnQuantumInputSent { get; set; }
	}

	public class PlayerInputService : IPlayerInputService, MatchServices.IMatchService, LocalInput.IGameplayActions
	{
		public LocalInput Input { get; }
		public IPlayerInputService.SetQuantumInput OverwriteCallbackInput { get; set; }
		public IPlayerInputService.QuantumInputSent OnQuantumInputSent { get; set; }

		private readonly IMatchServices _matchServices;
		private readonly IGameServices _gameServices;
		private readonly IGameDataProvider _dataProvider;

		private Quantum.Input _quantumInput;

		private Vector2 _direction;
		private Vector2 _aim;
		private bool _shooting;
		private bool _specialCancel;

		public PlayerInputService(IGameServices gameServices, IMatchServices matchServices,
								  IGameDataProvider dataProvider)
		{
			_matchServices = matchServices;
			_dataProvider = dataProvider;
			_gameServices = gameServices;

			Input = new LocalInput();
			Input.Gameplay.SetCallbacks(this);

			// TODO: Setup input enable / disable for specials based on current weapon
		}

		public void OnMatchStarted(QuantumGame game, bool isReconnect)
		{
			Input.Enable();
			QuantumCallback.SubscribeManual<CallbackPollInput>(this, PollInput);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSkydiveLand>(this, OnLocalPlayerSkydiveLand);

			if (!isReconnect)
			{
				DisableSkydivingControls(true);
			}
		}

		public void OnMatchEnded(QuantumGame game, bool isDisconnected)
		{
			Input.Disable();
			QuantumCallback.UnsubscribeListener(this);
			QuantumEvent.UnsubscribeListener(this);
		}

		public void Dispose()
		{
			Input.Dispose();
		}

		private void OnLocalPlayerSkydiveLand(EventOnLocalPlayerSkydiveLand callback)
		{
			DisableSkydivingControls(false);
		}

		private void DisableSkydivingControls(bool disable)
		{
			if (disable)
			{
				Input.Gameplay.Aim.Disable();
				Input.Gameplay.SpecialAim.Disable();
				Input.Gameplay.SpecialButton0.Disable();
				Input.Gameplay.SpecialButton1.Disable();
				Input.Gameplay.SwitchWeaponButton.Disable();
				Input.Gameplay.CancelButton.Disable();
				Input.Gameplay.Move.Disable();
			}
			else
			{
				Input.Gameplay.Aim.Enable();
				Input.Gameplay.SpecialAim.Enable();
				Input.Gameplay.SpecialButton0.Enable();
				Input.Gameplay.SpecialButton1.Enable();
				Input.Gameplay.SwitchWeaponButton.Enable();
				Input.Gameplay.CancelButton.Enable();
				Input.Gameplay.Move.Enable();
			}
		}

		public void OnMove(InputAction.CallbackContext context)
		{
			_direction = context.ReadValue<Vector2>();
		}

		public void OnAim(InputAction.CallbackContext context)
		{
			_aim = context.ReadValue<Vector2>();
		}

		public void OnAimButton(InputAction.CallbackContext context)
		{
			_shooting = context.ReadValueAsButton();
		}

		public void OnSpecialAim(InputAction.CallbackContext context)
		{
			// do nothing
		}

		public void OnSpecialButton0(InputAction.CallbackContext context)
		{
			if (!_specialCancel)
			{
				OnSpecialButtonUsed(context, 0);
			}
			else _specialCancel = false;
		}

		public void OnSpecialButton1(InputAction.CallbackContext context)
		{
			if (!_specialCancel)
			{
				OnSpecialButtonUsed(context, 1);
			}
			else _specialCancel = false;
		}

		public void OnCancelButton(InputAction.CallbackContext context)
		{
			_specialCancel = context.ReadValueAsButton();
		}

		public void OnSwitchWeaponButton(InputAction.CallbackContext context)
		{
			if (!context.ReadValueAsButton()) return;

			var data = QuantumRunner.Default.Game.GetLocalPlayerData(false, out var f);

			// Check if there is a point in switching or not. Avoid extra commands to save network message traffic $$$
			if (!f.TryGet<PlayerCharacter>(data.Entity, out var pc))
			{
				return;
			}

			int slotIndexToSwitch;

			if (pc.CurrentWeaponSlot == 0 && pc.WeaponSlots[1].Weapon.IsValid())
			{
				slotIndexToSwitch = 1;
			}
			else if (pc.CurrentWeaponSlot == 1)
			{
				slotIndexToSwitch = 0;
			}
			else
			{
				return;
			}

			QuantumRunner.Default.Game.SendCommand(new WeaponSlotSwitchCommand {WeaponSlotIndex = slotIndexToSwitch});
		}

		public void OnTeamPositionPing(InputAction.CallbackContext context)
		{
		}

		private void PollInput(CallbackPollInput callback)
		{
			if (OverwriteCallbackInput == null)
			{
				float moveSpeedPercentage = 100;
				if (_dataProvider.AppDataProvider.MovespeedControl)
				{
					moveSpeedPercentage = Math.Min(_direction.magnitude * 100, 100);
				}
				_quantumInput.SetInput(_aim.ToFPVector2(), _direction.ToFPVector2(), _shooting,
					FP.FromFloat_UNSAFE(moveSpeedPercentage));
			}
			else
			{
				OverwriteCallbackInput?.Invoke(callback, ref _quantumInput);
			}

			callback.SetInput(_quantumInput, DeterministicInputFlags.Repeatable);
			OnQuantumInputSent?.Invoke(_quantumInput);
		}


		private void OnSpecialButtonUsed(InputAction.CallbackContext context, int specialIndex)
		{
			if (!context.canceled)
			{
				return;
			}

			var aim = Input.Gameplay.SpecialAim.ReadValue<Vector2>();
			SendSpecialUsedCommand(specialIndex, aim);
		}

		private void SendSpecialUsedCommand(int specialIndex, Vector2 aimDirection)
		{
			var data = QuantumRunner.Default.Game.GetLocalPlayerData(false, out var f);

			// Check if there is a weapon equipped in the slot. Avoid extra commands to save network message traffic $$$
			if (!f.TryGet<PlayerInventory>(data.Entity, out var inventory) ||
				!inventory.Specials[specialIndex].IsUsable(f))
			{
				return;
			}

			var command = new SpecialUsedCommand
			{
				SpecialIndex = specialIndex,
				AimInput = aimDirection.ToFPVector2(),
			};

			QuantumRunner.Default.Game.SendCommand(command);
		}
	}
}
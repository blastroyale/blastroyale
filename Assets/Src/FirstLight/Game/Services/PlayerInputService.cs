using System;
using FirstLight.FLogger;
using FirstLight.Game.Input;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using Quantum.Commands;
using UnityEngine;
using UnityEngine.InputSystem;

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

		/// <summary>
		/// Enable Player spell control input 
		/// </summary>
		void EnableInput();

		/// <summary>
		/// Disable Player spell control input
		/// </summary>
		void DisableInput();
	}

	public class PlayerInputService : IPlayerInputService, MatchServices.IMatchService, LocalInput.IGameplayActions
	{
		public LocalInput Input { get; }

		private readonly IMatchServices _matchServices;
		private readonly IGameDataProvider _dataProvider;

		private Quantum.Input _quantumInput;

		private Vector2 _direction;
		private Vector2 _aim;
		private bool _shooting;

		public PlayerInputService(IMatchServices matchServices, IGameDataProvider dataProvider)
		{
			_matchServices = matchServices;
			_dataProvider = dataProvider;

			Input = new LocalInput();
			Input.Gameplay.SetCallbacks(this);

			// TODO: Setup input enable / disable for specials based on current weapon
		}

		public void EnableInput()
		{
			Input.Enable();
			QuantumCallback.SubscribeManual<CallbackPollInput>(this, PollInput);
		}

		public void DisableInput()
		{
			Input.Disable();
			QuantumCallback.UnsubscribeListener<CallbackPollInput>(this);
		}

		public void OnMatchStarted(QuantumGame game, bool isReconnect)
		{
			EnableInput();
		}

		public void OnMatchEnded(QuantumGame game, bool isDisconnected)
		{
			DisableInput();
		}

		public void Dispose()
		{
			Input.Dispose();
		}

		public void OnMove(InputAction.CallbackContext context)
		{
			_direction = context.ReadValue<Vector2>();

			// TODO: Try to move this to TutorialService
			// if (!_sentMovementMessage && _services.TutorialService.CurrentRunningTutorial.Value ==
			// 	TutorialSection.FIRST_GUIDE_MATCH)
			// {
			// 	_services.MessageBrokerService.Publish(new PlayerUsedMovementJoystick());
			// }
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
			// Do nothing here
		}

		public void OnSpecialButton0(InputAction.CallbackContext context)
		{
			OnSpecialButtonUsed(context, 0);
		}

		public void OnSpecialButton1(InputAction.CallbackContext context)
		{
			OnSpecialButtonUsed(context, 1);
		}

		public void OnCancelButton(InputAction.CallbackContext context)
		{
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
			float moveSpeedPercentage = 100;
			if (_dataProvider.AppDataProvider.MovespeedControl)
			{
				moveSpeedPercentage = Math.Min(_direction.magnitude * 100, 100);
			}

			_quantumInput.SetInput(_aim.ToFPVector2(), _direction.ToFPVector2(), _shooting,
				FP.FromFloat_UNSAFE(moveSpeedPercentage));
			callback.SetInput(_quantumInput, DeterministicInputFlags.Repeatable);
		}

		private void OnSpecialButtonUsed(InputAction.CallbackContext context, int specialIndex)
		{
			// TODO: Also check for valid position: || context.control.device is OnScreenControlsDevice && !specialButton.DraggingValidPosition()
			if (!context.canceled)
			{
				return;
			}

			var aim = Input.Gameplay.SpecialAim.ReadValue<Vector2>();
			SendSpecialUsedCommand(specialIndex, aim);
		}

		private unsafe void SendSpecialUsedCommand(int specialIndex, Vector2 aimDirection)
		{
			var data = QuantumRunner.Default.Game.GetLocalPlayerData(false, out var f);

			// Check if there is a weapon equipped in the slot. Avoid extra commands to save network message traffic $$$
			if (!f.TryGet<PlayerCharacter>(data.Entity, out var playerCharacter) ||
				!playerCharacter.WeaponSlot->Specials[specialIndex].IsUsable(f))
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
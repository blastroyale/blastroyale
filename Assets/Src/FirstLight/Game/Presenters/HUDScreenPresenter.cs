using FirstLight.Game.Input;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.UITK;
using FirstLight.UiService;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	public class HUDScreenPresenter : UiToolkitPresenterData<HUDScreenPresenter.StateData>
	{
		public struct StateData
		{
		}

		[SerializeField] private UnityInputScreenControl _moveDirectionJoystickInput;
		[SerializeField] private UnityInputScreenControl _moveDownJoystickInput;
		[SerializeField] private UnityInputScreenControl _aimDirectionJoystickInput;
		[SerializeField] private UnityInputScreenControl _aimDownJoystickInput;
		[SerializeField] private UnityInputScreenControl _weaponSwitchInput;
		[SerializeField] private UnityInputScreenControl _special0PressedInput;
		[SerializeField] private UnityInputScreenControl _special1PressedInput;
		[SerializeField] private UnityInputScreenControl _specialAimInput;

		private WeaponDisplayView _weaponDisplayView;
		private KillFeedView _killFeedView;
		private MatchStatusView _matchStatusView;
		private SpecialButtonsView _specialButtonsView;
		private DeviceStatusView _deviceStatusView;
		private SquadMembersView _squadMembersView;

		private JoystickElement _movementJoystick;
		private JoystickElement _shootingJoystick;

		private Vector2 _direction;
		private Vector2 _aim;
		private bool _shooting;
		private Quantum.Input _quantumInput;

		protected override void QueryElements(VisualElement root)
		{
			root.Q("WeaponDisplay").Required().AttachView(this, out _weaponDisplayView);
			root.Q("KillFeed").Required().AttachView(this, out _killFeedView);
			root.Q("MatchStatus").Required().AttachView(this, out _matchStatusView);
			root.AttachView(this, out _specialButtonsView);
			root.Q("DeviceStatus").Required().AttachView(this, out _deviceStatusView);
			root.Q("SquadMembers").Required().AttachView(this, out _squadMembersView);

			_movementJoystick = root.Q<JoystickElement>("MovementJoystick").Required();
			_shootingJoystick = root.Q<JoystickElement>("ShootingJoystick").Required();

			_movementJoystick.OnMove += _moveDirectionJoystickInput.SendValueToControl;
			_movementJoystick.OnClick += _moveDownJoystickInput.SendValueToControl;
			_shootingJoystick.OnMove += _aimDirectionJoystickInput.SendValueToControl;
			_shootingJoystick.OnClick += _aimDownJoystickInput.SendValueToControl;

			_weaponDisplayView.OnClick += _weaponSwitchInput.SendValueToControl;

			_specialButtonsView.OnSpecial0Pressed += _special0PressedInput.SendValueToControl;
			_specialButtonsView.OnSpecial1Pressed += _special1PressedInput.SendValueToControl;
			_specialButtonsView.OnDrag += _specialAimInput.SendValueToControl;
		}

		[Button]
		public void DebugSpawnFeed()
		{
			_killFeedView.SpawnDeathNotification("GAMESTERWITHAREALLYLONGNAME", "CUPCAKE");
		}

		[Button]
		public void DebugStartCountdown(long warningTime = 5000, long shrinkingTime = 7000)
		{
			_matchStatusView.StartCountdown(warningTime, shrinkingTime);
		}
	}
}
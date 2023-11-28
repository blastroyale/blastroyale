using System.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Game.Input;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.UITK;
using FirstLight.UiService;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	public class HUDScreenPresenter : UiToolkitPresenterData<HUDScreenPresenter.StateData>
	{
		public struct StateData
		{
		}

		private const string USS_SKYDIVING = "skydiving";

		[SerializeField, Required] private GameObject _legacyMinimap;

		[SerializeField, Required, TabGroup("Animation")]
		private PlayableDirector _areaShrinkingDirector;

		[SerializeField, Required, TabGroup("Animation")]
		private PlayableDirector _blasted1Director;

		[SerializeField, Required, TabGroup("Animation")]
		private PlayableDirector _blasted2Director;

		[SerializeField, Required, TabGroup("Animation")]
		private PlayableDirector _blasted3Director;

		[SerializeField, Required, TabGroup("Animation")]
		private PlayableDirector _blastedBeastDirector;

		[SerializeField, Required, TabGroup("Animation")]
		private Gradient _outOfAmmoGradient;

		[SerializeField, Required, TabGroup("Animation")]
		private int _lowHPThreshold = 50;

		[SerializeField, Required, TabGroup("Input")]
		private UnityInputScreenControl _moveDirectionJoystickInput;

		[SerializeField, Required, TabGroup("Input")]
		private UnityInputScreenControl _moveDownJoystickInput;

		[SerializeField, Required, TabGroup("Input")]
		private UnityInputScreenControl _aimDirectionJoystickInput;

		[SerializeField, Required, TabGroup("Input")]
		private UnityInputScreenControl _aimDownJoystickInput;

		[SerializeField, Required, TabGroup("Input")]
		private UnityInputScreenControl _weaponSwitchInput;

		[SerializeField, Required, TabGroup("Input")]
		private UnityInputScreenControl _special0PressedInput;

		[SerializeField, Required, TabGroup("Input")]
		private UnityInputScreenControl _special1PressedInput;

		[SerializeField, Required, TabGroup("Input")]
		private UnityInputScreenControl _specialAimInput;

		[SerializeField, Required, TabGroup("Input")]
		private UnityInputScreenControl _specialCancelInput;

		private IGameServices _gameServices;
		private IGameDataProvider _dataProvider;

		// ReSharper disable NotAccessedField.Local
		private WeaponDisplayView _weaponDisplayView;
		private KillFeedView _killFeedView;
		private MatchTimerView _matchTimerView;
		private SpecialButtonsView _specialButtonsView;
		private DeviceStatusView _deviceStatusView;
		private SquadMembersView _squadMembersView;
		private EquipmentDisplayView _equipmentDisplayView;
		private StatusBarsView _statusBarsView;
		private StatusNotificationsView _statusNotificationsView;
		private PlayerCountsView _playerCountsView;
		private LocalPlayerInfoView _localPlayerInfoView;

		// ReSharper restore NotAccessedField.Local

		private JoystickElement _movementJoystick;
		private JoystickElement _shootingJoystick;
		private ImageButton _menuButton;

		private Vector2 _direction;
		private Vector2 _aim;
		private bool _shooting;
		private Quantum.Input _quantumInput;

		protected override void QueryElements(VisualElement root)
		{
			_gameServices = MainInstaller.Resolve<IGameServices>();
			_dataProvider = MainInstaller.ResolveData();

			_gameServices.ControlsSetup.SetControlPositions(Root);

			root.Q("WeaponDisplay").Required().AttachView(this, out _weaponDisplayView);
			root.Q("KillFeed").Required().AttachView(this, out _killFeedView);
			root.Q("MatchStatus").Required().AttachView(this, out _matchTimerView);
			root.AttachView(this, out _specialButtonsView);
			root.Q("DeviceStatus").Required().AttachView(this, out _deviceStatusView);
			root.Q("SquadMembers").Required().AttachView(this, out _squadMembersView);
			// root.Q("EquipmentDisplay").Required().AttachView(this, out _equipmentDisplayView);
			root.Q("PlayerBars").Required().AttachView(this, out _statusBarsView);
			root.Q("StatusNotifications").Required().AttachView(this, out _statusNotificationsView);
			root.Q("PlayerCounts").Required().AttachView(this, out _playerCountsView);

			var localPlayerInfo = root.Q("LocalPlayerInfo").Required();
			if (_dataProvider.AppDataProvider.UseOverheadUI)
			{
				localPlayerInfo.SetDisplay(false);
			}
			else
			{
				localPlayerInfo.AttachView(this, out _localPlayerInfoView);
			}

			_weaponDisplayView.OutOfAmmoColors = _outOfAmmoGradient;
			_matchTimerView.SetAreaShrinkingDirector(_areaShrinkingDirector);
			_statusNotificationsView.Init(_blasted1Director, _blasted2Director, _blasted3Director, _blastedBeastDirector, _lowHPThreshold);

			_movementJoystick = root.Q<JoystickElement>("MovementJoystick").Required();
			_shootingJoystick = root.Q<JoystickElement>("ShootingJoystick").Required();
			_menuButton = root.Q<ImageButton>("MenuButton").Required();

			_menuButton.clicked += OnMenuClicked;
			_movementJoystick.OnMove += e => InputState.Change(_moveDirectionJoystickInput.control, e);
			_movementJoystick.OnClick += e => InputState.Change(_moveDownJoystickInput.control, e);
			_shootingJoystick.OnMove += e => InputState.Change(_aimDirectionJoystickInput.control, e);
			_shootingJoystick.OnClick += e => InputState.Change(_aimDownJoystickInput.control, e);

			_weaponDisplayView.OnClick += e => InputState.Change(_weaponSwitchInput.control, e);

			_specialButtonsView.OnSpecial0Pressed += e => InputState.Change(_special0PressedInput.control, e);
			_specialButtonsView.OnSpecial1Pressed += e => InputState.Change(_special1PressedInput.control, e);
			_specialButtonsView.OnDrag += e => InputState.Change(_specialAimInput.control, e);
			_specialButtonsView.OnCancel += e => InputState.Change(_specialCancelInput.control, e);
		}

		public JoystickElement MovementJoystick => _movementJoystick;
		public JoystickElement ShootingJoystick => _shootingJoystick;
		public SpecialButtonElement Special0 => _specialButtonsView._special0Button;
		public SpecialButtonElement Special1 => _specialButtonsView._special1Button;

		protected override void SubscribeToEvents()
		{
			base.SubscribeToEvents();
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSkydiveDrop>(this, _ => HideControls(true));
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSkydiveLand>(this, _ => HideControls(false));
		}

		protected override void UnsubscribeFromEvents()
		{
			base.UnsubscribeFromEvents();
			QuantumEvent.UnsubscribeListener(this);
			QuantumCallback.UnsubscribeListener(this);
		}

		public bool IsMenuVisible()
		{
			return !_gameServices.TutorialService.IsTutorialRunning &&
				_gameServices.RoomService.CurrentRoom.Properties.MatchType.Value == MatchType.Custom;
		}
	
		protected override void OnOpened()
		{
			base.OnOpened();
			_legacyMinimap.SetActive(_gameServices.RoomService.CurrentRoom.GameModeConfig.ShowUIMinimap);
			_menuButton.SetVisibility(IsMenuVisible());
			MainInstaller.ResolveMatchServices().RunOnMatchStart((isReconnect) =>
			{
				if (!isReconnect) return;
				var playerEntity = 	QuantumRunner.Default.Game.GetLocalPlayerEntityRef();
				// player died
				if (!QuantumRunner.Default.Game.Frames.Verified.Exists(playerEntity))
				{
					HideControls(true);
					return;
				}; 
					
				HideControls(false);
				_weaponDisplayView.UpdateFromLatestVerifiedFrame();
				_specialButtonsView.UpdateFromLatestVerifiedFrame();
				_localPlayerInfoView.UpdateFromLatestVerifiedFrame();
				_statusBarsView.InitAll();

			});
		}

		protected override Task OnClosed()
		{
			_legacyMinimap.SetActive(false);

			return base.OnClosed();
		}

		private void OnMenuClicked()
		{
			_gameServices.MessageBrokerService.Publish(new QuitGameClickedMessage());
		}

		private void HideControls(bool hide)
		{
			Root.EnableInClassList(USS_SKYDIVING, hide);
		}
	}
}
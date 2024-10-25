using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Ids;
using FirstLight.Game.Input;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MatchHudViews;
using FirstLight.Game.Views.UITK;
using FirstLight.UiService;
using FirstLight.UIService;
using Quantum;
using Quantum.Systems;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	public class HUDScreenPresenter : UIPresenter
	{
		private const string USS_SKYDIVING = "skydiving";

		[SerializeField, Required] private GameObject _legacyMinimap;
		[SerializeField, Required] private MiniMapView _legacyMinimapView;

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
		private int _lowHPThreshold = 10;

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
		
		[SerializeField, Required, TabGroup("Input")]
		private UnityInputScreenControl _minimapToggleInput;

		private IGameServices _gameServices;
		private IGameDataProvider _dataProvider;

		[SerializeField, TabGroup("Views")] private KnockOutNotificationView _knockOutNotificationView = new ();

		// ReSharper disable NotAccessedField.Local
		private WeaponDisplayView _weaponDisplayView;
		private KillFeedView _killFeedView;
		private MatchTimerView _matchTimerView;
		private SpecialButtonsView _specialButtonsView;
		private DeviceStatusView _deviceStatusView;
		private SquadMembersView _squadMembersView;
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

		protected override void QueryElements()
		{
			_gameServices = MainInstaller.Resolve<IGameServices>();
			_dataProvider = MainInstaller.ResolveData();

			_gameServices.ControlsSetup.SetControlPositions(Root);

			Root.Q("WeaponDisplay").Required().AttachView(this, out _weaponDisplayView);
			Root.Q("KillFeed").Required().AttachView(this, out _killFeedView);
			Root.Q("MatchStatus").Required().AttachView(this, out _matchTimerView);
			Root.AttachView(this, out _specialButtonsView);
			Root.Q("DeviceStatus").Required().AttachView(this, out _deviceStatusView);
			Root.Q("SquadMembers").Required().AttachView(this, out _squadMembersView);
			Root.Q("PlayerBars").Required().AttachView(this, out _statusBarsView);
			Root.Q("StatusNotifications").Required().AttachView(this, out _statusNotificationsView);
			Root.Q("PlayerCounts").Required().AttachView(this, out _playerCountsView);
			Root.AttachExistingView(this, _knockOutNotificationView);

			var localPlayerInfo = Root.Q("LocalPlayerInfo").Required();
			if (_gameServices.LocalPrefsService.UseOverheadUI)
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
			
			// TODO: Move all the joystick stuff into a view
			if (_gameServices.LocalPrefsService.SwapJoysticks)
			{
				_movementJoystick = Root.Q<JoystickElement>("RightJoystick").Required();
				_shootingJoystick = Root.Q<JoystickElement>("LeftJoystick").Required();
			}
			else
			{
				_movementJoystick = Root.Q<JoystickElement>("LeftJoystick").Required();
				_shootingJoystick = Root.Q<JoystickElement>("RightJoystick").Required();
			}

			// I can't find a cleaner way to do this
			_shootingJoystick.RemoveFromClassList("joystick--aim");
			_shootingJoystick.RemoveFromClassList("joystick--move");
			_movementJoystick.RemoveFromClassList("joystick--move");
			_movementJoystick.RemoveFromClassList("joystick--aim");

			_shootingJoystick.AddToClassList("joystick--aim");
			_movementJoystick.AddToClassList("joystick--move");

			_menuButton = Root.Q<ImageButton>("MenuButton").Required();

			_menuButton.clicked += OnMenuClicked;
			_movementJoystick.OnMove += e => InputState.Change(_moveDirectionJoystickInput.control, e);
			_movementJoystick.OnClick += e => InputState.Change(_moveDownJoystickInput.control, e);
			_shootingJoystick.OnMove += e => InputState.Change(_aimDirectionJoystickInput.control, e);
			_shootingJoystick.OnClick += e => InputState.Change(_aimDownJoystickInput.control, e);

			_weaponDisplayView.OnClick += e => InputState.Change(_weaponSwitchInput.control, e);
			_legacyMinimapView.OnClick += e => InputState.Change(_minimapToggleInput.control, e);

			_specialButtonsView.OnSpecial0Pressed += e => InputState.Change(_special0PressedInput.control, e);
			_specialButtonsView.OnSpecial1Pressed += e => InputState.Change(_special1PressedInput.control, e);
			_specialButtonsView.OnDrag += e => InputState.Change(_specialAimInput.control, e);
			_specialButtonsView.OnCancel += e => InputState.Change(_specialCancelInput.control, e);
			
			_legacyMinimap.SetActive(false);
		}

		public JoystickElement MovementJoystick => _movementJoystick;
		public JoystickElement ShootingJoystick => _shootingJoystick;
		public SpecialButtonElement Special0 => _specialButtonsView._special0Button;
		public SpecialButtonElement Special1 => _specialButtonsView._special1Button;

		protected override UniTask OnScreenOpen(bool reload)
		{
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSkydiveDrop>(this, _ => HideControls(true));
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSkydiveLand>(this, _ => HideControls(false));
			QuantumEvent.SubscribeManual<EventOnPlayerKnockedOut>(OnPlayerKnockedOut);
			QuantumEvent.SubscribeManual<EventOnPlayerRevived>(OnPlayerRevived);

			_menuButton.SetVisibility(IsMenuVisible());
			MainInstaller.ResolveMatchServices().RunOnMatchStart((isReconnect) =>
			{
				if (!isReconnect || QuantumRunner.Default.IsDefinedAndRunning(false)) return;
				var playerEntity = QuantumRunner.Default.Game.GetLocalPlayerEntityRef();
				// player died
				var f = QuantumRunner.Default.Game.Frames.Verified;
				if (!f.Exists(playerEntity))
				{
					HideControls(true);
					return;
				}

				HideControls(false);
				_weaponDisplayView.UpdateFromLatestVerifiedFrame();
				_specialButtonsView.UpdateFromLatestVerifiedFrame(playerEntity);
				_localPlayerInfoView.UpdateFromLatestVerifiedFrame();
				_statusBarsView.InitAll();
				SetKnockedOutStatus(ReviveSystem.IsKnockedOut(f, playerEntity));
			});
			
			ShowMinimapDelayed().Forget();
			
			return base.OnScreenOpen(reload);
		}

		protected override UniTask OnScreenClose()
		{
			QuantumEvent.UnsubscribeListener(this);
			QuantumCallback.UnsubscribeListener(this);
			_legacyMinimap.SetActive(false);
			return base.OnScreenClose();
		}

		// A hack to show the minimap after the transition screen is closed since the minimap is old UI and shows over it
		private async UniTaskVoid ShowMinimapDelayed()
		{
			await UniTask.WaitUntil(() => !_gameServices.UIService.IsScreenOpen<SwipeTransitionScreenPresenter>());
			// TECH DEBT, when reconnecting dead players will first open the HUD (this screen) then open the spectator in the 
			// next frame, so this is no longer visible
			// Ideally we would not open the hud if the player is dead, but currently we open the hud before starting the simulation
			// so we don't know if the player is dead or alive
			if (!Root.IsAttached()) return;
			_legacyMinimap.SetActive(_gameServices.RoomService.CurrentRoom.GameModeConfig.ShowUIMinimap);
		}

		private bool IsMenuVisible()
		{
			return _gameServices.RoomService.CurrentRoom.GameModeConfig.Id == GameConstants.Tutorial.SECOND_BOT_MODE_ID ||
				_gameServices.RoomService.CurrentRoom.Properties.SimulationMatchConfig.Value.MatchType == MatchType.Custom;
		}

		private void OnMenuClicked()
		{
			_gameServices.MessageBrokerService.Publish(new QuitGameClickedMessage());
		}

		private void HideControls(bool hide)
		{
			Root.EnableInClassList(USS_SKYDIVING, hide);
		}

		private void OnPlayerRevived(EventOnPlayerRevived callback)
		{
			var playerEntity = callback.Game.GetLocalPlayerEntityRef();
			if (callback.Entity != playerEntity) return;
			SetKnockedOutStatus(false);
		}

		private void OnPlayerKnockedOut(EventOnPlayerKnockedOut callback)
		{
			var playerEntity = callback.Game.GetLocalPlayerEntityRef();
			if (callback.Entity != playerEntity) return;
			SetKnockedOutStatus(true);
		}

		private void SetKnockedOutStatus(bool knockedOut)
		{
			_shootingJoystick.SetVisibility(!knockedOut);
		}
	}
}
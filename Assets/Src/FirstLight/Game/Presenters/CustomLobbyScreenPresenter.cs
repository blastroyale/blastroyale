using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Services.RoomService;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MainMenuViews;
using FirstLight.UiService;
using I2.Loc;
using NUnit.Framework;
using Photon.Realtime;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Players Waiting Screen UI by:
	/// - Showing the loading status
	/// </summary>
	public class CustomLobbyScreenPresenter : UiPresenterData<CustomLobbyScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action LeaveRoomClicked;
		}

		private const int MAX_SQUAD_ID = 30;

		public MapSelectionView mapSelectionView;

		[SerializeField, Required] private Button _backButton;
		[SerializeField, Required] private Button _homeButton;
		[SerializeField, Required] private GameObject _rootObject;
		[SerializeField, Required] private Button _lockRoomButton;
		[SerializeField, Required] private Button _kickButton;
		[SerializeField, Required] private Button _cancelKickButton;
		[SerializeField, Required] private Image[] _playersWaitingImage;
		[SerializeField, Required] private TextMeshProUGUI _playersFoundText;
		[SerializeField, Required] private TextMeshProUGUI _findingPlayersText;
		[SerializeField, Required] private TextMeshProUGUI _getReadyToRumbleText;
		[SerializeField, Required] private TextMeshProUGUI _roomNameText;
		[SerializeField, Required] private TextMeshProUGUI _selectedGameModeText;
		[SerializeField, Required] private TextMeshProUGUI _playerCountText;
		[SerializeField, Required] private TextMeshProUGUI _spectatorCountText;
		[SerializeField, Required] private TextMeshProUGUI _topTitleText;
		[SerializeField, Required] private TextMeshProUGUI _selectDropZoneText;
		[SerializeField, Required] private GameObject[] _kickOverlayObjects;
		[SerializeField, Required] private GameObject _loadingText;
		[SerializeField, Required] private GameObject _playerMatchmakingRootObject;
		[SerializeField, Required] private GameObject _playerCountHolder;
		[SerializeField, Required] private GameObject _selectDropZoneTextRootObject;
		[SerializeField, Required] private PlayerListHolderView _playerListHolder;
		[SerializeField, Required] private PlayerListHolderView _spectatorListHolder;
		[SerializeField, Required] private UiToggleButtonView _botsToggle;
		[SerializeField, Required] private UiToggleButtonView _spectateToggle;
		[SerializeField, Required] private GameObject _botsToggleObjectRoot;
		[SerializeField, Required] private GameObject _spectateToggleObjectRoot;
		[SerializeField] private Color _spectateDisabledColor;

		[SerializeField, Required] private GameObject _topTitleHolder;
		[SerializeField, Required] private GameObject _squadContainer;
		[SerializeField, Required] private TextMeshProUGUI _squadIdText;
		[SerializeField, Required] private Button _squadIdUpButton;
		[SerializeField, Required] private Button _squadIdDownButton;

		private IGameServices _services;
		private bool _spectatorToggleTimeOut;
		private bool _kickModeActive = false;

		private GameRoom CurrentRoom => _services.RoomService.CurrentRoom;
		private bool RejoiningRoom => _services.NetworkService.JoinSource.HasResync();

		private int _squadId = 1;
		private Tween _squadIdUpdateDelayed = null;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();

			foreach (var image in _playersWaitingImage)
			{
				image.gameObject.SetActive(false);
			}

			_backButton.onClick.AddListener(OnLeaveRoomClicked);
			_homeButton.onClick.AddListener(OnLeaveRoomClicked);
			_spectateToggle.onValueChanged.AddListener(OnSpectatorToggle);
			_services.NetworkService.QuantumClient.AddCallbackTarget(this);
			_lockRoomButton.onClick.AddListener(OnLockRoomClicked);
			_kickButton.onClick.AddListener(ActivateKickOverlay);
			_botsToggle.onValueChanged.AddListener(OnBotsToggleChanged);
			_cancelKickButton.onClick.AddListener(DeactivateKickOverlay);
			_squadIdDownButton.onClick.AddListener(OnSquadIdDown);
			_squadIdUpButton.onClick.AddListener(OnSquadIdUp);

			_services.RoomService.OnRoomPropertiesChanged += Update;
			_services.RoomService.OnMasterChanged += Update;
			_services.RoomService.OnPlayerPropertiesUpdated += Update;
		}

		private void OnDestroy()
		{
			_services.RoomService.OnRoomPropertiesChanged -= Update;
			_services.RoomService.OnMasterChanged -= Update;
			_services.RoomService.OnPlayerPropertiesUpdated -= Update;
			_services?.NetworkService?.QuantumClient?.RemoveCallbackTarget(this);
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		/// <inheritdoc />
		protected override void OnOpened()
		{
			if (_services.TutorialService.CurrentRunningTutorial.Value == TutorialSection.FIRST_GUIDE_MATCH) return;

			_rootObject.SetActive(true);

			var room = CurrentRoom;
			var gameModeConfig = room.GameModeConfig;


			mapSelectionView.SetupMapView(room.Properties.GameModeId.Value, room.Properties.MapId.Value.GetHashCode());

			if (RejoiningRoom)
			{
				_playerListHolder.Init(room.GetMaxPlayers(false), RequestKickPlayer);
				_spectatorListHolder.Init((uint) room.GetMaxSpectators(), RequestKickPlayer);

				_kickButton.gameObject.SetActive(false);
				_spectateToggleObjectRoot.SetActive(false);
				_botsToggleObjectRoot.SetActive(false);
				_lockRoomButton.gameObject.SetActive(false);
				_loadingText.SetActive(true);
				_squadContainer.SetActive(false);
				_topTitleHolder.SetActive(true);

				foreach (var playerKvp in CurrentRoom.Players)
				{
					AddOrUpdatePlayerInList(playerKvp.Value);
				}

				return;
			}

			_selectDropZoneTextRootObject.SetActive(gameModeConfig.SpawnSelection);
			_selectDropZoneText.text = room.Properties.MapId.Value.GetLocalization();
			_lockRoomButton.gameObject.SetActive(false);
			_getReadyToRumbleText.gameObject.SetActive(false);
			_playersFoundText.gameObject.SetActive(true);
			_findingPlayersText.gameObject.SetActive(true);
			_botsToggle.SetInitialValue(true);
			_botsToggleObjectRoot.SetActive(false);
			_spectateToggle.SetInitialValue(false);
			_spectateToggleObjectRoot.SetActive(false);
			_kickButton.gameObject.SetActive(false);
			_loadingText.SetActive(true);
			_playersFoundText.text = $"{0}/{room.GetMaxPlayers(false).ToString()}";
			_squadContainer.SetActive(gameModeConfig.Teams);
			_topTitleHolder.SetActive(!gameModeConfig.Teams);
			_squadIdText.text = _squadId.ToString();

			// TODO: Sets the initial TeamID. Hacky, should be somewhere else, but it should do for custom games for now.
			if (gameModeConfig.Teams)
			{
				CurrentRoom.LocalPlayerProperties.TeamId.Value = $"{GameConstants.Network.MANUAL_TEAM_ID_PREFIX}{_squadId}";
			}


			var matchType = room.Properties.MatchType.Value;
			var gameMode = room.Properties.GameModeId.Value;

			_selectedGameModeText.text = string.Format(ScriptLocalization.MainMenu.SelectedGameModeValue,
				matchType.ToString().ToUpper(), gameMode);

			UpdateRoomPlayerCounts();


			_playerListHolder.Init(room.GetMaxPlayers(false), RequestKickPlayer);
			_spectatorListHolder.Init((uint) room.GetMaxSpectators(), RequestKickPlayer);

			_playerListHolder.gameObject.SetActive(true);
			_spectatorListHolder.gameObject.SetActive(true);
			_playerMatchmakingRootObject.SetActive(false);
			_playerCountHolder.SetActive(true);

			_topTitleText.text = ScriptLocalization.MainMenu.PrepareForActionBasic;
			_roomNameText.text = string.Format(ScriptLocalization.MainMenu.RoomCurrentName, room.GetRoomName());

			_loadingText.SetActive(false);
			_spectateToggleObjectRoot.SetActive(true);

			Update();
		}

		public void Update()
		{
			if (!IsOpen)
			{
				return;
			}

			if (CurrentRoom == null)
			{
				return;
			}

			if (CurrentRoom.Properties.StartCustomGame.Value)
			{
				ReadyToPlay();
				return;
			}

			UpdateRoomPlayerCounts();
			// Update Players
			foreach (var currentRoomPlayer in CurrentRoom.Players)
			{
				AddOrUpdatePlayerInList(currentRoomPlayer.Value);
			}

			CheckPlayersToRemove(_spectatorListHolder);
			CheckPlayersToRemove(_playerListHolder);
			// Update master options
			if (CurrentRoom.LocalPlayer.IsMasterClient)
			{
				_kickButton.gameObject.SetActive(true);
				_lockRoomButton.gameObject.SetActive(true);
				_botsToggleObjectRoot.SetActive(CurrentRoom.GameModeConfig.AllowBots);
			}
			else
			{
				_kickButton.gameObject.SetActive(false);
				_lockRoomButton.gameObject.SetActive(false);
			}
		}


		private void UpdateRoomPlayerCounts()
		{
			_playerCountText.text = $"{CurrentRoom.GetRealPlayerAmount()}/{CurrentRoom.GetRealPlayerCapacity()}";
			_spectatorCountText.text = $"{CurrentRoom.GetSpectatorAmount()}/{CurrentRoom.GetMaxSpectators()}";
		}

		private void CheckPlayersToRemove(PlayerListHolderView view)
		{
			var toRemove = new List<Player>();
			foreach (var player in view.GetPlayers())
			{
				var isOnRoom = CurrentRoom.Players.Values.Any(x => x == player);
				if (!isOnRoom)
				{
					toRemove.Add(player);
				}
			}

			foreach (var player in toRemove)
			{
				view.RemovePlayer(player);
			}
		}

		protected override UniTask OnClosed()
		{
			_rootObject.SetActive(true);
			return UniTask.CompletedTask;
		}


		private void AddOrUpdatePlayerInList(Player player)
		{
			var isSpectator = CurrentRoom.GetPlayerProperties(player).Spectator.Value;

			if (isSpectator)
			{
				_spectatorListHolder.AddOrUpdatePlayer(player);

				if (_playerListHolder.Has(player))
				{
					_playerListHolder.RemovePlayer(player);
				}
			}
			else
			{
				_playerListHolder.AddOrUpdatePlayer(player);

				if (_spectatorListHolder.Has(player))
				{
					_spectatorListHolder.RemovePlayer(player);
				}
			}

			CheckEnableSpectatorToggle();
		}

		private void CheckEnableSpectatorToggle()
		{
			if (_spectatorToggleTimeOut)
			{
				return;
			}

			var isSpectator = _services.RoomService.IsLocalPlayerSpectator;
			int relevantPlayerAmount;
			int relevantPlayerCapacity;

			if (isSpectator)
			{
				relevantPlayerAmount = CurrentRoom.GetRealPlayerAmount();
				relevantPlayerCapacity = CurrentRoom.GetRealPlayerCapacity();
			}
			else
			{
				relevantPlayerAmount = CurrentRoom.GetSpectatorAmount();
				relevantPlayerCapacity = CurrentRoom.GetMaxSpectators();
			}

			SetSpectateInteractable(relevantPlayerAmount < relevantPlayerCapacity);
			CheckEnableLockRoomButton();
		}

		private void CheckEnableLockRoomButton()
		{
			var realPlayers = CurrentRoom.GetRealPlayerAmount();
			_lockRoomButton.interactable = realPlayers > 1 || realPlayers == 1 && _botsToggle.isOn;
		}

		private void SetSpectateInteractable(bool interactable)
		{
			_spectateToggle.interactable = interactable;

			if (interactable)
			{
				_spectateToggle.SetTargetCustomGraphicsColor(Color.white);
			}
			else
			{
				_spectateToggle.SetTargetCustomGraphicsColor(_spectateDisabledColor);
			}
		}


		private IEnumerator TimeoutSpectatorToggleCoroutine()
		{
			SetSpectateInteractable(false);
			_spectatorToggleTimeOut = true;

			yield return new WaitForSeconds(GameConstants.Data.SPECTATOR_TOGGLE_TIMEOUT);

			_spectatorToggleTimeOut = false;

			// The room can null out if we left the matchmaking while this coroutine still hasnt finished
			if (CurrentRoom != null)
			{
				CheckEnableSpectatorToggle();
			}
		}


		private void OnLockRoomClicked()
		{
			ReadyToPlay();
			CurrentRoom.Properties.HasBots.Value = _botsToggle.isOn;
			_services.RoomService.StartCustomGameLoading();
		}

		private void OnLeaveRoomClicked()
		{
			var desc = string.Format(ScriptLocalization.MainMenu.LeaveMatchMessage);
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.Yes,
				ButtonOnClick = () =>
				{
					_services.MessageBrokerService.Publish(new RoomLeaveClickedMessage());
					Data.LeaveRoomClicked();
				}
			};

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.confirmation, desc, true,
				confirmButton);
		}

		private void ReadyToPlay()
		{
			mapSelectionView.SelectionEnabled = false;

			DeactivateKickOverlay();
			_loadingText.SetActive(true);
			_lockRoomButton.gameObject.SetActive(false);
			_botsToggleObjectRoot.SetActive(false);
			_kickButton.gameObject.SetActive(false);
			_spectateToggleObjectRoot.SetActive(false);
			_squadContainer.SetActive(false);
			_topTitleHolder.SetActive(true);
		}

		private void ActivateKickOverlay()
		{
			foreach (var overlayObject in _kickOverlayObjects)
			{
				overlayObject.SetActive(true);
			}

			_kickModeActive = true;
		}

		private void OnBotsToggleChanged(bool _)
		{
			CheckEnableLockRoomButton();
		}

		private void DeactivateKickOverlay()
		{
			foreach (var overlayObject in _kickOverlayObjects)
			{
				overlayObject.SetActive(false);
			}

			_kickModeActive = false;
		}

		private void OnSquadIdUp()
		{
			_squadId = _squadId == MAX_SQUAD_ID ? 1 : Mathf.Clamp(_squadId + 1, 1, MAX_SQUAD_ID);
			UpdateSquadIdDelayed();
		}

		private void OnSquadIdDown()
		{
			_squadId = _squadId == 1 ? MAX_SQUAD_ID : Mathf.Clamp(_squadId - 1, 1, MAX_SQUAD_ID);
			UpdateSquadIdDelayed();
		}

		private void UpdateSquadIdDelayed()
		{
			_squadIdText.text = _squadId.ToString();

			_squadIdUpdateDelayed?.Kill();
			_squadIdUpdateDelayed = DOVirtual.DelayedCall(1f, () =>
			{
				CurrentRoom.LocalPlayerProperties.TeamId.Value = $"{GameConstants.Network.MANUAL_TEAM_ID_PREFIX}{_squadId}";
			});
		}

		private void RequestKickPlayer(Player player)
		{
			if (player.UserId == _services.NetworkService.LocalPlayer.UserId ||
				!_kickModeActive || !_services.NetworkService.LocalPlayer.IsMasterClient)
			{
				return;
			}

			var desc = string.Format(ScriptLocalization.MainMenu.MatchmakingKickConfirm, player.NickName).ToUpper();
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.Yes.ToUpper(),
				ButtonOnClick = () =>
				{
					_services.RoomService.KickPlayer(player);
					DeactivateKickOverlay();
				}
			};

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.confirmation, desc, true,
				confirmButton, DeactivateKickOverlay);
		}

		private void OnSpectatorToggle(bool isOn)
		{
			// Set lock room button to be inactive immediately - gets enabled when player properties change
			if (_services.NetworkService.LocalPlayer.IsMasterClient)
			{
				_lockRoomButton.interactable = false;
			}

			CurrentRoom.LocalPlayerProperties.Spectator.Value = isOn;
			_services.CoroutineService.StartCoroutine(TimeoutSpectatorToggleCoroutine());
		}
	}
}
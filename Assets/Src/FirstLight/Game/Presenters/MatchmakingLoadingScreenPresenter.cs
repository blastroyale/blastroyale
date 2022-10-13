using System;
using System.Collections;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MainMenuViews;
using FirstLight.UiService;
using I2.Loc;
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
	public class MatchmakingLoadingScreenPresenter : UiPresenterData<MatchmakingLoadingScreenPresenter.StateData>,
	                                                 IInRoomCallbacks
	{
		public struct StateData
		{
		}

		public MapSelectionView mapSelectionView;

		[SerializeField, Required] private GameObject _rootObject;
		[SerializeField, Required] private Button _lockRoomButton;
		[SerializeField, Required] private Button _leaveRoomButton;
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
		[SerializeField, Required] private GameObject[] _kickOverlayObjects;
		[SerializeField, Required] private GameObject _loadingText;
		[SerializeField, Required] private GameObject _roomNameRootObject;
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

		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;
		private bool _loadedCoreMatchAssets;
		private bool _spectatorToggleTimeOut;
		private bool _kickModeActive = false;

		private Room CurrentRoom => _services.NetworkService.QuantumClient.CurrentRoom;
		private bool RejoiningRoom => !_services.NetworkService.IsJoiningNewMatch;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();

			foreach (var image in _playersWaitingImage)
			{
				image.gameObject.SetActive(false);
			}

			_spectateToggle.onValueChanged.AddListener(OnSpectatorToggle);
			_services.NetworkService.QuantumClient.AddCallbackTarget(this);
			_lockRoomButton.onClick.AddListener(OnLockRoomClicked);
			_leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);
			_kickButton.onClick.AddListener(ActivateKickOverlay);
			_cancelKickButton.onClick.AddListener(DeactivateKickOverlay);
			_services.MessageBrokerService.Subscribe<CoreMatchAssetsLoadedMessage>(OnCoreMatchAssetsLoaded);
			_services.MessageBrokerService.Subscribe<StartedFinalPreloadMessage>(OnStartedFinalPreloadMessage);
		}

		private void OnDestroy()
		{
			_services?.NetworkService?.QuantumClient?.RemoveCallbackTarget(this);
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		/// <inheritdoc />
		protected override void OnOpened()
		{
			_rootObject.SetActive(true);

			var room = _services.NetworkService.QuantumClient.CurrentRoom;
			var mapConfig = _services.NetworkService.CurrentRoomMapConfig.Value;
			var gameModeConfig = _services.NetworkService.CurrentRoomGameModeConfig.Value;

			mapSelectionView.SetupMapView(room.GetGameModeId(), room.GetMapId());

			if (RejoiningRoom)
			{
				_playerListHolder.Init((uint) NetworkUtils.GetMaxPlayers(gameModeConfig, mapConfig), RequestKickPlayer);
				_spectatorListHolder.Init(GameConstants.Data.MATCH_SPECTATOR_SPOTS, RequestKickPlayer);

				_kickButton.gameObject.SetActive(false);
				_spectateToggleObjectRoot.SetActive(false);
				_botsToggleObjectRoot.SetActive(false);
				_lockRoomButton.gameObject.SetActive(false);
				_leaveRoomButton.gameObject.SetActive(false);
				_loadingText.SetActive(true);

				foreach (var playerKvp in CurrentRoom.Players)
				{
					AddOrUpdatePlayerInList(playerKvp.Value);
				}

				return;
			}
			
			_selectDropZoneTextRootObject.SetActive(gameModeConfig.SpawnSelection);
			_lockRoomButton.gameObject.SetActive(false);
			_leaveRoomButton.gameObject.SetActive(false);
			_getReadyToRumbleText.gameObject.SetActive(false);
			_playersFoundText.gameObject.SetActive(true);
			_findingPlayersText.gameObject.SetActive(true);
			_botsToggle.SetInitialValue(true);
			_botsToggleObjectRoot.SetActive(false);
			_spectateToggle.SetInitialValue(false);
			_spectateToggleObjectRoot.SetActive(false);
			_kickButton.gameObject.SetActive(false);
			_loadingText.SetActive(true);
			_playersFoundText.text = $"{0}/{room.MaxPlayers.ToString()}";
			
			var matchType = room.GetMatchType();
			var gameMode = room.GetGameModeId().ToUpper();
			var quantumGameConfigs = _services.ConfigsProvider.GetConfig<QuantumGameConfig>();
			var minPlayers = matchType == MatchType.Ranked ? quantumGameConfigs.RankedMatchmakingMinPlayers : 0;
			var matchmakingTime = matchType == MatchType.Ranked ? 
				                      quantumGameConfigs.RankedMatchmakingTime.AsFloat :
				                      quantumGameConfigs.CasualMatchmakingTime.AsFloat;
			
			_selectedGameModeText.text = string.Format(ScriptLocalization.MainMenu.SelectedGameModeValue, matchType.ToString().ToUpper(), gameMode);

			UpdateRoomPlayerCounts();

			if (CurrentRoom.IsMatchmakingRoom())
			{
				_playerListHolder.gameObject.SetActive(false);
				_spectatorListHolder.gameObject.SetActive(false);
				_playerMatchmakingRootObject.SetActive(true);
				_playerCountHolder.SetActive(false);
				_roomNameRootObject.SetActive(false);

				UpdatePlayersWaitingImages(room.GetRealPlayerCapacity(), room.GetRealPlayerAmount());
				StartCoroutine(MatchmakingTimerCoroutine(matchmakingTime, minPlayers));
			}
			else
			{
				_playerListHolder.Init((uint) NetworkUtils.GetMaxPlayers(gameModeConfig, mapConfig), RequestKickPlayer);
				_spectatorListHolder.Init(GameConstants.Data.MATCH_SPECTATOR_SPOTS, RequestKickPlayer);

				_playerListHolder.gameObject.SetActive(true);
				_spectatorListHolder.gameObject.SetActive(true);
				_playerMatchmakingRootObject.SetActive(false);
				_playerCountHolder.SetActive(true);

				_topTitleText.text = ScriptLocalization.MainMenu.PrepareForActionBasic;
				_roomNameText.text = string.Format(ScriptLocalization.MainMenu.RoomCurrentName, room.GetRoomName());
				_roomNameRootObject.SetActive(true);

				foreach (var playerKvp in CurrentRoom.Players)
				{
					AddOrUpdatePlayerInList(playerKvp.Value);
				}
			}
		}

		protected override void OnClosed()
		{
			mapSelectionView.CleanupMapView();
			_rootObject.SetActive(true);
		}

		private void OnCoreMatchAssetsLoaded(CoreMatchAssetsLoadedMessage msg)
		{
			_loadedCoreMatchAssets = true;

			if (RejoiningRoom)
			{
				return;
			}

			if (!_services.NetworkService.QuantumClient.CurrentRoom.IsOpen)
			{
				return;
			}

			_leaveRoomButton.gameObject.SetActive(true);
			_loadingText.SetActive(false);

			if (_services.NetworkService.QuantumClient.LocalPlayer.IsMasterClient && !CurrentRoom.IsMatchmakingRoom())
			{
				_lockRoomButton.gameObject.SetActive(true);
				_kickButton.gameObject.SetActive(true);
				_botsToggleObjectRoot.SetActive(_services.NetworkService.CurrentRoomGameModeConfig.Value.AllowBots);
			}

			if (!CurrentRoom.IsMatchmakingRoom())
			{
				_spectateToggleObjectRoot.SetActive(true);
			}
		}

		private void OnStartedFinalPreloadMessage(StartedFinalPreloadMessage msg)
		{
			foreach (var playerKvp in CurrentRoom.Players)
			{
				AddOrUpdatePlayerInList(playerKvp.Value);
			}
		}

		/// <inheritdoc />
		public void OnPlayerEnteredRoom(Player newPlayer)
		{
			UpdateRoomPlayerCounts();
			AddOrUpdatePlayerInList(newPlayer);
			UpdatePlayersWaitingImages(CurrentRoom.GetRealPlayerCapacity(), CurrentRoom.GetRealPlayerAmount());
		}

		/// <inheritdoc />
		public void OnPlayerLeftRoom(Player otherPlayer)
		{
			UpdateRoomPlayerCounts();
			RemovePlayerInAllLists(otherPlayer);
			UpdatePlayersWaitingImages(CurrentRoom.GetRealPlayerCapacity(), CurrentRoom.GetRealPlayerAmount());
		}

		/// <inheritdoc />
		public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
		{
			if (propertiesThatChanged.TryGetValue(GamePropertyKey.IsOpen, out var isOpen) && !(bool) isOpen)
			{
				if (!CurrentRoom.IsMatchmakingRoom())
				{
					_playerListHolder.SetFinalPreloadPhase(true);
					_spectatorListHolder.SetFinalPreloadPhase(true);
				}

				ReadyToPlay();
			}
		}

		/// <inheritdoc />
		public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
		{
			UpdateRoomPlayerCounts();
			AddOrUpdatePlayerInList(targetPlayer);
			CheckEnableLockRoomButton();
		}

		/// <inheritdoc />
		public void OnMasterClientSwitched(Player newMasterClient)
		{
			AddOrUpdatePlayerInList(newMasterClient);

			if (!CurrentRoom.IsMatchmakingRoom() && newMasterClient.IsLocal && _loadedCoreMatchAssets)
			{
				_kickButton.gameObject.SetActive(true);
				_lockRoomButton.gameObject.SetActive(true);
				_botsToggleObjectRoot.SetActive(_services.NetworkService.CurrentRoomGameModeConfig.Value.AllowBots);
			}
		}

		private void UpdateRoomPlayerCounts()
		{
			if (CurrentRoom.IsMatchmakingRoom())
			{
				return;
			}

			_playerCountText.text = $"{CurrentRoom.GetRealPlayerAmount()}/{CurrentRoom.GetRealPlayerCapacity()}";
			_spectatorCountText.text = $"{CurrentRoom.GetSpectatorAmount()}/{CurrentRoom.GetSpectatorCapacity()}";
		}

		private void AddOrUpdatePlayerInList(Player player)
		{
			if (CurrentRoom.IsMatchmakingRoom())
			{
				return;
			}

			var isSpectator = (bool) player.CustomProperties[GameConstants.Network.PLAYER_PROPS_SPECTATOR];

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

		private void RemovePlayerInAllLists(Player player)
		{
			if (CurrentRoom.IsMatchmakingRoom())
			{
				return;
			}

			if (_playerListHolder.Has(player))
			{
				_playerListHolder.RemovePlayer(player);
			}

			if (_spectatorListHolder.Has(player))
			{
				_spectatorListHolder.RemovePlayer(player);
			}
		}

		private void CheckEnableSpectatorToggle()
		{
			if (_spectatorToggleTimeOut)
			{
				return;
			}

			var isSpectator =
				(bool) _services.NetworkService.QuantumClient.LocalPlayer.CustomProperties
					[GameConstants.Network.PLAYER_PROPS_SPECTATOR];
			var relevantPlayerAmount = 0;
			var relevantPlayerCapacity = 0;

			if (isSpectator)
			{
				relevantPlayerAmount = CurrentRoom.GetRealPlayerAmount();
				relevantPlayerCapacity = CurrentRoom.GetRealPlayerCapacity();
			}
			else
			{
				relevantPlayerAmount = CurrentRoom.GetSpectatorAmount();
				relevantPlayerCapacity = CurrentRoom.GetSpectatorCapacity();
			}

			SetSpectateInteractable(relevantPlayerAmount < relevantPlayerCapacity);
			CheckEnableLockRoomButton();
		}

		private void CheckEnableLockRoomButton()
		{
			_lockRoomButton.interactable = CurrentRoom.GetRealPlayerAmount() > 0;
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

		private void UpdatePlayersWaitingImages(int maxPlayers, int playerAmount)
		{
			if (!CurrentRoom.IsMatchmakingRoom())
			{
				return;
			}

			for (var i = 0; i < _playersWaitingImage.Length; i++)
			{
				_playersWaitingImage[i].gameObject.SetActive((i + 1) <= playerAmount);
			}

			_playersFoundText.text = $"{playerAmount.ToString()}/{maxPlayers.ToString()}";
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

		private IEnumerator MatchmakingTimerCoroutine(float matchmakingTime, int minPlayers)
		{
			var roomCreateTime = CurrentRoom.GetRoomCreationDateTime();
			var matchmakingEndTime = roomCreateTime.AddSeconds(matchmakingTime);

			while (DateTime.UtcNow < matchmakingEndTime)
			{
				var timeLeft = (DateTime.UtcNow - matchmakingEndTime).Duration();
				_topTitleText.text = string.Format(ScriptLocalization.MainMenu.PrepareForActionTimer, timeLeft.TotalSeconds.ToString("F0"));
				
				yield return null;
			}

			if (CurrentRoom.GetRealPlayerAmount() >= minPlayers)
			{
				_topTitleText.text = ScriptLocalization.MainMenu.PrepareForActionBasic;
			}
			else
			{
				_topTitleText.text = ScriptLocalization.MainMenu.PrepareForActionWaiting;
			}
		}

		private void OnLockRoomClicked()
		{
			ReadyToPlay();
			_services.MessageBrokerService.Publish(new RoomLockClickedMessage() {AddBots = _botsToggle.isOn});
		}

		private void OnLeaveRoomClicked()
		{
			_services.MessageBrokerService.Publish(new RoomLeaveClickedMessage());
		}

		private void ReadyToPlay()
		{
			mapSelectionView.SelectionEnabled = false;
			
			DeactivateKickOverlay();
			_loadingText.SetActive(true);
			_lockRoomButton.gameObject.SetActive(false);
			_leaveRoomButton.gameObject.SetActive(false);
			_botsToggleObjectRoot.SetActive(false);
			_kickButton.gameObject.SetActive(false);
			_spectateToggleObjectRoot.SetActive(false);

			if (CurrentRoom.IsMatchmakingRoom())
			{
				_getReadyToRumbleText.gameObject.SetActive(true);
				_playersFoundText.gameObject.SetActive(false);
				_findingPlayersText.gameObject.SetActive(false);
			}
		}

		private void ActivateKickOverlay()
		{
			foreach (var overlayObject in _kickOverlayObjects)
			{
				overlayObject.SetActive(true);
			}

			_kickModeActive = true;
		}

		private void DeactivateKickOverlay()
		{
			foreach (var overlayObject in _kickOverlayObjects)
			{
				overlayObject.SetActive(false);
			}
			
			_kickModeActive = false;
		}

		private void RequestKickPlayer(Player player)
		{
			if (player.UserId == _services.NetworkService.QuantumClient.LocalPlayer.UserId ||
			    !_kickModeActive || !_services.NetworkService.QuantumClient.LocalPlayer.IsMasterClient ||
			    !player.LoadedCoreMatchAssets())
			{
				return;
			}

			var title = string.Format(ScriptLocalization.MainMenu.MatchmakingKickConfirm, player.NickName).ToUpper();
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.Yes.ToUpper(),
				ButtonOnClick = () =>
				{
					_services.MessageBrokerService.Publish(new RequestKickPlayerMessage() {Player = player});
					DeactivateKickOverlay();
				}
			};

			_services.GenericDialogService.OpenDialog(title, true, confirmButton, DeactivateKickOverlay);
		}

		private void OnSpectatorToggle(bool isOn)
		{
			// Set lock room button to be inactive immediately - gets enabled when player properties change
			if (_services.NetworkService.QuantumClient.LocalPlayer.IsMasterClient)
			{
				_lockRoomButton.interactable = false;
			}

			_services.MessageBrokerService.Publish(new SpectatorModeToggledMessage() {IsSpectator = isOn});
			_services.CoroutineService.StartCoroutine(TimeoutSpectatorToggleCoroutine());
		}
	}
}
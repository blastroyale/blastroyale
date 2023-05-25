using System;
using System.Collections;
using System.Threading.Tasks;
using DG.Tweening;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MainMenuViews;
using FirstLight.UiService;
using I2.Loc;
using Photon.Realtime;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Image = UnityEngine.UI.Image;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Players Waiting Screen UI by:
	/// - Showing the loading status
	/// </summary>
	public class CustomLobbyScreenPresenter : UiToolkitPresenterData<CustomLobbyScreenPresenter.StateData>,
											  IInRoomCallbacks
	{
		public struct StateData
		{
			public Action LeaveRoomClicked;
		}

		private const int MAX_SQUAD_ID = 30;

		public MapSelectionView mapSelectionView;
		
		[SerializeField, Required] private GameObject _rootObject;

		[SerializeField, Required] private Image[] _playersWaitingImage;
		[SerializeField, Required] private TextMeshProUGUI _playersFoundText;
		[SerializeField, Required] private TextMeshProUGUI _findingPlayersText;
		[SerializeField, Required] private TextMeshProUGUI _getReadyToRumbleText;

		[SerializeField, Required] private TextMeshProUGUI _playerCountText;
		[SerializeField, Required] private TextMeshProUGUI _spectatorCountText;
		
		[SerializeField, Required] private GameObject[] _kickOverlayObjects;
		[SerializeField, Required] private GameObject _loadingText;
		[SerializeField, Required] private GameObject _playerMatchmakingRootObject;
		[SerializeField, Required] private GameObject _playerCountHolder;
		[SerializeField, Required] private GameObject _selectDropZoneTextRootObject;
		[SerializeField, Required] private PlayerListHolderView _playerListHolder;
		[SerializeField, Required] private PlayerListHolderView _spectatorListHolder;

		private Button _lockRoomButton;
		private Button _kickButton;
		private VisualElement _squadHolder;
		private VisualElement _topTitleHolder;
		private Label _gameModeLabel;
		private Label _prepareForActionLabel;
		private Label _squadIDLabel;
		private Button _squadIdUpButton;
		private Button _squadIdDownButton;
		private LocalizedToggle _botsToggle;
		private LocalizedToggle _spectateToggle;
		private ScreenHeaderElement _header;
		
		private IGameServices _services;
		private bool _loadedCoreMatchAssets;
		private bool _spectatorToggleTimeOut;
		private bool _kickModeActive = false;

		private Room CurrentRoom => _services.NetworkService.QuantumClient.CurrentRoom;
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
			
			_services.NetworkService.QuantumClient.AddCallbackTarget(this);
			_services.MessageBrokerService.Subscribe<CoreMatchAssetsLoadedMessage>(OnCoreMatchAssetsLoaded);
			_services.MessageBrokerService.Subscribe<StartedFinalPreloadMessage>(OnStartedFinalPreloadMessage);
		}

		private void OnDestroy()
		{
			_services?.NetworkService?.QuantumClient?.RemoveCallbackTarget(this);
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		protected override void QueryElements(VisualElement root)
		{
			_header = root.Q<ScreenHeaderElement>("Header").Required();
			_header.backClicked += OnLeaveRoomClicked;
			_header.homeClicked += OnLeaveRoomClicked;

			_botsToggle = root.Q<LocalizedToggle>("BotsToggle").Required();
			SetupBotsToggle(root.Q<LocalizedToggle>("BotsToggle").Required(),
			() => _botsToggle.value,
			val => _botsToggle.value = val);

			_spectateToggle = root.Q<LocalizedToggle>("SpectateToggle").Required();
			SetupSpectatorToggle(root.Q<LocalizedToggle>("SpectateToggle").Required(),
				() => _spectateToggle.value,
				val => _spectateToggle.value = val);

			_lockRoomButton = root.Q<Button>("LockButton").Required();
			_lockRoomButton.clicked += OnLockRoomClicked;

			_kickButton = root.Q<Button>("KickButton").Required();
			_kickButton.clicked += ActivateKickOverlay;

			_squadHolder = root.Q<VisualElement>("SquadSelectHolder");
			_squadIDLabel = root.Q<Label>("SquadIDLabel");
			_squadIdDownButton = root.Q<Button>("SquadDownButton").Required();
			_squadIdDownButton.clicked += OnSquadIdDown;
			_squadIdUpButton = root.Q<Button>("SquadUpButton").Required();
			_squadIdUpButton.clicked += OnSquadIdUp;

			_topTitleHolder = root.Q<VisualElement>("TopTitleHolder").Required();
			_gameModeLabel = root.Q<Label>("GameModeLabel");
			_prepareForActionLabel = root.Q<Label>("GetReadyLabel");
		}
		
		private void SetupBotsToggle(Toggle toggle, Func<bool> getter, Action<bool> setter)
		{
			toggle.value = getter();
			toggle.RegisterCallback<ChangeEvent<bool>, Action<bool>>((e, s) =>
			{
				s(e.newValue);
				CheckEnableLockRoomButton();
			}, setter);
		}
		
		private void SetupSpectatorToggle(Toggle toggle, Func<bool> getter, Action<bool> setter)
		{
			toggle.value = getter();
			toggle.RegisterCallback<ChangeEvent<bool>, Action<bool>>((e, s) =>
			{
				s(e.newValue);
				OnSpectatorToggle(toggle.value);
			}, setter);
		}
		
		/// <inheritdoc />
		protected override void OnOpened()
		{
			base.OnOpened();
		
			if (_services.TutorialService.CurrentRunningTutorial.Value == TutorialSection.FIRST_GUIDE_MATCH) return;

			_rootObject.SetActive(true);

			var room = _services.NetworkService.QuantumClient.CurrentRoom;
			var mapConfig = _services.NetworkService.CurrentRoomMapConfig.Value;
			var gameModeConfig = _services.NetworkService.CurrentRoomGameModeConfig.Value;

			// This whole script will be culled when custom games are introduced.
			// This conditional code limits the garbage to this script only
			Vector3 dropzonePosRot = CurrentRoom.CustomProperties.ContainsKey(GameConstants.Network.DROP_ZONE_POS_ROT)
				? room.GetDropzonePosRot()
				: Vector3.zero;
			mapSelectionView.SetupMapView(room.GetGameModeId(), room.GetMapId(), dropzonePosRot);

			if (RejoiningRoom)
			{
				_playerListHolder.Init((uint) NetworkUtils.GetMaxPlayers(gameModeConfig, mapConfig), RequestKickPlayer);
				_spectatorListHolder.Init(GameConstants.Data.MATCH_SPECTATOR_SPOTS, RequestKickPlayer);

				_kickButton.SetDisplay(false);
				_botsToggle.SetDisplay(false);
				_spectateToggle.SetDisplay(false);
				_lockRoomButton.SetDisplay(false);
				_loadingText.SetActive(true);
				_squadHolder.SetDisplay(false);
				_topTitleHolder.SetDisplay(true);

				foreach (var playerKvp in CurrentRoom.Players)
				{
					AddOrUpdatePlayerInList(playerKvp.Value);
				}

				return;
			}

			_selectDropZoneTextRootObject.SetActive(gameModeConfig.SpawnSelection);
			_lockRoomButton.SetDisplay(false);
			_getReadyToRumbleText.gameObject.SetActive(false);
			_playersFoundText.gameObject.SetActive(true);
			_findingPlayersText.gameObject.SetActive(true);
			
			_botsToggle.SetValueWithoutNotify(true);
			_botsToggle.SetDisplay(false);
			_spectateToggle.SetValueWithoutNotify(false);
			_spectateToggle.SetDisplay(false);
			
			_kickButton.SetDisplay(false);
			_loadingText.SetActive(true);
			_playersFoundText.text = $"{0}/{room.MaxPlayers.ToString()}";
			_squadHolder.SetDisplay(gameModeConfig.Teams);
			_topTitleHolder.SetDisplay(!gameModeConfig.Teams);
			_squadIDLabel.text = _squadId.ToString();

			// TODO: Sets the initial TeamID. Hacky, should be somewhere else, but it should do for custom games for now.
			if (gameModeConfig.Teams)
			{
				_services.MessageBrokerService.Publish(new ManualTeamIdSetMessage
					{TeamId = $"{GameConstants.Network.MANUAL_TEAM_ID_PREFIX}{_squadId}"});
			}

			var matchType = room.GetMatchType();
			var gameMode = room.GetGameModeId().ToUpper();
			var quantumGameConfig = _services.ConfigsProvider.GetConfig<QuantumGameConfig>();
			var minPlayers = matchType == MatchType.Ranked ? quantumGameConfig.RankedMatchmakingMinPlayers : 0;
			var matchmakingTime = matchType == MatchType.Ranked
				? quantumGameConfig.RankedMatchmakingTime.AsFloat
				: quantumGameConfig.CasualMatchmakingTime.AsFloat;

			string cleanedGameMode = string.Format(ScriptLocalization.MainMenu.SelectedGameModeValue,
				matchType.ToString().ToUpper(), gameMode);
			cleanedGameMode = cleanedGameMode.Replace("\n", " ");
			_gameModeLabel.text = cleanedGameMode;

			UpdateRoomPlayerCounts();

			if (CurrentRoom.IsMatchmakingRoom())
			{
				_playerListHolder.gameObject.SetActive(false);
				_spectatorListHolder.gameObject.SetActive(false);
				_playerMatchmakingRootObject.SetActive(true);
				_playerCountHolder.SetActive(false);

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

				_prepareForActionLabel.text = ScriptLocalization.MainMenu.PrepareForActionBasic;
				_header.SetTitle(string.Format(ScriptLocalization.MainMenu.RoomCurrentName, room.GetRoomName()));

				foreach (var playerKvp in CurrentRoom.Players)
				{
					AddOrUpdatePlayerInList(playerKvp.Value);
				}
			}

			if (_services.NetworkService.LocalPlayer.LoadedCoreMatchAssets())
			{
				OnCoreMatchAssetsLoaded(new CoreMatchAssetsLoadedMessage());
			}
		}

		protected override Task OnClosed()
		{
			_rootObject.SetActive(true);
			return Task.CompletedTask;
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
			
			_loadingText.SetActive(false);

			if (_services.NetworkService.LocalPlayer.IsMasterClient && !CurrentRoom.IsMatchmakingRoom())
			{
				_lockRoomButton.SetDisplay(true);
				_kickButton.SetDisplay(true);
				_botsToggle.SetDisplay(_services.NetworkService.CurrentRoomGameModeConfig.Value.AllowBots);
			}

			if (!CurrentRoom.IsMatchmakingRoom())
			{
				_spectateToggle.SetDisplay(true);
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
				_kickButton.SetDisplay(true);
				_lockRoomButton.SetDisplay(true);
				_botsToggle.SetDisplay(_services.NetworkService.CurrentRoomGameModeConfig.Value.AllowBots);
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
				(bool) _services.NetworkService.LocalPlayer.CustomProperties
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
			var realPlayers = CurrentRoom.GetRealPlayerAmount();
			_lockRoomButton.SetEnabled(realPlayers > 1 || realPlayers == 1 && _botsToggle.value);
		}

		private void SetSpectateInteractable(bool interactable)
		{
			_spectateToggle.SetEnabled(interactable);
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
				_prepareForActionLabel.text = string.Format(ScriptLocalization.MainMenu.PrepareForActionTimer,
					timeLeft.TotalSeconds.ToString("F0"));

				yield return null;
			}

			if (CurrentRoom.GetRealPlayerAmount() >= minPlayers)
			{
				_prepareForActionLabel.text = ScriptLocalization.MainMenu.PrepareForActionBasic;
			}
			else
			{
				_prepareForActionLabel.text = ScriptLocalization.MainMenu.PrepareForActionWaiting;
			}
		}

		private void OnLockRoomClicked()
		{
			ReadyToPlay();
			_services.MessageBrokerService.Publish(new RoomLockClickedMessage() {AddBots = _botsToggle.value});
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
			_lockRoomButton.SetDisplay(false);
			_botsToggle.SetDisplay(false);
			_kickButton.SetDisplay(false);
			_spectateToggle.SetDisplay(false);
			_squadHolder.SetDisplay(false);
			_topTitleHolder.SetDisplay(true);

			if (CurrentRoom.IsMatchmakingRoom())
			{
				_getReadyToRumbleText.gameObject.SetActive(true);
				_playersFoundText.gameObject.SetActive(false);
				_findingPlayersText.gameObject.SetActive(false);
			}
		}

		private void ActivateKickOverlay()
		{
			_kickModeActive = !_kickModeActive;
			_kickButton.text = _kickModeActive ? ScriptLocalization.UITHomeScreen.cancel : ScriptLocalization.MainMenu.MatchmakingKickButton;
			
			_spectateToggle.SetDisplay(!_kickModeActive);
			_botsToggle.SetDisplay(!_kickModeActive);
			_lockRoomButton.SetDisplay(!_kickModeActive);
			_squadHolder.SetDisplay(!_kickModeActive);
			_squadIdDownButton.SetDisplay(!_kickModeActive);
			_squadIdUpButton.SetDisplay(!_kickModeActive);
			
			foreach (var overlayObject in _kickOverlayObjects)
			{
				overlayObject.SetActive(_kickModeActive);
			}
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
			_squadIDLabel.text = _squadId.ToString();

			_squadIdUpdateDelayed?.Kill();
			_squadIdUpdateDelayed = DOVirtual.DelayedCall(1f, () =>
			{
				_services.MessageBrokerService.Publish(new ManualTeamIdSetMessage
					{TeamId = $"{GameConstants.Network.MANUAL_TEAM_ID_PREFIX}{_squadId}"});
			});
		}

		private void RequestKickPlayer(Player player)
		{
			if (player.UserId == _services.NetworkService.LocalPlayer.UserId ||
				!_kickModeActive || !_services.NetworkService.LocalPlayer.IsMasterClient ||
				!player.LoadedCoreMatchAssets())
			{
				return;
			}

			var desc = string.Format(ScriptLocalization.MainMenu.MatchmakingKickConfirm, player.NickName).ToUpper();
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.Yes.ToUpper(),
				ButtonOnClick = () =>
				{
					_services.MessageBrokerService.Publish(new RequestKickPlayerMessage() {Player = player});
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
				_lockRoomButton.SetEnabled(false);
			}

			_services.MessageBrokerService.Publish(new SpectatorModeToggledMessage() {IsSpectator = isOn});
			_services.CoroutineService.StartCoroutine(TimeoutSpectatorToggleCoroutine());
		}
	}
}
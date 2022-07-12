using System.Collections;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MainMenuViews;
using FirstLight.UiService;
using I2.Loc;
using Photon.Realtime;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Random = UnityEngine.Random;

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

		public MapSelectionView MapSelectionView;

		[SerializeField, Required] private Button _lockRoomButton;
		[SerializeField, Required] private Button _leaveRoomButton;
		[SerializeField, Required] private Image[] _playersWaitingImage;
		[SerializeField, Required] private TextMeshProUGUI _playersFoundText;
		[SerializeField, Required] private TextMeshProUGUI _findingPlayersText;
		[SerializeField, Required] private TextMeshProUGUI _getReadyToRumbleText;
		[SerializeField, Required] private TextMeshProUGUI _roomNameText;
		[SerializeField, Required] private GameObject _loadingText;
		[SerializeField, Required] private GameObject _roomNameRootObject;
		[SerializeField, Required] private GameObject _playerMatchmakingRootObject;
		[SerializeField, Required] private PlayerListHolderView _playerListHolder;
		[SerializeField, Required] private PlayerListHolderView _spectatorListHolder;
		[SerializeField, Required] private UiToggleButtonView _botsToggle;
		[SerializeField, Required] private UiToggleButtonView _spectateToggle;
		[SerializeField, Required] private GameObject _botsToggleObjectRoot;
		[SerializeField, Required] private GameObject _spectateToggleObjectRoot;
		[SerializeField] private Color _spectateDisabledColor;
		
		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;
		private float _rndWaitingTimeLowest;
		private float _rndWaitingTimeBiggest;
		private bool _loadedCoreMatchAssets;
		private bool _spectatorToggleTimeOut;

		private Room CurrentRoom => _services.NetworkService.QuantumClient.CurrentRoom;
		private bool IsMatchmakingRoom => _services.NetworkService.IsCurrentRoomForMatchmaking;

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
			var room = _services.NetworkService.QuantumClient.CurrentRoom;
			var mapInfo = _services.NetworkService.CurrentRoomMapConfig.Value;
			
			MapSelectionView.SetupMapView(room.GetMapId());
			
			if (!_services.NetworkService.IsJoiningNewMatch)
			{
				_playerListHolder.Init((uint) mapInfo.PlayersLimit);
				_spectatorListHolder.Init(GameConstants.Data.MATCH_SPECTATOR_SPOTS);
				
				_spectateToggleObjectRoot.SetActive(false);
				_botsToggleObjectRoot.SetActive(false);
				_lockRoomButton.gameObject.SetActive(false);
				
				foreach (var playerKvp in CurrentRoom.Players)
				{
					AddOrUpdatePlayerInList(playerKvp.Value);
				}
				
				return;
			}
			
			_lockRoomButton.gameObject.SetActive(false);
			_leaveRoomButton.gameObject.SetActive(false);
			_getReadyToRumbleText.gameObject.SetActive(false);
			_playersFoundText.gameObject.SetActive(true);
			_findingPlayersText.gameObject.SetActive(true);
			_botsToggle.SetInitialValue(true);
			_botsToggleObjectRoot.SetActive(false);
			_spectateToggle.SetInitialValue(false);
			_spectateToggleObjectRoot.SetActive(false);
			_loadingText.SetActive(true);
			_playersFoundText.text = $"{0}/{room.MaxPlayers.ToString()}";
			_rndWaitingTimeLowest = 2f / room.MaxPlayers;
			_rndWaitingTimeBiggest = 8f / room.MaxPlayers;

			if (IsMatchmakingRoom)
			{
				_playerListHolder.gameObject.SetActive(false);
				_spectatorListHolder.gameObject.SetActive(false);
				_playerMatchmakingRootObject.SetActive(true);

				_roomNameRootObject.SetActive(false);
				StartCoroutine(TimeUpdateCoroutine(room.GetRealPlayerCapacity()));
				UpdatePlayersWaitingImages(room.GetRealPlayerCapacity(), room.GetRealPlayerAmount());
			}
			else
			{
				_playerListHolder.Init((uint) mapInfo.PlayersLimit);
				_spectatorListHolder.Init(GameConstants.Data.MATCH_SPECTATOR_SPOTS);

				_playerListHolder.gameObject.SetActive(true);
				_spectatorListHolder.gameObject.SetActive(true);
				_playerMatchmakingRootObject.SetActive(false);

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
			MapSelectionView.CleanupMapView();
		}

		private void OnCoreMatchAssetsLoaded(CoreMatchAssetsLoadedMessage msg)
		{
			// For custom games, only show leave room button if we are not loading straight into the match (if host locked room while we were loading)
			if (!IsMatchmakingRoom && !_services.NetworkService.QuantumClient.CurrentRoom.AreAllPlayersReady())
			{
				_loadingText.SetActive(false);
				_leaveRoomButton.gameObject.SetActive(true);
			}
			
			if (_services.NetworkService.QuantumClient.LocalPlayer.IsMasterClient && !IsMatchmakingRoom &&
			    _services.NetworkService.QuantumClient.CurrentRoom.IsOpen)
			{
				_lockRoomButton.gameObject.SetActive(true);
				_botsToggleObjectRoot.SetActive(true);
			}

			if (!IsMatchmakingRoom)
			{
				_spectateToggleObjectRoot.SetActive(true);
			}

			_loadedCoreMatchAssets = true;
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
			AddOrUpdatePlayerInList(newPlayer);
		}

		/// <inheritdoc />
		public void OnPlayerLeftRoom(Player otherPlayer)
		{
			RemovePlayerInAllLists(otherPlayer);
		}

		/// <inheritdoc />
		public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
		{
			if (propertiesThatChanged.TryGetValue(GamePropertyKey.IsOpen, out var isOpen) && !(bool) isOpen)
			{
				if (!IsMatchmakingRoom)
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
			AddOrUpdatePlayerInList(targetPlayer);
			CheckEnableLockRoomButton();
		}

		/// <inheritdoc />
		public void OnMasterClientSwitched(Player newMasterClient)
		{
			AddOrUpdatePlayerInList(newMasterClient);

			if (!IsMatchmakingRoom && newMasterClient.IsLocal && _loadedCoreMatchAssets)
			{
				_lockRoomButton.gameObject.SetActive(true);
				_botsToggleObjectRoot.SetActive(true);
			}
		}

		private void AddOrUpdatePlayerInList(Player player)
		{
			if (IsMatchmakingRoom)
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
			if (IsMatchmakingRoom)
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
			
			var isSpectator = (bool) _services.NetworkService.QuantumClient.LocalPlayer.CustomProperties[GameConstants.Network.PLAYER_PROPS_SPECTATOR];
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
			for (var i = 0; i < _playersWaitingImage.Length; i++)
			{
				_playersWaitingImage[i].gameObject.SetActive((i + 1) <= playerAmount);
			}

			_playersFoundText.text = $"{playerAmount.ToString()}/{maxPlayers.ToString()}";
		}

		private IEnumerator TimeUpdateCoroutine(int maxPlayers)
		{
			for (var i = 0; i < _playersWaitingImage.Length && i < maxPlayers; i++)
			{
				UpdatePlayersWaitingImages(maxPlayers, i + 1);
				yield return new WaitForSeconds(Random.Range(_rndWaitingTimeLowest, _rndWaitingTimeBiggest));
			}

			yield return new WaitForSeconds(0.5f);

			_getReadyToRumbleText.gameObject.SetActive(true);
			_playersFoundText.gameObject.SetActive(false);
			_findingPlayersText.gameObject.SetActive(false);
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
			_services.MessageBrokerService.Publish(new RoomLockClickedMessage() {AddBots = _botsToggle.isOn});
		}

		private void OnLeaveRoomClicked()
		{
			_services.MessageBrokerService.Publish(new RoomLeaveClickedMessage());
		}

		private void ReadyToPlay()
		{
			_loadingText.SetActive(true);
			_lockRoomButton.gameObject.SetActive(false);
			_leaveRoomButton.gameObject.SetActive(false);
			_botsToggleObjectRoot.SetActive(false);
			_spectateToggleObjectRoot.SetActive(false);

			if (IsMatchmakingRoom)
			{
				_getReadyToRumbleText.gameObject.SetActive(true);
				_playersFoundText.gameObject.SetActive(false);
				_findingPlayersText.gameObject.SetActive(false);
			}
		}

		private void OnSpectatorToggle(bool isOn)
		{
			_services.MessageBrokerService.Publish(new SpectatorModeToggledMessage() {IsSpectator = isOn});
			_services.CoroutineService.StartCoroutine(TimeoutSpectatorToggleCoroutine());
		}

		/* This code is not needed at the moment. This is legacy code an necessary when adding the character 3D model
		 again to the screen. Talk with Miguel about it 
		 
		private void OnSceneChanged(Scene previous, Scene current)
		{
			// Ignore scene changes that are not levels
			if (current.buildIndex != -1)
			{
				return;
			}
		}

		private void SetLayerState(bool state, bool forceUiAwakeCalls)
		{
			// Little hack to avoid UIs to spam over this screen
			for (var i = 0; i < Data.UiService.TotalLayers; i++)
			{
				if (!Data.UiService.TryGetLayer(i, out var layer))
				{
					continue;
				}

				if (forceUiAwakeCalls)
				{
					layer.SetActive(!state);
				
					foreach (var canvas in layer.GetComponentsInChildren<UiPresenter>(true))
					{
						// To force the UI awake calls
						canvas.gameObject.SetActive(true);
						canvas.gameObject.SetActive(false);
					}
				}
				
				layer.SetActive(state);
			}
		}*/
	}
}
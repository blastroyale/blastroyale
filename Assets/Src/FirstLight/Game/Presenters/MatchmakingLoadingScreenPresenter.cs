using System;
using System.Collections;
using FirstLight.Game.Configs;
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
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
	public class MatchmakingLoadingScreenPresenter : UiPresenterData<MatchmakingLoadingScreenPresenter.StateData>, IInRoomCallbacks
	{
		public struct StateData
		{
			public IUiService UiService;
		}

		public MapSelectionView MapSelectionView;
		
		[SerializeField] private Button _lockRoomButton;
		[SerializeField] private Button _leaveRoomButton;
		[SerializeField] private Image [] _playersWaitingImage;
		[SerializeField] private TextMeshProUGUI _playersFoundText;
		[SerializeField] private TextMeshProUGUI _findingPlayersText;
		[SerializeField] private TextMeshProUGUI _getReadyToRumbleText;
		[SerializeField] private TextMeshProUGUI _roomNameText;
		[SerializeField] private GameObject _loadingText;
		[SerializeField] private GameObject _roomNameRootObject;
		[SerializeField] private GameObject _playerMatchmakingRootObject;
		[SerializeField] private PlayerListHolderView _playerListHolder;
		
		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;
		private float _rndWaitingTimeLowest;
		private float _rndWaitingTimeBiggest;

		private Room CurrentRoom => _services.NetworkService.QuantumClient.CurrentRoom;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			
			foreach (var image in _playersWaitingImage)
			{
				image.gameObject.SetActive(false);
			}
			
			_services.NetworkService.QuantumClient.AddCallbackTarget(this);
			_lockRoomButton.onClick.AddListener(OnLockRoomClicked);
			_leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);
			_services.MessageBrokerService.Subscribe<CoreMatchAssetsLoadedMessage>(OnCoreMatchAssetsLoaded);
			_services.MessageBrokerService.Subscribe<AllMatchAssetsLoadedMessage>(OnAllMatchAssetsLoaded);
			_services.MessageBrokerService.Subscribe<StartedFinalPreloadMessage>(OnStartedFinalPreloadMessage);
			
			SceneManager.activeSceneChanged += OnSceneChanged;
		}

		private void OnDestroy()
		{
			_services?.NetworkService?.QuantumClient?.RemoveCallbackTarget(this);
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			
			SceneManager.activeSceneChanged -= OnSceneChanged;
		}

		/// <inheritdoc />
		protected override void OnOpened()
		{
			var room = _services.NetworkService.QuantumClient.CurrentRoom;
			
			_lockRoomButton.gameObject.SetActive(false);
			_leaveRoomButton.gameObject.SetActive(false);
			_getReadyToRumbleText.gameObject.SetActive(false);
			_playersFoundText.gameObject.SetActive(true);
			_findingPlayersText.gameObject.SetActive(true);
			_loadingText.SetActive(true);
			_playersFoundText.text = $"{0}/{room.MaxPlayers.ToString()}" ;
			_rndWaitingTimeLowest = 2f / room.MaxPlayers;
			_rndWaitingTimeBiggest = 8f / room.MaxPlayers;

			MapSelectionView.SetupMapView(room.GetMapId());
			_playerListHolder.WipeAllSlots();

			if (room.IsVisible)
			{
				_playerListHolder.gameObject.SetActive(false);
				_playerMatchmakingRootObject.SetActive(true);

				_roomNameRootObject.SetActive(false);
				StartCoroutine(TimeUpdateCoroutine(room.MaxPlayers));
				UpdatePlayersWaitingImages(room.MaxPlayers, room.PlayerCount);
			}
			else
			{
				_playerListHolder.gameObject.SetActive(true);
				_playerMatchmakingRootObject.SetActive(false);
				
				_roomNameText.text = string.Format(ScriptLocalization.MainMenu.RoomCurrentName, CurrentRoom.Name);
				_roomNameRootObject.SetActive(true);
				
				foreach (var playerKvp in CurrentRoom.Players)
				{
					var status = "";
					
					if (playerKvp.Value.IsLocal)
					{
						status = ScriptLocalization.AdventureMenu.ReadyStatusLoading;
					}
					else
					{
						status = ScriptLocalization.AdventureMenu.ReadyStatusReady;
					}
					
					AddOrUpdatePlayerInListHolder(playerKvp.Value, status);
				}
			}
		}
		
		private void OnCoreMatchAssetsLoaded(CoreMatchAssetsLoadedMessage msg)
		{
			_loadingText.SetActive(false);
			_leaveRoomButton.gameObject.SetActive(true);
			
			if (_services.NetworkService.QuantumClient.LocalPlayer.IsMasterClient)
			{
				_lockRoomButton.gameObject.SetActive(true);
			}
		}
		
		private void OnStartedFinalPreloadMessage(StartedFinalPreloadMessage msg)
		{
			foreach (var playerKvp in CurrentRoom.Players)
			{
				AddOrUpdatePlayerInListHolder(playerKvp.Value, ScriptLocalization.AdventureMenu.ReadyStatusLoading);
			}
		}
		
		private void OnAllMatchAssetsLoaded(AllMatchAssetsLoadedMessage msg)
		{
			string status = "";
			
			if (_services.NetworkService.QuantumClient.LocalPlayer.IsMasterClient)
			{
				status = ScriptLocalization.AdventureMenu.ReadyStatusHost;
			}
			else
			{
				status = ScriptLocalization.AdventureMenu.ReadyStatusReady;
			}
			
			AddOrUpdatePlayerInListHolder(_services.NetworkService.QuantumClient.LocalPlayer, status);
		}
		
		/// <inheritdoc />
		public void OnPlayerEnteredRoom(Player newPlayer)
		{
			var room = _services.NetworkService.QuantumClient.CurrentRoom;
			
			AddOrUpdatePlayerInListHolder(newPlayer, ScriptLocalization.AdventureMenu.ReadyStatusReady);
			
			UpdatePlayersWaitingImages(room.MaxPlayers, room.PlayerCount);
		}

		/// <inheritdoc />
		public void OnPlayerLeftRoom(Player otherPlayer)
		{
			var room = _services.NetworkService.QuantumClient.CurrentRoom;
			
			AddOrUpdatePlayerInListHolder(otherPlayer, ScriptLocalization.AdventureMenu.ReadyStatusReady);
			
			UpdatePlayersWaitingImages(room.MaxPlayers, room.PlayerCount);
		}

		/// <inheritdoc />
		public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
		{
			if (propertiesThatChanged.TryGetValue(GamePropertyKey.IsOpen, out var isOpen) && !(bool) isOpen)
			{
				ReadyToPlay();
			}
		}

		/// <inheritdoc />
		public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
		{
			if (changedProps.TryGetValue(GameConstants.PLAYER_PROPS_LOADED, out var loadedMatch) && (bool) loadedMatch)
			{
				var status = ScriptLocalization.AdventureMenu.ReadyStatusReady;

				if (targetPlayer.IsMasterClient)
				{
					status = ScriptLocalization.AdventureMenu.ReadyStatusHost;
				}

				AddOrUpdatePlayerInListHolder(targetPlayer, status);
			}
		}

		/// <inheritdoc />
		public void OnMasterClientSwitched(Player newMasterClient)
		{
			if (!_services.NetworkService.QuantumClient.CurrentRoom.IsVisible && newMasterClient.IsLocal)
			{
				_lockRoomButton.gameObject.SetActive(true);
			}
		}
		
		private void AddOrUpdatePlayerInListHolder(Player player, string status)
		{
			if (!CurrentRoom.IsVisible)
			{
				_playerListHolder.AddOrUpdatePlayer(player.NickName, status, player.IsLocal, player.IsMasterClient);
			}
		}
		
		private void UpdatePlayersWaitingImages(int maxPlayers, int playerAmount)
		{
			for (var i = 0; i < _playersWaitingImage.Length; i++)
			{
				_playersWaitingImage[i].gameObject.SetActive((i+1) <= playerAmount);
			}
			
			_playersFoundText.text = $"{playerAmount.ToString()}/{maxPlayers.ToString()}" ;
		}

		private void OnSceneChanged(Scene previous, Scene current)
		{
			// Ignore scene changes that are not levels
			if (current.buildIndex != -1)
			{
				return;
			}
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
		}

		private void OnLockRoomClicked()
		{
			ReadyToPlay();
			_services.MessageBrokerService.Publish(new RoomLockClickedMessage());
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

			if (CurrentRoom.IsVisible)
			{
				_getReadyToRumbleText.gameObject.SetActive(true);
				_playersFoundText.gameObject.SetActive(false);
				_findingPlayersText.gameObject.SetActive(false);
			}
		}
	}
}
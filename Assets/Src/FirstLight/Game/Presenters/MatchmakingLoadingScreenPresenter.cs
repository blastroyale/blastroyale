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
using Random = UnityEngine.Random;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Players Waiting Screen UI by:
	/// - Showing the loading status
	/// </summary>
	public class MatchmakingLoadingScreenPresenter : UiPresenterData<MatchmakingLoadingScreenPresenter.StateData>
	{
		public struct StateData
		{
			public IUiService UiService;
		}
		
		[SerializeField] private Transform _playerCharacterParent;
		[SerializeField] private Button _lockRoomButton;
		[SerializeField] private Button _leaveRoomButton;
		[SerializeField] private Image _mapImage;
		[SerializeField] private Image [] _playersWaitingImage;
		[SerializeField] private Animation _animation;
		[SerializeField] private TextMeshProUGUI _firstToXKillsText;
		[SerializeField] private TextMeshProUGUI _nextArenaText;
		[SerializeField] private TextMeshProUGUI _playersFoundText;
		[SerializeField] private TextMeshProUGUI _findingPlayersText;
		[SerializeField] private TextMeshProUGUI _getReadyToRumbleText;
		[SerializeField] private GameObject _roomNameRootObject;
		[SerializeField] private TextMeshProUGUI _roomNameText;
		[SerializeField] private GameObject _playerMatchmakingRootObject;
		[SerializeField] private PlayerListHolderView _playerListHolder;
		
		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;
		private float _rndWaitingTimeLowest;
		private float _rndWaitingTimeBiggest;
		private bool _allPlayersLoadedMatch = false;

		private Room CurrentRoom => _services.NetworkService.QuantumClient.CurrentRoom;

		public MapSelectionView MapSelectionView;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			
			foreach (var image in _playersWaitingImage)
			{
				image.gameObject.SetActive(false);
			}
			
			_lockRoomButton.onClick.AddListener(OnLockRoomClicked);
			_leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);
			
			_services.MessageBrokerService.Subscribe<PlayerJoinedRoomMessage>(OnPlayerJoinedRoom);
			_services.MessageBrokerService.Subscribe<PlayerLeftRoomMessage>(OnPlayerLeftRoom);
			_services.MessageBrokerService.Subscribe<AllPlayersLoadedMatchMessage>(OnAllPlayersLoadedMatchMessage);
			_services.MessageBrokerService.Subscribe<PlayerLoadedMatchMessage>(OnPlayerLoadedMatchMessage);
			
			SceneManager.activeSceneChanged += OnSceneChanged;
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			
			SceneManager.activeSceneChanged -= OnSceneChanged;
		}

		/// <inheritdoc />
		protected override void OnOpened()
		{
			var config = _gameDataProvider.AppDataProvider.SelectedMap.Value;
			_allPlayersLoadedMatch = false;
			
			_getReadyToRumbleText.gameObject.SetActive(false);
			_findingPlayersText.enabled = true;
			_playersFoundText.enabled = true;
			_playersFoundText.text = $"{0}/{config.PlayersLimit.ToString()}" ;
			_rndWaitingTimeLowest = 2f / config.PlayersLimit;
			_rndWaitingTimeBiggest = 8f / config.PlayersLimit;
			
			_lockRoomButton.gameObject.SetActive(false);
			_leaveRoomButton.gameObject.SetActive(false);
			_getReadyToRumbleText.gameObject.SetActive(false);
			MapSelectionView.SetupMapView();
			_playerListHolder.WipeAllSlots();
			
			if (CurrentRoom.IsVisible)
			{
				_playerListHolder.gameObject.SetActive(false);
				_playerMatchmakingRootObject.SetActive(true);
				
				_roomNameRootObject.SetActive(false);
				StartCoroutine(TimeUpdateCoroutine(config));
			}
			else
			{
				_playerListHolder.gameObject.SetActive(true);
				_playerMatchmakingRootObject.SetActive(false);
				
				_roomNameText.text = string.Format(ScriptLocalization.MainMenu.RoomCurrentName, CurrentRoom.Name);

				foreach (var playerKvp in CurrentRoom.Players)
				{
					AddOrUpdatePlayerInListHolder(playerKvp.Value, ScriptLocalization.AdventureMenu.ReadyStatusLoading);
				}
			}
		}

		private void AddOrUpdatePlayerInListHolder(Player player, string status)
		{
			if (!CurrentRoom.IsVisible)
			{
				_playerListHolder.AddOrUpdatePlayer(player.NickName, status, player.IsMasterClient);
			}
		}

		private void OnAllPlayersLoadedMatchMessage(AllPlayersLoadedMatchMessage msg)
		{
			_allPlayersLoadedMatch = true;
			
			// Only custom rooms can show room controls
			if (!_services.NetworkService.QuantumClient.CurrentRoom.IsVisible && 
			    _services.NetworkService.QuantumClient.LocalPlayer.IsMasterClient)
			{
				_lockRoomButton.gameObject.SetActive(true);
			}

			if (!_services.NetworkService.QuantumClient.CurrentRoom.IsVisible)
			{
				_leaveRoomButton.gameObject.SetActive(true);
			}
		}

		private void OnPlayerLoadedMatchMessage(PlayerLoadedMatchMessage msg)
		{
			string status = ScriptLocalization.AdventureMenu.ReadyStatusReady;
			
			if (msg.Player.IsMasterClient)
			{
				status = ScriptLocalization.AdventureMenu.ReadyStatusHost;
			}
			
			AddOrUpdatePlayerInListHolder(msg.Player, status);
		}

		private void OnPlayerJoinedRoom(PlayerJoinedRoomMessage msg)
		{
			_allPlayersLoadedMatch = false;
			_lockRoomButton.gameObject.SetActive(false);
			UpdatePlayersWaitingImages(CurrentRoom.PlayerCount);
			AddOrUpdatePlayerInListHolder(msg.Player, ScriptLocalization.AdventureMenu.ReadyStatusLoading);
		}
		
		private void OnPlayerLeftRoom(PlayerLeftRoomMessage msg)
		{
			UpdatePlayersWaitingImages(_services.NetworkService.QuantumClient.CurrentRoom.PlayerCount);

			if (!CurrentRoom.IsVisible)
			{
				_playerListHolder.RemovePlayer(msg.Player.NickName);
			}
			
			// Only custom rooms can show room controls
			if (!_services.NetworkService.QuantumClient.CurrentRoom.IsVisible && 
			    _services.NetworkService.QuantumClient.LocalPlayer.IsMasterClient && _allPlayersLoadedMatch)
			{
				_lockRoomButton.gameObject.SetActive(true);
			}
		}

		private void UpdatePlayersWaitingImages(int playerAmount)
		{
			var maxPlayers = _gameDataProvider.AppDataProvider.SelectedMap.Value.PlayersLimit;
			
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

		private IEnumerator TimeUpdateCoroutine(MapConfig config)
		{
			for (var i = 0; i < _playersWaitingImage.Length && i < config.PlayersLimit; i++)
			{
				UpdatePlayersWaitingImages(i + 1);
				yield return new WaitForSeconds(Random.Range(_rndWaitingTimeLowest, _rndWaitingTimeBiggest));
			}

			yield return new WaitForSeconds(0.5f);
			
			_getReadyToRumbleText.gameObject.SetActive(true);
			_findingPlayersText.enabled = false;
			_playersFoundText.enabled = false;
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
			_lockRoomButton.gameObject.SetActive(false);
			_leaveRoomButton.gameObject.SetActive(false);
			_getReadyToRumbleText.gameObject.SetActive(true);
			_playersFoundText.gameObject.SetActive(false);
			_findingPlayersText.gameObject.SetActive(false);
			_services.MessageBrokerService.Publish(new RoomLockClickedMessage());
		}

		private void OnLeaveRoomClicked()
		{
			_services.MessageBrokerService.Publish(new RoomLeaveClickedMessage());
		}
	}
}
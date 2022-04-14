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
	// TODO - Refactor for both matchmaking & custom game functionality, after NetworkState/matchmaking flow is refactored
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
		
		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;
		private float _rndWaitingTimeLowest;
		private float _rndWaitingTimeBiggest;

		private Room CurrentRoom => _services.NetworkService.QuantumClient.CurrentRoom;

		public MapSelectionView MapSelectionView;

		private void Start()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			
			foreach (var image in _playersWaitingImage)
			{
				image.gameObject.SetActive(false);
			}
			
			_lockRoomButton.onClick.AddListener(OnLockRoomClicked);

			// TODO - Subscribe to the event instead of calling this, when this presenter is available from main menu state
			// TODO - Currently the callbacks won't trigger as room is joined in main menu state, and presenter is spawned in match state
			Invoke(nameof(FakeOnJoinedRoom),1f);
			//_services.MessageBrokerService.Subscribe<JoinedRoomMessage>(OnJoinedRoom);
			_services.MessageBrokerService.Subscribe<PlayerJoinedRoomMessage>(OnPlayerJoinedRoom);
			_services.MessageBrokerService.Subscribe<PlayerLeftRoomMessage>(OnPlayerLeftRoom);
			
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
			var config = _gameDataProvider.AppDataProvider.CurrentMapConfig;

			// Only show room code if player is coming from custom game - join/create
			if (CurrentRoom.IsVisible)
			{
				_roomNameRootObject.SetActive(false);
			}
			else
			{
				_roomNameText.text = string.Format(ScriptLocalization.MainMenu.RoomCurrentName, CurrentRoom.Name);
			}

			_playersFoundText.text = $"{0}/{config.PlayersLimit.ToString()}" ;
			_rndWaitingTimeLowest = 2f / config.PlayersLimit;
			_rndWaitingTimeBiggest = 8f / config.PlayersLimit;
			
			_getReadyToRumbleText.gameObject.SetActive(false);
			transform.SetParent(null);
			SetLayerState(false, false);
			_animation.Rewind();
			_animation.Play();
			
			if (CurrentRoom.IsVisible)
			{
				StartCoroutine(TimeUpdateCoroutine(config));
			}
			else
			{
				UpdatePlayersWaitingImages(CurrentRoom.PlayerCount);
			}
		}

		// TODO - Remove in next pass, when this presenter is moved to main menu
		private void FakeOnJoinedRoom()
		{
			OnJoinedRoom(new JoinedRoomMessage());
		}

		protected override void OnClosed()
		{
			SetLayerState(true, false);
		}

		private void OnJoinedRoom(JoinedRoomMessage message)
		{
			Debug.LogError(CurrentRoom.IsVisible);
			if (CurrentRoom.IsVisible)
			{
				return;
			}
			
			var masterClientPlayer = _services.NetworkService.QuantumClient.CurrentRoom.GetPlayer(0, true);
			var localPlayer = _services.NetworkService.QuantumClient.LocalPlayer;
			var localPlayerIsMaster = localPlayer.UserId == masterClientPlayer.UserId;

			_lockRoomButton.gameObject.SetActive(localPlayerIsMaster);
			
			UpdatePlayersWaitingImages(CurrentRoom.PlayerCount);
		}

		private void OnPlayerJoinedRoom(PlayerJoinedRoomMessage message)
		{
			UpdatePlayersWaitingImages(CurrentRoom.PlayerCount);
		}
		
		private void OnPlayerLeftRoom(PlayerLeftRoomMessage message)
		{
			UpdatePlayersWaitingImages(_services.NetworkService.QuantumClient.CurrentRoom.PlayerCount);
		}

		private void UpdatePlayersWaitingImages(int playerAmount)
		{
			var maxPlayers = _gameDataProvider.AppDataProvider.CurrentMapConfig.PlayersLimit;
			
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
			
			// Little hack to avoid UIs to spam over this screen
			SetLayerState(false, true);
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
			_services.NetworkService.QuantumClient.CurrentRoom.IsOpen = false;
		}
	}
}
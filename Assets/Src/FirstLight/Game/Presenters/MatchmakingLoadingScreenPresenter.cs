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
		
		public MapSelectionView MapSelectionView;

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

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			
			foreach (var image in _playersWaitingImage)
			{
				image.gameObject.SetActive(false);
			}
			
			_lockRoomButton.onClick.AddListener(OnLockRoomClicked);
			
			
			_services.MessageBrokerService.Subscribe<MatchJoinedRoomMessage>(OnJoinedRoom);
			_services.MessageBrokerService.Subscribe<PlayerJoinedMatchMessage>(OnPlayerJoinedRoom);
			_services.MessageBrokerService.Subscribe<PlayerLeftMatchMessage>(OnPlayerLeftRoom);
			
			SceneManager.activeSceneChanged += OnSceneChanged;
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			
			SceneManager.activeSceneChanged -= OnSceneChanged;
		}

		/// <inheritdoc />
		protected override async void OnOpened()
		{
			var config = _gameDataProvider.AppDataProvider.CurrentMapConfig;
			var selectedRoomEntryType = _gameDataProvider.AppDataProvider.SelectedRoomEntryType.Value;
			var selectedRoomName = _gameDataProvider.AppDataProvider.SelectedRoomName.Value;

			// Only show room code if player is coming from custom game - join/create
			if (selectedRoomEntryType != RoomEntryID.Matchmaking && !string.IsNullOrEmpty(selectedRoomName))
			{
				_roomNameText.text = string.Format(ScriptLocalization.MainMenu.RoomCurrentName, selectedRoomName);
			}
			else
			{
				_roomNameRootObject.SetActive(false);
			}

			_playersFoundText.text = $"{0}/{config.PlayersLimit.ToString()}" ;
			_rndWaitingTimeLowest = 2f / config.PlayersLimit;
			_rndWaitingTimeBiggest = 8f / config.PlayersLimit;
			
			_getReadyToRumbleText.gameObject.SetActive(false);
			transform.SetParent(null);
			SetLayerState(false, false);
			_animation.Rewind();
			_animation.Play();
			
			_mapImage.enabled = false;
			_mapImage.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(config.Map, false);
			_mapImage.enabled = true;
			
			MapSelectionView.InitSelection(_gameDataProvider.AppDataProvider.SelectedGameMode.Value == GameMode.BattleRoyale);

			if (_gameDataProvider.AppDataProvider.SelectedRoomEntryType.Value == RoomEntryID.Matchmaking)
			{
				StartCoroutine(TimeUpdateCoroutine(config));
			}
		}

		protected override void OnClosed()
		{
			SetLayerState(true, false);
		}

		private void OnJoinedRoom(MatchJoinedRoomMessage message)
		{
			if (_gameDataProvider.AppDataProvider.SelectedRoomEntryType.Value == RoomEntryID.Matchmaking)
			{
				return;
			}
			
			var masterClientPlayer = _services.NetworkService.QuantumClient.CurrentRoom.GetPlayer(0, true);
			var localPlayer = _services.NetworkService.QuantumClient.LocalPlayer;
			var localPlayerIsMaster = localPlayer.UserId == masterClientPlayer.UserId;
			
			var canShowLockButton = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>().IsDevMode ||
			                         (_gameDataProvider.AppDataProvider.SelectedRoomEntryType.Value != RoomEntryID.Matchmaking && localPlayerIsMaster);
			
			Debug.LogError("JOINED ROOM, LOCAL IS MASTER? = " + localPlayerIsMaster);
			_lockRoomButton.gameObject.SetActive(canShowLockButton);
			
			UpdatePlayersWaitingImages();
		}

		private void OnPlayerJoinedRoom(PlayerJoinedMatchMessage message)
		{
			UpdatePlayersWaitingImages();
		}
		
		private void OnPlayerLeftRoom(PlayerLeftMatchMessage message)
		{
			UpdatePlayersWaitingImages();
		}

		private void UpdatePlayersWaitingImages()
		{
			if (_gameDataProvider.AppDataProvider.SelectedRoomEntryType.Value == RoomEntryID.Matchmaking)
			{
				return;
			}
			
			var playersInRoom = (int)_services.NetworkService.QuantumClient.CurrentRoom.PlayerCount;
			var maxPlayers = (int) _services.NetworkService.QuantumClient.CurrentRoom.MaxPlayers;
			
			for (var i = 0; i < _playersWaitingImage.Length; i++)
			{
				_playersWaitingImage[i].gameObject.SetActive((i+1) <= playersInRoom);
			}
			
			_playersFoundText.text = $"{playersInRoom.ToString()}/{maxPlayers.ToString()}" ;
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
				_playersWaitingImage[i].gameObject.SetActive(true);
				_playersFoundText.text = $"{(i + 1).ToString()}/{config.PlayersLimit.ToString()}" ;
				
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
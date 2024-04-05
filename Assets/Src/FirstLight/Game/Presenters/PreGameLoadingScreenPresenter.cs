using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Services.RoomService;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.UIService;
using I2.Loc;
using Photon.Realtime;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter handles displaying matchmaking UI.
	/// In current iteration, this is just a standalone screen for matchmaking only.
	/// In future iteration with new custom lobby screen, this screen will become a loading screen for both
	/// matchmaking and custom lobby, just before players are dropped into the match.
	/// </summary>
	public class PreGameLoadingScreenPresenter : UIPresenterData<PreGameLoadingScreenPresenter.StateData>
	{
		private const int TIMER_PADDING_MS = 2000;
		private const int DISABLE_LEAVE_AFTER = 3;

		public class StateData
		{
			public Action LeaveRoomClicked;
		}

		private VisualElement _mapHolder;
		private VisualElement _mapTitleBg;
		private VisualElement _mapMarker;
		private VisualElement _mapImage;
		private VisualElement _squadContainer;
		private VisualElement _partyMarkers;
		private Label _squadLabel;
		private ListView _squadMembersList;
		private Label _mapMarkerTitle;
		private Label _loadStatusLabel;
		private Label _locationLabel;
		private ScreenHeaderElement _header;
		private Label _modeDescTopLabel;
		private Label _modeDescBotLabel;
		private Label _debugPlayerCountLabel;
		private Label _debugMasterClient;
		private IGameServices _services;
		private IGameDataProvider _dataProvider;
		private GameRoom CurrentRoom => _services.RoomService.CurrentRoom;
		private Coroutine _gameStartTimerCoroutine;
		private Tweener _planeFlyTween;
		private bool _dropSelectionAllowed;
		private bool _matchStarting;

		private List<Player> _squadMembers = new ();

		private MapAreaConfig _mapAreaConfig;
		private Vector2 _markerLocalPosition;

		private bool RejoiningRoom => _services.NetworkService.JoinSource.HasResync();

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_dataProvider = MainInstaller.ResolveData();
			_services.NetworkService.QuantumClient.AddCallbackTarget(this);
		}

		private void OnDestroy()
		{
			_services?.NetworkService?.QuantumClient?.RemoveCallbackTarget(this);
		}

		protected override void QueryElements()
		{
			_mapHolder = Root.Q("Map").Required();
			_mapImage = Root.Q("MapImage").Required();
			_mapMarker = Root.Q("MapMarker").Required();
			_mapMarkerTitle = Root.Q<Label>("MapMarkerTitle").Required();
			_mapTitleBg = Root.Q("MapTitleBg").Required();
			_loadStatusLabel = Root.Q<Label>("LoadStatusLabel").Required();
			_locationLabel = Root.Q<Label>("LocationLabel").Required();
			_header = Root.Q<ScreenHeaderElement>("Header").Required();
			_modeDescTopLabel = Root.Q<Label>("ModeDescTop").Required();
			_modeDescBotLabel = Root.Q<Label>("ModeDescBot").Required();
			_debugPlayerCountLabel = Root.Q<Label>("DebugPlayerCount").Required();
			_debugMasterClient = Root.Q<Label>("DebugMasterClient").Required();
			_squadContainer = Root.Q("SquadContainer").Required();
			_squadMembersList = Root.Q<ListView>("SquadList").Required();
			_partyMarkers = Root.Q("PartyMarkers").Required();

			_squadMembersList.DisableScrollbars();
			_squadMembersList.makeItem = CreateSquadListEntry;
			_squadMembersList.bindItem = BindSquadListEntry;

			_header.backClicked += OnCloseClicked;
		}


		protected override UniTask OnScreenOpen(bool reload)
		{
			_mapHolder.RegisterCallback<GeometryChangedEvent>(InitMap);

			_services.RoomService.OnPlayersChange += OnPlayersChanged;
			_services.RoomService.OnMasterChanged += UpdateMasterClient;
			_services.RoomService.OnPlayerPropertiesUpdated += OnPlayerPropertiesUpdate;
			_services.MessageBrokerService.Subscribe<WaitingMandatoryMatchAssetsMessage>(OnWaitingMandatoryMatchAssets);

			RefreshPartyList();
			UpdateMasterClient();

			return base.OnScreenOpen(reload);
		}

		protected override UniTask OnScreenClose()
		{
			if (_gameStartTimerCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_gameStartTimerCoroutine);
			}

			_services.RoomService.OnPlayersChange -= OnPlayersChanged;
			_services.RoomService.OnMasterChanged -= UpdateMasterClient;
			_services.RoomService.OnPlayerPropertiesUpdated -= OnPlayerPropertiesUpdate;

			_services.MessageBrokerService.Unsubscribe<WaitingMandatoryMatchAssetsMessage>(OnWaitingMandatoryMatchAssets);

			return base.OnScreenClose();
		}

		private void RefreshPartyList()
		{
			var isSquadGame = CurrentRoom.IsTeamGame;

			if (isSquadGame)
			{
				var teamId = CurrentRoom.GetTeamForPlayer(CurrentRoom.LocalPlayer);

				_squadContainer.SetDisplay(true);
				_squadMembers = CurrentRoom.Players.Values
					.Where(p => CurrentRoom.GetTeamForPlayer(p) == teamId)
					.ToList();

				_squadMembersList.itemsSource = _squadMembers;
				_squadMembersList.RefreshItems();

				RefreshPartyMarkers();
			}
			else
			{
				_squadContainer.SetDisplay(false);
				_squadMembers.Clear();
				_partyMarkers.Clear();
			}
		}

		private void RefreshPartyMarkers()
		{
			_partyMarkers.Clear();

			foreach (var squadMember in _squadMembers)
			{
				if (squadMember.IsLocal) continue;

				var memberDropPosition = CurrentRoom.GetPlayerProperties(squadMember).DropPosition.Value;
				var marker = new VisualElement {name = "marker"};
				marker.AddToClassList("map-marker-party");
				var props = CurrentRoom.GetPlayerProperties(squadMember);
				var nameColor = _services.TeamService.GetTeamMemberColor(props);
				marker.style.backgroundColor = nameColor ?? Color.white;
				var mapWidth = _mapImage.contentRect.width;
				var markerPos = new Vector2(memberDropPosition.x * mapWidth, -memberDropPosition.y * mapWidth);

				_partyMarkers.Add(marker);
				marker.transform.position = markerPos;
			}
		}

		private void BindSquadListEntry(VisualElement element, int index)
		{
			if (index < 0 || index >= _squadMembers.Count) return;

			var props = CurrentRoom.GetPlayerProperties(_squadMembers[index]);
			var nameColor = _services.TeamService.GetTeamMemberColor(props);


			((Label) element).text = _squadMembers[index].NickName;
			((Label) element).style.color = nameColor ?? Color.white;
		}

		private VisualElement CreateSquadListEntry()
		{
			var label = new Label();
			label.AddToClassList("squad-member");
			return label;
		}

		/// <summary>
		///  Select the drop zone based on percentages of the map
		/// Range of the input is 0 to 1
		/// </summary>
		public void SelectDropZone(float x, float y)
		{
			var mapWidth = _mapImage.contentRect.width;
			var mapHeight = _mapImage.contentRect.height;

			if (TrySetMarkerPosition(new Vector2(x * mapWidth, y * mapHeight)))
			{
				ConfirmMarkerPosition(_markerLocalPosition, false);
			}
			else
			{
				FLog.Error($"Drop zone position could not be set on x: {x}, y: {y}.");
			}
		}

		private async UniTask LoadMapAsset(QuantumMapConfig mapConfig)
		{
			if (_services.AssetResolverService.TryGetAssetReference<GameId, Sprite>(mapConfig.Map, out _))
			{
				var sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(mapConfig.Map, false);
				_mapImage.style.backgroundImage = new StyleBackground(sprite);
				_mapAreaConfig = _services.ConfigsProvider.GetConfig<MapAreaConfigs>().GetMapAreaConfig(mapConfig.Map);
			}
			else
			{
				FLog.Warn("Map sprite for map " + mapConfig.Map + " not found");
			}
		}

		// TODO: Should not be here, should only have a listener to timer events
		private void StartLabelTimerCoroutine()
		{
			_gameStartTimerCoroutine =
				_services.CoroutineService.StartCoroutine(GameStartTimerCoroutine());
		}

		private async void InitMap(GeometryChangedEvent evt)
		{
			if (CurrentRoom == null) return;

			// Have to unregister callback immediately, as when the plane animates within the map holder,
			// the geometry changed event fires constantly.
			_mapHolder.UnregisterCallback<GeometryChangedEvent>(InitMap);

			var matchType = CurrentRoom.Properties.MatchType.Value;
			var gameModeConfig = CurrentRoom.GameModeConfig;
			var mapConfig = CurrentRoom.MapConfig;
			var modeDesc = GetGameModeDescriptions(gameModeConfig.CompletionStrategy);

			_locationLabel.text = mapConfig.Map.GetLocalization();
			_header.SetTitle(LocalizationUtils.GetTranslationForGameModeAndTeamSize(gameModeConfig.Id, CurrentRoom.Properties.TeamSize.Value),
				matchType.GetLocalization().ToUpper());

			_modeDescTopLabel.text = modeDesc[0];
			_modeDescBotLabel.text = modeDesc[1];
			_header.SetButtonsVisibility(!_services.TutorialService.IsTutorialRunning);

			UpdatePlayerCount();
			UpdateMasterClient();
			StartLabelTimerCoroutine();

			if (!gameModeConfig.SkydiveSpawn || RejoiningRoom)
			{
				_mapMarker.SetDisplay(false);
				_mapTitleBg.SetDisplay(false);
				LoadMapAsset(mapConfig).Forget();

				return;
			}

			_dropSelectionAllowed = !RejoiningRoom;

			if (RejoiningRoom)
			{
				OnWaitingMandatoryMatchAssets(new WaitingMandatoryMatchAssetsMessage());
			}

			await LoadMapAsset(mapConfig);
			InitSkydiveSpawnMapData();
			RefreshPartyList();
		}

		private void InitSkydiveSpawnMapData()
		{
			var posX = Random.Range(0.3f, 0.7f);
			var posY = Random.Range(0.3f, 0.7f);
			SelectDropZone(posX, posY);

			// This weird register is just a slight performance improvement that prevents a closure allocation
			_mapImage.RegisterCallback<PointerDownEvent, PreGameLoadingScreenPresenter>((ev, args) => args.OnMapPointerDown(ev), this);
			_mapImage.RegisterCallback<PointerMoveEvent, PreGameLoadingScreenPresenter>((ev, arg) => arg.OnMapPointerMove(ev), this);
			_mapImage.RegisterCallback<PointerUpEvent, PreGameLoadingScreenPresenter>((ev, arg) => arg.OnMapPointerUp(ev), this);
		}

		private void OnMapPointerDown(PointerDownEvent e)
		{
			_mapImage.CapturePointer(e.pointerId);

			TrySetMarkerPosition(e.localPosition);
		}

		private void OnMapPointerMove(PointerMoveEvent e)
		{
			if (!_mapImage.HasPointerCapture(e.pointerId)) return;

			TrySetMarkerPosition(e.localPosition);
		}

		private void OnMapPointerUp(PointerUpEvent e)
		{
			_mapImage.ReleasePointer(e.pointerId);

			ConfirmMarkerPosition(_markerLocalPosition, true);
		}

		private bool TrySetMarkerPosition(Vector2 localPos)
		{
			if (!_services.RoomService.InRoom) return false;
			if (!_dropSelectionAllowed) return false;

			var mapWidth = _mapImage.contentRect.width;
			var mapHeight = _mapImage.contentRect.height;
			var mapWidthHalf = mapWidth / 2;
			var mapHeightHalf = mapHeight / 2;
			var mapAreaPosition = new Vector2(localPos.x / mapWidth, 1f - localPos.y / mapHeight);

			if (mapAreaPosition.x >= 1f || mapAreaPosition.x < 0f || mapAreaPosition.y >= 1f || mapAreaPosition.y < 0f)
			{
				return false;
			}

			if (_mapAreaConfig != null)
			{
				var areaName = _mapAreaConfig.GetAreaName(mapAreaPosition);

				// No area name means invalid area
				if (string.IsNullOrEmpty(areaName))
				{
					return false;
				}

				_mapMarkerTitle.SetDisplay(true);
				_mapMarkerTitle.text = areaName;
			}
			else
			{
				_mapMarkerTitle.SetDisplay(false);
			}

			_markerLocalPosition = new Vector3(localPos.x - mapWidthHalf, localPos.y - mapHeightHalf, 0);
			_mapMarker.transform.position = _markerLocalPosition;

			return true;
		}

		/// <summary>
		/// Accepts the position in the canvas (-mapWidth/2, -mapHeight/2) to (mapWidth/2, mapHeight/2)
		/// </summary>
		/// <param name="localPos"></param>
		/// <param name="sendEvent"></param>
		private void ConfirmMarkerPosition(Vector2 localPos, bool sendEvent)
		{
			if (!_services.RoomService.InRoom) return;
			if (!_dropSelectionAllowed) return;

			if (sendEvent)
			{
				_services.MessageBrokerService.Publish(new MapDropPointSelectedMessage());
			}

			var mapWidth = _mapImage.contentRect.width;
			var mapHeight = _mapImage.contentRect.height;
			var mapWidthHalf = mapWidth / 2;
			var mapHeightHalf = mapHeight / 2;

			// Get normalized position for spawn positions in quantum, -0.5 to 0.5 range
			var quantumSelectPos = new Vector2((localPos.x - mapWidthHalf) / mapWidth + 0.5f, -(localPos.y - mapHeightHalf) / mapHeight - 0.5f);
			_services.RoomService.CurrentRoom.LocalPlayerProperties.DropPosition.Value = quantumSelectPos;
		}

		private void OnPlayersChanged(Player p, PlayerChangeReason r)
		{
			UpdatePlayerCount();
			RefreshPartyList();
		}

		private void OnWaitingMandatoryMatchAssets(WaitingMandatoryMatchAssetsMessage obj)
		{
			if (_gameStartTimerCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_gameStartTimerCoroutine);
			}

			_header.SetButtonsVisibility(false);

			_loadStatusLabel.text = RejoiningRoom
				? "Reconnecting to Game!"
				: // todo translation
				ScriptLocalization.UITMatchmaking.loading_status_starting;

			_dropSelectionAllowed = false;
		}

		private void UpdatePlayerCount()
		{
			_debugPlayerCountLabel.text = CanSeeDebugInfo()
				? string.Format(ScriptLocalization.UITMatchmaking.current_player_amount,
					CurrentRoom.GetRealPlayerAmount(), CurrentRoom.GetRealPlayerCapacity())
				: "";
		}

		private void UpdateMasterClient()
		{
			if (!CanSeeDebugInfo())
			{
				_debugMasterClient.SetDisplay(false);
				return;
			}

			_debugMasterClient.SetDisplay(_services.NetworkService.LocalPlayer.IsMasterClient);
		}

		private bool CanSeeDebugInfo()
		{
			return Debug.isDebugBuild || _dataProvider.PlayerDataProvider.Flags.HasFlag(PlayerFlags.FLGOfficial);
		}

		/// <summary>
		///  Used only for updating the labels!!!!!!!!
		/// </summary>
		/// <returns></returns>
		private IEnumerator GameStartTimerCoroutine()
		{
			var wait = new WaitForSeconds(0.5f);
			UpdateTimer();
			while (true)
			{
				yield return wait;
				if (CurrentRoom == null || CurrentRoom.GameStarted) break;
				UpdateTimer();
			}
		}

		private void UpdateTimer()
		{
			if (CurrentRoom.ShouldTimerRun())
			{
				if (CurrentRoom.GameStarted)
				{
					_loadStatusLabel.text = ScriptLocalization.UITMatchmaking.loading_status_starting;
					return;
				}

				var timeLeft = CurrentRoom.TimeLeftToGameStart().Add(TimeSpan.FromMilliseconds(-TIMER_PADDING_MS));
				if (timeLeft.Seconds <= DISABLE_LEAVE_AFTER)
				{
					_header.SetButtonsVisibility(false);
					_services.GenericDialogService.CloseDialog();
				}

				if (timeLeft.Milliseconds < 0)
				{
					// If the user war dragging on map after drop selection is disabled we need to confirm his last position
					ConfirmMarkerPosition(_markerLocalPosition, true);

					_dropSelectionAllowed = false;
					_loadStatusLabel.text = ScriptLocalization.UITMatchmaking.loading_status_waiting;
					return;
				}

				_loadStatusLabel.text = string.Format(ScriptLocalization.UITMatchmaking.loading_status_waiting_timer,
					timeLeft.TotalSeconds.ToString("F0"));
			}
			else
			{
				_loadStatusLabel.text = ScriptLocalization.UITMatchmaking.loading_status_waiting;
			}
		}


		private string[] GetGameModeDescriptions(GameCompletionStrategy strategy)
		{
			var descriptions = new string[2];
			descriptions[0] = strategy.GetLocalization();
			descriptions[1] = ScriptLocalization.UITMatchmaking.wins_the_match;

			return descriptions;
		}


		public void OnPlayerPropertiesUpdate()
		{
			RefreshPartyList();
		}

		private void OnCloseClicked()
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
	}
}

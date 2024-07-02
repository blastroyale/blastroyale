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
using Sirenix.OdinInspector;
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

		private VisualElement[] _squadContainers;
		private Label[] _nameLabels;
		
		private VisualElement _mapHolder;
		private VisualElement _mapTitleBg;
		private VisualElement _mapMarker;
		private VisualElement _mapMarkerIcon;
		private VisualElement _mapImage;
		private VisualElement _squadContainer;
		private VisualElement _partyMarkers;
		private Label _squadLabel;
		//private ListView _squadMembersList;
		private Label _mapMarkerTitle;
		private Label _loadStatusLabel;
		private Label _locationLabel;
		private ScreenHeaderElement _header;
		//private Label _modeDescTopLabel;
		//private Label _modeDescBotLabel;
		private Label _debugPlayerCountLabel;
		private Label _debugMasterClient;
		private IGameServices _services;
		private IGameDataProvider _dataProvider;
		private GameRoom CurrentRoom => _services.RoomService.CurrentRoom;
		private Coroutine _gameStartTimerCoroutine;
		private Tweener _planeFlyTween;
		private bool _dropSelectionAllowed;
		private bool _matchStarting;
		
		private PlayerMemberElement[] _playerMemberElements;

		private List<Player> _squadMembers = new ();

		private MapAreaConfig _mapAreaConfig;
		private Vector2 _markerLocalPosition;
		
		private const int MaxSquadPlayers = 4; 

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
			_squadContainers = new VisualElement[MaxSquadPlayers];
			_playerMemberElements = new PlayerMemberElement[MaxSquadPlayers];
			
			_nameLabels = new Label[MaxSquadPlayers];
			
			_mapHolder = Root.Q("Map").Required();
			_mapImage = Root.Q("MapImage").Required();
			_mapMarker = Root.Q("MapMarker").Required();
			_mapMarkerIcon = Root.Q("MapMarkerIcon").Required();
			_mapMarkerTitle = Root.Q<Label>("MapMarkerTitle").Required();
			_mapTitleBg = Root.Q("MapTitleBg").Required();
			_loadStatusLabel = Root.Q<Label>("LoadStatusLabel").Required();
			_locationLabel = Root.Q<Label>("LocationLabel").Required();
			_header = Root.Q<ScreenHeaderElement>("Header").Required();
			//_modeDescTopLabel = Root.Q<Label>("ModeDescTop").Required();
			//_modeDescBotLabel = Root.Q<Label>("ModeDescBot").Required();
			_debugPlayerCountLabel = Root.Q<Label>("DebugPlayerCount").Required();
			_debugMasterClient = Root.Q<Label>("DebugMasterClient").Required();
			_squadContainer = Root.Q("SquadContainer").Required();
			//_squadMembersList = Root.Q<ListView>("SquadList").Required();
			_partyMarkers = Root.Q("PartyMarkers").Required();
			_header.SetSubtitle("");

			//_squadMembersList.DisableScrollbars();
			//_squadMembersList.makeItem = CreateSquadListEntry;
			//_squadMembersList.bindItem = BindSquadListEntry;

			_header.backClicked += OnCloseClicked;

			for (var i = 0; i < MaxSquadPlayers; i++)
			{
				_squadContainers[i] = Root.Q<VisualElement>($"Row{i}").Required();
				_squadContainers[i].visible = false;
				
				_playerMemberElements[i] = Root.Q<PlayerMemberElement>($"PlayerMemberElement{i}").Required();
				_nameLabels[i] = Root.Q<Label>($"Name{i}").Required();
			}
			
			Debug.Log("");
		}

		[Button]
		private void Button1()
		{
			Debug.Log($"Map holder rect: " +
				$"{_mapHolder.worldBound.width} {_mapHolder.worldBound.height}");
			
			Debug.Log($"Root rect: " +
				$"{Root.worldBound.width} {Root.worldBound.height}");
		}


		protected override UniTask OnScreenOpen(bool reload)
		{
			_mapHolder.RegisterCallback<GeometryChangedEvent>(InitMap);

			_services.RoomService.OnPlayersChange += OnPlayersChanged;
			_services.RoomService.OnMasterChanged += UpdateMasterClient;
			_services.RoomService.OnPlayerPropertiesUpdated += OnPlayerPropertiesUpdate;

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


			return base.OnScreenClose();
		}
		
		private void RefreshPartyList()
		{
			var isSquadGame = CurrentRoom.IsTeamGame;

			if (isSquadGame)
			{
				var teamId = _services.TeamService.GetTeamForPlayer(CurrentRoom.LocalPlayer);

				_squadContainer.SetDisplay(true);
				_squadMembers = CurrentRoom.Players.Values
					.Where(p => _services.TeamService.GetTeamForPlayer(p) == teamId)
					.ToList();

				for (var i = 0; i < MaxSquadPlayers; i++)
				{
					_squadContainers[i].visible = false;
				}

				for (var i = 0; i < _squadMembers.Count; i++)
				{
					_squadContainers[i].visible = true;
					
					var loadout = CurrentRoom.GetPlayerProperties(_squadMembers[i]).Loadout;

					_services.CollectionService.LoadCollectionItemSprite(
						_services.CollectionService.GetCosmeticForGroup(loadout.Value.ToArray(), 
							GameIdGroup.PlayerSkin)).ContinueWith(_playerMemberElements[i].SetPfpImage);

					var nameColor = _services.TeamService.GetTeamMemberColor(_squadMembers[i]);
					_playerMemberElements[i].SetTeamColor(nameColor ?? Color.white);
					
					_nameLabels[i].text = _squadMembers[i].NickName;
				}
				
				//_squadMembersList.itemsSource = _squadMembers;
				//_squadMembersList.RefreshItems();

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

				//var playerMarker = new PlayerMemberElement {name = "player-marker"};
				
				var marker = new VisualElement {name = "marker"};
				marker.AddToClassList("map-marker-party");
				var nameColor = _services.TeamService.GetTeamMemberColor(squadMember);
				//playerMarker.SetTeamColor(nameColor ?? Color.white);
				marker.style.backgroundColor = nameColor ?? Color.white;
				var mapWidth = _mapImage.contentRect.width;
				var markerPos = new Vector2(memberDropPosition.x * mapWidth, -memberDropPosition.y * mapWidth);

				_partyMarkers.Add(marker);
				marker.transform.position = markerPos;
				//playerMarker.transform.position = markerPos;
			}
		}

		private void BindSquadListEntry(VisualElement element, int index)
		{
			if (index < 0 || index >= _squadMembers.Count) return;

			//var nameColor = _services.TeamService.GetTeamMemberColor(_squadMembers[index]);
			
			((Label) element).text = _squadMembers[index].NickName;
			((Label) element).style.color =  Color.white;
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
				
				Debug.Log("Screen Width, Height: " + Screen.width + " " + Screen.height);
				
				Debug.Log($"PreGameLoadingScreenPresenter->LoadMapAsset map image wh:" +
					$"{_mapImage.worldBound.width} {_mapImage.worldBound.height}");
				_mapAreaConfig = _services.ConfigsProvider.GetConfig<MapAreaConfigs>().GetMapAreaConfig(mapConfig.Map);

				Debug.Log($"PreGameLoadingScreenPresenter->LoadMapAsset map holder style wh:" +
					$"{_mapHolder.style.width} {_mapHolder.style.height}");

				if ((Screen.width / 2) >= Screen.height)
				{
					_mapHolder.style.width = _mapImage.worldBound.height;
				}
				else if (Screen.height > Screen.width)
				{
					_mapHolder.style.height = _mapImage.worldBound.width;
				}
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

			var simulationConfig = CurrentRoom.Properties.SimulationMatchConfig.Value;
			//var matchType = simulationConfig.MatchType;
			var gameModeConfig = CurrentRoom.GameModeConfig;
			var mapConfig = CurrentRoom.MapConfig;
			//var modeDesc = GetGameModeDescriptions(gameModeConfig.CompletionStrategy);

			_locationLabel.text = mapConfig.Map.GetLocalization();
			_header.SetTitle(LocalizationUtils.GetTranslationForGameModeAndTeamSize(gameModeConfig.Id, simulationConfig.TeamSize));

			//_modeDescTopLabel.text = modeDesc[0];
			//_modeDescBotLabel.text = modeDesc[1];
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
				OnWaitingMandatoryMatchAssets();
			}

			await LoadMapAsset(mapConfig);
			InitSkydiveSpawnMapData();
			RefreshPartyList();
		}

		private void InitSkydiveSpawnMapData()
		{
			var posX = Random.Range(0.3f, 0.7f);
			var posY = Random.Range(0.3f, 0.7f);
			if (FeatureFlags.GetLocalConfiguration().UseBotBehaviour)
			{
				posX = 0.5f;
				posY = 0.5f;
			}
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
			
			Debug.Log($"TrySetMarkerPosition->locaPos {localPos}");

			var mapWidth = _mapImage.contentRect.width;
			var mapHeight = _mapImage.contentRect.height;
			var mapWidthHalf = mapWidth / 2;
			var mapHeightHalf = mapHeight / 2;
			var mapAreaPosition = new Vector2(localPos.x / mapWidth, 1f - localPos.y / mapHeight);
			
			
			Debug.Log($"TrySetMarkerPosition->mapAreaPosition {mapAreaPosition}");

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

			UpdateMarkerPosition().Forget();
			
			return true;
		}

		private async UniTaskVoid UpdateMarkerPosition()
		{
			 await UniTask.DelayFrame(1);
			 _markerLocalPosition.x -= (_mapMarker.worldBound.width / 2) - (_mapMarkerIcon.worldBound.width / 2);
			 _mapMarker.transform.position = _markerLocalPosition;
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

		private void OnWaitingMandatoryMatchAssets()
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

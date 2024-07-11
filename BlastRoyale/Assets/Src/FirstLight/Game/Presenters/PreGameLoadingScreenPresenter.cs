using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

		private VisualElement[] _squadContainers;
		private Label[] _nameLabels;
		
		private VisualElement _mapHolder;
		private VisualElement _mapTitleBg;
		private VisualElement _mapMarker;
		private VisualElement _mapImage;
		private VisualElement _squadContainer;
		private VisualElement[] _partyMarkers;
		private Label _squadLabel;
		private Label _mapMarkerTitle;
		private Label _loadStatusLabel;
		private Label _locationLabel;
		private ScreenHeaderElement _header;
		private Label _debugPlayerCountLabel;
		private Label _debugMasterClient;
		private IGameServices _services;
		private IGameDataProvider _dataProvider;
		private GameRoom CurrentRoom => _services.RoomService.CurrentRoom;
		private Coroutine _gameStartTimerCoroutine;
		private Tweener _planeFlyTween;
		private bool _dropSelectionAllowed;
		private bool _matchStarting;

		private Dictionary<Player, VisualElement> _playerSquadEntries = new ();
		
		private PlayerMemberElement[] _playerMemberElements;

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
			var size = GameConstants.Data.MAX_SQUAD_MEMBERS + 1;
			_squadContainers = new VisualElement[size];
			_playerMemberElements = new PlayerMemberElement[size];
			_partyMarkers = new VisualElement[size-1];
			
			_nameLabels = new Label[size];
			
			_mapHolder = Root.Q("Map").Required();
			_mapImage = Root.Q("MapImage").Required();
			_mapMarker = Root.Q("MapMarker").Required();
			_mapMarkerTitle = Root.Q<Label>("MapMarkerTitle").Required();
			_mapTitleBg = Root.Q("MapTitleBg").Required();
			_loadStatusLabel = Root.Q<Label>("LoadStatusLabel").Required();
			_locationLabel = Root.Q<Label>("LocationLabel").Required();
			_header = Root.Q<ScreenHeaderElement>("Header").Required();
			_debugPlayerCountLabel = Root.Q<Label>("DebugPlayerCount").Required();
			_debugMasterClient = Root.Q<Label>("DebugMasterClient").Required();
			_squadContainer = Root.Q("SquadContainer").Required();
			_header.SetSubtitle("");
			
			_header.backClicked += OnCloseClicked;

			for (var i = 0; i < size-1; i++)
			{
				_partyMarkers[i] = Root.Q<VisualElement>($"MarkerPoint{i}").Required();
				_partyMarkers[i].visible = false;
			}

			for (var i = 0; i < size; i++)
			{
				_squadContainers[i] = Root.Q<VisualElement>($"Row{i}").Required();
				_squadContainers[i].SetDisplay(false);
				_squadContainers[i].visible = false;
				_playerMemberElements[i] = Root.Q<PlayerMemberElement>($"PlayerMemberElement{i}").Required();
				_nameLabels[i] = Root.Q<Label>($"Name{i}").Required();
			}
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
				_playerSquadEntries.Clear();

				SyncSquadMembersData();

				var size = GameConstants.Data.MAX_SQUAD_MEMBERS + 1;
				
				for (var i = 0; i < size; i++)
				{
					_squadContainers[i].SetDisplay(false);
					_squadContainers[i].visible = false;
				}

				for (var i = 0; i < _squadMembers.Count; i++)
				{
					SetPlayerSquadEntryVisualData(_squadMembers[i], i);
					
					_playerSquadEntries.Add(_squadMembers[i], _squadContainers[i]);
				}
				
				RefreshPartyMarkers();
			}
			else
			{
				_squadContainer.SetDisplay(false);
				_squadMembers.Clear();
			}
		}

		private void RefreshPartyMarkers()
		{
			var isSquadGame = CurrentRoom.IsTeamGame;

			if (!isSquadGame)
			{
				return;
			}
			
			foreach (var partyMarker in _partyMarkers)
			{
				partyMarker.visible = false;
			}

			var players = _squadMembers.Where(p => !p.IsLocal).ToList();
			
			for(var i=0; i<players.Count(); i++)
			{
				var squadMember = players[i];
				
				_partyMarkers[i].visible = true;
				
				var memberDropPosition = CurrentRoom.GetPlayerProperties(squadMember).DropPosition.Value;
				
				var nameColor = _services.TeamService.GetTeamMemberColor(squadMember);
				_partyMarkers[i].style.backgroundColor = nameColor ?? Color.white;
				var mapWidth = _mapImage.contentRect.width;
				var markerPos = new Vector2(memberDropPosition.x * mapWidth, -memberDropPosition.y * mapWidth);
				
				_partyMarkers[i].transform.position = markerPos;
			}
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
				var halfWidth = Screen.width / 2;
				
				if (halfWidth >= Screen.height)
				{
					_mapHolder.style.width = _mapImage.worldBound.height;
				}
				else
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
			var gameModeConfig = CurrentRoom.GameModeConfig;
			var mapConfig = CurrentRoom.MapConfig;
			
			_mapAreaConfig = _services.ConfigsProvider.GetConfig<MapAreaConfigs>().GetMapAreaConfig(mapConfig.Map);
			_locationLabel.text = mapConfig.Map.GetLocalization();
			_header.SetTitle(LocalizationUtils.GetTranslationForGameModeAndTeamSize(gameModeConfig.Id, simulationConfig.TeamSize));

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

		private void OnPlayersChanged(Player player, PlayerChangeReason reason)
		{
			UpdatePlayerCount();
			
			if (!CurrentRoom.IsTeamGame)
			{
				return;
			}
			
			SyncSquadMembersData();
			
			if (reason == PlayerChangeReason.Leave)
			{
				_playerSquadEntries[player].SetDisplay(false);
				_playerSquadEntries[player].visible = false;
				_playerSquadEntries.Remove(player);
			}
			else
			{
				for (var i = 0; i < _squadContainers.Length; i++)
				{
					if (_squadContainers[i].style.display == DisplayStyle.None)
					{
						SetPlayerSquadEntryVisualData(player, i);
						
						_playerSquadEntries.Add(player, _squadContainers[i]);
						
						break;
					}
				}
			}
			
			RefreshPartyMarkers();
		}

		private void SetPlayerSquadEntryVisualData(Player player, int index)
		{
			_squadContainers[index].SetDisplay(true);
			_squadContainers[index].visible = true;
						
			var loadout = CurrentRoom.GetPlayerProperties(player).Loadout;

			_services.CollectionService.LoadCollectionItemSprite(
				_services.CollectionService.GetCosmeticForGroup(loadout.Value.ToArray(), 
					GameIdGroup.PlayerSkin)).ContinueWith(_playerMemberElements[index].SetPfpImage);

			var nameColor = _services.TeamService.GetTeamMemberColor(player);
			_playerMemberElements[index].SetTeamColor(nameColor ?? Color.white);
					
			_nameLabels[index].text = player.NickName;
		}
		
		private void SyncSquadMembersData()
		{
			var teamId = _services.TeamService.GetTeamForPlayer(CurrentRoom.LocalPlayer);
			
			_squadMembers = CurrentRoom.Players.Values
				.Where(p => _services.TeamService.GetTeamForPlayer(p) == teamId)
				.ToList();
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
		
		public void OnPlayerPropertiesUpdate()
		{
			RefreshPartyMarkers();
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

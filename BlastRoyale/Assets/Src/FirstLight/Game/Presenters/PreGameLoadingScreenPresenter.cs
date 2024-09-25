using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
		private const string USS_MAP_MARKER_LEFT = "map-marker--left";
		private const string USS_TEAM_MEMBER_MARKER = "map-marker-party";
		
		private const int TIMER_PADDING_MS = 2000;
		private const int MOVE_LOCATION_LEFT = -375;

		public class StateData
		{
			public Action LeaveRoomClicked;
		}

		private VisualElement _teamMembersList;
		private VisualElement _mapHolder;
		private VisualElement _mapTitleBg;
		private VisualElement _mapMarker;
		private VisualElement _mapImage;
		private VisualElement _squadContainer;
		private InGamePlayerAvatar[] _partyMarkers;
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

		private List<Player> _squadMembers = new ();
		private MapAreaConfig _mapAreaConfig;
		private Vector2 _markerLocalPosition;
		private Dictionary<int, CancellationTokenSource> _popMarkerAnimations = new ();


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
			_teamMembersList = Root.Q<VisualElement>("TeamMembersList").Required();
			_teamMembersList.Clear();
			var partyMarkersContainer = Root.Q<VisualElement>("PartyMarkers");
			_header.SetSubtitle("");
			_header.SetButtonsVisibility(false);

			partyMarkersContainer.Clear();
			_partyMarkers = new InGamePlayerAvatar[size - 1];
			for (var i = 0; i < size - 1; i++)
			{
				_partyMarkers[i] = new InGamePlayerAvatar()
				{
					visible = false
				};
				_partyMarkers[i].AddToClassList(USS_TEAM_MEMBER_MARKER);
				partyMarkersContainer.Add(_partyMarkers[i]);
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

			if (isSquadGame && !MainInstaller.ResolveServices().RoomService.IsLocalPlayerSpectator)
			{
				var teamId = _services.TeamService.GetTeamForPlayer(CurrentRoom.LocalPlayer);

				_squadMembers = CurrentRoom.Players.Values
					.Where(p => _services.TeamService.GetTeamForPlayer(p) == teamId)
					.ToList();
				RefreshPartyMarkers();

				while (_squadMembers.Count > _teamMembersList.childCount)
				{
					var el = new PlayerMemberElement();
					_teamMembersList.Add(el);
				}

				while (_teamMembersList.childCount > _squadMembers.Count)
				{
					var last = _teamMembersList[_teamMembersList.childCount - 1];
					_teamMembersList.Remove(last);
				}

				for (var i = 0; i < _squadMembers.Count; i++)
				{
					BindTeamMember(_squadMembers[i], (PlayerMemberElement) _teamMembersList[i]);
				}
			}

			else
			{
				_squadContainer.SetDisplay(false);
				_squadMembers.Clear();
			}
		}

		private void BindTeamMember(Player player, PlayerMemberElement element)
		{
			var nameColor = _services.TeamService.GetTeamMemberColor(player);

			element.SetData(player.NickName, nameColor ?? Color.white);

			var loadout = CurrentRoom.GetPlayerProperties(player).Loadout;

			_services.CollectionService.LoadCollectionItemSprite(
				_services.CollectionService.GetCosmeticForGroup(loadout.Value.ToArray(),
					GameIdGroup.PlayerSkin)).ContinueWith(element.SetPfpImage);
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

			for (var i = 0; i < players.Count(); i++)
			{
				var squadMember = players[i];
				
				var partyMarker = _partyMarkers[i];
				partyMarker.visible = true;

				var memberDropPosition = CurrentRoom.GetPlayerProperties(squadMember).DropPosition.Value;
				var loadout = CurrentRoom.GetPlayerProperties(squadMember).Loadout;
				var nameColor = _services.TeamService.GetTeamMemberColor(squadMember);
				partyMarker.SetTeamColor(nameColor);
				_services.CollectionService.LoadCollectionItemSprite(
					_services.CollectionService.GetCosmeticForGroup(loadout.Value.ToArray(),
						GameIdGroup.PlayerSkin)).ContinueWith(partyMarker.SetSprite);

				var mapWidth = _mapImage.contentRect.width;
				var markerPos = new Vector2(memberDropPosition.x * mapWidth, -memberDropPosition.y * mapWidth);

				var oldPos = partyMarker.transform.position;
				partyMarker.transform.position = markerPos;
				var oldVec = new Vector2(oldPos.x, oldPos.y);
				if (Vector2.Distance(oldVec, markerPos) > 150)
				{
					if (_popMarkerAnimations.TryGetValue(i, out var source))
					{
						source.Cancel();
					}

					var src = CancellationTokenSource.CreateLinkedTokenSource(GetCancellationTokenOnClose());
					_popMarkerAnimations[i] = src;
					UniTask.Void(async cc =>
					{
						await UniTask.Delay(666, cancellationToken: cc);
						partyMarker.AnimatePing(1.3f, 150);
					}, src.Token);
				}
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
			var mutators = simulationConfig.Mutators.GetSetFlags();
			var mutatorsString = "";
			var subtitlesCounter = 0;
			
			// Combining a list of mutators
			for (int i = 0; i < mutators.Length; i++)
			{
				mutatorsString += LocalizationManager.GetTranslation(mutators[i].GetLocalizationKey());
				mutatorsString += i < (mutators.Length - 1) ? ", " : "";
				subtitlesCounter++;
				if (subtitlesCounter == 4)
				{
					mutatorsString += "\n";
				}
			}
			
			// Combining a list of weapons (in case weapon filter is active)
			for (int i = 0; i < simulationConfig.WeaponsSelectionOverwrite.Length; i++)
			{
				if (i == 0 && !string.IsNullOrEmpty(mutatorsString))
				{
					mutatorsString += ", ";
				}
				mutatorsString += LocalizationUtils.GetTranslationGameIdString(simulationConfig.WeaponsSelectionOverwrite[i]);
				mutatorsString += i < (simulationConfig.WeaponsSelectionOverwrite.Length - 1) ? ", " : "";
				subtitlesCounter++;
				if (subtitlesCounter == 4)
				{
					mutatorsString += "\n";
				}
			}
			
			_mapAreaConfig = _services.ConfigsProvider.GetConfig<MapAreaConfigs>().GetMapAreaConfig(mapConfig.Map);
			_locationLabel.text = mapConfig.Map.GetLocalization();
			_header.SetTitle(LocalizationUtils.GetTranslationForGameModeAndTeamSize(gameModeConfig.Id, simulationConfig.TeamSize));
			_header.SetSubtitle(mutatorsString);
			
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
			FLog.Verbose("Map local position " + _markerLocalPosition);
			_mapMarker.EnableInClassList(USS_MAP_MARKER_LEFT, _markerLocalPosition.y < MOVE_LOCATION_LEFT);
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

			RefreshPartyList();
		}

		private void OnWaitingMandatoryMatchAssets()
		{
			if (_gameStartTimerCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_gameStartTimerCoroutine);
			}

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
	}
}
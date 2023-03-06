using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using Photon.Realtime;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter handles displaying matchmaking UI.
	/// In current iteration, this is just a standalone screen for matchmaking only.
	/// In future iteration with new custom lobby screen, this screen will become a loading screen for both
	/// matchmaking and custom lobby, just before players are dropped into the match.
	/// </summary>
	[LoadSynchronously]
	public class MatchmakingScreenPresenter : UiToolkitPresenterData<MatchmakingScreenPresenter.StateData>,
											  IInRoomCallbacks
	{
		public struct StateData
		{
			public Action LeaveRoomClicked;
		}

		[SerializeField] private int _planeFlyDurationMs = 4500;

		private ImageButton _closeButton;
		private VisualElement _dropzone;
		private VisualElement _mapHolder;
		private VisualElement _mapTitleBg;
		private VisualElement _mapMarker;
		private VisualElement _mapMarkerIcon;
		private VisualElement _mapImage;
		private VisualElement _plane;
		private VisualElement _squadContainer;
		private VisualElement _partyMarkers;
		private Label _squadLabel;
		private ListView _squadMembersList;
		private Label _mapMarkerTitle;
		private Label _loadStatusLabel;
		private Label _locationLabel;
		private Label _headerTitleLabel;
		private Label _headerSubtitleLabel;
		private Label _modeDescTopLabel;
		private Label _modeDescBotLabel;
		private Label _debugPlayerCountLabel;
		private IGameServices _services;
		private Coroutine _matchmakingTimerCoroutine;
		private Tweener _planeFlyTween;
		private bool _dropSelectionAllowed;
		private bool _matchStarting;

		private List<Player> _squadMembers = new();

		private Room CurrentRoom => _services.NetworkService.CurrentRoom;
		private bool RejoiningRoom => !_services.NetworkService.IsJoiningNewMatch;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_services.NetworkService.QuantumClient.AddCallbackTarget(this);
		}

		private void OnDestroy()
		{
			_services?.NetworkService?.QuantumClient?.RemoveCallbackTarget(this);
		}

		protected override void QueryElements(VisualElement root)
		{
			base.QueryElements(root);

			_closeButton = root.Q<ImageButton>("CloseButton").Required();
			_dropzone = root.Q("DropZone").Required();
			_mapHolder = root.Q("Map").Required();
			_mapImage = root.Q("MapImage").Required();
			_plane = root.Q("Plane").Required();
			_mapMarker = root.Q("MapMarker").Required();
			_mapMarkerTitle = root.Q<Label>("MapMarkerTitle").Required();
			_mapMarkerIcon = root.Q("MapMarkerIcon").Required();
			_mapTitleBg = root.Q("MapTitleBg").Required();
			_loadStatusLabel = root.Q<Label>("LoadStatusLabel").Required();
			_locationLabel = root.Q<Label>("LocationLabel").Required();
			_headerTitleLabel = root.Q<Label>("title").Required();
			_headerSubtitleLabel = root.Q<Label>("subtitle").Required();
			_modeDescTopLabel = root.Q<Label>("ModeDescTop").Required();
			_modeDescBotLabel = root.Q<Label>("ModeDescBot").Required();
			_debugPlayerCountLabel = root.Q<Label>("DebugPlayerCount").Required();
			_squadContainer = root.Q("SquadContainer").Required();
			_squadMembersList = root.Q<ListView>("SquadList").Required();
			_partyMarkers = root.Q("PartyMarkers").Required();

			_squadMembersList.DisableScrollbars();
			_squadMembersList.makeItem = CreateSquadListEntry;
			_squadMembersList.bindItem = BindSquadListEntry;

			_closeButton.clicked += OnCloseClicked;
		}

		protected override void SubscribeToEvents()
		{
			base.SubscribeToEvents();

			_mapHolder.RegisterCallback<GeometryChangedEvent>(InitMap);

			_services.MessageBrokerService.Subscribe<StartedFinalPreloadMessage>(OnStartedFinalPreloadMessage);
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			RefreshPartyList();
		}

		private void RefreshPartyList()
		{
			var isSquadGame = _services.NetworkService.CurrentRoomGameModeConfig!.Value!.Teams;

			if (isSquadGame)
			{
				var teamId = _services.NetworkService.CurrentRoom.Players.Values.First(p => p.IsLocal).GetTeamId();

				_squadContainer.SetDisplay(true);
				_squadMembers = _services.NetworkService.CurrentRoom.Players.Values
					.Where(p => p.GetTeamId() == teamId)
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

				var memberDropPosition = squadMember.GetDropPosition();
				var marker = new VisualElement {name = "marker"};
				marker.AddToClassList("map-marker-party");
				var mapWidth = _mapImage.contentRect.width;
				var markerPos = new Vector2(memberDropPosition.x * mapWidth, -memberDropPosition.y * mapWidth);

				_partyMarkers.Add(marker);
				marker.transform.position = markerPos;
			}
		}

		private void BindSquadListEntry(VisualElement element, int index)
		{
			((Label) element).text = _squadMembers[index].NickName;
		}

		private VisualElement CreateSquadListEntry()
		{
			var label = new Label();
			label.AddToClassList("squad-member");
			return label;
		}

		protected override void UnsubscribeFromEvents()
		{
			base.UnsubscribeFromEvents();

			if (_matchmakingTimerCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_matchmakingTimerCoroutine);
			}

			_services.MessageBrokerService.Unsubscribe<StartedFinalPreloadMessage>(OnStartedFinalPreloadMessage);
		}

		private void OnMapClicked(ClickEvent evt)
		{
			SelectMapPosition(evt.localPosition, true, true);
		}

		private void SelectMapPosition(Vector2 localPos, bool offsetCoors, bool checkClickWithinRadius)
		{
			if (!_dropSelectionAllowed || (checkClickWithinRadius && !IsWithinMapRadius(localPos))) return;

			var mapGridConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();
			var mapWidth = _mapImage.contentRect.width;
			var mapHeight = _mapImage.contentRect.height;
			var mapWidthHalf = mapWidth / 2;
			var mapHeightHalf = mapHeight / 2;

			// Set map marker at click point
			if (offsetCoors)
			{
				localPos = new Vector3(localPos.x - mapWidthHalf, localPos.y - mapHeightHalf, 0);
			}

			_mapMarker.transform.position = localPos;

			// Get normalized position for spawn positions in quantum, -0.5 to 0.5 range
			var quantumSelectPos = new Vector2(localPos.x / mapWidth, -localPos.y / mapWidth);
			_services.NetworkService.SetDropPosition(quantumSelectPos);

			// Get normalized position for the whole map, 0-1 range, used for grid configs
			var mapNormX = Mathf.InverseLerp(-mapWidthHalf, mapWidthHalf, localPos.x);
			var mapNormY = Mathf.InverseLerp(-mapHeightHalf, mapHeightHalf, localPos.y);
			var mapSelectNorm = new Vector2(mapNormX, mapNormY);

			// Set map grid config related data
			var gridX = Mathf.FloorToInt(mapGridConfigs.GetSize().x * mapSelectNorm.x);
			var gridY = Mathf.FloorToInt(mapGridConfigs.GetSize().y * mapSelectNorm.y);
			var selectedGrid = mapGridConfigs.GetConfig(gridX, gridY);

			if (selectedGrid.IsValidNamedArea)
			{
				_mapMarkerTitle.SetDisplay(true);
				_mapMarkerTitle.text = selectedGrid.AreaName.GetMapDropPointLocalization().ToUpper();
			}
			else
			{
				_mapMarkerTitle.SetDisplay(false);
			}
		}

		private bool IsWithinMapRadius(Vector3 dropPos)
		{
			var mapRadius = _mapImage.contentRect.width / 2;
			var mapCenter = new Vector3(_mapImage.transform.position.x + mapRadius,
				_mapImage.transform.position.y + mapRadius, _mapImage.transform.position.z);

			return Vector3.Distance(mapCenter, dropPos) < mapRadius;
		}

		private async void InitMap(GeometryChangedEvent evt)
		{
			if (CurrentRoom == null) return;

			// Have to unregister callback immediately, as when the plane animates within the map holder,
			// the geometry changed event fires constantly.
			_mapHolder.UnregisterCallback<GeometryChangedEvent>(InitMap);

			var matchType = CurrentRoom.GetMatchType();
			var gameMode = CurrentRoom.GetGameModeId();
			var gameModeConfig = _services.NetworkService.CurrentRoomGameModeConfig.Value;
			var mapConfig = _services.NetworkService.CurrentRoomMapConfig.Value;
			var quantumGameConfig = _services.ConfigsProvider.GetConfig<QuantumGameConfig>();
			var minPlayers = matchType == MatchType.Ranked ? quantumGameConfig.RankedMatchmakingMinPlayers : 0;
			var modeDesc = GetGameModeDescriptions(gameModeConfig.CompletionStrategy);

			var matchmakingTime = NetworkUtils.GetMatchmakingTime(matchType, gameModeConfig, quantumGameConfig);

			_locationLabel.text = mapConfig.Map.GetLocalization();
			_headerTitleLabel.text = gameMode.GetTranslationGameIdString()?.ToUpper();
			_headerSubtitleLabel.text = matchType.GetLocalization().ToUpper();

			_modeDescTopLabel.text = modeDesc[0];
			_modeDescBotLabel.text = modeDesc[1];

			_closeButton.SetDisplay(!_services.TutorialService.IsTutorialRunning);

			UpdatePlayerCount();

			if (!gameModeConfig.SkydiveSpawn)
			{
				_dropzone.SetDisplay(false);
				_mapMarker.SetDisplay(false);
				_mapTitleBg.SetDisplay(false);
				var sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(mapConfig.Map, false);
				_mapImage.style.backgroundImage = new StyleBackground(sprite);
				return;
			}

			_dropSelectionAllowed = !RejoiningRoom;

			if (RejoiningRoom)
			{
				OnStartedFinalPreloadMessage(new StartedFinalPreloadMessage());
			}
			else
			{
				_matchmakingTimerCoroutine =
					_services.CoroutineService.StartCoroutine(MatchmakingTimerCoroutine(matchmakingTime, minPlayers));
				StartPlaneFlyAnimLoop();
			}

			InitSkydiveSpawnMapData();
		}

		private void InitSkydiveSpawnMapData()
		{
			// Init DZ position/rotation
			var dropzonePosRot = CurrentRoom.GetDropzonePosRot();
			var mapWidth = _mapHolder.contentRect.width;
			var mapHeight = _mapHolder.contentRect.height;
			var posX = mapWidth * dropzonePosRot.x;
			var posY = mapHeight * dropzonePosRot.y;

			_dropzone.transform.position = new Vector3(posX, posY);
			_dropzone.transform.rotation = Quaternion.Euler(0, 0, dropzonePosRot.z);

			SelectMapPosition(new Vector2(posX, posY), false, false);

			_mapImage.RegisterCallback<ClickEvent>(OnMapClicked);
		}

		public void OnPlayerEnteredRoom(Player newPlayer)
		{
			UpdatePlayerCount();
			RefreshPartyList();
		}

		public void OnPlayerLeftRoom(Player otherPlayer)
		{
			UpdatePlayerCount();
			RefreshPartyList();
		}

		private void OnStartedFinalPreloadMessage(StartedFinalPreloadMessage obj)
		{
			if (_matchmakingTimerCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_matchmakingTimerCoroutine);
			}

			_closeButton.SetDisplay(false);
			_loadStatusLabel.text = ScriptLocalization.UITMatchmaking.loading_status_starting;
			_dropSelectionAllowed = false;
		}

		private void UpdatePlayerCount()
		{
			_debugPlayerCountLabel.text = Debug.isDebugBuild
				? string.Format(ScriptLocalization.UITMatchmaking.current_player_amount,
					CurrentRoom.GetRealPlayerAmount(), CurrentRoom.GetRealPlayerCapacity())
				: "";
		}

		private IEnumerator MatchmakingTimerCoroutine(float matchmakingTime, int minPlayers)
		{
			var roomCreateTime = CurrentRoom.GetRoomCreationDateTime();
			var matchmakingEndTime = roomCreateTime.AddSeconds(matchmakingTime);


			while (DateTime.UtcNow < matchmakingEndTime && !CurrentRoom.IsAtFullPlayerCapacity(_services.ConfigsProvider))
			{
				var timeLeft = (DateTime.UtcNow - matchmakingEndTime).Duration();
				var translation = CurrentRoom.ShouldUsePlayFabMatchmaking(_services.ConfigsProvider)
					? ScriptLocalization.UITMatchmaking.loading_status_waiting_timer
					: ScriptLocalization.UITMatchmaking.loading_status_timer;
				_loadStatusLabel.text = string.Format(translation, timeLeft.TotalSeconds.ToString("F0"));
				
				yield return new WaitForSeconds(.2f);
			}

			if (CurrentRoom.IsAtFullPlayerCapacity(_services.ConfigsProvider))
			{
				_loadStatusLabel.text = ScriptLocalization.UITMatchmaking.loading_status_waiting;
			}
			else if (CurrentRoom.GetRealPlayerAmount() >= minPlayers)
			{
				_loadStatusLabel.text = ScriptLocalization.UITMatchmaking.loading_status_starting;
			}
			else
			{
				_loadStatusLabel.text = ScriptLocalization.UITMatchmaking.loading_status_waiting;
			}
		}

		private void StartPlaneFlyAnimLoop()
		{
			_plane.experimental.animation.Start(0, 100f, _planeFlyDurationMs,
				(ve, val) => { ve.style.bottom = new Length(val, LengthUnit.Percent); }).OnCompleted(() =>
			{
				if (_dropSelectionAllowed)
				{
					StartPlaneFlyAnimLoop();
				}
			});
		}

		private string[] GetGameModeDescriptions(GameCompletionStrategy strategy)
		{
			var descriptions = new string[2];
			descriptions[0] = strategy.GetLocalization();
			descriptions[1] = ScriptLocalization.UITMatchmaking.wins_the_match;

			return descriptions;
		}

		public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
		{
		}

		public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
		{
			RefreshPartyList();
		}

		public void OnMasterClientSwitched(Player newMasterClient)
		{
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
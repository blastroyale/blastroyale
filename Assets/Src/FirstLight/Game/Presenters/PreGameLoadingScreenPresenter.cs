using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Services.RoomService;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
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
	[LoadSynchronously]
	public class PreGameLoadingScreenPresenter : UiToolkitPresenterData<PreGameLoadingScreenPresenter.StateData>
	{
		private const int TIMER_PADDING_MS = 2000;
		private const int DISABLE_LEAVE_AFTER = 3;

		public struct StateData
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

		protected override void QueryElements(VisualElement root)
		{
			base.QueryElements(root);

			_mapHolder = root.Q("Map").Required();
			_mapImage = root.Q("MapImage").Required();
			_mapMarker = root.Q("MapMarker").Required();
			_mapMarkerTitle = root.Q<Label>("MapMarkerTitle").Required();
			_mapTitleBg = root.Q("MapTitleBg").Required();
			_loadStatusLabel = root.Q<Label>("LoadStatusLabel").Required();
			_locationLabel = root.Q<Label>("LocationLabel").Required();
			_header = root.Q<ScreenHeaderElement>("Header").Required();
			_modeDescTopLabel = root.Q<Label>("ModeDescTop").Required();
			_modeDescBotLabel = root.Q<Label>("ModeDescBot").Required();
			_debugPlayerCountLabel = root.Q<Label>("DebugPlayerCount").Required();
			_debugMasterClient = root.Q<Label>("DebugMasterClient").Required();
			_squadContainer = root.Q("SquadContainer").Required();
			_squadMembersList = root.Q<ListView>("SquadList").Required();
			_partyMarkers = root.Q("PartyMarkers").Required();

			_squadMembersList.DisableScrollbars();
			_squadMembersList.makeItem = CreateSquadListEntry;
			_squadMembersList.bindItem = BindSquadListEntry;

			_header.homeClicked += OnCloseClicked;
		}

		protected override void SubscribeToEvents()
		{
			base.SubscribeToEvents();

			_mapHolder.RegisterCallback<GeometryChangedEvent>(InitMap);

			_services.RoomService.OnPlayersChange += OnPlayersChanged;
			_services.RoomService.OnMasterChanged += UpdateMasterClient;
			_services.RoomService.OnPlayerPropertiesUpdated += OnPlayerPropertiesUpdate;
			_services.MessageBrokerService.Subscribe<WaitingMandatoryMatchAssetsMessage>(OnWaitingMandatoryMatchAssets);
		}


		protected override void OnOpened()
		{
			base.OnOpened();
			RefreshPartyList();
			UpdateMasterClient();
		}

		private void RefreshPartyList()
		{
			var isSquadGame = CurrentRoom.GameModeConfig.Teams;

			if (isSquadGame)
			{
				var teamId = CurrentRoom.LocalPlayerProperties.TeamId.Value;

				_squadContainer.SetDisplay(true);
				_squadMembers = CurrentRoom.Players.Values
					.Where(p => CurrentRoom.GetPlayerProperties(p).TeamId.Value == teamId)
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

		protected override void UnsubscribeFromEvents()
		{
			base.UnsubscribeFromEvents();

			if (_gameStartTimerCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_gameStartTimerCoroutine);
			}

			_services.MessageBrokerService.Unsubscribe<WaitingMandatoryMatchAssetsMessage>(OnWaitingMandatoryMatchAssets);
		}

		private void OnMapClicked(ClickEvent evt)
		{
			SelectMapPosition(evt.localPosition, true, true);
		}

		/// <summary>
		///  Select the drop zone based on percentages of the map
		/// </summary>
		public void SelectDropZone(float x, float y)
		{
			var mapWidth = _mapImage.contentRect.width;

			SelectMapPosition(new Vector2(x * mapWidth, y * mapWidth), true, false);
		}

		private void SelectMapPosition(Vector2 localPos, bool offsetCoors, bool checkClickWithinRadius)
		{
			if (_mapImage == null) return;
			if (!_services.RoomService.InRoom) return;

			if (!_dropSelectionAllowed || (checkClickWithinRadius && !IsWithinMapRadius(localPos))) return;

			if (checkClickWithinRadius)
			{
				_services.MessageBrokerService.Publish(new MapDropPointSelectedMessage());
			}

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
			_services.RoomService.CurrentRoom.LocalPlayerProperties.DropPosition.Value = quantumSelectPos;

			_mapMarkerTitle.SetDisplay(false);
		}

		private bool IsWithinMapRadius(Vector3 dropPos)
		{
			var mapRadius = _mapImage.contentRect.width / 2;
			var mapCenter = new Vector3(_mapImage.transform.position.x + mapRadius,
				_mapImage.transform.position.y + mapRadius, _mapImage.transform.position.z);

			return Vector3.Distance(mapCenter, dropPos) < mapRadius;
		}

		private async Task LoadMapAsset(QuantumMapConfig mapConfig)
		{
			if (_services.AssetResolverService.TryGetAssetReference<GameId, Sprite>(mapConfig.Map, out _))
			{
				var sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(mapConfig.Map, false);
				_mapImage.style.backgroundImage = new StyleBackground(sprite);
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

		private void InitMap(GeometryChangedEvent evt)
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
			_header.SetTitle(gameModeConfig.Id.GetTranslationGameIdString()?.ToUpper(), matchType.GetLocalization().ToUpper());

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
				_ = LoadMapAsset(mapConfig);

				return;
			}

			_dropSelectionAllowed = !RejoiningRoom;

			if (RejoiningRoom)
			{
				OnWaitingMandatoryMatchAssets(new WaitingMandatoryMatchAssetsMessage());
			}

			InitSkydiveSpawnMapData();
			_ = LoadMapAsset(mapConfig);
		}

		private void InitSkydiveSpawnMapData()
		{
			var posX = Random.Range(0.3f, 0.7f);
			var posY = Random.Range(0.3f, 0.7f);
			SelectDropZone(posX, posY);
			_mapImage.RegisterCallback<ClickEvent>(OnMapClicked);
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
				if (CurrentRoom.GameStarted) break;
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
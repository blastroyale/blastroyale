using System;
using System.Collections;
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

		private Room CurrentRoom => _services.NetworkService.QuantumClient.CurrentRoom;
		private bool RejoiningRoom => !_services.NetworkService.IsJoiningNewMatch;
		
		private ImageButton _closeButton;
		private VisualElement _dropzone;
		private VisualElement _mapHolder;
		private VisualElement _mapTitleBg;
		private VisualElement _mapMarker;
		private VisualElement _mapMarkerIcon;
		private VisualElement _mapImage;
		private Label _mapMarkerTitle;
		private Label _loadStatusLabel;
		private Label _locationLabel;
		private Label _modeTitleLabel;
		private Label _modeDescTopLabel;
		private Label _modeDescBotLabel;
		private Label _debugPlayerCountLabel;
		private IGameServices _services;
		private Coroutine _matchmakingTimerCoroutine;
		private bool _dropSelectionAllowed;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements(VisualElement root)
		{
			base.QueryElements(root);

			_closeButton = root.Q<ImageButton>("CloseButton").Required();
			_dropzone = root.Q("DropZone").Required();
			_mapHolder = root.Q("Map").Required();
			_mapImage = root.Q("MapImage").Required();
			_mapMarker = root.Q("MapMarker").Required();
			_mapMarkerTitle = root.Q<Label>("MapMarkerTitle").Required();
			_mapMarkerIcon = root.Q("MapMarkerIcon").Required();
			_mapTitleBg = root.Q("MapTitleBg").Required();
			_loadStatusLabel = root.Q<Label>("LoadStatusLabel").Required();
			_locationLabel = root.Q<Label>("LocationLabel").Required();
			_modeTitleLabel = root.Q<Label>("HeaderTitle").Required();
			_modeDescTopLabel = root.Q<Label>("ModeDescTop").Required();
			_modeDescBotLabel = root.Q<Label>("ModeDescBot").Required();
			_debugPlayerCountLabel = root.Q<Label>("DebugPlayerCount").Required();

			_closeButton.clicked += OnCloseClicked;
		}

		protected override void SubscribeToEvents()
		{
			base.SubscribeToEvents();
			
			_mapHolder.RegisterCallback<GeometryChangedEvent>(InitMap);
			
			_services.MessageBrokerService.Subscribe<StartedFinalPreloadMessage>(OnStartedFinalPreloadMessage);
		}

		protected override void UnsubscribeFromEvents()
		{
			base.UnsubscribeFromEvents();
			
			if (_matchmakingTimerCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_matchmakingTimerCoroutine);
			}
			
			_mapHolder.UnregisterCallback<GeometryChangedEvent>(InitMap);
			
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
			var mapDiameter = _mapImage.contentRect.width;
			var mapRadius = mapDiameter / 2;

			// Set map marker at click point
			if (offsetCoors)
			{
				localPos = new Vector3(localPos.x - mapRadius, localPos.y - mapRadius, 0);
			}
			
			_mapMarker.transform.position = localPos;

			// Get normalized position for spawn positions in quantum, -0.5 to 0.5 range
			var quantumSelectPos = new Vector2(localPos.x / mapDiameter, -localPos.y / mapDiameter);
			_services.MatchmakingService.NormalizedMapSelectedPosition = quantumSelectPos;

			// Get normalized position for the whole map, 0-1 range, used for grid configs
			var mapNormX = Mathf.InverseLerp(-mapRadius, mapRadius,localPos.x);
			var mapNormY = Mathf.InverseLerp(-mapRadius, mapRadius,localPos.y);
			var mapSelectNorm = new Vector2(mapNormX, mapNormY);
			
			// Set map grid config related data
			var gridX = Mathf.FloorToInt(mapGridConfigs.GetSize().x * mapSelectNorm.x);
			var gridY = Mathf.FloorToInt(mapGridConfigs.GetSize().y * mapSelectNorm.y);
			var selectedGrid = mapGridConfigs.GetConfig(gridX, gridY);
			
			if (selectedGrid.IsValidNamedArea)
			{
				_mapMarkerTitle.SetDisplay(true);
				_mapMarkerTitle.text = selectedGrid.AreaName.ToUpper();
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
			
			var matchType = CurrentRoom.GetMatchType();
			var gameMode = CurrentRoom.GetGameModeId();
			var gameModeConfig = _services.NetworkService.CurrentRoomGameModeConfig.Value;
			var mapConfig = _services.NetworkService.CurrentRoomMapConfig.Value;
			var quantumGameConfig = _services.ConfigsProvider.GetConfig<QuantumGameConfig>();
			var minPlayers = matchType == MatchType.Ranked ? quantumGameConfig.RankedMatchmakingMinPlayers : 0;
			var modeDesc = GetGameModeDescriptions(gameModeConfig.CompletionStrategy);
			var matchmakingTime = matchType == MatchType.Ranked
									  ? quantumGameConfig.RankedMatchmakingTime.AsFloat
									  : quantumGameConfig.CasualMatchmakingTime.AsFloat;

			_locationLabel.text = mapConfig.Map.GetTranslation();
			_modeTitleLabel.text = string.Format(ScriptLocalization.UITMatchmaking.mode_header_title,
												 gameMode.GetTranslationGameIdString().ToUpper(),
												 matchType.GetTranslation().ToUpper());
			_modeDescTopLabel.text = modeDesc[0];
			_modeDescBotLabel.text = modeDesc[1];
			
			_closeButton.SetDisplay(true);
			
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
				_matchmakingTimerCoroutine = _services.CoroutineService.StartCoroutine(MatchmakingTimerCoroutine(matchmakingTime, minPlayers));
			}

			InitSkydiveSpawnMapData();
		}

		private void InitSkydiveSpawnMapData()
		{
			// Init DZ position/rotation
			var dropzonePosRot = CurrentRoom.GetDropzonePosRot();
			var mapDiameter = _mapHolder.contentRect.width;
			var posX = mapDiameter * dropzonePosRot.x;
			var posY = mapDiameter * dropzonePosRot.y;

			_dropzone.transform.position = new Vector3(posX, posY);
			_dropzone.transform.rotation = Quaternion.Euler(0, 0, dropzonePosRot.z);

			SelectMapPosition(new Vector2(posX, posY), false, false);

			_mapImage.RegisterCallback<ClickEvent>(OnMapClicked);
		}

		public void OnPlayerEnteredRoom(Player newPlayer)
		{
			UpdatePlayerCount();
		}

		public void OnPlayerLeftRoom(Player otherPlayer)
		{
			UpdatePlayerCount();
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

			while (DateTime.UtcNow < matchmakingEndTime)
			{
				var timeLeft = (DateTime.UtcNow - matchmakingEndTime).Duration();
				_loadStatusLabel.text = string.Format(ScriptLocalization.UITMatchmaking.loading_status_timer,
													  timeLeft.TotalSeconds.ToString("F0"));

				yield return null;
			}

			if (CurrentRoom.GetRealPlayerAmount() >= minPlayers)
			{
				_loadStatusLabel.text = ScriptLocalization.UITMatchmaking.loading_status_starting;
			}
			else
			{
				_loadStatusLabel.text = ScriptLocalization.UITMatchmaking.loading_status_waiting;
			}
		}

		private string[] GetGameModeDescriptions(GameCompletionStrategy strategy)
		{
			var descriptions = new string[2];
			descriptions[0] = strategy.GetTranslation();
			descriptions[1] = ScriptLocalization.UITMatchmaking.wins_the_match;

			return descriptions;
		}
		
		public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
		{
		}

		public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
		{
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

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.confirmation, desc, true, confirmButton);
		}
	}
}
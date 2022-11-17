using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
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
	public class MatchmakingScreenPresenter : UiToolkitPresenterData<MatchmakingScreenPresenter.StateData>,
											  IInRoomCallbacks
	{
		public struct StateData
		{
			public Action LeaveRoomClicked;
		}

		private Room CurrentRoom => _services.NetworkService.QuantumClient.CurrentRoom;

		private ImageButton _closeButton;
		private VisualElement _dropzone;
		private VisualElement _mapHolder;
		private VisualElement _mapTitleBg;
		private VisualElement _mapMarker;
		private VisualElement _dropzonePath;
		private VisualElement _mapImage;
		private Label _loadStatusLabel;
		private Label _locationLabel;
		private Label _placeNameLabel;
		private Label _modeTitleLabel;
		private Label _modeDescTopLabel;
		private Label _modeDescBotLabel;
		private IGameServices _services;

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
			_dropzonePath = root.Q("Path").Required();
			_mapTitleBg = root.Q("MapTitleBg").Required();
			_loadStatusLabel = root.Q<Label>("LoadStatusLabel").Required();
			_locationLabel = root.Q<Label>("LocationLabel").Required();
			_placeNameLabel = root.Q<Label>("PlaceNameLabel").Required();
			_modeTitleLabel = root.Q<Label>("HeaderTitle").Required();
			_modeDescTopLabel = root.Q<Label>("ModeDescTop").Required();
			_modeDescTopLabel = root.Q<Label>("ModeDescBot").Required();
			
			_closeButton.clicked += OnCloseClicked;
			_mapHolder.RegisterCallback<GeometryChangedEvent>(InitMap);
		}

		private void OnMapClicked(ClickEvent evt)
		{
			SelectMapPosition(evt.localPosition);
		}

		private void SelectMapPosition(Vector2 localPos)
		{
			if (!IsWithinMapRadius(localPos)) return;

			var mapGridConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();
			var mapRadius = _mapImage.contentRect.width / 2;

			// Set map marker at click point
			var offsetCoors = new Vector3(localPos.x - mapRadius, localPos.y - mapRadius, 0);
			_mapMarker.transform.position = offsetCoors;

			// Set normalized position used for spawning in quantum
			var normSelectPos = new Vector2(localPos.x / _mapImage.contentRect.width,
				localPos.y / _mapImage.contentRect.height);
			_services.MatchmakingService.NormalizedMapSelectedPosition = normSelectPos;

			// Set map grid config related data
			var gridX = Mathf.RoundToInt(mapGridConfigs.GetSize().x * normSelectPos.x);
			var gridY = Mathf.RoundToInt(mapGridConfigs.GetSize().y * normSelectPos.y);
			var selectedGrid = mapGridConfigs.GetConfig(gridX, gridY);

			if (selectedGrid.IsValidNamedArea)
			{
				_placeNameLabel.SetDisplayActive(true);
				_placeNameLabel.text = selectedGrid.AreaName.ToUpper();
			}
			else
			{
				_placeNameLabel.SetDisplayActive(false);
			}
		}

		private bool IsWithinMapRadius(Vector3 dropPos)
		{
			var mapRadius = _mapImage.contentRect.width / 2;
			var mapCenter = new Vector3(_mapImage.transform.position.x + mapRadius,
				_mapImage.transform.position.y + mapRadius, _mapImage.transform.position.z);
			var withinMapRadius = Vector3.Distance(mapCenter, dropPos) < mapRadius;

			return withinMapRadius;
		}

		private async void InitMap(GeometryChangedEvent evt)
		{
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
				gameMode.GetTranslationGameIdString().ToUpper(), matchType.GetTranslation().ToUpper());
			_modeDescTopLabel.text = modeDesc[0];
			_modeDescTopLabel.text = modeDesc[1];
			
			_services.CoroutineService.StartCoroutine(MatchmakingTimerCoroutine(matchmakingTime, minPlayers));

			if (!gameModeConfig.SkydiveSpawn)
			{
				var sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(mapConfig.Map, false);
				_mapImage.style.backgroundImage = new StyleBackground(sprite);
				_dropzone.SetDisplayActive(false);
				_mapMarker.SetDisplayActive(false);
				_mapTitleBg.SetDisplayActive(false);
				return;
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

			SelectMapPosition(new Vector2(posX, posY));

			_mapImage.RegisterCallback<ClickEvent>(OnMapClicked);
		}

		public void OnPlayerEnteredRoom(Player newPlayer)
		{
		}

		public void OnPlayerLeftRoom(Player otherPlayer)
		{
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

		private string GetPlayerCountString()
		{
			return Debug.isDebugBuild
				? ""
				: " | " + string.Format(ScriptLocalization.UITMatchmaking.current_player_amount,
					CurrentRoom.PlayerCount);
		}

		public void OnCloseClicked()
		{
			Data.LeaveRoomClicked();
		}
	}
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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
	public class MatchmakingScreenPresenter : UiToolkitPresenterData<MatchmakingScreenPresenter.StateData>, IInRoomCallbacks
	{
		public struct StateData
		{
			public Action LeaveRoomClicked;
		}

		private Room CurrentRoom => _services.NetworkService.QuantumClient.CurrentRoom;

		private ImageButton _closeButton;
		private VisualElement _dropzone;
		private VisualElement _mapHolder;
		private VisualElement _mapMarker;
		private VisualElement _dropzonePath;
		private VisualElement _mapImage;
		private Label _mapTitleLabel;
		private Label _playerCountLabel;
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
			_mapTitleLabel = root.Q<Label>("Title").Required();
			_playerCountLabel = root.Q<Label>("PlayerCountLabel").Required();
			
			_closeButton.clicked += OnCloseClicked;
			_mapHolder.RegisterCallback<GeometryChangedEvent>(InitMap);
		}

		private void OnMapClicked(ClickEvent evt)
		{
			if (!IsWithinMapRadius(evt.localPosition)) return;
			
			var mapRadius = _mapImage.contentRect.width / 2;
			var offsetCoors = new Vector3(evt.localPosition.x - mapRadius, evt.localPosition.y - mapRadius, 0);
			
			// For some reason, the map marker is always offset by width/2 of the map in both width and height, no matter what...
			_mapMarker.transform.position = offsetCoors;
		}

		private bool IsWithinMapRadius(Vector3 dropPos)
		{
			var mapRadius = _mapImage.contentRect.width / 2;
			var mapCenter = new Vector3(_mapImage.transform.position.x + mapRadius, _mapImage.transform.position.y + mapRadius, _mapImage.transform.position.z);
			var withinMapRadius = Vector3.Distance(mapCenter, dropPos) < mapRadius;

			return withinMapRadius;
		}
		
		private async void InitMap(GeometryChangedEvent evt)
		{
			var gameModeConfig = _services.NetworkService.CurrentRoomGameModeConfig.Value;
			var mapConfig = _services.NetworkService.CurrentRoomMapConfig.Value;

			if (!gameModeConfig.SkydiveSpawn)
			{
				var sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(mapConfig.Map, false);
				_mapImage.style.backgroundImage = new StyleBackground(sprite);
				_mapTitleLabel.text = mapConfig.Map.GetTranslation();
				_dropzone.SetDisplayActive(false);
				_mapMarker.SetDisplayActive(false);
				return;
			}
			
			_mapTitleLabel.text = ScriptLocalization.UITMatchmaking.select_dropzone;

			// Init DZ position/rotation
			var dropzonePosRot = CurrentRoom.GetDropzonePosRot();
			var mapDiameter = _mapHolder.contentRect.width;
			var posX = mapDiameter * dropzonePosRot.x;
			var posY = mapDiameter * dropzonePosRot.y;
			
			_dropzone.transform.position = new Vector3(posX, posY);
			_dropzone.transform.rotation = Quaternion.Euler(0,0,dropzonePosRot.z);
			_mapMarker.transform.position = new Vector3(posX, posY);
			
			_mapImage.RegisterCallback<ClickEvent>(OnMapClicked);
		}

		public void OnPlayerEnteredRoom(Player newPlayer)
		{
			UpdatePlayerCountText();
		}

		public void OnPlayerLeftRoom(Player otherPlayer)
		{
			UpdatePlayerCountText();
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

		private void UpdatePlayerCountText()
		{
			var playerCountString = string.Format(ScriptLocalization.UITMatchmaking.current_player_amount, CurrentRoom.PlayerCount);
			//var statusString
		}

		public void OnCloseClicked()
		{
			Data.LeaveRoomClicked();
		}
	}
}
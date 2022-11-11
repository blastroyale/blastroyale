using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UIElements;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace FirstLight.Game.Presenters
{
	public class MatchmakingScreenPresenter : UiToolkitPresenterData<MatchmakingScreenPresenter.StateData>, IInRoomCallbacks
	{
		public struct StateData
		{
			public Action LeaveRoomClicked;
		}
		
		private Button _leaveButton;
		private VisualElement _dropzonePath;
		private VisualElement _mapHolder;
		private IGameServices _services;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}
		
		protected override void QueryElements(VisualElement root)
		{
			base.QueryElements(root);

			_leaveButton = root.Q<Button>("LeaveButton").Required();
			_dropzonePath = root.Q("DropzonePathRoot").Required();
			_mapHolder = root.Q("MapHolder").Required();
			
			_leaveButton.clicked += OnLeaveRoomClicked;

			_mapHolder.RegisterCallback<GeometryChangedEvent>(RepositionMap);
			//RepositionMap();
		}

		private void RepositionMap(GeometryChangedEvent evt)
		{
			var dropzonePosRot = _services.NetworkService.QuantumClient.CurrentRoom.GetDropzonePosRot();
			var mapDiameter = _mapHolder.contentRect.width;
			var posOffsetX = mapDiameter * dropzonePosRot.x;
			var posOffsetY = mapDiameter * dropzonePosRot.y;
			
			_dropzonePath.transform.position = new Vector3(posOffsetX, posOffsetY);
			_dropzonePath.transform.rotation = Quaternion.Euler(0,0,dropzonePosRot.z);
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

		public void OnLeaveRoomClicked()
		{
			Data.LeaveRoomClicked();
		}
	}
}
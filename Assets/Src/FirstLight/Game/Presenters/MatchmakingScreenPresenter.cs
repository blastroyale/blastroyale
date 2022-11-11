using System;
using System.Collections;
using System.Collections.Generic;
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
		private Button _leaveButton;
		
		public struct StateData
		{
			public Action LeaveRoomClicked;
		}
		
		protected override void QueryElements(VisualElement root)
		{
			base.QueryElements(root);
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
	}
}
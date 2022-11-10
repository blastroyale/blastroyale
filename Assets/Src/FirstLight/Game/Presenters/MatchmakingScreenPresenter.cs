using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.UiService;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace FirstLight.Game.Presenters
{
	public class MatchmakingScreenPresenter : UiToolkitPresenterData<MatchmakingScreenPresenter.StateData>, IInRoomCallbacks
	{
		public struct StateData
		{
			public Action LeaveRoomClicked;
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
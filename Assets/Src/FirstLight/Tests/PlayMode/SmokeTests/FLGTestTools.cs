using System.Collections;
using FirstLight.Game.Presenters;
using FirstLight.Game.UIElements;
using FirstLight.Game.Views;
using FirstLight.Game.Views.MainMenuViews;
using FirstLight.UiService;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Toggle = UnityEngine.UI.Toggle;

namespace FirstLight.Tests.PlayTests
{
#region WAIT_FOR_MENU
	public class FLGTestTools
	{
		public static IEnumerator WaitForMainMenu()
		{
			yield return TestTools.UntilObjectOfType<HomeScreenPresenter>();
		}

		public static IEnumerator WaitForGameModeSelectionScreen()
		{
			yield return TestTools.UntilObjectOfType<GameModeSelectionPresenter>();
		}
		
		public static IEnumerator WaitForCustomGameMenu()
		{
			yield return TestTools.UntilObjectOfType<RoomJoinCreateScreenPresenter>();
		}
		
		public static IEnumerator WaitForMatchMakingScreen()
		{
			yield return TestTools.UntilObjectOfType<CustomLobbyScreenPresenter>();
		}
		
		public static IEnumerator WaitForGameCompleteScreen()
		{
			yield return TestTools.UntilObjectOfType<WinnerScreenPresenter>();
		}
		
		public static IEnumerator WaitForMatchEndScreen()
		{
			yield return TestTools.UntilObjectOfType<MatchEndScreenPresenter>();
		}
		
		public static IEnumerator WaitForResultsScreen()
		{
			yield return TestTools.UntilObjectOfType<ResultsScreenPresenter>();
		}
#endregion

		public static void ClickNextButton<T>() where T : UiPresenter
		{
			TestTools.ClickUIToolKitButton<Button>(Object.FindObjectOfType<T>().GetComponent<UIDocument>(), "PlayButton");
		}

		public static void ClickPlayButton()
		{
			TestTools.ClickUIToolKitButton<Button>(Object.FindObjectOfType<HomeScreenPresenter>().GetComponent<UIDocument>(), "PlayButton");
		}
	
		public static void ClickResultsHome()
		{
			GameObject.Find("UiButtonSlim_Blue_Home").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
		}
		
		public static void ClickDeadScreenLeave()
		{
			GameObject.Find("Leave Button").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
		}

		public static void ClickGameCompleteContinue()
		{
			var gameCompleteScreen = Object.FindObjectOfType<WinnerScreenPresenter>();
			var completeButton = gameCompleteScreen.gameObject.GetComponentInChildren<UnityEngine.UI.Button>();
			completeButton.onClick.Invoke();
		}

		public static void SelectWaterPosition()
		{
			var map = Object.FindObjectOfType<MapSelectionView>();
			map.SelectWaterPosition();
		}

		public static void ClickGameModeSelectionButton()
		{
			TestTools.ClickUIToolKitButton<ImageButton>(Object.FindObjectOfType<HomeScreenPresenter>().GetComponent<UIDocument>(), "GameModeButton");
		}

		public static void ClickCustomGameButton()
		{
			TestTools.ClickUIToolKitButton<Button>(Object.FindObjectOfType<GameModeSelectionPresenter>().GetComponent<UIDocument>(), "CustomGameButton");
		}

		public static void ClickCreateRoom()
		{
			GameObject.Find("Create Room").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
		}
		
		public static void ToggleBots(bool botsOn)
		{
			var botsToggleParent = GameObject.Find("BotsToggle");
			botsToggleParent.GetComponentInChildren<Toggle>().isOn = botsOn;
		}
		
		public static void ClickLockRoomAndPlay()
		{
			GameObject.Find("LockRoomButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
		}
	}
}
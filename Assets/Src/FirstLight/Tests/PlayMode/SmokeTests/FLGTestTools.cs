using System.Collections;
using FirstLight.Game.Presenters;
using FirstLight.Game.Views;
using FirstLight.Game.Views.MainMenuViews;
using FirstLight.UiService;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
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
		
		public static IEnumerator WaitForBRDeadScreenScreen()
		{
			yield return TestTools.UntilObjectOfType<MatchEndScreenPresenter>();
		}
		
		public static IEnumerator WaitForResultsScreen()
		{
			yield return TestTools.UntilObjectOfType<ResultsScreenPresenter>();
		}
#endregion
		
		public static void ClickPlayButton()
		{
			TestTools.ClickUIToolKitButton(Object.FindObjectOfType<HomeScreenPresenter>().GetComponent<UIDocument>(), "PlayButton");
		}
	
		public static void ClickResultsHome()
		{
			GameObject.Find("UiButtonSlim_Blue_Home").GetComponent<Button>().onClick.Invoke();
		}
		
		public static void ClickDeadScreenLeave()
		{
			GameObject.Find("Leave Button").GetComponent<Button>().onClick.Invoke();
		}

		public static void ClickGameCompleteContinue()
		{
			var gameCompleteScreen = Object.FindObjectOfType<WinnerScreenPresenter>();
			var completeButton = gameCompleteScreen.gameObject.GetComponentInChildren<Button>();
			completeButton.onClick.Invoke();
		}

		public static void SelectWaterPosition()
		{
			var map = Object.FindObjectOfType<MapSelectionView>();
			map.SelectWaterPosition();
		}

		public static void ClickGameModeSelectionButton()
		{
			TestTools.ClickUIToolKitImageButton(Object.FindObjectOfType<HomeScreenPresenter>().GetComponent<UIDocument>(), "GameModeButton");
		}

		public static void ClickCustomGameButton()
		{
			TestTools.ClickUIToolKitButton(Object.FindObjectOfType<HomeScreenPresenter>().GetComponent<UIDocument>(), "CustomGameButton");
		}

		public static void ClickCreateRoom()
		{
			GameObject.Find("Create Room").GetComponent<Button>().onClick.Invoke();
		}
		
		public static void ToggleBots(bool botsOn)
		{
			var botsToggleParent = GameObject.Find("BotsToggle");
			botsToggleParent.GetComponentInChildren<Toggle>().isOn = botsOn;
		}
		
		public static void ClickLockRoomAndPlay()
		{
			GameObject.Find("LockRoomButton").GetComponent<Button>().onClick.Invoke();
		}
	}
}
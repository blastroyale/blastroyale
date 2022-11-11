using System.Collections;
using FirstLight.Game.Presenters;
using FirstLight.Game.Views.MainMenuViews;
using FirstLight.Game.Views;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Object = UnityEngine.Object;
using Toggle = UnityEngine.UI.Toggle;

namespace FirstLight.Tests.PlayTests
{
	public class FLGTestTools
	{
#region WAIT_FOR_SCREENS
		public static IEnumerator WaitForMainMenu()
		{
			yield return TestTools.UntilObjectOfType<HomeScreenPresenter>();
		}

		public static IEnumerator WaitForHeroesMenu() {
			yield return TestTools.UntilObjectOfType<PlayerSkinScreenPresenter>();
		}
		
		public static IEnumerator WaitForCustomGameMenu()
		{
			yield return TestTools.UntilObjectOfType<RoomJoinCreateScreenPresenter>();
		}
		
		public static IEnumerator WaitForMatchMakingScreen()
		{
			yield return TestTools.UntilObjectOfType<MatchmakingLoadingScreenPresenter>();
		}
		
		public static IEnumerator WaitForGameCompleteScreen()
		{
			yield return TestTools.UntilObjectOfType<GameCompleteScreenPresenter>();
		}
		
		public static IEnumerator WaitForBRDeadScreenScreen()
		{
			yield return TestTools.UntilObjectOfType<BattleRoyaleDeadScreenPresenter>();
		}
		
		public static IEnumerator WaitForResultsScreen()
		{
			yield return TestTools.UntilObjectOfType<ResultsScreenPresenter>();
		}
	#endregion

	#region CLICK_BUTTONS
		//UNIVERSAL
		//Back Button
		public static void ClickBackButton_OldUI(System.Type scriptType) 
		{
			GameObject go = (GameObject)Object.FindObjectOfType(scriptType);
			go.transform.Find("BackButton").GetComponent<UiButtonView>().onClick.Invoke();
		}

		public static void ClickBackButton_OldUI() {
			GameObject.Find("BackButton").GetComponent<UiButtonView>().onClick.Invoke();
		}

		//Select Button
		public static void ClickSelectButton_OldUI(System.Type scriptType) {
			GameObject go = (GameObject)Object.FindObjectOfType(scriptType);
			go.transform.Find("SelectButton").GetComponent<UiButtonView>().onClick.Invoke();
		}
		public static bool ClickSelectButton_OldUI() {
			var selectButton = GameObject.Find("SelectButton").GetComponent<UiButtonView>();
			if(selectButton.IsActive()) { selectButton.onClick.Invoke(); }
			return selectButton.IsActive();
		}
		///MAIN MENU
		//HERO NAME
		public static void ClickPlayerNameButton()
		{
			//TestTools.ClickUIToolKitButton(Object.FindObjectOfType<HomeScreenPresenter>().GetComponent<UIDocument>(), "?");
		}
		//EQUIPMENT
		public static void ClickEquipmentButton()
		{
			TestTools.ClickUIToolKitButton(Object.FindObjectOfType<HomeScreenPresenter>().GetComponent<UIDocument>(), "EquipmentButton");
		}
		//HEROES
		public static void ClickHeroSelectionButton() 
		{
			TestTools.ClickUIToolKitButton(Object.FindObjectOfType<HomeScreenPresenter>().GetComponent<UIDocument>(), "HeroesButton");
		}

		public static void ClickHeroButton(UiButtonView heroButton) 
		{
			heroButton.onClick.Invoke();
		}

		//LEADERBOARDS
		public static void ClickLeaderboardsButton() 
		{
			TestTools.ClickUIToolKitButton(Object.FindObjectOfType<HomeScreenPresenter>().GetComponent<UIDocument>(), "LeaderboardsButton");
		}
		//BLAST PASS
		public static void ClickBlastPassButton() 
		{
			TestTools.ClickUIToolKitButton(Object.FindObjectOfType<HomeScreenPresenter>().GetComponent<UIDocument>(), "BattlePassButton");
		}
		//DISCORD - Not needed right now (opens external)
		public static void ClickDiscordButton() 
		{
			TestTools.ClickUIToolKitButton(Object.FindObjectOfType<HomeScreenPresenter>().GetComponent<UIDocument>(), "DiscordButton");
		}
		//SETTINGS
		public static void ClickSettingsButton() 
		{
			TestTools.ClickUIToolKitButton(Object.FindObjectOfType<HomeScreenPresenter>().GetComponent<UIDocument>(), "SettingsButton");
		}
		//SHOP
		public static void ClickStoreButton() 
		{
			TestTools.ClickUIToolKitButton(Object.FindObjectOfType<HomeScreenPresenter>().GetComponent<UIDocument>(), "StoreButton");
		}
		//GAMEMODES
		//Gamemode Parent
		public static void ClickSelectGamemodeButton()
		{
			TestTools.ClickUIToolKitButton(Object.FindObjectOfType<HomeScreenPresenter>().GetComponent<UIDocument>(), "GameModeButton");
		}

		//Rotation Gamemode

		//Ranked BR Gamemode

		//Custom Gamemode
		public static void ClickCustomGameButton()
		{
			//TestTools.ClickUIToolKitButton(Object.FindObjectOfType<HomeScreenPresenter>().GetComponent<UIDocument>(), "CustomGameButton");
			//var gmPresenter = Object.FindObjectOfType<GameModeSelectionPresenter>();
			TestTools.ClickUIToolKitButtons(Object.FindObjectOfType<GameModeSelectionPresenter>().GetComponent<UIDocument>(), "GameModeButton");
		}
		//PLAY
		public static void ClickPlayButton()
		{
			TestTools.ClickUIToolKitButton(Object.FindObjectOfType<HomeScreenPresenter>().GetComponent<UIDocument>(), "PlayButton");
		}
	//IN-GAME
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
			var gameCompleteScreen = Object.FindObjectOfType<GameCompleteScreenPresenter>();
			var completeButton = gameCompleteScreen.gameObject.GetComponentInChildren<Button>();
			completeButton.onClick.Invoke();
		}

		public static void ClickCreateRoom()
		{
			GameObject.Find("Create Room").GetComponent<Button>().onClick.Invoke();
		}
		public static void ClickLockRoomAndPlay()
		{
			GameObject.Find("LockRoomButton").GetComponent<Button>().onClick.Invoke();
		}
		public static void ToggleBots(bool botsOn)
		{
			var botsToggleParent = GameObject.Find("BotsToggle");
			botsToggleParent.GetComponentInChildren<Toggle>().isOn = botsOn;
		}
	#endregion
		public static void SelectWaterPosition()
		{
			var map = Object.FindObjectOfType<MapSelectionView>();
			map.SelectWaterPosition();
		}
	}
}
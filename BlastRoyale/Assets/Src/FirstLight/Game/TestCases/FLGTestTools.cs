using System;
using System.Collections;
using FirstLight.Game.Presenters;
using FirstLight.Game.UIElements;
using FirstLight.Game.Views.MatchHudViews;
using FirstLight.UiService;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Toggle = UnityEngine.UI.Toggle;

namespace FirstLight.Game.TestCases
{
	[Obsolete]
	public class FLGTestTools
	{
		// public static IEnumerator WaitForPresenter<T>() where T : UiPresenter
		// {
		// 	yield return TestTools.UntilObjectOfType<T>();
		// }
		//
		// public static void ClickNextButton<T>() where T : UiPresenter
		// {
		// 	TestTools.ClickUIToolKitButton<Button>(Object.FindObjectOfType<T>().GetComponent<UIDocument>(), "NextButton");
		// }
		//
		// public static void ClickLeaveButton<T>() where T : UiPresenter
		// {
		// 	TestTools.ClickUIToolKitButton<Button>(Object.FindObjectOfType<T>().GetComponent<UIDocument>(), "LeaveButton");
		// }
		//
		// public static void ClickPlayButton()
		// {
		// 	TestTools.ClickUIToolKitButton<Button>(Object.FindObjectOfType<HomeScreenPresenter>().GetComponent<UIDocument>(), "PlayButton");
		// }
		//
		// public static void ClickResultsHome()
		// {
		// 	GameObject.Find("UiButtonSlim_Blue_Home").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
		// }
		//
		// public static void ClickDeadScreenLeave()
		// {
		// 	GameObject.Find("Leave Button").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
		// }
		//
		// public static void ClickGameCompleteContinue()
		// {
		// 	var gameCompleteScreen = Object.FindObjectOfType<WinnerScreenPresenter>();
		// 	var completeButton = gameCompleteScreen.gameObject.GetComponentInChildren<UnityEngine.UI.Button>();
		// 	completeButton.onClick.Invoke();
		// }
		//
		// public static void ClickGameModeSelectionButton()
		// {
		// 	TestTools.ClickUIToolKitButton<ImageButton>(Object.FindObjectOfType<HomeScreenPresenter>().GetComponent<UIDocument>(), "GameModeButton");
		// }
		//
		// public static void ClickCustomGameButton()
		// {
		// 	TestTools.ClickUIToolKitButton<Button>(Object.FindObjectOfType<GameModeScreenPresenter>().GetComponent<UIDocument>(), "CustomGameButton");
		// }
		//
		// public static void ClickCreateRoom()
		// {
		// 	GameObject.Find("Create Room").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
		// }
		//
		// public static void ToggleBots(bool botsOn)
		// {
		// 	var botsToggleParent = GameObject.Find("BotsToggle");
		// 	botsToggleParent.GetComponentInChildren<Toggle>().isOn = botsOn;
		// }
		//
		// public static void ClickLockRoomAndPlay()
		// {
		// 	GameObject.Find("LockRoomButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
		// }
		//
		// public static void ClickEquipmentButton()
		// {
		// 	TestTools.ClickUIToolKitButton<Button>(Object.FindObjectOfType<HomeScreenPresenter>().GetComponent<UIDocument>(), "EquipmentButton" );
		// }
		//
		// public static void ClickWeaponCategory()
		// {
		// 	TestTools.ClickUIToolKitButton<Button>(Object.FindObjectOfType<EquipmentPresenter>().GetComponent<UIDocument>(), "WeaponCategory");
		// }
		//
		// public static void ClickHelmetCategory()
		// {
		// 	TestTools.ClickUIToolKitButton<Button>(Object.FindObjectOfType<EquipmentPresenter>().GetComponent<UIDocument>(), "HelmetCategory");
		// }
		//
		// public static void ClickShieldCategory()
		// {
		// 	TestTools.ClickUIToolKitButton<Button>(Object.FindObjectOfType<EquipmentPresenter>().GetComponent<UIDocument>(), "ShieldCategory");
		// }
		//
		// public static void ClickAmuletCategory()
		// {
		// 	TestTools.ClickUIToolKitButton<Button>(Object.FindObjectOfType<EquipmentPresenter>().GetComponent<UIDocument>(), "AmuletCategory");
		// }
		//
		// public static void ClickArmorCategory()
		// {
		// 	TestTools.ClickUIToolKitButton<Button>(Object.FindObjectOfType<EquipmentPresenter>().GetComponent<UIDocument>(), "ArmorCategory");
		// }
		//
		// public static void ClickEquipmentSlot()
		// {
		// 	TestTools.ClickUIToolKitButton<ImageButton>(Object.FindObjectOfType<EquipmentSelectionPresenter>().GetComponent<UIDocument>(), "item-1");
		// }
		//
		// public static void ClickEquipmentSlot2()
		// {
		// 	TestTools.ClickUIToolKitButton<ImageButton>(Object.FindObjectOfType<EquipmentSelectionPresenter>().GetComponent<UIDocument>(), "item-2");
		// }
		//
		// public static void ClickEquipButton()
		// {
		// 	TestTools.ClickUIToolKitButton<Button>(Object.FindObjectOfType<EquipmentSelectionPresenter>().GetComponent<UIDocument>(), "EquipButton");
		// }
		//
		// public static void CLickBackButton() 
		// {
		// 	TestTools.ClickUIToolKitButton<ImageButton>(Object.FindObjectOfType<EquipmentSelectionPresenter>().GetComponent<UIDocument>(), "back");
		// }
		//
		// public static void ClickHomeButton()
		// {
		// 	TestTools.ClickUIToolKitButton<ImageButton>(Object.FindObjectOfType<EquipmentSelectionPresenter>().GetComponent<UIDocument>(), "home");
		// }
	}
}
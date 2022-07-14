using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace FirstLight.Editor.EditorTools
{
	/// <summary>
	/// This editor class helps creating Unity editor shortcuts
	/// </summary>
	public class EditorShortcuts
	{
		[MenuItem("FLG/Scene/Open FTUE Deck Scene &0")]
		private static void OpenFtueDeckScene()
		{
			EditorSceneManager.OpenScene(GetScenePath("FtueDeck"));
		}
		
		[MenuItem("FLG/Scene/Open Main Scene &1")]
		private static void OpenMainScene()
		{
			var list = new List<string>(AssetDatabase.FindAssets("t:scene Main"));
			
			list.Sort();
			
			EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(list[0]));
		}
		
		[MenuItem("FLG/Scene/Open Main Menu Scene &2")]
		private static void OpenMainMenuScene()
		{
			EditorSceneManager.OpenScene(GetScenePath("MainMenu"));
		}
		
		[MenuItem("FLG/Scene/Open FloodCity Scene &3")]
		private static void OpenFloodCityScene()
		{
			EditorSceneManager.OpenScene(GetScenePath("FloodCity"));
		}
		
		[MenuItem("FLG/Scene/Open SmallWilderness Scene &4")]
		private static void OpenSmallWildernessScene()
		{
			EditorSceneManager.OpenScene(GetScenePath("SmallWilderness"));
		}
		
		[MenuItem("FLG/Scene/Open MainDeck Scene &5")]
		private static void OpenMainDeckScene()
		{
			EditorSceneManager.OpenScene(GetScenePath("MainDeck"));
		}
		
		[MenuItem("FLG/Scene/Open BlimpDeck Scene")]
		private static void OpenBlimpDeckScene()
		{
			EditorSceneManager.OpenScene(GetScenePath("BlimpDeck"));
		}
		
		[MenuItem("FLG/Scene/Open FloodCitySimple Scene")]
		private static void OpenFloodCitySimpleScene()
		{
			EditorSceneManager.OpenScene(GetScenePath("FloodCitySimple"));
		}
		
		[MenuItem("FLG/Scene/Open BRGenesis Scene")]
		private static void OpenBRGenesisScene()
		{
			EditorSceneManager.OpenScene(GetScenePath("BRGenesis"));
		}
		
		[MenuItem("FLG/Scene/Open Boot Scene &9")]
		private static void OpenBootScene()
		{
			EditorSceneManager.OpenScene(GetScenePath("Boot"));
		}

		[MenuItem("FLG/Scene/Open Test Scene")]
		private static void OpenTestScene()
		{
			EditorSceneManager.OpenScene(GetScenePath("TestScene"));
		}

#if DEVELOPMENT_BUILD
		[MenuItem("FLG/Cheats/Refill Ammo And Specials %m")]
		private static void RefillAmmoAndSpecials()
		{
			SROptions.Current.RefillAmmoAndSpecials();
		}
		
		[MenuItem("FLG/Cheats/Make Player Super Tough %l")]
		private static void MakePlayerSuperTough()
		{
			SROptions.Current.MakeLocalPlayerSuperTough();
		}
		
		[MenuItem("FLG/Cheats/Skip Tutorial Step %o")]
		private static void SkipTutorialStep()
		{
			SROptions.Current.SkipTutorialStep();
		}
#endif
		private static string GetScenePath(string scene)
		{
			return AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets($"t:scene {scene}")[0]);
		}
	}
}
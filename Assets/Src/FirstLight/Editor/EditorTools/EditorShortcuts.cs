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
		[MenuItem("First Light Games/Scene/Open FTUE Deck Scene &0")]
		private static void OpenFtueDeckScene()
		{
			EditorSceneManager.OpenScene(GetScenePath("FtueDeck"));
		}
		
		[MenuItem("First Light Games/Scene/Open Main Scene &1")]
		private static void OpenMainScene()
		{
			var list = new List<string>(AssetDatabase.FindAssets("t:scene Main"));
			
			list.Sort();
			
			EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(list[0]));
		}
		
		[MenuItem("First Light Games/Scene/Open Main Menu Scene &2")]
		private static void OpenMainMenuScene()
		{
			EditorSceneManager.OpenScene(GetScenePath("MainMenu"));
		}
		
		[MenuItem("First Light Games/Scene/Open FloodCity Scene &3")]
		private static void OpenFloodCityScene()
		{
			EditorSceneManager.OpenScene(GetScenePath("FloodCity"));
		}
		
		[MenuItem("First Light Games/Scene/Open SmallWilderness Scene &4")]
		private static void OpenSmallWildernessScene()
		{
			EditorSceneManager.OpenScene(GetScenePath("SmallWilderness"));
		}
		
		[MenuItem("First Light Games/Scene/Open MainDeck Scene &5")]
		private static void OpenMainDeckScene()
		{
			EditorSceneManager.OpenScene(GetScenePath("MainDeck"));
		}
		
		[MenuItem("First Light Games/Scene/Open BlimpDeck Scene")]
		private static void OpenBlimpDeckScene()
		{
			EditorSceneManager.OpenScene(GetScenePath("BlimpDeck"));
		}
		
		[MenuItem("First Light Games/Scene/Open FloodCitySimple Scene")]
		private static void OpenFloodCitySimpleScene()
		{
			EditorSceneManager.OpenScene(GetScenePath("FloodCitySimple"));
		}
		
		[MenuItem("First Light Games/Scene/Open BRGenesis Scene")]
		private static void OpenBRGenesisScene()
		{
			EditorSceneManager.OpenScene(GetScenePath("BRGenesis"));
		}
		
		[MenuItem("First Light Games/Scene/Open Boot Scene &9")]
		private static void OpenBootScene()
		{
			EditorSceneManager.OpenScene(GetScenePath("Boot"));
		}

		[MenuItem("First Light Games/Scene/Open Test Scene")]
		private static void OpenTestScene()
		{
			EditorSceneManager.OpenScene(GetScenePath("TestScene"));
		}

#if DEVELOPMENT_BUILD
		[MenuItem("First Light Games/Cheats/Make Player Big Damager %m")]
		private static void MakePlayerBigDamager()
		{
			SROptions.Current.MakeLocalPlayerBigDamager();
		}
		
		[MenuItem("First Light Games/Cheats/Make Player Super Tough %l")]
		private static void MakePlayerSuperTough()
		{
			SROptions.Current.MakeLocalPlayerSuperTough();
		}
		
		[MenuItem("First Light Games/Cheats/Skip Tutorial Step %o")]
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
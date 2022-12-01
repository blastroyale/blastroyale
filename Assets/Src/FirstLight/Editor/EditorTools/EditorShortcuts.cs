using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

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

		[MenuItem("FLG/Art/Merge Colliders %#m")]
		private static void MergeColliders()
		{
			var go = Selection.activeGameObject;
			if (go == null)
			{
				Debug.LogError("Select a Game Object first!");
				return;
			}

			var boxColliders = go.GetComponentsInChildren<BoxCollider>();
			var sphereColliders = go.GetComponentsInChildren<SphereCollider>();
			var capsuleColliders = go.GetComponentsInChildren<CapsuleCollider>();

			var rootPos = go.transform.position;

			Undo.RegisterCompleteObjectUndo(go.GetComponentsInChildren<Collider>().Cast<UnityEngine.Object>().Append(go).ToArray(), "Merge Colliders");

			foreach (var bc in boxColliders)
			{
				var elementPos = bc.gameObject.transform.position;
				var localToRoot = elementPos - rootPos;

				var rootBc = Undo.AddComponent<BoxCollider>(go);
				rootBc.center = localToRoot + bc.center;
				rootBc.size = bc.size;
				rootBc.isTrigger = bc.isTrigger;

				if (bc.GetComponents(typeof(Component)).Length == 2 && go.transform.childCount == 0)
				{
					Undo.DestroyObjectImmediate(bc.gameObject);
				}
				else
				{
					Undo.DestroyObjectImmediate(bc);
				}
			}

			foreach (var sc in sphereColliders)
			{
				var elementPos = sc.gameObject.transform.position;
				var localToRoot = elementPos - rootPos;

				var rootSc = Undo.AddComponent<SphereCollider>(go);
				rootSc.center = localToRoot + sc.center;
				rootSc.radius = sc.radius;
				rootSc.isTrigger = sc.isTrigger;

				if (sc.GetComponents(typeof(Component)).Length == 2 && go.transform.childCount == 0)
				{
					Undo.DestroyObjectImmediate(sc.gameObject);
				}
				else
				{
					Undo.DestroyObjectImmediate(sc);
				}
			}

			foreach (var cc in capsuleColliders)
			{
				var elementPos = cc.gameObject.transform.position;
				var localToRoot = elementPos - rootPos;

				var rootCc = Undo.AddComponent<CapsuleCollider>(go);
				rootCc.center = localToRoot + cc.center;
				rootCc.radius = cc.radius;
				rootCc.height = cc.height;
				rootCc.direction = cc.direction;
				rootCc.isTrigger = cc.isTrigger;

				if (cc.GetComponents(typeof(Component)).Length == 2 && go.transform.childCount == 0)
				{
					Undo.DestroyObjectImmediate(cc.gameObject);
				}
				else
				{
					Undo.DestroyObjectImmediate(cc);
				}
			}
		}
	}
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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

			Undo.RegisterCompleteObjectUndo(
				go.GetComponentsInChildren<Collider>().Cast<UnityEngine.Object>().Append(go).ToArray(),
				"Merge Colliders");

			foreach (var bc in boxColliders)
			{
				var elementPos = bc.gameObject.transform.position;
				var localToRoot = elementPos - rootPos;

				var rootBc = Undo.AddComponent<BoxCollider>(go);
				rootBc.center = localToRoot + bc.center;
				rootBc.size = bc.size;
				rootBc.isTrigger = bc.isTrigger;

				if (bc.GetComponents(typeof(Component)).Length == 2 && bc.transform.childCount == 0)
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

				if (sc.GetComponents(typeof(Component)).Length == 2 && sc.transform.childCount == 0)
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

				if (cc.GetComponents(typeof(Component)).Length == 2 && cc.transform.childCount == 0)
				{
					Undo.DestroyObjectImmediate(cc.gameObject);
				}
				else
				{
					Undo.DestroyObjectImmediate(cc);
				}
			}
		}

		[MenuItem("FLG/Art/Unmerge Colliders %#u")]
		private static void UnmergeColliders()
		{
			var go = Selection.activeGameObject;
			if (go == null)
			{
				Debug.LogError("Select a Game Object first!");
				return;
			}

			var boxColliders = go.GetComponents<BoxCollider>();
			var sphereColliders = go.GetComponents<SphereCollider>();
			var capsuleColliders = go.GetComponents<CapsuleCollider>();

			Undo.RegisterCompleteObjectUndo(go, "Unmerge Colliders");

			for (var i = 0; i < boxColliders.Length; i++)
			{
				var bc = boxColliders[i];

				var childGo = new GameObject($"BoxCollider{i}");
				Undo.RegisterCreatedObjectUndo(childGo, "");

				childGo.transform.SetParent(go.transform);
				childGo.transform.localPosition = bc.center;

				var boxCollider = Undo.AddComponent<BoxCollider>(childGo);
				boxCollider.size = bc.size;
				boxCollider.isTrigger = bc.isTrigger;

				Undo.DestroyObjectImmediate(bc);
			}

			for (var i = 0; i < sphereColliders.Length; i++)
			{
				var sc = sphereColliders[i];

				var childGo = new GameObject($"SphereCollider{i}");
				Undo.RegisterCreatedObjectUndo(childGo, "");

				childGo.transform.SetParent(go.transform);
				childGo.transform.localPosition = sc.center;

				var sphereCollider = Undo.AddComponent<SphereCollider>(childGo);
				sphereCollider.radius = sc.radius;
				sphereCollider.isTrigger = sc.isTrigger;

				Undo.DestroyObjectImmediate(sc);
			}

			for (var i = 0; i < capsuleColliders.Length; i++)
			{
				var cc = capsuleColliders[i];

				var childGo = new GameObject($"CapsuleCollider{i}");
				Undo.RegisterCreatedObjectUndo(childGo, "");

				childGo.transform.SetParent(go.transform);
				childGo.transform.localPosition = cc.center;

				var capsuleCollider = Undo.AddComponent<CapsuleCollider>(childGo);
				capsuleCollider.radius = cc.radius;
				capsuleCollider.direction = cc.direction;
				capsuleCollider.height = cc.height;
				capsuleCollider.isTrigger = cc.isTrigger;

				Undo.DestroyObjectImmediate(cc);
			}
		}

		[MenuItem("FLG/Generate Sprite USS")]
		private static void GenerateSpriteUss()
		{
			const string SPRITES_FOLDER = "Assets/Art/UI/Sprites/";
			const string STYLES_FOLDER = "Assets/Art/UI/Styles/";

			foreach (var grouping in AssetDatabase.GetAllAssetPaths()
						 .OrderBy(s => s)
						 .Where(path =>
							 path.StartsWith(SPRITES_FOLDER) && !Directory.Exists(path) &&
							 AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(Texture2D) &&
							 !path.Contains("SpritesOld"))
						 .Select(s =>
						 {
							 Debug.Log(
								 $"Path: {s} type: {AssetDatabase.GetMainAssetTypeAtPath(s) == typeof(Texture2D)}");
							 return s;
						 })
						 .GroupBy(str => str.Split(Path.DirectorySeparatorChar)[4]))
			{
				Debug.Log($"Generating USS: {grouping.Key}");
				var uss = GenerateSpriteUss(grouping);
				Debug.Log(uss);

				var stylePathRelative = STYLES_FOLDER + $"Sprites-{grouping.Key}.uss";
				var stylePathAbsolute = Application.dataPath.Replace("Assets", "") + stylePathRelative;

				Debug.Log($"Writing USS: {grouping.Key} to file '{stylePathRelative}'");

				File.WriteAllText(stylePathAbsolute, uss);
				AssetDatabase.ImportAsset(stylePathRelative);
				Debug.Log($"USS processed: {grouping.Key}");
			}

			Debug.Log($"Sprite USS generation finished.");
		}

		private static string GenerateSpriteUss(IGrouping<string, string> arg)
		{
			var sb = new StringBuilder();

			var names = new HashSet<string>();

			// Generate variables
			sb.AppendLine("/* AUTO GENERATED */");
			sb.AppendLine(":root {");
			foreach (var path in arg)
			{
				var name = GenerateSpriteVar(arg.Key, path, true);

				if (names.Contains(name))
				{
					throw new NotSupportedException($"Found duplicate sprite name in {arg.Key}: {name}");
				}

				names.Add(name);

				sb.AppendLine("    " + GenerateSpriteVar(arg.Key, path, true));
			}

			sb.AppendLine("}");
			sb.AppendLine();

			// Generate classes
			foreach (var path in arg)
			{
				var filename = Path.GetFileNameWithoutExtension(path);

				// Pressed versions get the :active pseudo class
				if (filename.EndsWith("-pressed"))
				{
					sb.AppendLine(
						$".sprite-{arg.Key.ToLowerInvariant()}__{filename.Replace("-pressed", "")}:active {{");
					sb.AppendLine($"    background-image: var({GenerateSpriteVar(arg.Key, path, false)})");
					sb.AppendLine("}");
				}
				else
				{
					sb.AppendLine($".sprite-{arg.Key.ToLowerInvariant()}__{Path.GetFileNameWithoutExtension(path)} {{");
					sb.AppendLine($"    background-image: var({GenerateSpriteVar(arg.Key, path, false)})");
					sb.AppendLine("}");
				}

				sb.AppendLine();
			}

			return sb.ToString();
		}

		private static string GenerateSpriteVar(string atlas, string path, bool full)
		{
			return $"--sprite-{atlas.ToLowerInvariant()}__{Path.GetFileNameWithoutExtension(path)}" +
				(full ? $": url('/{path}');" : "");
		}
	}
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Server.SDK.Modules;
using FirstLight.Services;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FirstLight.Editor.EditorTools
{
	/// <summary>
	/// This editor class helps creating Unity editor shortcuts
	/// </summary>
	public class EditorShortcuts
	{
		[MenuItem("FLG/Scene/Open FTUE Deck Scene")]
		private static void OpenFtueDeckScene()
		{
			EditorSceneManager.OpenScene(GetScenePath("FtueDeck"));
		}

		[MenuItem("FLG/Build/Quantum")]
		public static void BuildQuantum()
		{
			var progressId = Progress.Start("Building Quantum", "Builds Quantum as Debug using msbuild.",
				Progress.Options.Indefinite | Progress.Options.Unmanaged);

			var fileName = EditorPrefs.GetString(FLGSettingsRegister.KEY_MSBUILD);
			if (string.IsNullOrEmpty(fileName))
			{
				Debug.LogWarning("Please set MSBUILD and optional PATH values in the settings under FLG");
				return;
			}

			var startInfo = new ProcessStartInfo
			{
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				FileName = fileName,
				Arguments = "./Quantum/quantum_code/quantum_code.sln -restore -p:Configuration=Debug -p:RestorePackagesConfig=true",
				CreateNoWindow = true,
				WorkingDirectory = Application.dataPath.Replace("/Assets", "")
			};

			startInfo.EnvironmentVariables["PATH"] =
				$"{startInfo.EnvironmentVariables["PATH"]}:{EditorPrefs.GetString(FLGSettingsRegister.KEY_CUSTOM_PATH)}";

			var p = new Process {StartInfo = startInfo, EnableRaisingEvents = true};

			p.OutputDataReceived += (sender, args) =>
			{
				if (args.Data == null) return;
				Progress.Report(progressId, 0, args.Data);
			};

			p.ErrorDataReceived += (sender, args) =>
			{
				if (args.Data == null) return;
				Debug.LogError(args.Data);
			};

			p.Exited += (_, _) =>
			{
				var exitCode = p.ExitCode;

				if (exitCode == 0)
				{
					Progress.Finish(progressId);
				}
				else
				{
					Progress.Finish(progressId, Progress.Status.Failed);
				}
			};

			try
			{
				p.Start();
				p.BeginOutputReadLine();
				p.BeginErrorReadLine();
			}
			catch (Exception e)
			{
				Debug.LogError("Error building Quantum: " + e.Message);
				Progress.Finish(progressId, Progress.Status.Failed);
			}
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
		private static void SkipTutorialSection()
		{
			SROptions.Current.SkipTutorialSection();
		}

		[MenuItem("FLG/Backend/Netcode/Simulate Disconnection")]
		private static void SimulateDisconnection()
		{
			var client = MainInstaller.Resolve<IGameServices>().NetworkService.QuantumClient;
			client.LoadBalancingPeer.NetworkSimulationSettings.IncomingLossPercentage = 100;
			client.LoadBalancingPeer.NetworkSimulationSettings.OutgoingLossPercentage = 100;
			client.LoadBalancingPeer.IsSimulationEnabled = true;
		}

		[MenuItem("FLG/Backend/Netcode/Simulate Lag")]
		private static void SimulateLag()
		{
			var client = MainInstaller.Resolve<IGameServices>().NetworkService.QuantumClient;
			client.LoadBalancingPeer.NetworkSimulationSettings.IncomingLossPercentage = 3;
			client.LoadBalancingPeer.NetworkSimulationSettings.OutgoingLossPercentage = 3;
			client.LoadBalancingPeer.NetworkSimulationSettings.OutgoingLag = 100;
			client.LoadBalancingPeer.NetworkSimulationSettings.IncomingLag = 100;
			client.LoadBalancingPeer.NetworkSimulationSettings.IncomingJitter = 10;
			client.LoadBalancingPeer.NetworkSimulationSettings.OutgoingJitter = 10;
			client.LoadBalancingPeer.IsSimulationEnabled = true;
		}

		[MenuItem("FLG/Backend/Netcode/Normal Internet")]
		private static void Normal()
		{
			var client = MainInstaller.Resolve<IGameServices>().NetworkService.QuantumClient;
			client.LoadBalancingPeer.IsSimulationEnabled = false;
		}

		[MenuItem("FLG/Cheats/Disconnect")]
		private static void Disconnect()
		{
			var client = MainInstaller.Resolve<IGameServices>().NetworkService.QuantumClient;
			client.Disconnect();
		}
#endif
		private static string GetScenePath(string scene)
		{
			return AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets($"t:scene {scene}")[0]);
		}

		[MenuItem("FLG/Art/Copy to box Colliders %#m")]
		private static void CopyToBoxColliders()
		{
			var go = Selection.activeGameObject;
			if (go == null)
			{
				Debug.LogError("Select a Game Object first!");
				return;
			}

			BoxCollider[] childColliders = go.GetComponentsInChildren<BoxCollider>();

			foreach (BoxCollider childCollider in childColliders)
			{
				// Skip if the child collider is a trigger
				if (childCollider.isTrigger)
					continue;

				// Get the position, rotation, and scale of the child collider
				Vector3 position = childCollider.transform.position;
				Vector3 scale = childCollider.transform.localScale;

				// Create a new BoxCollider on the target object
				BoxCollider newCollider = go.AddComponent<BoxCollider>();

				// Set the position, rotation, and scale of the new collider
				newCollider.center = position;
				newCollider.size = scale;

				// Adds a Quantum Box collider to the object 
				QuantumStaticBoxCollider3D newQuantumCollider = go.AddComponent<QuantumStaticBoxCollider3D>();
				newQuantumCollider.SourceCollider = newCollider;
			}
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

		public static Dictionary<string, string> GetAllGeneratedClassNames()
		{
			return GetSpritesGroupedByDirectory(false)
				.SelectMany(GetGeneratedClasses)
				.ToDictionary(k => k.Key, v => v.Value);
		}

		[MenuItem("FLG/Generators/Generate Sprite USS")]
		private static void GenerateSpriteUss()
		{
			const string STYLES_FOLDER = "Assets/Art/UI/Styles/";

			foreach (var grouping in GetSpritesGroupedByDirectory())
			{
				Debug.Log($"Generating USS: {grouping.Key}");
				var uss = GenerateSpriteUss(grouping);
				Debug.Log(uss);

				var stylePathRelative = STYLES_FOLDER + $"Sprites-{GetCleanAtlasName(grouping.Key, false)}.uss";
				var stylePathAbsolute = Application.dataPath.Replace("Assets", "") + stylePathRelative;

				Debug.Log($"Writing USS: {grouping.Key} to file '{stylePathRelative}'");

				File.WriteAllText(stylePathAbsolute, uss);
				AssetDatabase.ImportAsset(stylePathRelative);
				Debug.Log($"USS processed: {grouping.Key}");
			}

			EditorUtility.UnloadUnusedAssetsImmediate();
			Debug.Log($"Sprite USS generation finished.");
		}

		private static IEnumerable<IGrouping<string, string>> GetSpritesGroupedByDirectory(bool log = true)
		{
			const string SPRITES_FOLDER = "Assets/Art/UI/Sprites/";

			return AssetDatabase.GetAllAssetPaths()
				.OrderBy(s => s)
				.Where(path =>
					path.StartsWith(SPRITES_FOLDER) && !Directory.Exists(path) &&
					AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(Texture2D))
				.Select(s =>
				{
					if (log)
						Debug.Log(
							$"Path: {s} type: {AssetDatabase.GetMainAssetTypeAtPath(s) == typeof(Texture2D)}");
					return s;
				})
				.GroupBy(str => str.Split('/')[4]);
		}

		private static Dictionary<string, string> GetGeneratedClasses(IGrouping<string, string> arg)
		{
			var names = new Dictionary<string, string>();

			// Generate classes
			foreach (var path in arg)
			{
				names[path] = $"sprite-{GetCleanAtlasName(arg.Key)}__{Path.GetFileNameWithoutExtension(path)}";
			}

			return names;
		}

		public static string GetClassForSprite(string path)
		{
			var folder = path.Split('/')[4];
			return $"sprite-{GetCleanAtlasName(folder)}__{Path.GetFileNameWithoutExtension(path)}";
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
						$".sprite-{GetCleanAtlasName(arg.Key)}__{filename.Replace("-pressed", "")}:active {{");
					sb.AppendLine($"    background-image: var({GenerateSpriteVar(arg.Key, path, false)});");
					sb.AppendLine("}");
				}
				else
				{
					var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
					if (sprite == null)
					{
						throw new NotSupportedException($"Found a file that isn't a sprite: {path}");
					}

					sb.AppendLine($".sprite-{GetCleanAtlasName(arg.Key)}__{Path.GetFileNameWithoutExtension(path)} {{");
					sb.AppendLine($"    background-image: var({GenerateSpriteVar(arg.Key, path, false)});");
					sb.AppendLine($"    width: {sprite.texture.width}px;");
					sb.AppendLine($"    height: {sprite.texture.height}px;");
					sb.AppendLine("}");
				}

				sb.AppendLine();
			}

			return sb.ToString();
		}


		private static string GenerateSpriteVar(string atlas, string path, bool full)
		{
			return $"--sprite-{GetCleanAtlasName(atlas)}__{Path.GetFileNameWithoutExtension(path)}" +
				(full ? $": url('/{path}');" : "");
		}

		private static string GetCleanAtlasName(string atlas, bool lowercase = true)
		{
			if (lowercase)
			{
				atlas = atlas.ToLowerInvariant();
			}

			while (atlas.Contains(" "))
			{
				atlas = atlas.Replace(" ", string.Empty);
			}

			return atlas;
		}

		[MenuItem("FLG/Print App Data")]
		private static void PrintAppData()
		{
			var srv = new DataService();
			var data = srv.LoadData<AppData>();
			EditorUtility.DisplayDialog("Data", ModelSerializer.PrettySerialize(data), "Ok");
			FLog.Info(ModelSerializer.PrettySerialize(data));
		}
	}
}
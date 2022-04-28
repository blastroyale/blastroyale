using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.Build
{
	/// <summary>
	/// Set the internal version in any VersionService instances in the project before building
	/// and whenever the project loads.
	/// </summary>
	public static class VersionEditorUtils
	{
		private const int ShortenedCommitLength = 8;
		
		/// <summary>
		/// Set the internal version before building the app.
		/// </summary>
		public static void SetAndSaveInternalVersion()
		{
			var newVersionData = GenerateInternalVersionSuffix();
			var newVersionDataSerialized = JsonUtility.ToJson(newVersionData);
			var oldVersionDataSerialized = VersionEditorUtils.LoadVersionDataSerializedSync();
			if (newVersionDataSerialized.Equals(oldVersionDataSerialized, StringComparison.Ordinal))
			{
				return;
			}
			
			Debug.Log($"Saving new version data: {newVersionDataSerialized}");
			SaveVersionData(newVersionDataSerialized);
		}

		/// <summary>
		/// Loads the game version saved in disk into string format
		/// </summary>
		public static string LoadVersionDataSerializedSync ()
		{
			var textAsset = Resources.Load<TextAsset>(VersionUtils.VersionDataFilename);
			if (!textAsset)
			{
				Debug.LogError("Could not load internal version from Resources.");
				return string.Empty;
			}

			var serialized = textAsset.text;
			Resources.UnloadAsset(textAsset);
			return serialized;
		}

		/// <summary>
		/// Set the internal version for when the app plays in editor.
		/// </summary>
		[InitializeOnLoadMethod]
		private static void OnEditorLoad()
		{
			SetAndSaveInternalVersion();
		}

		private static VersionUtils.VersionData GenerateInternalVersionSuffix()
		{
			var data = new VersionUtils.VersionData();

			data.DayNumber = GetDayNumber();
			
			using (var repo = new GitProcess(Application.dataPath))
			{
				try
				{
					if (!repo.IsValidRepo())
					{
						Debug.LogWarning("Project is not a git repo. Internal version not set.");
					}
					else
					{
						var branch = repo.GetBranch();
						if (string.IsNullOrEmpty(branch))
						{
							Debug.LogWarning("Could not get git branch for internal version");
						}
						else
						{
							data.BranchName = branch;
						}
							
						var commitHash = repo.GetCommitHash();
						if (string.IsNullOrEmpty(commitHash))
						{
							Debug.LogWarning("Could not get git commit for internal version");
						}
						else
						{
							data.Commit = commitHash.Substring(0, ShortenedCommitLength);
						}
					}
				}
				catch (Exception e)
				{
					Debug.LogException(e);
					Debug.LogWarning("Could not execute git commands. Internal version not set.");
				}
			}
			
			data.BuildType = string.Empty;
			#if DEVELOPMENT_BUILD
			data.BuildType = "dev";
			#elif OPTIMIZE_BUILD
			data.BuildType = "opt";
			#endif

			return data;
		}

		private static int GetDayNumber()
		{
			var jan1st2020 = new DateTime(2020, 1, 1);
			var delta = DateTime.UtcNow - jan1st2020;
			var daysSinceJan1st2020 = (int) delta.TotalDays;
			return daysSinceJan1st2020;
		}
		
		/// <summary>
		/// If unity has started in batch mode try and set the version code of the build
		/// </summary>
		public static void TrySetBuildNumberFromCommandLineArgs(string[] args)
		{
			if (!FirstLightBuildUtil.TryGetBuildNumberFromCommandLineArgs(out var buildNumber, args))
			{
				return;
			}

			PlayerSettings.Android.bundleVersionCode = buildNumber;
			PlayerSettings.iOS.buildNumber = buildNumber.ToString();
		}
		
		/// <summary>
		/// Set the internal version of this application and save it in resources. This should be
		/// called at edit/build time.
		/// </summary>
		private static void SaveVersionData(string serializedData)
		{
			const string assets = "Assets";
			const string resources = "Build/Resources";
			
			var absDirPath = Path.Combine(Application.dataPath, resources);
			if (!Directory.Exists(absDirPath))
			{
				Directory.CreateDirectory(absDirPath);
			}

			// delete old file with incorrect extension
			const string assetExtension = ".asset";
			var absFilePath = Path.Combine(absDirPath, VersionUtils.VersionDataFilename);
			if (File.Exists(Path.ChangeExtension(absFilePath, assetExtension)))
			{
				AssetDatabase.DeleteAsset(
					Path.Combine(assets, resources,
						Path.ChangeExtension(VersionUtils.VersionDataFilename, assetExtension)));
			}
			
			// create new text file
			const string textExtension = ".txt";
			File.WriteAllText(Path.ChangeExtension(absFilePath, textExtension), serializedData);
			
			AssetDatabase.ImportAsset(
				Path.Combine(assets, resources,
					Path.ChangeExtension(VersionUtils.VersionDataFilename, textExtension)));
		}
	}
}
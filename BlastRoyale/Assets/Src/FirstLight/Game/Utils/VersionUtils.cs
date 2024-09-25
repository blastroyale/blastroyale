using System;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.TestCases;
using UnityEngine;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Utility methods to get the version of the app.
	/// </summary>
	public static class VersionUtils
	{
		public const string VersionDataFilename = "version-data";

		/// <summary>
		/// Official application version only (M.m.p)
		/// </summary>
		public static string VersionExternal => Application.version;

		/// <summary>
		/// Internal version (M.m.p-b.branch.commit)
		/// </summary>
		public static string VersionInternal => IsLoaded()
			? FormatInternalVersion(_versionData)
			: Application.version;

		/// <summary>
		/// Name of the git branch that this app was built from.
		/// </summary>
		public static string Branch => IsLoaded() ? _versionData.BranchName : string.Empty;

		/// <summary>
		/// Logic Server Commit Hash
		/// </summary>
		public static string ServerBuildCommit { get; set; } = "n/a";

		/// <summary>
		/// Logic server build number
		/// </summary>
		public static string ServerBuildNumber { get; set; } = "n/a";

		/// <summary>
		/// Short hash of the commit this app was built from.
		/// </summary>
		public static string Commit => IsLoaded() ? _versionData.Commit : string.Empty;

		/// <summary>
		/// Build number for this build of the app.
		/// </summary>
		public static string BuildNumber => IsLoaded() ? _versionData.BuildNumber : string.Empty;

		private static VersionData _versionData;
		private static bool _loaded = false;

		/// <summary>
		/// Load the internal version string from resources async. Should be called once when the
		/// app is started.
		/// </summary>
		public static async UniTask LoadVersionDataAsync()
		{
			if (_loaded) return;
			var source = new TaskCompletionSource<TextAsset>();
			var request = Resources.LoadAsync<TextAsset>(VersionDataFilename);

			request.completed += _ => source.SetResult(request.asset as TextAsset);

			var textAsset = await source.Task.AsUniTask();
			LoadVersionData(textAsset);
			Resources.UnloadAsset(textAsset);
		}
		
		public static void LoadVersionData()
		{
			if (_loaded) return;
			var textAsset = Resources.Load<TextAsset>(VersionDataFilename);
			LoadVersionData(textAsset);
			Resources.UnloadAsset(textAsset);
		}

		private static void LoadVersionData(TextAsset textAsset)
		{
			if (!textAsset)
			{
				Debug.LogError("Could not async load version data from Resources.");
				_loaded = false;
				return;
			}

			_versionData = JsonUtility.FromJson<VersionData>(textAsset.text);
			_loaded = true;

#if !DISABLE_SRDEBUGGER
			if (Debug.isDebugBuild && SRDebug.Instance != null)
			{
				SRDebug.Instance.AddSystemInfo(SRDebugger.InfoEntry.Create("Version", VersionUtils.VersionInternal), "Game");
			}
#endif
		}

		/// <summary>
		/// Requests to check if the provided version is newer compared to the local app version
		/// </summary>
		public static bool IsOutdatedVersion(string version)
		{
			var appVersion = VersionExternal.Split('.');
			var otherVersion = version.Split('.');

			var majorApp = int.Parse(appVersion[0]);
			var majorOther = int.Parse(otherVersion[0]);

			var minorApp = int.Parse(appVersion[1]);
			var minorOther = int.Parse(otherVersion[1]);

			var patchApp = int.Parse(appVersion[2]);
			var patchOther = int.Parse(otherVersion[2]);

			if (majorApp != majorOther)
			{
				return majorOther > majorApp;
			}

			if (minorApp != minorOther)
			{
				return minorOther > minorApp;
			}

			return patchOther > patchApp;
		}

		/// <summary>
		/// Formats VersionData into the long internal version string for the app.
		/// </summary>
		public static string FormatInternalVersion(VersionData data)
		{
			string version = $"{Application.version}-{data.BuildNumber}.{data.BranchName}.{data.Commit}";

			if (!string.IsNullOrEmpty(FLEnvironment.Current.Name))
			{
				version += $".{FLEnvironment.Current.Name}";
			}

			return version;
		}

		private static bool IsLoaded()
		{
			return _loaded ? true : throw new Exception("Version Data not loaded.");
		}

		[Serializable]
		public struct VersionData
		{
			public string Commit;
			public string BranchName;
			public string Environment;
			public string BuildNumber;
		}

		public static void ValidateServer()
		{
			FLog.Info("Server commit: " + ServerBuildCommit);
			FLog.Info("Client commit: " + Commit);
#if !UNITY_EDITOR
			if (IsOutOfSync() && !FLGTestRunner.Instance.IsRunning())
			{
				FLog.Warn("Mismatch server and client commits, desyncs may occur!");
			}
#endif
		}

		public static bool IsOutOfSync()
		{
			// The game doesn't have the full hash
			return !ServerBuildCommit.StartsWith(Commit);
		}
	}
}
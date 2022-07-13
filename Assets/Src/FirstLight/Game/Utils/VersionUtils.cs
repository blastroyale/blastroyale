using System;
using System.Text;
using System.Threading.Tasks;
using SRDebugger;
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
		public static async Task LoadVersionDataAsync()
		{
			var source = new TaskCompletionSource<TextAsset>();
			var request = Resources.LoadAsync<TextAsset>(VersionDataFilename);
			
			request.completed += operation => source.SetResult(request.asset as TextAsset);
			
			var textAsset = await source.Task;
			
			if (!textAsset)
			{
				Debug.LogError("Could not async load version data from Resources.");
				_loaded = false;
				return;
			}
			
			_versionData = JsonUtility.FromJson<VersionData>(textAsset.text);
			_loaded = true;
			
			if (Debug.isDebugBuild && SRDebug.Instance != null)
			{
				SRDebug.Instance.AddSystemInfo(InfoEntry.Create("Version", VersionUtils.VersionInternal), "Game");
			}
			
			Resources.UnloadAsset(textAsset);
		}

		/// <summary>
		/// Formats VersionData into the long internal version string for the app.
		/// </summary>
		public static string FormatInternalVersion(VersionData data)
		{
			string version = $"{Application.version}-{data.BuildNumber}.{data.BranchName}.{data.Commit}";

			if (!string.IsNullOrEmpty(data.BuildType))
			{
				version += $".{data.BuildType}";
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
			public string BuildType;
			public string BuildNumber;
		}
	}
}
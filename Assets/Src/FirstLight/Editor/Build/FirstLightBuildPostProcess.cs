using System.Collections.Generic;
using System.IO;
using System.Text;
using FirstLight.Game.Utils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FirstLight.Editor.Build
{
	/// <summary>
	/// Stuff we want to do AFTER every build.
	/// </summary>
	public class FirstLightBuildPostProcess : IPostprocessBuildWithReport
	{
		public int callbackOrder => 1000;

		/// <inheritdoc />
		public void OnPostprocessBuild(BuildReport report)
		{
			Debug.Log("FirstLightBuildPostProcess.OnPostprocessBuild Executing");
			WriteBuildPropertiesFile();
		}


		/// <summary>
		/// Write a properties file containing useful information that an external process
		/// (jenkins build job) can use.
		/// </summary>
		private static void WriteBuildPropertiesFile()
		{
			const string fileName = "build-output.properties";

			var lines = new List<string>();
			var serializedVersionData = VersionEditorUtils.LoadVersionDataSerializedSync();
			var versionData = JsonUtility.FromJson<VersionUtils.VersionData>(serializedVersionData);
			var internalVersion = VersionUtils.FormatInternalVersion(versionData);
			var internalVersionFilename = new StringBuilder(internalVersion);
			var invalidChars = Path.GetInvalidFileNameChars();
			var appIdentifier = PlayerSettings.applicationIdentifier;
			var split = appIdentifier.Split('.');
			var shortAppIdentifier = split[split.Length - 1];
			var androidVersionCode = PlayerSettings.Android.bundleVersionCode;
			var iOSVersionCode = PlayerSettings.iOS.buildNumber;
			var obbName = $"main.{androidVersionCode.ToString()}.{appIdentifier}.obb";
			var filePath = Path.Combine(Application.dataPath, "..", fileName);

			internalVersionFilename.Replace('/', '_');
			internalVersionFilename.Replace('\\', '_');

			foreach (var invalidChar in invalidChars)
			{
				internalVersionFilename.Replace(invalidChar, '_');
			}

			lines.Add($"FL_EXTERNAL_VERSION={PlayerSettings.bundleVersion}");
			lines.Add($"FL_INTERNAL_VERSION={internalVersion}");
			lines.Add($"FL_INTERNAL_VERSION_FILENAME={internalVersionFilename}");
			lines.Add($"FL_APP_ID={appIdentifier}");
			lines.Add($"FL_APP_ID_SHORT={shortAppIdentifier}");
			lines.Add($"FL_ANDROID_VERSION_CODE={androidVersionCode.ToString()}");
			lines.Add($"FL_OBB_NAME={obbName}");
			lines.Add($"FL_IOS_VERSION_CODE={iOSVersionCode}");

			Debug.Log($"Writing build properties file: {filePath}");
			Debug.Log(string.Join("\n", lines));

			File.WriteAllLines(filePath, lines, Encoding.ASCII);
		}
	}
}
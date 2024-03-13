#if UNITY_IOS

using System.IO;
using I2.Loc;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;


namespace FirstLight.Editor.Build
{
	/// <summary>
	/// Post build step to modify permissions to add tracking data permission on iOS app
	/// </summary>
	public class IosPostBuild
	{
		[PostProcessBuild(999)]
		public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToXcode)
		{
			if (buildTarget == BuildTarget.iOS)
			{
				ConfigureXcode(pathToXcode);
			}
		}

		// Implement a function to read and write values to the plist file:
		private static void ConfigureXcode(string pathToXcode)
		{
			var projectPath = UnityEditor.iOS.Xcode.PBXProject.GetPBXProjectPath(pathToXcode);
			var schemePath = pathToXcode + "/Unity-iPhone.xcodeproj/xcshareddata/xcschemes/Unity-iPhone.xcscheme";
			var plistPath = pathToXcode + "/Info.plist";
			var pbxProject = new UnityEditor.iOS.Xcode.PBXProject();
			var scheme = new UnityEditor.iOS.Xcode.XcScheme();
			var plist = new UnityEditor.iOS.Xcode.PlistDocument();

			pbxProject.ReadFromFile(projectPath);

			var mainTargetGuid = pbxProject.GetUnityMainTargetGuid();
			var frameworkTargetGuid = pbxProject.GetUnityFrameworkTargetGuid();

			scheme.ReadFromFile(schemePath);
			scheme.SetDebugExecutable(false);
			scheme.WriteToFile(schemePath);

			plist.ReadFromFile(plistPath);
			plist.root.SetString("NSUserTrackingUsageDescription", ScriptLocalization.General.ATTDescription);
			plist.WriteToFile(plistPath);

			pbxProject.SetBuildProperty(frameworkTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");
			pbxProject.SetBuildProperty(mainTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
			pbxProject.SetBuildProperty(mainTargetGuid, "SWIFT_VERSION", "5.1");

			// Xcode 15 fix
			pbxProject.AddBuildProperty(frameworkTargetGuid, "OTHER_LDFLAGS", "-ld64");

			// Disable bitcode
			pbxProject.SetBuildProperty(mainTargetGuid, "ENABLE_BITCODE", "NO");
			pbxProject.SetBuildProperty(frameworkTargetGuid, "ENABLE_BITCODE", "NO");

			pbxProject.WriteToFile(projectPath);
		}
	}
}

#endif
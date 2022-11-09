#if UNITY_IOS
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace FirstLight.Editor.Build
{
	/// <summary>
	/// Class that adds the necessary libraries to the iOS build for Singular
	/// </summary>
	public class SingularBuildPostProcess : IPostprocessBuildWithReport
	{
		public int callbackOrder => 1100;

		private List<string> _requiredLibraries = new()
		{
			"Security.framework",
			"SystemConfiguration.framework",
			"iAD.framework",
			"AdSupport.framework",
			"WebKit.framework",
			"libsqlite3.0.tbd",
			"libz.tbd",
			"StoreKit.framework",
			"AdServices.framework"
		};

		public void OnPostprocessBuild(BuildReport report)
		{
			var needsToWriteChanges = false;
			var pbxProject = new UnityEditor.iOS.Xcode.PBXProject();
			var projectPath = UnityEditor.iOS.Xcode.PBXProject.GetPBXProjectPath(report.summary.outputPath);
			pbxProject.ReadFromFile(projectPath);
			
			var frameworkTargetGuid = pbxProject.GetUnityFrameworkTargetGuid();

			foreach (var libName in _requiredLibraries)
			{
				if (!pbxProject.ContainsFramework(frameworkTargetGuid, libName))
				{
					pbxProject.AddFrameworkToProject(frameworkTargetGuid, libName, false);
					needsToWriteChanges = true;
				}
			}

			if (needsToWriteChanges)
			{
				pbxProject.WriteToFile(projectPath);
			}
		}
	}
}

#endif
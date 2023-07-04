using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

#if UNITY_EDITOR
namespace FirstLight.Editor.Build
{
	/// <summary>
	/// Prebuild script to allow us to build directly from editor.
	/// This is so we can build + attach to mobile phones and natively build in editor without having to
	/// call the build script manually - meaning we can use debugger and other handy tools.
	/// </summary>
	public class EditorBuild : IPreprocessBuildWithReport
	{
		public int callbackOrder { get; set; } = 0;
		
		public void OnPreprocessBuild(BuildReport report)
		{
			Debug.Log("Setting up editor FLG Build");
			FirstLightBuildConfig.SetupDevelopmentConfig();
		}

	}
}
#endif
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FirstLight.Editor.Build
{
	/// <summary>
	/// Stuff we want to do BEFORE every build
	/// </summary>
	public class FirstLightBuildPreprocess : IPreprocessBuildWithReport
	{
		public int callbackOrder => 0;
		
		/// <inheritdoc />
		public void OnPreprocessBuild(BuildReport report)
		{
			Debug.Log("FirstLightBuildPreprocess.OnPreprocessBuild Executing");
			
#if UNITY_ANDROID
			FirstLightBuildConfig.SetAndroidKeystore();
#endif
			if (!Application.isBatchMode)
			{
				// These methods are triggered on editor load so they will have already been called
				// in batch mode. For manual builds project state may have been changed by the user
				// so we need to re-run them here.
				VersionEditorUtils.SetAndSaveInternalVersion();
			}
		}
	}
}
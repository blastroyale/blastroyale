#if UNITY_IOS
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using HsUnityEditor.iOS.Xcode;
using HsUnityEditor.iOS.Xcode.Extensions;
public class HelpshiftPostProcess : MonoBehaviour
{
    // Set PostProcess priority to a high number to ensure that the this is started last.
    [PostProcessBuild(900)]
    public static void OnPostprocessBuild(BuildTarget target, string path)
    {
        // Don't do anything if build target is not iOS
        if (target != BuildTarget.iOS)
        {
            return;
        }

        // Create a PBXProject object for the generated Xcode project
        PBXProject project = new PBXProject();
        string pbxProjectPath = PBXProject.GetPBXProjectPath(path);
        project.ReadFromFile(pbxProjectPath);

        // GUID of main app target. This is the target whose build settings we will be modifying.
        string unityAppTargetGuid = project.TargetGuidByName("Unity-iPhone");
        // GUID of UnityFramework target. This target is only present in Unity 2019.3 and above.
        // We need to add our app controller mm file to this target, if present. Else we add
        // it to app target.
        string unityFrameworkTargetGuid = project.TargetGuidByName("UnityFramework");

        // Add HelpshiftX-Unity.h header file to project, if not already added.
        string unityHeaderProjectPath = "Libraries/Helpshift/Plugins/iOS/HelpshiftX-Unity.h";
        string unityHeaderGuid = project.FindFileGuidByProjectPath(unityHeaderProjectPath);
        if (unityHeaderGuid == null)
        {
            string unityHeaderDiskPath = Application.dataPath + "/Helpshift/Plugins/iOS/HelpshiftX-Unity.h";
            project.AddFile(unityHeaderDiskPath, unityHeaderProjectPath, PBXSourceTree.Absolute);
        }

        // Add HsUnityAppController.mm file to project, if not already added.
        string unityAppControllerProjectPath = "Libraries/Helpshift/Plugins/iOS/HsUnityAppController.mm";
        string unityAppControllerGuid = project.FindFileGuidByProjectPath(unityHeaderProjectPath);
        if (unityAppControllerGuid == null)
        {
            string unityAppControllerDiskPath = Application.dataPath + "/Helpshift/Plugins/iOS/HsUnityAppController.mm";
            unityAppControllerGuid = project.AddFile(unityAppControllerDiskPath, unityAppControllerProjectPath, PBXSourceTree.Absolute);
            project.AddFileToBuild(unityFrameworkTargetGuid ?? unityAppTargetGuid, unityAppControllerGuid);
        }

        // Add HelpshiftX.framework to project, if not already added.
        string frameworkProjectPath = "Frameworks/Helpshift/Plugins/iOS/HelpshiftX.framework";
        string frameworkGuid = project.FindFileGuidByProjectPath(frameworkProjectPath);
        if (frameworkGuid == null)
        {
            string frameworkDiskPath = Application.dataPath + "/Helpshift/Plugins/iOS/HelpshiftX.framework";
            frameworkGuid = project.AddFile(frameworkDiskPath, frameworkProjectPath, PBXSourceTree.Absolute);
            project.AddFileToBuild(unityFrameworkTargetGuid ?? unityAppTargetGuid, frameworkGuid);
            // If framework is added to project via this script, we also need to set the Framework Search Path to path of framework on disk.
            project.AddBuildProperty(unityFrameworkTargetGuid ?? unityAppTargetGuid, "FRAMEWORK_SEARCH_PATHS", frameworkDiskPath.Replace("/HelpshiftX.framework", ""));
        }

        // Embed HelpshiftX.framework into main app target
        PBXProjectExtensions.AddFileToEmbedFrameworks(project, unityAppTargetGuid, frameworkGuid);

        // Add script phase to strip simulator slices from HelpshiftX.framework in app target, if not already added
        if (!project.isShellScriptAdded(unityAppTargetGuid, "HS Strip Simulator Slices"))
        {
            project.AddShellScriptBuildPhase(unityAppTargetGuid,
            "HS Strip Simulator Slices",
            "/bin/sh",
            "bash \"${BUILT_PRODUCTS_DIR}/${FRAMEWORKS_FOLDER_PATH}/HelpshiftX.framework/strip_frameworks.sh\"");
        }

        // Add @executable_path/Frameworks to Runpath Search Paths for app target, if not already added
        if (!project.IsBuildPropertyAdded(unityAppTargetGuid, "LD_RUNPATH_SEARCH_PATHS", "@executable_path/Frameworks"))
        {
            project.AddBuildProperty(unityAppTargetGuid, "LD_RUNPATH_SEARCH_PATHS", "@executable_path/Frameworks");
        }

        // Set Validate Workspace to YES. If you are setting this to NO elsewhere in your build pipeline,
        // comment this line out.
        project.SetBuildProperty(unityAppTargetGuid, "VALIDATE_WORKSPACE", "YES");

        // Add UserNotifications.framework system framework
        project.AddFrameworkToProject(unityFrameworkTargetGuid ?? unityAppTargetGuid, "UserNotifications.framework", false);

        // Save modified Xcode project
        project.WriteToFile(pbxProjectPath);

        // Enable remote notifications
        string preprocessorPath = path + "/Classes/Preprocessor.h";
        string text = File.ReadAllText(preprocessorPath);
        text = text.Replace("UNITY_USES_REMOTE_NOTIFICATIONS 0", "UNITY_USES_REMOTE_NOTIFICATIONS 1");
        File.WriteAllText(preprocessorPath, text);
    }
}
#endif

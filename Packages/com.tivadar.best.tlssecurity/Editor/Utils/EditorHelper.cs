using System;
using System.IO;
using System.Linq;

using UnityEditor;

namespace Best.TLSSecurity.Editor.Utils
{
    public static class EditorHelper
    {
        public static string Folder_Plugin = "com.tivadar.best.tlssecurity";
        public static string EditorASMDEF_GUID = "84667e556b6db354c9af2e23cf4a292c";

        public static string GetPluginFolder()
        {
            // Maybe use UnityEditor.Compilation.CompilationPipeline.* ?
            //  -Unfortunately they don't work if the package is in the Packages folder.
            var current = Directory.GetCurrentDirectory();
            var matchedDirectories = Directory.GetDirectories(current, Folder_Plugin, SearchOption.AllDirectories);
            if (matchedDirectories == null || matchedDirectories.Length == 0)
                throw new Exception("Couldn't find plugin directory!");
            return matchedDirectories.FirstOrDefault();
        }

        public static string GetPluginResourcesFolder()
        {
            return Path.Combine(GetPluginFolder(), "Runtime", "Resources");
        }

        public static string GetPackageRootPath()
        {
            var packageEditorAssetPath = AssetDatabase.GUIDToAssetPath(EditorASMDEF_GUID);
            if (packageEditorAssetPath == string.Empty)
            {
                throw new FileNotFoundException($"Couldn't find Visual Scripting package folder.");
            }

            // The root VS folder path is 1 directories up from our Editor folder
            return Path.GetDirectoryName(packageEditorAssetPath);
        }

        public static string GetEditorFolder()
        {
            return GetPackageRootPath();
            /*UnityEngine.Debug.Log(GetPackageRootPath());

            string editorAsmFilePath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyReference(CompilationPipeline.GUIDToAssemblyDefinitionReferenceGUID(EditorASMDEF_GUID));
            UnityEngine.Debug.Log(editorAsmFilePath);

            return Path.GetDirectoryName(editorAsmFilePath);*/
        }
    }
}

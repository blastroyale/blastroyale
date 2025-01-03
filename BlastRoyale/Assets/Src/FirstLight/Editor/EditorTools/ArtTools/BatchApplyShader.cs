namespace FirstLight.Editor.EditorTools.ArtTools
{
using UnityEditor;
using UnityEngine;

public class BatchApplyShader : EditorWindow
{
    private Shader shaderToApply;

	[MenuItem("FLG/Art/Batch Apply Shader")]
    public static void ShowWindow()
    {
        GetWindow<BatchApplyShader>("Batch Apply Shader");
    }

    private void OnGUI()
    {
        GUILayout.Label("Batch Apply Shader", EditorStyles.boldLabel);

        shaderToApply = (Shader)EditorGUILayout.ObjectField("Shader", shaderToApply, typeof(Shader), false);

        if (GUILayout.Button("Apply Shader to Selected Materials"))
        {
            ApplyShaderToSelectedMaterials();
        }

        if (GUILayout.Button("Apply Shader to Folder Materials"))
        {
            ApplyShaderToMaterialsInFolder();
        }
    }

    private void ApplyShaderToSelectedMaterials()
    {
        if (shaderToApply == null)
        {
            Debug.LogError("Please select a shader to apply.");
            return;
        }

        Object[] selectedObjects = Selection.objects;
        foreach (var obj in selectedObjects)
        {
            if (obj is Material material)
            {
                Undo.RecordObject(material, "Batch Apply Shader");
                material.shader = shaderToApply;
                EditorUtility.SetDirty(material);
            }
        }

        Debug.Log("Shader applied to selected materials.");
    }

    private void ApplyShaderToMaterialsInFolder()
    {
        if (shaderToApply == null)
        {
            Debug.LogError("Please select a shader to apply.");
            return;
        }

        string folderPath = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");

        if (string.IsNullOrEmpty(folderPath))
        {
            return;
        }

        string relativePath = "Assets" + folderPath.Replace(Application.dataPath, "");
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { relativePath });

        foreach (string guid in materialGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

            if (material != null)
            {
                Undo.RecordObject(material, "Batch Apply Shader");
                material.shader = shaderToApply;
                EditorUtility.SetDirty(material);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Shader applied to materials in folder.");
    }
}

}
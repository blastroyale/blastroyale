#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class ObjectGroupsFromPrefabLibsCreationSettings : PluginSettings<ObjectGroupsFromPrefabLibsCreationSettings>
    {
        [SerializeField]
        private bool                _flatHierarchy          = true;
        [SerializeField]
        private string              _namePrefix             = defaultNamePrefix;
        [SerializeField]
        private string              _nameSuffix             = defaultNameSuffix;
        [SerializeField]
        private string              _sourceScenePath        = string.Empty;
      
        public bool                 flatHierarchy           { get { return _flatHierarchy; } set { UndoEx.record(this); _flatHierarchy = value; EditorUtility.SetDirty(this); } }
        public string               namePrefix              { get { return _namePrefix; } set { UndoEx.record(this); _namePrefix = value; EditorUtility.SetDirty(this); } }
        public string               nameSuffix              { get { return _nameSuffix; } set { UndoEx.record(this); _nameSuffix = value; EditorUtility.SetDirty(this); } }
        public SceneAsset           sourceSceneAsset
        {
            get
            {
                if (string.IsNullOrEmpty(_sourceScenePath)) return null;
                return AssetDbEx.loadAsset<SceneAsset>(_sourceScenePath);
            }
        }

        public static bool          defaultFlatHierarchy    { get { return true; } }
        public static string        defaultNamePrefix       { get { return string.Empty; } }
        public static string        defaultNameSuffix       { get { return string.Empty; } }

        public override void useDefaults()
        {
            flatHierarchy   = defaultFlatHierarchy;
            namePrefix      = defaultNamePrefix;
            nameSuffix      = defaultNameSuffix;

            EditorUtility.SetDirty(this);
        }

        public void buildUI(VisualElement parent)
        {
            const float labelWidth = 150.0f;

            VisualElement ctrl = UI.createToggle("_flatHierarchy", serializedObject, "Flat hierarchy", "If this is checked, the created object groups will ignore the libraries' parent-child relationships.", parent);
            ctrl.setChildLabelWidth(labelWidth);
            ctrl = UI.createTextField("_namePrefix", serializedObject, "Name prefix", "The prefix that will be added to the names of all created object groups.", parent);
            ctrl.setChildLabelWidth(labelWidth);
            ctrl = UI.createTextField("_nameSuffix", serializedObject, "Name suffix", "The suffix that will be added to the names of all created object groups.", parent);
            ctrl.setChildLabelWidth(labelWidth);
        }
    }
}
#endif
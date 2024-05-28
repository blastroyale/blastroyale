#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class ModularWallsObjectSpawnSettings : PluginSettings<ModularWallsObjectSpawnSettings>
    {
        [NonSerialized]
        private VisualElement   _ui;
        [SerializeField]
        private string          _modularWallPrefabProfileName       = defaultModularWallPrefabProfileName;
        [SerializeField]
        private int             _maxSegmentLength                   = defaultMaxSegmentLength;
        [SerializeField]
        private bool            _eraseExisting                      = defaultEraseExisting;

        public VisualElement            ui                              { get { return _ui; } }
        public ModularWallPrefabProfile modularWallPrefabProfile
        {
            get
            {
                var profile = ModularWallPrefabProfileDb.instance.findProfile(_modularWallPrefabProfileName);
                if (profile == null) profile = ModularWallPrefabProfileDb.instance.defaultProfile;
                return profile;
            }
        }
        public int              maxSegmentLength                        { get { return _maxSegmentLength; } set { UndoEx.record(this); _maxSegmentLength = Mathf.Max(2, value); EditorUtility.SetDirty(this); } }
        public bool             eraseExisting                           { get { return _eraseExisting; } set { UndoEx.record(this); _eraseExisting = value; EditorUtility.SetDirty(this); } }

        public static string    defaultModularWallPrefabProfileName     { get { return "Default"; } }
        public static int       defaultMaxSegmentLength                 { get { return 200; } }
        public static bool      defaultEraseExisting                    { get { return false; } }

        public void buildUI(VisualElement parent)
        {
            const float labelWidth = 160.0f;

            _ui = new VisualElement();
            parent.Add(_ui);

            IMGUIContainer imGUIContainer = UI.createIMGUIContainer(_ui);
            imGUIContainer.onGUIHandler = () =>
            {
                string newName = EditorUIEx.profileNameSelectionField<ModularWallPrefabProfileDb, ModularWallPrefabProfile>
                    (ModularWallPrefabProfileDb.instance, "Modular wall prefab profile", labelWidth, _modularWallPrefabProfileName);
                if (newName != _modularWallPrefabProfileName)
                {
                    UndoEx.record(this);
                    _modularWallPrefabProfileName = newName;
                    EditorUtility.SetDirty(this);

                    if (ObjectSpawn.instance != null)
                        ObjectSpawn.instance.modularWallObjectSpawn.onModularWallPrefabProfileChanged();
                }
            };

            VisualElement ctrl = UI.createIntegerField("_maxSegmentLength", serializedObject, "Max segment length", "The maximum length a wall segment can have. Useful to prevent " +
                "segments from getting too long for certain camera angles.", 2, _ui);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createToggle("_eraseExisting", serializedObject, "Erase existing", 
                "If checked, any existing wall pieces that overlap the ones that are being spawned will be erased.", parent);
            ctrl.setChildLabelWidth(labelWidth);
        }

        public override void useDefaults()
        {
            _modularWallPrefabProfileName   = defaultModularWallPrefabProfileName;
            maxSegmentLength                = defaultMaxSegmentLength;
            eraseExisting              = defaultEraseExisting;

            EditorUtility.SetDirty(this);
        }
    }
}
#endif
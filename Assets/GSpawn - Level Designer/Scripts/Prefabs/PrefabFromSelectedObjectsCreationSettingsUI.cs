#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace GSpawn
{
    public class PrefabFromSelectedObjectsCreationSettingsUI : PluginUI
    {
        public static PrefabFromSelectedObjectsCreationSettingsUI instance { get { return PrefabLibProfileDb.instance.prefabCreationSettingsUI; } }

        protected override void onBuild()
        {
            contentContainer.style.setMargins(UIValues.wndMargin);
            PrefabLibProfileDb.instance.prefabCreationSettings.buildUI(contentContainer);

            var buttons                 = new VisualElement();
            contentContainer.Add(buttons);
            buttons.style.marginTop     = 10.0f;
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.flexShrink    = 0.0f;

            UI.createUseDefaultsButton(() => PrefabLibProfileDb.instance.prefabCreationSettings.useDefaults(), buttons);

            VisualElement indent        = UI.createHorizontalSpacer(buttons);
            var button                  = new Button();
            button.text                 = "Create";
            button.tooltip              = "Create a prefab from the currently selected objects and add it to the selected libraries.";
            button.clicked              += () => { createPrefab(); };
            buttons.Add(button);

            button                      = new Button();
            button.text                 = "Cancel";
            button.clicked              += () => { targetWindow.Close(); };
            buttons.Add(button);
        }

        protected override void onRefresh()
        {
        }

        private void createPrefab()
        {
            if (ObjectSelection.instance.numSelectedObjects == 0)
            {
                EditorUtility.DisplayDialog("Prefab Creation Failed", "No objects selected. Prefab creation failed.", "Ok");
                return;
            }

            GameObject prefabAsset = ObjectSelection.instance.createPrefabFromSelectedObjects(PrefabLibProfileDb.instance.prefabCreationSettings);
            if (prefabAsset != null)
            {
                EditorUtility.DisplayDialog("Success", "Prefab successfully created.", "Ok");
                prefabAsset.pingPrefabAsset();
            }
            else EditorUtility.DisplayDialog("Failure", "Failed to create a prefab from the currently selected objects.", "Ok");
        }
    }
}
#endif
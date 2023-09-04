#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class PrefabsFromObjectGroupsCreationSettingsUI : PluginUI
    {
        public static PrefabsFromObjectGroupsCreationSettingsUI instance { get { return ObjectGroupDb.instance.prefabCreationSettingsUI; } }

        protected override void onBuild()
        {
            contentContainer.style.setMargins(UIValues.wndMargin);
            ObjectGroupDb.instance.prefabCreationSettings.buildUI(contentContainer);

            var buttons                 = new VisualElement();
            contentContainer.Add(buttons);
            buttons.style.marginTop     = 10.0f;
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.flexShrink    = 0.0f;

            UI.createUseDefaultsButton(() => ObjectGroupDb.instance.prefabCreationSettings.useDefaults(), buttons);

            VisualElement indent        = UI.createHorizontalSpacer(buttons);
            var button                  = new Button();
            button.text                 = "Create";
            button.tooltip              = "Create object group prefabs.";
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
            if (ObjectGroupDb.instance.numObjectGroups == 0)
            {
                EditorUtility.DisplayDialog("Prefab Creation Failed", "No object groups available. Prefab creation failed.", "Ok");
                return;
            }

            if (ObjectGroupDb.instance.createPrefabsFromObjectGroups(null)) 
                EditorUtility.DisplayDialog("Success", "Prefabs successfully created.", "Ok");
            else EditorUtility.DisplayDialog("Failure", "Failed to create object group prefabs.", "Ok");
        }
    }
}
#endif
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectGroupsFromPrefabLibsCreationSettingsUI : PluginUI
    {
        private Dictionary<ObjectGroup, ObjectGroup>                _childGroupToParent = new Dictionary<ObjectGroup, ObjectGroup>();

        public static ObjectGroupsFromPrefabLibsCreationSettingsUI  instance            { get { return PrefabLibProfileDb.instance.objectGroupCreationSettingsUI; } }

        protected override void onBuild()
        {
            contentContainer.style.setMargins(UIValues.wndMargin);
            PrefabLibProfileDb.instance.objectGroupCreationSettings.buildUI(contentContainer);

            var buttons                 = new VisualElement();
            contentContainer.Add(buttons);
            buttons.style.marginTop     = 10.0f;
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.flexShrink    = 0.0f;

            UI.createUseDefaultsButton(() => PrefabLibProfileDb.instance.objectGroupCreationSettings.useDefaults(), buttons);

            VisualElement indent        = UI.createHorizontalSpacer(buttons);
            var button                  = new Button();
            button.text                 = "Create";
            button.tooltip              = "Create object groups for each prefab library and associate the groups with the prefabs in each library.";
            button.clicked              += () => 
            {
                createObjectGroupsFromPrefabLibs();
            };
            buttons.Add(button);

            button                      = new Button();
            button.text                 = "Cancel";
            button.clicked              += () => { targetWindow.Close(); };
            buttons.Add(button);
        }

        protected override void onRefresh()
        {
        }

        private void createObjectGroupsFromPrefabLibs()
        {
            // Note: Disable Undo/Redo for this operation. Seems to be quite tricky to make it work properly.
            UndoEx.saveEnabledState();
            UndoEx.enabled = false;
            Undo.ClearAll();

            var rootLibs    = new List<PrefabLib>();
            PrefabLibProfileDb.instance.activeProfile.getRootLibs(rootLibs);
            int numRootLibs = rootLibs.Count;

            PluginProgressDialog.begin("Creating Object Groups");
            for (int libIndex = 0; libIndex < numRootLibs; ++libIndex)
            {
                var rootLib = rootLibs[libIndex];
                PluginProgressDialog.updateItemProgress(rootLib.libName, (libIndex + 1) / numRootLibs);

                ObjectGroup objectGroup = createObjectGroupForLib(rootLib, null);
                rootLib.setPrefabsObjectGroup(objectGroup);
                createObjectGroupsRecurse(rootLib, objectGroup);
            }
            PluginProgressDialog.end();

            // Note: Update parent-child relationships in a second pass in order for undo/redo to work properly.
            if (_childGroupToParent.Count != 0)
            {
                int childParentPairIndex = 0;
                int numChildParentPairs = _childGroupToParent.Count;
                PluginProgressDialog.begin("Updating Parent-Child Relationships");
                foreach (var pair in _childGroupToParent)
                {
                    PluginProgressDialog.updateItemProgress(pair.Key.gameObject.name, (childParentPairIndex + 1) / numChildParentPairs);
                    ++childParentPairIndex;

                    pair.Key.setParentObjectGroup(pair.Value);
                }
                PluginProgressDialog.end();
                _childGroupToParent.Clear();
            }

            UndoEx.restoreEnabledState();
            EditorApplication.RepaintHierarchyWindow();
            ObjectGroupDbUI.instance.refresh();
        }

        private void createObjectGroupsRecurse(PrefabLib parentLib, ObjectGroup parentGroup)
        {
            int numDirectChildren = parentLib.numDirectChildren;
            for (int childIndex = 0; childIndex < numDirectChildren; ++childIndex)
            {
                var childLib = parentLib.getDirectChild(childIndex);
                ObjectGroup objectGroup = createObjectGroupForLib(childLib, parentGroup);
                childLib.setPrefabsObjectGroup(objectGroup);

                createObjectGroupsRecurse(childLib, objectGroup);
            }
        }

        private ObjectGroup createObjectGroupForLib(PrefabLib prefabLib, ObjectGroup parentGroup)
        {
            string objectGroupName = string.Empty;
            if (!string.IsNullOrEmpty(PrefabLibProfileDb.instance.objectGroupCreationSettings.namePrefix)) objectGroupName += PrefabLibProfileDb.instance.objectGroupCreationSettings.namePrefix;
            objectGroupName += prefabLib.libName;
            if (!string.IsNullOrEmpty(PrefabLibProfileDb.instance.objectGroupCreationSettings.nameSuffix)) objectGroupName += PrefabLibProfileDb.instance.objectGroupCreationSettings.nameSuffix;

            ObjectGroup objectGroup = ObjectGroupDb.instance.createObjectGroup(objectGroupName);
            if (parentGroup != null && !PrefabLibProfileDb.instance.objectGroupCreationSettings.flatHierarchy)
                _childGroupToParent.Add(objectGroup, parentGroup);

            return objectGroup;
        }
    }
}
#endif
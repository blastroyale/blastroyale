#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace GSpawn
{
    public class UITileRuleGridItem : ListViewItem<TileRuleGrid>
    {
        private VisualElement       _row0;
        private VisualElement       _row1;
        private VisualElement       _row2;
        private VisualElement       _row3;
        private VisualElement       _row4;
        private VisualElement       _expandedStateBtn;
        private Button              _toggleActiveBtn;        

        public override PluginGuid  guid            { get { return getItemId(data); } }
        public override string      displayName     { get { return data.gridName; } }

        public static PluginGuid getItemId(TileRuleGrid grid)
        {
            return grid.guid;
        }

        public static void getItemIds(List<TileRuleGrid> grids, List<PluginGuid> ids)
        {
            ids.Clear();
            foreach (var curve in grids)
                ids.Add(getItemId(curve));
        }

        protected override void onRefreshUI()
        {
            applyVariableStates();
        }

        protected override void onSelectedStateChanged(bool selected)
        {
            applyVariableStates();
        }

        protected override void onBuildUIBeforeDisplayName()
        {
            style.flexDirection = FlexDirection.Column;

            _row0               = createRow();
            _row1               = createRow();
            _row2               = createRow();

            UI.createRowSeparator(this);
            _row3               = createRow();
            _row4               = createRow();

            _expandedStateBtn   = UI.createIcon(TexturePool.instance.itemArrowRight, _row0);
            _expandedStateBtn.RegisterCallback<MouseDownEvent>(p => 
            { 
                data.uiExpanded = !data.uiExpanded; 
                applyVariableStates(); 
                p.StopPropagation();        // Note: To disable selection/deselection of the item itself when pressing the expand/collapse button.
            });

            var icon                = UI.createIcon(TexturePool.instance.tileGrid, UIValues.smallIconSize, _row0);
            icon.style.marginTop    = 2.0f;

            _toggleActiveBtn        = UI.createButton(TexturePool.instance.lightBulb, UI.ButtonStyle.Push, UIValues.smallButtonSize, _row0);
            UI.useDefaultMargins(_toggleActiveBtn);
            _toggleActiveBtn.clicked += () => 
            {
                UndoEx.record(data.gameObject);
                if (data.gameObject.activeSelf) data.gameObject.SetActive(false);
                else data.gameObject.SetActive(true);
                applyToggleActiveButtonStates();
            };

            var btn                 = UI.createButton(TexturePool.instance.clear, UI.ButtonStyle.Push, UIValues.smallButtonSize, _row0);
            btn.tooltip             = "Clear grid. Deletes all tiles inside the grid. Note: Objects groups will remain.";
            btn.style.marginTop     = 2.0f;
            btn.style.marginLeft    = -2.0f;
            btn.clicked             += () => { data.deleteAllTiles(); };
        }

        protected override void onBuildUIAfterDisplayName()
        {
            reparentMainControls(_row0);

            _displayNameLabel.style.flexGrow                = 1.0f;
            _displayNameLabel.style.marginLeft              = 2.0f;
            _displayNameLabel.style.color                   = UIValues.listItemTextColor;
            _displayNameLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
            _displayNameLabel.style.alignSelf               = Align.FlexStart;
            _displayNameLabel.style.marginTop               = 2.0f;
            _displayNameLabel.style.width                   = 90.0f;

            _renameField.style.flexGrow = 1.0f;
            _renameField.style.height   = 20.0f;
            UI.createHorizontalSpacer(_row0);

            var btn                     = UI.createButton(TexturePool.instance.refresh, UI.ButtonStyle.Push, UIValues.smallButtonSize, _row0);
            btn.tooltip                 = "Use default settings.";
            btn.style.marginTop         = 2.0f;
            btn.clicked += () =>
            {
                data.settings.wireColor = TileRuleGridSettings.defaultWireColor;
                data.settings.fillColor = TileRuleGridSettings.defaultFillColor;

                if (data.settings.tileRuleNeighborRadius != TileRuleGridSettings.defaultTileRuleNeighborRadius)
                {
                    data.settings.tileRuleNeighborRadius = TileRuleGridSettings.defaultTileRuleNeighborRadius;
                    data.refreshTiles();
                }
            };
          
            // Note: Can't use UIElements here because they would need to be bound to the data.settings.serializedObject properties
            //       and this can cause issues because the same list item can be reused for different grid items (i.e. they are pooled).
            //       A grid may be deleted, the item will be freed (stored in the pool) and then reused later for another grid,
            //       but its UI elements would still be linked to the old serialized object which no longer exists. Although there
            //       may be ways around this, it seems much cleaner to just use an IMGUIContainer.
            var guiContainer            = UI.createIMGUIContainer(_row0);
            guiContainer.style.flexGrow = 1.0f;
            guiContainer.onGUIHandler = () => 
            {
                const float colorFieldWidth = 40.0f;
                var guiContent = new GUIContent();

                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();
                guiContent.text = "";
                guiContent.tooltip = "This is the radius that will be used to check for adjacent tile neighbors.";
                var newRadius = (TileRuleNeighborRadius)EditorGUILayout.EnumPopup(guiContent, data.settings.tileRuleNeighborRadius, GUILayout.MaxWidth(55.0f));
                if (EditorGUI.EndChangeCheck())
                {
                    data.settings.tileRuleNeighborRadius = newRadius;
                    data.refreshTiles();
                }

                EditorGUI.BeginChangeCheck();
                guiContent.text = "";
                guiContent.tooltip = "Grid cell line color.";
                var color = EditorGUILayout.ColorField(guiContent, data.settings.wireColor, GUILayout.MaxWidth(colorFieldWidth));
                if (EditorGUI.EndChangeCheck()) data.settings.wireColor = color;

                EditorGUI.BeginChangeCheck();
                guiContent.text = "";
                guiContent.tooltip = "Grid cell area color.";
                color = EditorGUILayout.ColorField(guiContent, data.settings.fillColor, GUILayout.MaxWidth(colorFieldWidth));
                if (EditorGUI.EndChangeCheck()) data.settings.fillColor = color;

                EditorGUILayout.EndHorizontal();
            };

            const float labelWidth      = 90.0f;
            const float floatFieldWidth = 30.0f;
            
            // Reset origin to 0
            _row1.style.marginTop       = 3.0f;
            var parent                  = new VisualElement();
            parent.style.flexGrow       = 1.0f;
            parent.style.flexDirection  = FlexDirection.Row;
            _row1.Add(parent);
            btn                         = UI.createButton(TexturePool.instance.refresh, UI.ButtonStyle.Push, UIValues.smallButtonSize, parent);
            btn.style.marginTop         = 2.0f;
            btn.tooltip                 = "Reset origin to 0.";
            btn.clicked += () =>        { data.gridOrigin = Vector3.zero; };

            guiContainer            = UI.createIMGUIContainer(parent);
            guiContainer.style.flexGrow = 1.0f;
            guiContainer.onGUIHandler   = () => 
            {
                var guiContent = new GUIContent();
                EditorGUILayout.BeginHorizontal();
                guiContent.text     = "Origin";
                guiContent.tooltip  = "The position of the grid origin.";
                EditorGUILayout.LabelField(guiContent, GUILayout.Width(labelWidth));
                var vecRes = EditorUIEx.vector3FieldEx(data.gridOrigin, floatFieldWidth);
                if (vecRes.hasChanged)
                {
                    UndoEx.record(data.transform);
                    data.gridOrigin = vecRes.newValue;
                }
                EditorGUILayout.EndHorizontal();
            };

            // Reset rotation to 0
            parent = new VisualElement();
            parent.style.flexGrow       = 1.0f;
            parent.style.flexDirection  = FlexDirection.Row;
            _row2.Add(parent);
            btn                         = UI.createButton(TexturePool.instance.refresh, UI.ButtonStyle.Push, UIValues.smallButtonSize, parent);
            btn.style.marginTop         = 2.0f;
            btn.tooltip                 = "Reset rotation to 0.";
            btn.clicked                 += () => { data.gridRotation = Quaternion.identity; };

            guiContainer                = UI.createIMGUIContainer(parent);
            guiContainer.style.flexGrow = 1.0f;
            guiContainer.onGUIHandler   = () =>
            {
                var guiContent = new GUIContent();

                EditorGUILayout.BeginHorizontal();
                guiContent.text     = "Rotation";
                guiContent.tooltip  = "The grid rotation.";
                EditorGUILayout.LabelField(guiContent, GUILayout.Width(labelWidth));
                var vecRes = EditorUIEx.vector3FieldEx(data.gridRotation.eulerAngles, floatFieldWidth);
                if (vecRes.hasChanged)
                {
                    UndoEx.record(data.transform);
                    data.gridRotation = Quaternion.Euler(vecRes.newValue);
                }
                EditorGUILayout.EndHorizontal();
            };

            // Mirroring
            parent                      = new VisualElement();
            parent.style.flexGrow       = 1.0f;
            parent.style.flexDirection  = FlexDirection.Row;
            _row3.Add(parent);

            guiContainer                = UI.createIMGUIContainer(parent);
            guiContainer.style.flexGrow = 1.0f;
            guiContainer.onGUIHandler   = () =>
            {
                var guiContent = new GUIContent();

                EditorGUILayout.BeginHorizontal();
                guiContent.text = "Mirroring enabled";
                guiContent.tooltip = 
                    ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(GlobalShortcutNames.mirrorGizmo_Toggle, 
                    "If checked, mirroring will be used when painting or erasing tiles.");
                EditorGUI.BeginChangeCheck();
                var mirroringEnabled = EditorGUILayout.ToggleLeft(guiContent, data.mirroringEnabled);
                if (EditorGUI.EndChangeCheck())
                {
                    data.mirroringEnabled = mirroringEnabled;
                }
                EditorGUILayout.EndHorizontal();
            };

            guiContainer = new IMGUIContainer();
            guiContainer.style.marginLeft = 4.0f;
            parent.Add(guiContainer);
            guiContainer.onGUIHandler = () =>
            {
                EditorUIEx.objectMirrorGizmoPlaneToggle(data.mirrorGizmoSettings);
            };

            btn = UI.createButton(TexturePool.instance.refresh, UI.ButtonStyle.Push, UIValues.smallButtonSize, _row4);
            btn.style.marginTop = 2.0f;
            btn.tooltip = "Reset mirror cell coordinates to 0.";
            btn.clicked += () => 
            {
                data.mirrorGizmoCellCoords = Vector3Int.zero;
            };

            guiContainer = new IMGUIContainer();
            guiContainer.style.marginLeft = 4.0f;
            _row4.Add(guiContainer);
            guiContainer.onGUIHandler = () =>
            {
                var guiContent      = new GUIContent();
                guiContent.text     = "Mirror cell";
                guiContent.tooltip  = "The cell coordinate of the mirror gizmo.";

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(guiContent, GUILayout.Width(labelWidth));
                EditorGUI.BeginChangeCheck();
                var cellCoords = EditorGUILayout.Vector3IntField("", data.mirrorGizmoCellCoords);
                if (EditorGUI.EndChangeCheck())
                {
                    data.mirrorGizmoCellCoords = cellCoords;
                }
                EditorGUILayout.EndHorizontal();
            };

            applyVariableStates();
        }

        protected override void onBeginRename()
        {
            _renameField.SetValueWithoutNotify(displayName);
        }

        protected override void onEndRename(bool commit)
        {
            if (commit)
            {
                TileRuleGridDb.instance.renameGrid(data, _renameField.text);
            }
        }

        private void applyVariableStates()
        {
            if (data.uiExpanded) style.height   = 110.0f;
            else style.height                   = 20.0f;

            _row1.setDisplayVisible(data.uiExpanded);
            _row2.setDisplayVisible(data.uiExpanded);
            _row3.setDisplayVisible(data.uiExpanded);
            _row4.setDisplayVisible(data.uiExpanded);

            _displayNameLabel.text              = displayName;
            _displayNameLabel.style.color       = UIValues.listItemTextColor;
            _displayNameLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
            _expandedStateBtn.style.backgroundImage         = data.uiExpanded ? TexturePool.instance.itemArrowDown : TexturePool.instance.itemArrowRight;

            if (selected)
            {
                if (ObjectSpawn.instance.tileRuleObjectSpawn.findCurrentGrid() == data)
                {
                    _displayNameLabel.style.color = UIValues.isProSkin ? Color.green : Color.green;
                    _displayNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                }          
            }

            applyToggleActiveButtonStates();
        }

        private void applyToggleActiveButtonStates()
        {
            bool active = data.gameObject.activeSelf;
            if (active)
            {
                _toggleActiveBtn.style.backgroundImage = TexturePool.instance.lightBulbGray;
                _toggleActiveBtn.tooltip = "Deactivate grid object.";
            }
            else
            {
                _toggleActiveBtn.style.backgroundImage = TexturePool.instance.lightBulb;
                _toggleActiveBtn.tooltip = "Activate grid object.";
            }
        }
    }
}
#endif
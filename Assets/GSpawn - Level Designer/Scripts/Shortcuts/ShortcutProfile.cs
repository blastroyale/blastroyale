#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GSpawn
{
    public class ShortcutProfile : Profile
    {
        [SerializeField]
        private List<ShortcutCategory>  _shortcutCategories     = new List<ShortcutCategory>();

        public int                      numShortcutCategories   { get { return _shortcutCategories.Count; } }

        public Shortcut findShortcut(string shortcutName)
        {
            foreach(var category in _shortcutCategories)
            {
                Shortcut shortcut = category.findShortcut(shortcutName);
                if (shortcut != null) return shortcut;
            }

            return null;
        }

        public void useDefaults()
        {
            if (_shortcutCategories.Count != 0)
            {
                foreach(var category in _shortcutCategories)
                {
                    int numShortcuts = category.numShortcuts;
                    for (int shIndex = 0; shIndex < numShortcuts; ++shIndex)
                        useDefault(category.getShortcut(shIndex), false, false);
                }

                // Note: Reseting to defaults eliminates all conflicts.
                foreach (var c in _shortcutCategories)
                    c.clearShortcutConflicts();

                UIRefresh.refreshShortcutToolTips();
                ShortcutProfileDbUI.instance.refresh();
            }
        }

        public void useDefault(Shortcut shortcut, bool checkForConflicts, bool refreshUI)
        {
            UndoEx.record(this);
            shortcut.keyCombo.state = ShortcutDefaults.shortcutDefault(shortcut.shortcutName);
            if (checkForConflicts) detectConflicts(false);
            if (refreshUI)
            {
                UIRefresh.refreshShortcutToolTips();
                ShortcutProfileDbUI.instance.refresh();
            }
            EditorUtility.SetDirty(this);
        }

        public void useDefault(string shortcutName, bool checkForConflicts, bool refreshUI)
        {
            useDefault(findShortcut(shortcutName), checkForConflicts, refreshUI);
        }

        public void setShortcutKeyComboState(Shortcut shortcut, KeyCombo.State keyComboState)
        {
            shortcut.keyCombo.state = keyComboState;
            EditorUtility.SetDirty(this);
            UIRefresh.refreshShortcutToolTips();
            ShortcutProfileDbUI.instance.refresh();
        }

        public void detectConflicts(bool refreshUI)
        {
            List<Shortcut> shortcutsMain    = new List<Shortcut>();
            List<Shortcut> shortcutsSec     = new List<Shortcut>();
            HashSet<Shortcut> processed     = new HashSet<Shortcut>();

            foreach (var category in _shortcutCategories)
            {
                category.getShortcuts(shortcutsMain);
                foreach(var shortcut in shortcutsMain)
                {
                    if (!processed.Contains(shortcut))
                    {
                        shortcut.clearConflicts();
                        processed.Add(shortcut);
                    }

                    foreach(var c in _shortcutCategories)
                    {
                        c.getShortcuts(shortcutsSec);
                        foreach(var s in shortcutsSec)
                        {
                            if (shortcut.conflictsWith(s))
                            {
                                shortcut.addConflict(new ShortcutConflict() { categoryName = c.categoryName, shortcutName = s.shortcutName });
                            }
                        }
                    }
                }
            }

            if (refreshUI)
                ShortcutProfileDbUI.instance.refresh();
        }

        public string getShortcutUITooltip(string shortcutName)
        {
            return " [Hotkey: " + findShortcut(shortcutName).keyCombo.ToString() + "]";
        }

        public string getShortcutUITooltip(string shortcutName, string description)
        {
            return " [Hotkey: " + findShortcut(shortcutName).keyCombo.ToString() + "]" + "\r\n\r\n" + description;
        }

        public bool processEvent(Event e)
        {
            GlobalShortcutContext.instance.evaluateHierarchy();
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode != KeyCode.None)
                {
                    foreach (var c in _shortcutCategories)
                        if (c.executeCommands()) return true;
                }
            }
            else
            if (e.type == EventType.KeyUp)
            {
                if (e.keyCode != KeyCode.None)
                {
                    bool disabledCommand = false;
                    foreach (var c in _shortcutCategories)
                        disabledCommand |= c.disableCommands();

                    return false;
                }
            }
            else
            if (e.type == EventType.Repaint || e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                // Note: Check for modifiers only. We check all, because in some situations more than one hotkey
                //       can use the same modifiers (even if in the same context).
                bool foundActive = false;
                foreach (var c in _shortcutCategories)
                    foundActive |= c.executeOrDisableModifierCommands();

                return foundActive;
            }

            return false;
        }

        public ShortcutCategory findShortcutCategory(string categoryName)
        {
            return _shortcutCategories.Find(item => item.categoryName == categoryName);
        }

        public void getShortcutCategoryNames(List<string> names)
        {
            names.Clear();
            foreach (var c in _shortcutCategories)
                names.Add(c.categoryName);
        }

        public void getShortcutCategories(List<ShortcutCategory> categories)
        {
            categories.Clear();
            foreach (var c in _shortcutCategories)
                categories.Add(c);
        }

        private ShortcutCategory getOrCreateShortcutCategory(string categoryName)
        {
            var category = findShortcutCategory(categoryName);
            if (category == null)
            {
                category                = new ShortcutCategory();
                category.categoryName   = categoryName;

                _shortcutCategories.Add(category);
                EditorUtility.SetDirty(this);
            }

            return category;
        }

        private Shortcut getOrCreateShortcut(ShortcutCategory category, string shortcutName, ShortcutContext context, PluginCommand command)
        {
            var sh = findShortcut(shortcutName);
            if (sh == null)
            {
                sh = category.getOrCreateShortcut(shortcutName, context, command);
                useDefault(sh, false, false);
            }

            // Note: Need to always set these as they are not serializable.
            sh.command = command;
            sh.context = context;

            return sh;
        }

        private void OnEnable()
        {
            // ================================= Global ================================= //
            ShortcutCategory category = getOrCreateShortcutCategory(ShortcutCategoryNames.global);
            getOrCreateShortcut(category, GlobalShortcutNames.objectSpawn, GlobalShortcutContext.instance, new Global_EnableObjectSpawnTool());
            getOrCreateShortcut(category, GlobalShortcutNames.objectSelection, GlobalShortcutContext.instance, new Global_EnableObjectSelectionTool());
            getOrCreateShortcut(category, GlobalShortcutNames.objectErase, GlobalShortcutContext.instance, new Global_EnableObjectEraseTool());
            getOrCreateShortcut(category, GlobalShortcutNames.grid_VerticalStepDown, GlobalShortcutContext.instance, new Global_Grid_VerticalStepDown());
            getOrCreateShortcut(category, GlobalShortcutNames.grid_VerticalStepUp, GlobalShortcutContext.instance, new Global_Grid_VerticalStepUp());
            getOrCreateShortcut(category, GlobalShortcutNames.grid_SnapToPickedObject, GlobalShortcutContext.instance, new Global_Grid_EnableSnapToPickedObject());
            getOrCreateShortcut(category, GlobalShortcutNames.transform_RotateAroundX, GlobalShortcutContext.instance, new Global_Transform_RotateAroundX());
            getOrCreateShortcut(category, GlobalShortcutNames.transform_RotateAroundY, GlobalShortcutContext.instance, new Global_Transform_RotateAroundY());
            getOrCreateShortcut(category, GlobalShortcutNames.transform_RotateAroundZ, GlobalShortcutContext.instance, new Global_Transform_RotateAroundZ());
            getOrCreateShortcut(category, GlobalShortcutNames.transform_RotateAroundXAroundCenter, GlobalShortcutContext.instance, new Global_Transform_RotateAroundXAroundCenter());
            getOrCreateShortcut(category, GlobalShortcutNames.transform_RotateAroundYAroundCenter, GlobalShortcutContext.instance, new Global_Transform_RotateAroundYAroundCenter());
            getOrCreateShortcut(category, GlobalShortcutNames.transform_RotateAroundZAroundCenter, GlobalShortcutContext.instance, new Global_Transform_RotateAroundZAroundCenter());
            getOrCreateShortcut(category, GlobalShortcutNames.transform_ResetRotationToOriginal, GlobalShortcutContext.instance, new Global_Transform_ResetRotationToOriginal());
            getOrCreateShortcut(category, GlobalShortcutNames.transform_ResetScaleToOriginal, GlobalShortcutContext.instance, new Global_Transform_ResetScaleToOriginal());
            getOrCreateShortcut(category, GlobalShortcutNames.mirrorGizmo_Toggle, GlobalShortcutContext.instance, new Global_MirrorGizmo_Toggle());
            getOrCreateShortcut(category, GlobalShortcutNames.mirrorGizmo_SnapToView, GlobalShortcutContext.instance, new Global_MirrorGizmo_SnapToView());
            getOrCreateShortcut(category, GlobalShortcutNames.selection_FrameSelected, GlobalShortcutContext.instance, new Global_Selection_FrameSelected());
            getOrCreateShortcut(category, GlobalShortcutNames.selection_DeleteSelected, GlobalShortcutContext.instance, new Global_Selection_DeleteSelected());
            getOrCreateShortcut(category, GlobalShortcutNames.selection_DuplicateSelected, GlobalShortcutContext.instance, new Global_Selection_DuplicateSelected());

            // ================================= Object Transform Sessions ================================= //
            category = getOrCreateShortcutCategory(ShortcutCategoryNames.objectTransformSessions);
            getOrCreateShortcut(category, ObjectTransformSessionsShortcutNames.modularSnap_VerticalStepUp, GlobalShortcutContext.instance.objectModularSnapShortcutContext, new ObjectTransformSession_ModularSnap_VerticalStepUp());
            getOrCreateShortcut(category, ObjectTransformSessionsShortcutNames.modularSnap_VerticalStepDown, GlobalShortcutContext.instance.objectModularSnapShortcutContext, new ObjectTransformSession_ModularSnap_VerticalStepDown());
            getOrCreateShortcut(category, ObjectTransformSessionsShortcutNames.modularSnap_ResetVerticalStep, GlobalShortcutContext.instance.objectModularSnapShortcutContext, new ObjectTransformSession_ModularSnap_ResetVerticalStep());
            getOrCreateShortcut(category, ObjectTransformSessionsShortcutNames.modularSnap_ResetVerticalStepToOriginal, GlobalShortcutContext.instance.objectModularSnapShortcutContext, new ObjectTransformSession_ModularSnap_ResetVerticalStepToOriginal());
            getOrCreateShortcut(category, ObjectTransformSessionsShortcutNames.modularSnap_ToggleHalfSpace, GlobalShortcutContext.instance.objectModularSnapShortcutContext, new ObjectTransformSession_ModularSnap_ToggleHalfSpace());
            getOrCreateShortcut(category, ObjectTransformSessionsShortcutNames.modularSnap_ToggleObject2ObjectSnap, GlobalShortcutContext.instance.objectModularSnapShortcutContext, new ObjectTransformSession_ModularSnap_ToggleObject2ObjectSnap());
            getOrCreateShortcut(category, ObjectTransformSessionsShortcutNames.modularSnap_ToggleGridSnapClimb, GlobalShortcutContext.instance.objectModularSnapShortcutContext, new ObjectTransformSession_ModularSnap_ToggleGridSnapObjectClimb());
            getOrCreateShortcut(category, ObjectTransformSessionsShortcutNames.modularSnap_ToggleAlignmentHighlights, GlobalShortcutContext.instance.objectModularSnapShortcutContext, new ObjectTransformSession_ModularSnap_ToggleAlignmentHighlights());
            getOrCreateShortcut(category, ObjectTransformSessionsShortcutNames.modularSnap_ToggleAlignmentHints, GlobalShortcutContext.instance.objectModularSnapShortcutContext, new ObjectTransformSession_ModularSnap_ToggleAlignmentHints());
            getOrCreateShortcut(category, ObjectTransformSessionsShortcutNames.surfaceSnap_ResetMouseOffsetFromSurface, GlobalShortcutContext.instance.objectSurfaceSnapShortcutContext, new ObjectTransformSession_SurfaceSnap_ResetMouseOffsetFromSurface());
            getOrCreateShortcut(category, ObjectTransformSessionsShortcutNames.surfaceSnap_ToggleAxisAlignment, GlobalShortcutContext.instance.objectSurfaceSnapShortcutContext, new ObjectTransformSession_SurfaceSnap_ToggleAxisAlignment());

            // ================================= Object Spawn ================================= //
            category = getOrCreateShortcutCategory(ShortcutCategoryNames.objectSpawn);
            getOrCreateShortcut(category, ObjectSpawnShortcutNames.spawnGuide_SyncGridCellSize, GlobalShortcutContext.instance.objectSpawnContext, new ObjectSpawn_SpawnGuide_SyncGridCellSize());
            getOrCreateShortcut(category, ObjectSpawnShortcutNames.spawnGuide_ToggleDecorRules, GlobalShortcutContext.instance.objectSpawnContext, new ObjectSpawn_SpawnGuide_ToggleDecorRules());
            getOrCreateShortcut(category, ObjectSpawnShortcutNames.tileRuleSpawn_Paint, GlobalShortcutContext.instance.objectSpawn_TileRules_ShortcutContext, new ObjectSpawn_TileRuleSpawn_EnablePaintMode());
            getOrCreateShortcut(category, ObjectSpawnShortcutNames.tileRuleSpawn_Ramp, GlobalShortcutContext.instance.objectSpawn_TileRules_ShortcutContext, new ObjectSpawn_TileRuleSpawn_EnableRampMode());
            getOrCreateShortcut(category, ObjectSpawnShortcutNames.tileRuleSpawn_Erase, GlobalShortcutContext.instance.objectSpawn_TileRules_ShortcutContext, new ObjectSpawn_TileRuleSpawn_EnableEraseMode());
            getOrCreateShortcut(category, ObjectSpawnShortcutNames.tileRuleSpawn_Connect, GlobalShortcutContext.instance.objectSpawn_TileRules_ShortcutContext, new ObjectSpawn_TileRuleSpawn_EnableConnectMode());
            getOrCreateShortcut(category, ObjectSpawnShortcutNames.tileRuleSpawn_Connect_ChangeMajorAxis, GlobalShortcutContext.instance.objectSpawn_TileRules_ShortcutContext, new ObjectSpawn_TileRuleSpawn_ConnectMode_ChangeMajorAxis());
            getOrCreateShortcut(category, ObjectSpawnShortcutNames.tileRuleSpawn_BoxBrush, GlobalShortcutContext.instance.objectSpawn_TileRules_ShortcutContext, new ObjectSpawn_TileRuleSpawn_EnableBoxBrush());
            getOrCreateShortcut(category, ObjectSpawnShortcutNames.tileRuleSpawn_FlexiBoxBrush, GlobalShortcutContext.instance.objectSpawn_TileRules_ShortcutContext, new ObjectSpawn_TileRuleSpawn_EnableFlexiBoxBrush());
            getOrCreateShortcut(category, ObjectSpawnShortcutNames.tileRuleSpawn_SegmentsBrush, GlobalShortcutContext.instance.objectSpawn_TileRules_ShortcutContext, new ObjectSpawn_TileRuleSpawn_EnableSegmentsBrush());
            getOrCreateShortcut(category, ObjectSpawnShortcutNames.curveSpawn_SelectAllControlPoints, GlobalShortcutContext.instance.objectSpawn_Curve_ShortcutContext, new ObjectSpawn_CurveSpawn_SelectAllControlPoints());
            getOrCreateShortcut(category, ObjectSpawnShortcutNames.curveSpawn_InsertControlPoint, GlobalShortcutContext.instance.objectSpawn_Curve_ShortcutContext, new ObjectSpawn_CurveSpawn_EnableInsertControlPoint());
            getOrCreateShortcut(category, ObjectSpawnShortcutNames.curveSpawn_ProjectSelectedControlPoints, GlobalShortcutContext.instance.objectSpawn_Curve_ShortcutContext, new ObjectSpawn_CurveSpawn_ProjectSelectedControlPoints());
            getOrCreateShortcut(category, ObjectSpawnShortcutNames.curveSpawn_MoveGizmo, GlobalShortcutContext.instance.objectSpawn_Curve_ShortcutContext, new ObjectSpawn_CurveSpawn_EnableMoveGizmo());
            getOrCreateShortcut(category, ObjectSpawnShortcutNames.curveSpawn_RotationGizmo, GlobalShortcutContext.instance.objectSpawn_Curve_ShortcutContext, new ObjectSpawn_CurveSpawn_EnableRotationGizmo());
            getOrCreateShortcut(category, ObjectSpawnShortcutNames.curveSpawn_ScaleGizmo, GlobalShortcutContext.instance.objectSpawn_Curve_ShortcutContext, new ObjectSpawn_CurveSpawn_EnableScaleGizmo());
           
            // ================================= Object Selection ================================= //
            category = getOrCreateShortcutCategory(ShortcutCategoryNames.objectSelection);
            getOrCreateShortcut(category, ObjectSelectionShortcutNames.snapAllAxes, GlobalShortcutContext.instance.objectSelectionContext, new ObjectSelection_SnapAllAxes());
            getOrCreateShortcut(category, ObjectSelectionShortcutNames.selectSimilarPrefabs, GlobalShortcutContext.instance.objectSelectionContext, new ObjectSelection_SelectSimilarPrefabs());
            getOrCreateShortcut(category, ObjectSelectionShortcutNames.selectPrefabsInManager, GlobalShortcutContext.instance.objectSelectionContext, new ObjectSelection_SelectPrefabsInManager());
            getOrCreateShortcut(category, ObjectSelectionShortcutNames.projectOnGrid, GlobalShortcutContext.instance.objectSelectionContext, new ObjectSelection_ProjectOnGrid());
            getOrCreateShortcut(category, ObjectSelectionShortcutNames.projectOnObject, GlobalShortcutContext.instance.objectSelectionContext, new ObjectSelection_BeginProjectOnObject());
            getOrCreateShortcut(category, ObjectSelectionShortcutNames.moveGizmo, GlobalShortcutContext.instance.objectSelectionContext, new ObjectSelection_EnableMoveGizmo());
            getOrCreateShortcut(category, ObjectSelectionShortcutNames.rotationGizmo, GlobalShortcutContext.instance.objectSelectionContext, new ObjectSelection_EnableRotationGizmo());
            getOrCreateShortcut(category, ObjectSelectionShortcutNames.scaleGizmo, GlobalShortcutContext.instance.objectSelectionContext, new ObjectSelection_EnableScaleGizmo());
            getOrCreateShortcut(category, ObjectSelectionShortcutNames.universalGizmo, GlobalShortcutContext.instance.objectSelectionContext, new ObjectSelection_EnableUniversalGizmo());
            getOrCreateShortcut(category, ObjectSelectionShortcutNames.extrudeGizmo, GlobalShortcutContext.instance.objectSelectionContext, new ObjectSelection_EnableExtrudeGizmo());
            getOrCreateShortcut(category, ObjectSelectionShortcutNames.selectionRect, GlobalShortcutContext.instance.objectSelectionContext, new ObjectSelection_EnableSelectionRectangle());
            getOrCreateShortcut(category, ObjectSelectionShortcutNames.selectionSegments, GlobalShortcutContext.instance.objectSelectionContext, new ObjectSelection_EnableSelectionSegments());
            getOrCreateShortcut(category, ObjectSelectionShortcutNames.selectionBox, GlobalShortcutContext.instance.objectSelectionContext, new ObjectSelection_EnableSelectionBox());
            getOrCreateShortcut(category, ObjectSelectionShortcutNames.mirrorSelected, GlobalShortcutContext.instance.objectSelectionContext, new ObjectSelection_MirrorSelected());
            getOrCreateShortcut(category, ObjectSelectionShortcutNames.modularSnap, GlobalShortcutContext.instance.objectSelectionContext, new ObjectSelection_EnableModularSnap());
            getOrCreateShortcut(category, ObjectSelectionShortcutNames.surfaceSnap, GlobalShortcutContext.instance.objectSelectionContext, new ObjectSelection_EnableSurfaceSnap());       
            getOrCreateShortcut(category, ObjectSelectionShortcutNames.vertexSnap, GlobalShortcutContext.instance.objectSelectionContext, new ObjectSelection_EnableVertexSnap());
            getOrCreateShortcut(category, ObjectSelectionShortcutNames.boxSnap, GlobalShortcutContext.instance.objectSelectionContext, new ObjectSelection_EnableBoxSnap());
            getOrCreateShortcut(category, ObjectSelectionShortcutNames.filterOutOfView, GlobalShortcutContext.instance.objectSelectionContext, new ObjectSelection_FilterOutOfView());
            getOrCreateShortcut(category, ObjectSelectionShortcutNames.grow, GlobalShortcutContext.instance.objectSelectionContext, new ObjectSelection_Grow());

            // ================================= Object Erase ================================= //
            category = getOrCreateShortcutCategory(ShortcutCategoryNames.objectErase);
            getOrCreateShortcut(category, ObjectEraseShortcutNames.eraseCursor, GlobalShortcutContext.instance.objectEraseContext, new ObjectErase_EnableEraseCursor());
            getOrCreateShortcut(category, ObjectEraseShortcutNames.eraseBrush2D, GlobalShortcutContext.instance.objectEraseContext, new ObjectErase_EnableEraseBrush2D());
            getOrCreateShortcut(category, ObjectEraseShortcutNames.eraseBrush3D, GlobalShortcutContext.instance.objectEraseContext, new ObjectErase_EnableEraseBrush3D());

            // Note: Useful to ensure that no conflicts are introduced during development.
            detectConflicts(false);
        }
    }
}
#endif
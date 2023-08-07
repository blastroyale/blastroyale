#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public static class ShortcutDefaults
    {
        private static Dictionary<string, KeyCombo.State> _defaultMap = new Dictionary<string, KeyCombo.State>();

        static ShortcutDefaults()
        {
            // ================================= Global ================================= //
            _defaultMap.Add(GlobalShortcutNames.objectSpawn, new KeyCombo.State() { key = KeyCode.Alpha1 });
            _defaultMap.Add(GlobalShortcutNames.objectSelection, new KeyCombo.State() { key = KeyCode.Alpha2 });
            _defaultMap.Add(GlobalShortcutNames.objectErase, new KeyCombo.State() { key = KeyCode.Alpha3 });
            _defaultMap.Add(GlobalShortcutNames.grid_VerticalStepDown, new KeyCombo.State() { key = KeyCode.LeftBracket });
            _defaultMap.Add(GlobalShortcutNames.grid_VerticalStepUp, new KeyCombo.State() { key = KeyCode.RightBracket });
            _defaultMap.Add(GlobalShortcutNames.grid_SnapToPickedObject, new KeyCombo.State() { key = KeyCode.G });
            _defaultMap.Add(GlobalShortcutNames.transform_RotateAroundX, new KeyCombo.State() { key = KeyCode.X });
            _defaultMap.Add(GlobalShortcutNames.transform_RotateAroundY, new KeyCombo.State() { key = KeyCode.Y });
            _defaultMap.Add(GlobalShortcutNames.transform_RotateAroundZ, new KeyCombo.State() { key = KeyCode.Z });
            _defaultMap.Add(GlobalShortcutNames.transform_RotateAroundXAroundCenter, new KeyCombo.State() { shift = true, key = KeyCode.X });
            _defaultMap.Add(GlobalShortcutNames.transform_RotateAroundYAroundCenter, new KeyCombo.State() { shift = true, key = KeyCode.Y });
            _defaultMap.Add(GlobalShortcutNames.transform_RotateAroundZAroundCenter, new KeyCombo.State() { shift = true, key = KeyCode.Z });
            _defaultMap.Add(GlobalShortcutNames.transform_ResetRotationToOriginal, new KeyCombo.State() { key = KeyCode.I });
            _defaultMap.Add(GlobalShortcutNames.transform_ResetScaleToOriginal, new KeyCombo.State() { key = KeyCode.O });
            _defaultMap.Add(GlobalShortcutNames.mirrorGizmo_Toggle, new KeyCombo.State() { ctrl = true, key = KeyCode.Q });
            _defaultMap.Add(GlobalShortcutNames.mirrorGizmo_SnapToView, new KeyCombo.State() { ctrl = true, key = KeyCode.F });
            _defaultMap.Add(GlobalShortcutNames.selection_FrameSelected, new KeyCombo.State() { key = KeyCode.F });
            _defaultMap.Add(GlobalShortcutNames.selection_DeleteSelected, new KeyCombo.State() { key = KeyCode.Delete });
            _defaultMap.Add(GlobalShortcutNames.selection_DuplicateSelected, new KeyCombo.State() { ctrl = true, key = KeyCode.D });

            // ================================= Object Transform Sessions ================================= //
            _defaultMap.Add(ObjectTransformSessionsShortcutNames.modularSnap_VerticalStepUp, new KeyCombo.State() { key = KeyCode.E });
            _defaultMap.Add(ObjectTransformSessionsShortcutNames.modularSnap_VerticalStepDown, new KeyCombo.State() { key = KeyCode.Q });
            _defaultMap.Add(ObjectTransformSessionsShortcutNames.modularSnap_ResetVerticalStep, new KeyCombo.State() { key = KeyCode.R });
            _defaultMap.Add(ObjectTransformSessionsShortcutNames.modularSnap_ResetVerticalStepToOriginal, new KeyCombo.State() { shift = true, key = KeyCode.R });
            _defaultMap.Add(ObjectTransformSessionsShortcutNames.modularSnap_ToggleHalfSpace, new KeyCombo.State() { key = KeyCode.N });
            _defaultMap.Add(ObjectTransformSessionsShortcutNames.modularSnap_ToggleObject2ObjectSnap, new KeyCombo.State() { key = KeyCode.S });
            _defaultMap.Add(ObjectTransformSessionsShortcutNames.modularSnap_ToggleGridSnapClimb, new KeyCombo.State() { key = KeyCode.C, shift = true });
            _defaultMap.Add(ObjectTransformSessionsShortcutNames.modularSnap_ToggleAlignmentHighlights, new KeyCombo.State() { key = KeyCode.Space });
            _defaultMap.Add(ObjectTransformSessionsShortcutNames.modularSnap_ToggleAlignmentHints, new KeyCombo.State() { key = KeyCode.Space, shift = true });
            _defaultMap.Add(ObjectTransformSessionsShortcutNames.surfaceSnap_ResetMouseOffsetFromSurface, new KeyCombo.State() { key = KeyCode.R });
            _defaultMap.Add(ObjectTransformSessionsShortcutNames.surfaceSnap_ToggleAxisAlignment, new KeyCombo.State() { shift = true, key = KeyCode.A });

            // ================================= Object Spawn ================================= //
            _defaultMap.Add(ObjectSpawnShortcutNames.spawnGuide_SyncGridCellSize, new KeyCombo.State() { key = KeyCode.K });
            _defaultMap.Add(ObjectSpawnShortcutNames.spawnGuide_ToggleDecorRules, new KeyCombo.State() { key = KeyCode.V, shift = true });
            _defaultMap.Add(ObjectSpawnShortcutNames.tileRuleSpawn_Paint, new KeyCombo.State() { key = KeyCode.Q });
            _defaultMap.Add(ObjectSpawnShortcutNames.tileRuleSpawn_Ramp, new KeyCombo.State() { key = KeyCode.W });
            _defaultMap.Add(ObjectSpawnShortcutNames.tileRuleSpawn_Erase, new KeyCombo.State() { key = KeyCode.E });
            _defaultMap.Add(ObjectSpawnShortcutNames.tileRuleSpawn_Connect, new KeyCombo.State() { key = KeyCode.R });
            _defaultMap.Add(ObjectSpawnShortcutNames.tileRuleSpawn_Connect_ChangeMajorAxis, new KeyCombo.State() { key = KeyCode.Space });
            _defaultMap.Add(ObjectSpawnShortcutNames.tileRuleSpawn_BoxBrush, new KeyCombo.State() { key = KeyCode.Q, shift = true });
            _defaultMap.Add(ObjectSpawnShortcutNames.tileRuleSpawn_FlexiBoxBrush, new KeyCombo.State() { key = KeyCode.W, shift = true });
            _defaultMap.Add(ObjectSpawnShortcutNames.tileRuleSpawn_SegmentsBrush, new KeyCombo.State() { key = KeyCode.E, shift = true });
            _defaultMap.Add(ObjectSpawnShortcutNames.curveSpawn_SelectAllControlPoints, new KeyCombo.State() { ctrl = true, key = KeyCode.A });
            _defaultMap.Add(ObjectSpawnShortcutNames.curveSpawn_InsertControlPoint, new KeyCombo.State() { key = KeyCode.C });
            _defaultMap.Add(ObjectSpawnShortcutNames.curveSpawn_ProjectSelectedControlPoints, new KeyCombo.State() { shift = true, key = KeyCode.F });
            _defaultMap.Add(ObjectSpawnShortcutNames.curveSpawn_MoveGizmo, new KeyCombo.State() { key = KeyCode.W });
            _defaultMap.Add(ObjectSpawnShortcutNames.curveSpawn_RotationGizmo, new KeyCombo.State() { key = KeyCode.E });
            _defaultMap.Add(ObjectSpawnShortcutNames.curveSpawn_ScaleGizmo, new KeyCombo.State() { key = KeyCode.R });

            // ================================= Object Selection ================================= //
            _defaultMap.Add(ObjectSelectionShortcutNames.snapAllAxes, new KeyCombo.State() { shift = true, key = KeyCode.S });
            _defaultMap.Add(ObjectSelectionShortcutNames.selectSimilarPrefabs, new KeyCombo.State() { shift = true, key = KeyCode.A });
            _defaultMap.Add(ObjectSelectionShortcutNames.selectPrefabsInManager, new KeyCombo.State() { shift = true, key = KeyCode.C });
            _defaultMap.Add(ObjectSelectionShortcutNames.projectOnGrid, new KeyCombo.State() { shift = true, key = KeyCode.G });
            _defaultMap.Add(ObjectSelectionShortcutNames.projectOnObject, new KeyCombo.State() { shift = true, key = KeyCode.F });
            _defaultMap.Add(ObjectSelectionShortcutNames.moveGizmo, new KeyCombo.State() { key = KeyCode.W });
            _defaultMap.Add(ObjectSelectionShortcutNames.rotationGizmo, new KeyCombo.State() { key = KeyCode.E });
            _defaultMap.Add(ObjectSelectionShortcutNames.scaleGizmo, new KeyCombo.State() { key = KeyCode.R });
            _defaultMap.Add(ObjectSelectionShortcutNames.universalGizmo, new KeyCombo.State() { key = KeyCode.T });
            _defaultMap.Add(ObjectSelectionShortcutNames.extrudeGizmo, new KeyCombo.State() { key = KeyCode.U });
            _defaultMap.Add(ObjectSelectionShortcutNames.selectionRect, new KeyCombo.State() { shift = true, key = KeyCode.Alpha1 });
            _defaultMap.Add(ObjectSelectionShortcutNames.selectionSegments, new KeyCombo.State() { shift = true, key = KeyCode.Alpha2 });
            _defaultMap.Add(ObjectSelectionShortcutNames.selectionBox, new KeyCombo.State() { shift = true, key = KeyCode.Alpha3 });
            _defaultMap.Add(ObjectSelectionShortcutNames.mirrorSelected, new KeyCombo.State() { key = KeyCode.M });
            _defaultMap.Add(ObjectSelectionShortcutNames.vertexSnap, new KeyCombo.State() { key = KeyCode.V });
            _defaultMap.Add(ObjectSelectionShortcutNames.boxSnap, new KeyCombo.State() { key = KeyCode.B });
            _defaultMap.Add(ObjectSelectionShortcutNames.surfaceSnap, new KeyCombo.State() { key = KeyCode.C });
            _defaultMap.Add(ObjectSelectionShortcutNames.modularSnap, new KeyCombo.State() { key = KeyCode.D });
            _defaultMap.Add(ObjectSelectionShortcutNames.filterOutOfView, new KeyCombo.State() { shift = true, key = KeyCode.V });
            _defaultMap.Add(ObjectSelectionShortcutNames.grow, new KeyCombo.State() { key = KeyCode.Space });

            // ================================= Object Erase ================================= //
            _defaultMap.Add(ObjectEraseShortcutNames.eraseCursor, new KeyCombo.State() { shift = true, key = KeyCode.Alpha1 });
            _defaultMap.Add(ObjectEraseShortcutNames.eraseBrush2D, new KeyCombo.State() { shift = true, key = KeyCode.Alpha2 });
            _defaultMap.Add(ObjectEraseShortcutNames.eraseBrush3D, new KeyCombo.State() { shift = true, key = KeyCode.Alpha3 });
        }

        public static KeyCombo.State shortcutDefault(string shortcutName)
        {
            return _defaultMap[shortcutName];
        }
    }
}
#endif
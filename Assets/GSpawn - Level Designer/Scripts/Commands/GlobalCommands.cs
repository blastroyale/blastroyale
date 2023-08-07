#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public class Global_EnableObjectSpawnTool : PluginCommand
    {
        protected override void onEnter()
        {
            GSpawn.active.levelDesignToolId = LevelDesignToolId.ObjectSpawn;
        }
    }

    public class Global_EnableObjectSelectionTool : PluginCommand
    {
        protected override void onEnter()
        {
            GSpawn.active.levelDesignToolId = LevelDesignToolId.ObjectSelection;
        }
    }

    public class Global_EnableObjectEraseTool : PluginCommand
    {
        protected override void onEnter()
        {
            GSpawn.active.levelDesignToolId = LevelDesignToolId.ObjectErase;
        }
    }

    public class Global_Grid_VerticalStepDown : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.TileRules) return;

            var grid = PluginScene.instance.grid;
            grid.activeSettings.localOriginYOffset -= grid.activeSettings.cellSizeY;
        }
    }

    public class Global_Grid_VerticalStepUp : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn && 
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.TileRules) return;

            var grid = PluginScene.instance.grid;
            grid.activeSettings.localOriginYOffset += grid.activeSettings.cellSizeY;
        }
    }

    public class Global_Grid_EnableSnapToPickedObject : PluginCommand
    {
        protected override void onEnter()
        {
            ObjectSelection.instance.clickSelectEnabled         = false;
            ObjectSelection.instance.gizmosEnabled              = false;
            ObjectSelection.instance.multiSelectEnabled         = false;
            PluginScene.instance.snapGridToPickedObjectEnabled  = true;
        }

        protected override void onExit()
        {
            ObjectSelection.instance.clickSelectEnabled         = true;
            ObjectSelection.instance.gizmosEnabled              = true;
            ObjectSelection.instance.multiSelectEnabled         = true;
            PluginScene.instance.snapGridToPickedObjectEnabled  = false;
        }
    }

    public class Global_Transform_RotateAroundX : PluginCommand
    {
        protected override void onEnter()
        {
            var toolId = GSpawn.active.levelDesignToolId;
            Vector3 rotationAxis = InputPrefs.instance.getRotationAxis(0);

            if (toolId == LevelDesignToolId.ObjectSpawn) ObjectSpawn.instance.rotateSpawnGuide(rotationAxis, InputPrefs.instance.keyboardXRotationStep);
            else if (toolId == LevelDesignToolId.ObjectSelection) ObjectSelection.instance.rotate(rotationAxis, InputPrefs.instance.keyboardXRotationStep);
        }
    }

    public class Global_Transform_RotateAroundXAroundCenter : PluginCommand
    {
        protected override void onEnter()
        {
            var toolId = GSpawn.active.levelDesignToolId;
            Vector3 rotationAxis = InputPrefs.instance.getRotationAxis(0);

            if (toolId == LevelDesignToolId.ObjectSpawn) ObjectSpawn.instance.rotateSpawnGuide(ObjectSpawn.instance.calcSpawnGuideWorldOBB().center, rotationAxis, InputPrefs.instance.keyboardXRotationStep);
            else if (toolId == LevelDesignToolId.ObjectSelection) ObjectSelection.instance.rotate(ObjectSelection.instance.calcSelectionCenter(), rotationAxis, InputPrefs.instance.keyboardXRotationStep);
        }
    }

    public class Global_Transform_RotateAroundY : PluginCommand
    {
        protected override void onEnter()
        {
            var toolId = GSpawn.active.levelDesignToolId;
            Vector3 rotationAxis = InputPrefs.instance.getRotationAxis(1);

            if (toolId == LevelDesignToolId.ObjectSpawn)
            {
                ObjectSpawn objectSpawn = ObjectSpawn.instance;
                if (objectSpawn.activeToolId != ObjectSpawnToolId.TileRules)
                    ObjectSpawn.instance.rotateSpawnGuide(rotationAxis, InputPrefs.instance.keyboardYRotationStep);
                else
                {
                    var tileRuleObjectSpawn = objectSpawn.tileRuleObjectSpawn;
                    if (tileRuleObjectSpawn.activeToolId == TileRuleSpawnToolId.RampPaint)
                    {
                        var currentGrid = tileRuleObjectSpawn.findCurrentGrid();
                        if (currentGrid != null)
                        {
                            currentGrid.rotateRamp(tileRuleObjectSpawn.rampBrushCellCoords);
                        }
                    }
                }
            }
            else if (toolId == LevelDesignToolId.ObjectSelection) ObjectSelection.instance.rotate(rotationAxis, InputPrefs.instance.keyboardYRotationStep);
        }
    }

    public class Global_Transform_RotateAroundYAroundCenter : PluginCommand
    {
        protected override void onEnter()
        {
            var toolId = GSpawn.active.levelDesignToolId;
            Vector3 rotationAxis = InputPrefs.instance.getRotationAxis(1);

            if (toolId == LevelDesignToolId.ObjectSpawn) ObjectSpawn.instance.rotateSpawnGuide(ObjectSpawn.instance.calcSpawnGuideWorldOBB().center, rotationAxis, InputPrefs.instance.keyboardYRotationStep);
            else if (toolId == LevelDesignToolId.ObjectSelection) ObjectSelection.instance.rotate(ObjectSelection.instance.calcSelectionCenter(), rotationAxis, InputPrefs.instance.keyboardYRotationStep);
        }
    }

    public class Global_Transform_RotateAroundZ : PluginCommand
    {
        protected override void onEnter()
        {
            var toolId = GSpawn.active.levelDesignToolId;
            Vector3 rotationAxis = InputPrefs.instance.getRotationAxis(2);

            if (toolId == LevelDesignToolId.ObjectSpawn) ObjectSpawn.instance.rotateSpawnGuide(rotationAxis, InputPrefs.instance.keyboardZRotationStep);
            else if (toolId == LevelDesignToolId.ObjectSelection) ObjectSelection.instance.rotate(rotationAxis, InputPrefs.instance.keyboardZRotationStep);
        }
    }

    public class Global_Transform_RotateAroundZAroundCenter : PluginCommand
    {
        protected override void onEnter()
        {
            var toolId = GSpawn.active.levelDesignToolId;
            Vector3 rotationAxis = InputPrefs.instance.getRotationAxis(2);

            if (toolId == LevelDesignToolId.ObjectSpawn) ObjectSpawn.instance.rotateSpawnGuide(ObjectSpawn.instance.calcSpawnGuideWorldOBB().center, rotationAxis, InputPrefs.instance.keyboardZRotationStep);
            else if (toolId == LevelDesignToolId.ObjectSelection) ObjectSelection.instance.rotate(ObjectSelection.instance.calcSelectionCenter(), rotationAxis, InputPrefs.instance.keyboardZRotationStep);
        }
    }

    public class Global_Transform_ResetRotationToOriginal : PluginCommand
    {
        protected override void onEnter()
        {
            var toolId = GSpawn.active.levelDesignToolId;
            if (toolId == LevelDesignToolId.ObjectSpawn) ObjectSpawn.instance.resetSpawnGuideRotationToOriginal();
            else if (toolId == LevelDesignToolId.ObjectSelection) ObjectSelection.instance.resetRotationToOriginal();
        }
    }

    public class Global_Transform_ResetScaleToOriginal : PluginCommand
    {
        protected override void onEnter()
        {
            var toolId = GSpawn.active.levelDesignToolId;
            if (toolId == LevelDesignToolId.ObjectSpawn) ObjectSpawn.instance.resetSpawnGuideScaleToOriginal();
            else if (toolId == LevelDesignToolId.ObjectSelection) ObjectSelection.instance.resetScaleToOriginal();
        }
    }

    public class Global_MirrorGizmo_Toggle : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn)
            {
                if (ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.TileRules)
                {
                    var currentGrid = ObjectSpawn.instance.tileRuleObjectSpawn.findCurrentGrid();
                    if (currentGrid != null)
                    {
                        currentGrid.mirroringEnabled = !currentGrid.mirroringEnabled;
                        TileRuleObjectSpawnUI.instance.refresh();
                    }
                }
                else ObjectSpawn.instance.setMirrorGizmoEnabled(!ObjectSpawn.instance.isMirrorGizmoEnabled);
            }
            else if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelectionGizmos.instance.setGizmoEnabled(ObjectSelectionGizmoId.Mirror, !ObjectSelectionGizmos.instance.isGizmoEnabled(ObjectSelectionGizmoId.Mirror), false);
        }
    }

    public class Global_MirrorGizmo_SnapToView : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn)
            {
                if (ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.TileRules)
                {
                    var currentGrid = ObjectSpawn.instance.tileRuleObjectSpawn.findCurrentGrid();
                    if (currentGrid != null) currentGrid.snapMirrorGizmoToView(true);
                }
                else ObjectSpawn.instance.snapMirrorGizmoToView(true);
            }
            else if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelectionGizmos.instance.snapMirrorGizmoToView(true);
        }
    }

    public class Global_Selection_FrameSelected : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelection.instance.frameSelected();
            else 
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn)
            {
                if (ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.Curve)
                    ObjectSpawnCurveDb.instance.frameSelectedCurves();
            }
        }
    }

    public class Global_Selection_DeleteSelected : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelection.instance.deleteSelected();
            else
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.Curve)
            {
                int numCurves = ObjectSpawnCurveDb.instance.numCurves;
                for (int i = 0; i < numCurves; ++i)
                {
                    var curve = ObjectSpawnCurveDb.instance.getCurve(i);
                    if (curve.uiSelected) curve.removeSelectedControlPoints();
                }
            }
        }
    }

    public class Global_Selection_DuplicateSelected : PluginCommand
    {
        private static List<ObjectSpawnCurve> _curveBuffer = new List<ObjectSpawnCurve>();

        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelection.instance.duplicateSelected();
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.Curve)
            {
                _curveBuffer.Clear();
                int numCurves = ObjectSpawnCurveDb.instance.numCurves;
                for (int i = 0; i < numCurves; ++i)
                {
                    var curve = ObjectSpawnCurveDb.instance.getCurve(i);
                    if (curve.uiSelected)
                    {
                        var clonedCurve = ObjectSpawnCurveDb.instance.cloneCurve(curve);
                        _curveBuffer.Add(clonedCurve);
                    }
                }

                if (_curveBuffer.Count != 0)
                {
                    // Note: Refresh UI to add curves to curve view and then mark them as selected.
                    CurveObjectSpawnUI.instance.refresh();
                    CurveObjectSpawnUI.instance.setSelectedCurves(_curveBuffer);
                }
            }
        }
    }
}
#endif
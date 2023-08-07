#if UNITY_EDITOR
namespace GSpawn
{
    public class ObjectSpawn_EnableModularSnapSpawn : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn)
                ObjectSpawn.instance.activeToolId = ObjectSpawnToolId.ModularSnap;
        }
    }

    public class ObjectSpawn_EnableSegmentsSpawn : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn)
                ObjectSpawn.instance.activeToolId = ObjectSpawnToolId.Segments;
        }
    }

    public class ObjectSpawn_EnableBoxSpawn : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn)
                ObjectSpawn.instance.activeToolId = ObjectSpawnToolId.Box;
        }
    }

    public class ObjectSpawn_EnablePropsSpawn : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn)
                ObjectSpawn.instance.activeToolId = ObjectSpawnToolId.Props;
        }
    }

    public class ObjectSpawn_EnableScatterBrushSpawn : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn)
                ObjectSpawn.instance.activeToolId = ObjectSpawnToolId.ScatterBrush;
        }
    }

    public class ObjectSpawn_EnableTileRuleSpawn : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn)
                ObjectSpawn.instance.activeToolId = ObjectSpawnToolId.TileRules;
        }
    }

    public class ObjectSpawn_EnableCurveSpawn : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn)
                ObjectSpawn.instance.activeToolId = ObjectSpawnToolId.Curve;
        }
    }

    public class ObjectSpawn_EnablePhysicsSpawn : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn)
                ObjectSpawn.instance.activeToolId = ObjectSpawnToolId.Physics;
        }
    }

    public class ObjectSpawn_SpawnGuide_ToggleDecorRules : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn)
            {
                var objectSpawn = ObjectSpawn.instance;
                var toolId      = ObjectSpawn.instance.activeToolId;
                if (toolId == ObjectSpawnToolId.Props)
                {
                    UndoEx.saveEnabledState();
                    UndoEx.enabled = false;
                    var spawnGuideSettings = objectSpawn.propsObjectSpawn.spawnGuideSettings;
                    spawnGuideSettings.applyDecorRules = !spawnGuideSettings.applyDecorRules;
                    UndoEx.restoreEnabledState();
                    PluginInspectorUI.instance.targetEditor.Repaint();
                }
            }
        }
    }

    public class ObjectSpawn_SpawnGuide_SyncGridCellSize : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn)
            {
                var objectSpawn = ObjectSpawn.instance;
                var toolId      = ObjectSpawn.instance.activeToolId;
                if (toolId == ObjectSpawnToolId.ModularSnap || 
                    (toolId == ObjectSpawnToolId.ModularWalls && !objectSpawn.modularWallObjectSpawn.isBuildingWalls) ||
                    (toolId == ObjectSpawnToolId.Segments && !objectSpawn.segmentsObjectSpawn.isBuildingSegments) ||
                    (toolId == ObjectSpawnToolId.Box && !objectSpawn.boxObjectSpawn.isBuildingBox))
                {
                    var spawnGuide = objectSpawn.activeTool.spawnGuide;
                    if (spawnGuide != null) spawnGuide.syncGridCellSizeToPrefabSize();
                }
            }
        }
    }

    public class ObjectSpawn_SegmentsSpawn_Raise : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.Segments &&
                ObjectSpawn.instance.segmentsObjectSpawn.isBuildingSegments)
            {
                ObjectSpawn.instance.segmentsObjectSpawn.raiseCurrentHeight();
            }
        }
    }

    public class ObjectSpawn_SegmentsSpawn_Lower : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.Segments &&
                ObjectSpawn.instance.segmentsObjectSpawn.isBuildingSegments)
            {
                ObjectSpawn.instance.segmentsObjectSpawn.lowerCurrentHeight();
            }
        }
    }

    public class ObjectSpawn_BoxSpawn_Raise : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.Box &&
                ObjectSpawn.instance.boxObjectSpawn.isBuildingBox)
            {
                ObjectSpawn.instance.boxObjectSpawn.raiseCurrentHeight();
            }
        }
    }

    public class ObjectSpawn_BoxSpawn_Lower : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.Box &&
                ObjectSpawn.instance.boxObjectSpawn.isBuildingBox)
            {
                ObjectSpawn.instance.boxObjectSpawn.lowerCurrentHeight();
            }
        }
    }

    public class ObjectSpawn_TileRuleSpawn_EnablePaintMode : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.TileRules)
            {
                ObjectSpawn.instance.tileRuleObjectSpawn.activeToolId = TileRuleSpawnToolId.Paint;
            }
        }
    }

    public class ObjectSpawn_TileRuleSpawn_EnableRampMode : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.TileRules)
            {
                ObjectSpawn.instance.tileRuleObjectSpawn.activeToolId = TileRuleSpawnToolId.RampPaint;
            }
        }
    }

    public class ObjectSpawn_TileRuleSpawn_EnableEraseMode : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.TileRules)
            {
                ObjectSpawn.instance.tileRuleObjectSpawn.activeToolId = TileRuleSpawnToolId.Erase;
            }
        }
    }

    public class ObjectSpawn_TileRuleSpawn_EnableConnectMode : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.TileRules)
            {
                ObjectSpawn.instance.tileRuleObjectSpawn.activeToolId = TileRuleSpawnToolId.Connect;
            }
        }
    }

    public class ObjectSpawn_TileRuleSpawn_ConnectMode_ChangeMajorAxis : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.TileRules)
            {
                var settings = ObjectSpawn.instance.tileRuleObjectSpawn.settings;
                if (settings.connectMajorAxis == TileRuleConnectMajorAxis.X) settings.connectMajorAxis = TileRuleConnectMajorAxis.Z;
                else settings.connectMajorAxis = TileRuleConnectMajorAxis.X;
            }
        }
    }

    public class ObjectSpawn_TileRuleSpawn_EnableBoxBrush : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.TileRules)
            {
                ObjectSpawn.instance.tileRuleObjectSpawn.activeBrushId = TileRuleSpawnBrushId.Box;
            }
        }
    }

    public class ObjectSpawn_TileRuleSpawn_EnableFlexiBoxBrush : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.TileRules)
            {
                ObjectSpawn.instance.tileRuleObjectSpawn.activeBrushId = TileRuleSpawnBrushId.FlexiBox;
            }
        }
    }

    public class ObjectSpawn_TileRuleSpawn_EnableSegmentsBrush : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.TileRules)
            {
                ObjectSpawn.instance.tileRuleObjectSpawn.activeBrushId = TileRuleSpawnBrushId.Segments;
            }
        }
    }

    public class ObjectSpawn_CurveSpawn_SelectAllControlPoints : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.Curve)
            {
                int numCurves = ObjectSpawnCurveDb.instance.numCurves;
                for (int i = 0; i < numCurves; ++i)
                {
                    var curve = ObjectSpawnCurveDb.instance.getCurve(i);
                    if (curve.uiSelected) curve.selectAllControlPoints();
                }
            }
        }
    }

    public class ObjectSpawn_CurveSpawn_EnableInsertControlPoint : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.Curve)
            {
                int numCurves = ObjectSpawnCurveDb.instance.numCurves;
                for (int i = 0; i < numCurves; ++i)
                {
                    var curve = ObjectSpawnCurveDb.instance.getCurve(i);
                    if (curve.uiSelected) curve.editMode = ObjectSpawnCurveEditMode.InsertControlPoints;
                }
            }
        }
    }

    public class ObjectSpawn_CurveSpawn_ProjectSelectedControlPoints : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.Curve)
            {
                int numCurves = ObjectSpawnCurveDb.instance.numCurves;
                for (int i = 0; i < numCurves; ++i)
                {
                    var curve = ObjectSpawnCurveDb.instance.getCurve(i);
                    if (curve.uiSelected) curve.projectSelectedControlPoints();
                }
            }
        }
    }

    public class ObjectSpawn_CurveSpawn_EnableMoveGizmo : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.Curve) ObjectSpawn.instance.curveObjectSpawn.activeGizmoId = ObjectSpawnCurveGizmoId.Move;
                
        }
    }

    public class ObjectSpawn_CurveSpawn_EnableRotationGizmo : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.Curve) ObjectSpawn.instance.curveObjectSpawn.activeGizmoId = ObjectSpawnCurveGizmoId.Rotate;

        }
    }

    public class ObjectSpawn_CurveSpawn_EnableScaleGizmo : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.Curve) ObjectSpawn.instance.curveObjectSpawn.activeGizmoId = ObjectSpawnCurveGizmoId.Scale;

        }
    }
}
#endif
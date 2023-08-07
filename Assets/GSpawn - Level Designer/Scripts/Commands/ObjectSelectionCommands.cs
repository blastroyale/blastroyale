#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectSelection_SnapAllAxes : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelection.instance.snapAllAxes(PluginScene.instance.grid);
        }
    }

    public class ObjectSelection_ProjectOnGrid : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelection.instance.projectOnGrid(PluginScene.instance.grid);
        }
    }

    public class ObjectSelection_BeginProjectOnObject : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelection.instance.beginTransformSession(ObjectTransformSessionType.Projection);
        }
    }

    public class ObjectSelection_EnableMoveGizmo : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelectionGizmos.instance.setGizmoEnabled(ObjectSelectionGizmoId.Move, true, true, ObjectSelectionGizmoId.Mirror);
        }
    }

    public class ObjectSelection_EnableRotationGizmo : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelectionGizmos.instance.setGizmoEnabled(ObjectSelectionGizmoId.Rotate, true, true, ObjectSelectionGizmoId.Mirror);
        }
    }

    public class ObjectSelection_EnableScaleGizmo : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelectionGizmos.instance.setGizmoEnabled(ObjectSelectionGizmoId.Scale, true, true, ObjectSelectionGizmoId.Mirror);
        }
    }

    public class ObjectSelection_EnableUniversalGizmo : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelectionGizmos.instance.setGizmoEnabled(ObjectSelectionGizmoId.Universal, true, true, ObjectSelectionGizmoId.Mirror);
        }
    }

    public class ObjectSelection_EnableExtrudeGizmo : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelectionGizmos.instance.setGizmoEnabled(ObjectSelectionGizmoId.Extrude, true, true, ObjectSelectionGizmoId.Mirror);
        }
    }

    public class ObjectSelection_EnableSelectionRectangle : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelection.instance.selectionShapeType = ObjectSelectionShape.Type.Rect;
        }
    }

    public class ObjectSelection_EnableSelectionSegments : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelection.instance.selectionShapeType = ObjectSelectionShape.Type.Segments;
        }
    }

    public class ObjectSelection_EnableSelectionBox : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelection.instance.selectionShapeType = ObjectSelectionShape.Type.Box;
        }
    }

    public class ObjectSelection_MirrorSelected : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelectionGizmos.instance.mirrorTargets();
        }
    }

    public class ObjectSelection_EnableVertexSnap : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelection.instance.beginTransformSession(ObjectTransformSessionType.VertexSnap);
        }

        protected override void onExit()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelection.instance.endActiveTransformSession(ObjectTransformSessionType.VertexSnap);
        }
    }

    public class ObjectSelection_EnableBoxSnap : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelection.instance.beginTransformSession(ObjectTransformSessionType.BoxSnap);
        }

        protected override void onExit()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelection.instance.endActiveTransformSession(ObjectTransformSessionType.BoxSnap);
        }
    }

    public class ObjectSelection_EnableSurfaceSnap : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelection.instance.beginTransformSession(ObjectTransformSessionType.SurfaceSnap);
        }
    }

    public class ObjectSelection_EnableModularSnap : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                ObjectSelection.instance.beginTransformSession(ObjectTransformSessionType.ModularSnap);
        }
    }

    public class ObjectSelection_SelectSimilarPrefabs : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection &&
                !ObjectSelection.instance.isAnyTransformSessionActive)
                ObjectSelection.instance.selectSimilarPrefabInstances();
        }
    }

    public class ObjectSelection_SelectPrefabsInManager : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection &&
                !ObjectSelection.instance.isAnyTransformSessionActive)
            {
                var selectedPrefabs = new List<GameObject>();
                ObjectSelection.instance.getSelectedPrefabs(selectedPrefabs);
                PrefabLibProfileDbUI.instance.selectOwnerLibsOfPrefabAssets(selectedPrefabs);
                PluginPrefabManagerUI.instance.selectAndScrollToPrefabs(selectedPrefabs);
            }
        }
    }

    public class ObjectSelection_FilterOutOfView : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection &&
                !ObjectSelection.instance.isAnyTransformSessionActive)
            {
                ObjectSelection.instance.filterOutOfView();
            }
        }
    }

    public class ObjectSelection_Grow : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection &&
                 !ObjectSelection.instance.isAnyTransformSessionActive)
            {
                ObjectSelection.instance.grow();
            }
        }
    }
}
#endif
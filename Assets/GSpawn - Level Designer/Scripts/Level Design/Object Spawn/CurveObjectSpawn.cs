#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GSpawn
{
    public class CurveObjectSpawn : ObjectSpawnTool
    {
        [SerializeField]
        private ObjectSpawnCurve                _currentCurve;
        [SerializeField]
        private ObjectSpawnCurveGizmoId         _activeGizmoId      = ObjectSpawnCurveGizmoId.Move;

        [NonSerialized]
        private ObjectRaycastConfig             _raycastConfig      = ObjectRaycastConfig.defaultConfig;
        [NonSerialized]
        private SceneRaycastFilter              _raycastFilter      = new SceneRaycastFilter()
        {
            objectTypes = GameObjectType.Terrain | GameObjectType.Mesh | GameObjectType.Sprite,
            raycastGrid = true,
            raycastObjects = true,
        };
        [NonSerialized]
        private List<ObjectSpawnCurve>          _curveBuffer        = new List<ObjectSpawnCurve>();

        public ObjectSpawnCurveGizmoId          activeGizmoId
        {
            get { return _activeGizmoId; }
            set
            {
                _activeGizmoId = value;
                EditorUtility.SetDirty(this);
                PluginInspectorUI.instance.refresh();
                SceneView.RepaintAll();
            }
        }
        public override ObjectSpawnToolId       spawnToolId             { get { return ObjectSpawnToolId.Curve; } }
        public override bool                    requiresSpawnGuide      { get { return false; } }

        public override void onNoLongerActive()
        {
            int numCurves = ObjectSpawnCurveDb.instance.numCurves;
            for (int i = 0; i < numCurves; ++i)
                ObjectSpawnCurveDb.instance.getCurve(i).editMode = ObjectSpawnCurveEditMode.SelectControlPoints;
        }

        public void createNewCurve(string curveName)
        {
            if (string.IsNullOrEmpty(curveName)) curveName = "ObjectSpawnCurve";

            _currentCurve           = ObjectSpawnCurveDb.instance.createCurve(curveName);
            CurveObjectSpawnUI.instance.refresh();
            _currentCurve.editMode  = ObjectSpawnCurveEditMode.None;
            _currentCurve.settings.copy(CurveObjectSpawnSettingsProfileDb.instance.activeProfile.settings);
            CurveObjectSpawnUI.instance.setSelectedCurve(_currentCurve);
        }

        protected override void doOnSceneGUI()
        {
            Event e = Event.current;
            if (FixedShortcuts.cancelAction(e))
            {
                if (_currentCurve != null)
                {
                    _currentCurve.destroySpawnedObjectsNoUndoRedo();
                    ObjectSpawnCurveDb.instance.deleteCurve(_currentCurve);
                    CurveObjectSpawnUI.instance.refresh();
                    _currentCurve   = null;
                }
            }
            else if (FixedShortcuts.structureBuild_EnableCommitOnLeftClick(e) && e.isLeftMouseButtonDownEvent())
            {
                if (_currentCurve != null)
                {
                    _currentCurve.editMode  = ObjectSpawnCurveEditMode.SelectControlPoints;
                    _currentCurve.settings.copy(CurveObjectSpawnSettingsProfileDb.instance.activeProfile.settings);
                    CurveObjectSpawnUI.instance.setSelectedCurve(_currentCurve);
                    _currentCurve           = null;
                }
            }
            
            if (_currentCurve != null)
            {
                if (e.type == EventType.MouseMove || e.type == EventType.MouseDown)
                {
                    Vector3 ctrlPointPos;
                    if (pickControlPointPosition(out ctrlPointPos))
                    {
                        if (_currentCurve.numControlPoints == 0)
                        {
                            _currentCurve.addControlPoint(ctrlPointPos);
                            _currentCurve.addControlPoint(ctrlPointPos);
                        }

                        if (e.isLeftMouseButtonDownEvent())
                        {
                            if (_currentCurve.numControlPoints > 2) _currentCurve.removeLastPoint();
                            _currentCurve.addControlPoint(ctrlPointPos);
                            _currentCurve.addControlPoint(ctrlPointPos);
                        }

                        _currentCurve.setPenultimateControlPoint(ctrlPointPos);
                        _currentCurve.setLastControlPoint(ctrlPointPos);
                    }
                }
            }
        }

        protected override void draw()
        {
            if (_currentCurve != null)
                _currentCurve.draw();

            CurveObjectSpawnUI.instance.getVisibleSelectedCurves(_curveBuffer);
            foreach (var curve in _curveBuffer)
            {
                if (_currentCurve != curve)
                    curve.draw();
            }
        }

        private bool pickControlPointPosition(out Vector3 pos)
        {
            pos = Vector3.zero;

            _raycastFilter.clearIgnoredObjects();
            if (_currentCurve != null) _currentCurve.ignoreSpawnedObjectsDuringRaycast(_raycastFilter);

            var rayCast = PluginScene.instance.raycastClosest(PluginCamera.camera.getCursorRay(), _raycastFilter, _raycastConfig);
            if (rayCast.anyHit)
            {
                pos = rayCast.getClosestRayHit().hitPoint;
                if (FixedShortcuts.curveObjectSpawn_EnableControlPointSnapToGrid(Event.current))
                    pos = PluginScene.instance.grid.snapAllAxes(pos);
                return true;
            }
            else return false;
        }
    }
}
#endif
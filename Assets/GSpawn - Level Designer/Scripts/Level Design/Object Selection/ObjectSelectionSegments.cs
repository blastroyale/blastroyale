#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectSelectionSegments : ObjectSelectionShape
    {
        private enum State
        {
            Idle = 0,
            Selecting
        }

        private State                       _state                  = State.Idle;
        private List<Vector3>               _gridCellPoints         = new List<Vector3>();
        private HashSet<GameObject>         _overlappedSet          = new HashSet<GameObject>();
        private SceneRaycastFilter          _raycastFilter          = new SceneRaycastFilter();
        private ObjectOverlapConfig         _overlapConfig          = new ObjectOverlapConfig();
        private ObjectOverlapFilter         _overlapFilter          = new ObjectOverlapFilter();
        private ObjectBounds.QueryConfig    _boundQConfig           = new ObjectBounds.QueryConfig();
        private List<GameObject>            _objectOverlapResult    = new List<GameObject>();
        private LineStrip                   _lineStrip              = new LineStrip();

        public override bool                selecting               { get { return _state == State.Selecting; } }
        public override Type                shapeType               { get { return Type.Segments; } }

        public ObjectSelectionSegments()
        {
            _raycastFilter.objectTypes          = GameObjectType.Mesh | GameObjectType.Terrain | GameObjectType.Sprite;
            _boundQConfig.objectTypes           = GameObjectType.Mesh | GameObjectType.Sprite;
            _overlapFilter.objectTypes          = GameObjectType.Mesh | GameObjectType.Sprite;
            _overlapFilter.customFilter         = (go) => { return LayerEx.isPickingEnabled(go.layer) && !SceneVisibilityManager.instance.IsPickingDisabled(go); };

            _overlapConfig.requireFullOverlap   = false;
            _overlapConfig.prefabMode           = ObjectOverlapPrefabMode.PrefabInstanceRootIfPossible;
        }

        public override void cancel()
        {
            _lineStrip.clearKeepLastPoint();
            updateState();
        }

        public void removeLastNode()
        {
            if (_lineStrip.numPoints > 1)
            {
                _lineStrip.removeLastPoint();
                updateState();
            }
        }

        protected override void update()
        {
            if (!ObjectSelection.instance.multiSelectEnabled)
            {
                _lineStrip.clearKeepLastPoint();
                updateState();
                return;
            }

            Event e = Event.current;
            if (e.type == EventType.MouseMove) pickPoint();
            else
            if (e.type == EventType.MouseDown && !e.alt)
            {
                if (e.button == (int)MouseButton.LeftMouse)
                {
                    if (FixedShortcuts.structureBuild_EnableCommitOnLeftClick(e))
                    {
                        _lineStrip.clearKeepLastPoint();
                        e.disable();
                    }
                    else
                    {
                        if (_lineStrip.numPoints == 1) _undoConfig.groupIndex = Undo.GetCurrentGroup();
                        _lineStrip.duplicateLastPoint();
                        e.disable();
                    }
                }
                else
                if (e.button == (int)MouseButton.RightMouse)
                {
                    if (FixedShortcuts.selectionSegments_EnableStepBack(e)) removeLastNode();
                }
            }
            else
            if (e.type == EventType.KeyDown)
            {
                if (FixedShortcuts.cancelAction(e))
                {
                    _lineStrip.clearKeepLastPoint();
                    e.disable();
                }
            }

            updateState();
        }

        protected override void detectOverlappedObjects()
        {
            _overlappedObjects.Clear();
            _overlappedSet.Clear();
            if (!ObjectSelection.instance.multiSelectEnabled) return;

            const float segmentThickness = 1e-7f;

            int numPoints = _lineStrip.numPoints;
            for (int segmentIndex = 0; segmentIndex < numPoints - 1; ++segmentIndex)
            {
                Vector3 firstPt = _lineStrip.getPoint(segmentIndex);
                Vector3 secondPt = _lineStrip.getPoint(segmentIndex + 1);

                OBB overlapOBB = OBB.createFromSegment(firstPt, secondPt, segmentThickness);
                if (!overlapOBB.isValid) overlapOBB = new OBB(firstPt, Vector3Ex.create(segmentThickness), Quaternion.identity);
                if (PluginScene.instance.overlapBox(overlapOBB, _overlapFilter, _overlapConfig, _objectOverlapResult))
                {
                    foreach (var go in _objectOverlapResult)
                    {
                        // Note: We need the set to check for duplicates because segments may overlap
                        //       or, in any case, they may share objects.
                        if (!_overlappedSet.Contains(go))
                        {
                            _overlappedObjects.Add(go);
                            _overlappedSet.Add(go);
                        }
                    }
                }
            }

            _overlappedSet.Clear();
        }

        protected override void draw()
        {
            _lineStrip.segmentColor     = ObjectSelectionPrefs.instance.selSegmentsSegmentColor;
            _lineStrip.tickColor        = ObjectSelectionPrefs.instance.selSegmentsTickColor;
            _lineStrip.tickSize         = ObjectSelectionPrefs.instance.selSegmentsTickSize;
            _lineStrip.draw();
        }

        private void updateState()
        {
            if (_lineStrip.numPoints > 1) _state = State.Selecting;
            else _state = State.Idle;
        }

        private void pickPoint()
        {
            var rayHit = PluginScene.instance.raycastClosest(PluginCamera.camera.getCursorRay(), _raycastFilter, ObjectRaycastConfig.defaultConfig);
            if (!rayHit.anyHit) return;

            Vector3 pt = Vector3.zero;
            if (rayHit.wasObjectHit && rayHit.wasGridHit)
            {
                if (rayHit.gridHit.hitEnter < rayHit.objectHit.hitEnter &&
                    Mathf.Abs(rayHit.gridHit.hitEnter - rayHit.objectHit.hitEnter) > 1e-4f) pt = pointFromGridHit(rayHit.gridHit);
                else pt = pointFromObjectHit(rayHit.objectHit);
            }
            else if (rayHit.wasObjectHit) pt = pointFromObjectHit(rayHit.objectHit);
            else if (rayHit.wasGridHit) pt = pointFromGridHit(rayHit.gridHit);

            _lineStrip.replaceLastPointOrAdd(pt);
        }

        private Vector3 pointFromObjectHit(ObjectRayHit objectHit)
        {
            GameObjectType objectType = GameObjectDataDb.instance.getGameObjectType(objectHit.hitObject);
            if (objectType == GameObjectType.Terrain) return objectHit.hitPoint;
            else
            {
                OBB worldOBB = ObjectBounds.calcWorldOBB(objectHit.hitObject, _boundQConfig);
                if (worldOBB.isValid) return worldOBB.center;
            }

            return Vector3.zero;
        }

        private Vector3 pointFromGridHit(GridRayHit gridHit)
        {
            gridHit.hitGrid.calcCellCenterAndCorners(gridHit.hitCell, true, _gridCellPoints);
            int closestPtIndex = Vector3Ex.findIndexOfPointClosestToPoint(_gridCellPoints, gridHit.hitPoint);
            if (closestPtIndex >= 0) return _gridCellPoints[closestPtIndex];

            return Vector3.zero;
        }
    }
}
#endif
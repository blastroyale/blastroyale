#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace GSpawn
{
    public class ObjectSelectionBox : ObjectSelectionShape
    {
        private enum State
        {
            Ready = 0,
            SelectingHrz,
            SelectingVert
        }

        private OBB     _box            = new OBB(true);
        private Vector3 _extensionAnchorHrz;
        private Vector3 _extensionAnchorVert;
        private State   _state          = State.Ready;

        private SceneRaycastFilter      _raycastFilter          = new SceneRaycastFilter();
        private ObjectOverlapFilter     _overlapFilter          = new ObjectOverlapFilter();
        private ObjectOverlapConfig     _overlapConfig          = new ObjectOverlapConfig();

        public override bool selecting { get { return _state == State.SelectingHrz || _state == State.SelectingVert; } }
        public override Type shapeType { get { return Type.Box; } }

        public ObjectSelectionBox()
        {
            _raycastFilter.objectTypes          = GameObjectType.Mesh | GameObjectType.Terrain | GameObjectType.Sprite;

            _overlapFilter.objectTypes          = GameObjectType.Mesh | GameObjectType.Sprite;
            _overlapFilter.customFilter         = (go) => { return LayerEx.isPickingEnabled(go.layer) && !SceneVisibilityManager.instance.IsPickingDisabled(go); };

            _overlapConfig.requireFullOverlap   = false;
            _overlapConfig.prefabMode           = ObjectOverlapPrefabMode.PrefabInstanceRootIfPossible;
        }

        public override void cancel()
        {
            _state      = State.Ready;
            _box.size   = PluginScene.instance.grid.activeSettings.cellSize;
        }

        protected override void detectOverlappedObjects()
        {
            if (!ObjectSelection.instance.multiSelectEnabled) return;

            if (selecting)
            {
                OBB overlapOBB = _box;
                overlapOBB.inflate(-0.001f);
                PluginScene.instance.overlapBox(overlapOBB, _overlapFilter, _overlapConfig, _overlappedObjects);
            }
        }

        protected override void draw()
        {
            if (_box.isValid)
            {
                HandlesEx.saveColor();
                HandlesEx.saveMatrix();
                HandlesEx.saveZTest();

                Handles.zTest   = UnityEngine.Rendering.CompareFunction.LessEqual;
                Handles.color   = ObjectSelectionPrefs.instance.selBoxWireColor;

                OBB drawOBB     = _box;
                drawOBB.inflate(0.02f);
                Handles.matrix  = drawOBB.transformMatrix;
                HandlesEx.drawUnitWireCube();

                HandlesEx.restoreZTest();
                HandlesEx.restoreMatrix();
                HandlesEx.restoreColor();
            }
        }

        protected override void update()
        {
            Event e     = Event.current;
            var grid    = PluginScene.instance.grid;

            if (!ObjectSelection.instance.multiSelectEnabled)
            {
                cancel();
                return;
            }

            if (e.isLeftMouseButtonDownEvent())
            {
                if (_state == State.Ready && e.noShiftCtrlCmdAlt())
                {
                    _state = State.SelectingHrz;
                    _extensionAnchorHrz = _box.center;
                }
                else if (_state == State.SelectingHrz)
                {
                    _state                  = State.SelectingVert;
                    _extensionAnchorVert    = _box.center;
                }
                else
                if (_state == State.SelectingVert) _state = State.Ready;

                e.disable();
            }

            _box.rotation = grid.rotation;
            if (!e.isMouseMoveEvent()) return;

            if (_state == State.Ready)
            {
                _box.size   = grid.activeSettings.cellSize;
                pickBoxCenter();
            }
            else
            if (_state == State.SelectingHrz)
            {
                float t;
                Ray ray     = PluginCamera.camera.getCursorRay();
                Plane plane = new Plane(grid.up, _box.center);
                if (plane.Raycast(ray, out t))
                {
                    Vector3 cellSize        = grid.activeSettings.cellSize;
                    Vector3 pt              = grid.snapAllAxes(ray.GetPoint(t));
                    Vector3 toPt            = pt - _extensionAnchorHrz;

                    float rightSize         = Vector3.Dot(toPt, grid.right);
                    float lookSize          = Vector3.Dot(toPt, grid.look);

                    _box.center             = (_extensionAnchorHrz + pt) / 2.0f;
                    _box.size               = new Vector3(Mathf.Abs(rightSize) + cellSize.x, _box.size.y, Mathf.Abs(lookSize) + cellSize.z);
                }
            }
            else
            if (_state == State.SelectingVert)
            {
                Vector3 cameraLook  = PluginCamera.camera.transform.forward;

                float t;
                Ray ray     = PluginCamera.camera.getCursorRay();
                Plane plane = new Plane(cameraLook, _extensionAnchorVert);
                if (plane.Raycast(ray, out t))
                {
                    Vector3 cellSize    = grid.activeSettings.cellSize;
                    Vector3 pt          = grid.snapAxis(ray.GetPoint(t), 1);
                    Vector3 toPt        = pt - _extensionAnchorVert;

                    float upSize        = Vector3.Dot(toPt, grid.up);

                    Vector3 upPt        = _extensionAnchorVert + grid.up * upSize;
                    _box.center         = (_extensionAnchorVert + upPt) * 0.5f;
                    Vector3 oldBoxSize  = _box.size;
                    _box.size           = new Vector3(oldBoxSize.x, Mathf.Abs(upSize) + cellSize.y, oldBoxSize.z);
                }
            }
        }

        private bool pickBoxCenter()
        {
            var rayHit = PluginScene.instance.raycastClosest(PluginCamera.camera.getCursorRay(), _raycastFilter, ObjectRaycastConfig.defaultConfig);
            if (rayHit.anyHit)
            {
                var closestHit  = rayHit.getClosestRayHit();
                var closestPt   = closestHit.hitPoint;
                if (rayHit.wasObjectHit && closestHit == rayHit.objectHit) closestPt -= closestHit.hitNormal * 1e-3f;
                _box.center = PluginScene.instance.grid.snapAllAxes(closestPt);

                return true;
            }

            return false;
        }
    }
}
#endif
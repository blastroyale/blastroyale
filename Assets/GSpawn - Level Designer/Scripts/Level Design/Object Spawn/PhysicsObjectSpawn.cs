#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

namespace GSpawn
{
    public class PhysicsObjectSpawn : ObjectSpawnTool
    {
        private class Surface
        {
            public GameObject   surfaceObject;
            public Vector3      pickPoint;
            public bool         isValid;
        }

        [NonSerialized]
        private PhysicsObjectSpawnSettings      _settings;
        [NonSerialized]
        private Surface                         _surface            = new Surface();
        [NonSerialized]
        private SceneRaycastFilter              _raycastFilter      = new SceneRaycastFilter()
        {
            objectTypes = GameObjectType.Mesh | GameObjectType.Terrain,
            raycastGrid = true,
            raycastObjects = true,
        };
        [NonSerialized]
        private float                           _lastUpdateTime;

        public override ObjectSpawnToolId       spawnToolId             { get { return ObjectSpawnToolId.Physics; } }
        public override bool                    requiresSpawnGuide      { get { return false; } }
        public PhysicsObjectSpawnSettings       settings
        {
            get
            {
                if (_settings == null) _settings = AssetDbEx.loadScriptableObject<PhysicsObjectSpawnSettings>(PluginFolders.settings);
                return _settings;
            }
        }

        protected override void doOnSceneGUI()
        {
            Event e = Event.current;
            if (FixedShortcuts.changeRadiusByScrollWheel(e))
            {
                e.disable();
                settings.dropRadius -= 0.1f * e.delta.y;
            }
            else
            if (FixedShortcuts.changeHeightByScrollWheel(e))
            {
                e.disable();
                settings.dropHeight -= 0.1f * e.delta.y;
            }

            pickSurface();

            if (_surface.isValid)
            {
                if (e.isLeftMouseButtonDownEvent() || e.isLeftMouseButtonDragEvent())
                {
                    float timeSinceStartip = Time.realtimeSinceStartup;
                    if ((timeSinceStartip - _lastUpdateTime) >= settings.dropInterval)
                    {
                        _lastUpdateTime = timeSinceStartip;
                        spawnObject();
                    }
                }
            }
        }

        protected override void draw()
        {
            if (_surface.isValid)
            {
                HandlesEx.saveColor();

                Vector3 circleCenter        = calcSpawnCircleCenter();
                Handles.color               = ObjectSpawnPrefs.instance.physicsSpawnCircleColor;
                Handles.DrawWireDisc(circleCenter, Vector3.up, settings.dropRadius);

                Handles.color               = ObjectSpawnPrefs.instance.physicsSpawnHeightLineColor;
                Handles.DrawLine(_surface.pickPoint, circleCenter);

                float markerLineLength      = ObjectSpawnPrefs.instance.physicsSpawnMarkerSize * HandleUtility.GetHandleSize(_surface.pickPoint);
                float halfMarkerLineLength  = markerLineLength * 0.5f;
                Handles.color               = ObjectSpawnPrefs.instance.physicsSpawnMarkerColor;
                Handles.DrawLine(_surface.pickPoint + Vector3.left * halfMarkerLineLength, _surface.pickPoint + Vector3.right * halfMarkerLineLength);
                Handles.DrawLine(_surface.pickPoint + Vector3.back * halfMarkerLineLength, _surface.pickPoint + Vector3.forward * halfMarkerLineLength);

                HandlesEx.restoreColor();

                if (PhysicsSimulation.instance.isRunning)
                {
                    Handles.BeginGUI();
                    Handles.Label(circleCenter + Vector3.up * 2.0f,
                        "Physics simulation time: " + PhysicsSimulation.instance.simulationTime.ToString("F4"),
                        GUIStyleDb.instance.sceneViewInfoLabel);
                    Handles.EndGUI();
                }
            }
        }

        private void spawnObject()
        {
            Vector3 spawnPosition       = Circle3D.calcRandomPoint(calcSpawnCircleCenter(), settings.dropRadius, Vector3.right, Vector3.back);
            RandomPrefab randomPrefab   = settings.randomPrefabProfile.pickPrefab();
            if (randomPrefab != null)
            {
                bool is2DObject = randomPrefab.prefabAsset.hierarchyHasSprite(false, false);
                if (is2DObject && PluginScene.instance.grid.activeSettings.orientation == GridOrientation.XY)
                    spawnPosition = PluginScene.instance.grid.plane.projectPoint(spawnPosition);

                GameObject spawnedObject = randomPrefab.pluginPrefab.spawn(spawnPosition, Quaternion.identity, randomPrefab.prefabAsset.transform.lossyScale);

                if (settings.randomizeRotation)
                {
                    if (is2DObject) spawnedObject.transform.Rotate(Vector3.forward, MathEx.randomAngle(), Space.World);
                    else spawnedObject.transform.rotation = Quaternion.Euler(MathEx.randomAngle(), MathEx.randomAngle(), MathEx.randomAngle());
                }
                
                // Note: We need to increment the Undo group in order to fix the issue where
                //       the rigid body of the object is restored (having been added by the
                //       physics simulation module if missing).
                // Note: Doesn't work because it leaks the rigid body component.
                //Undo.IncrementCurrentGroup();

                PhysicsSimulationConfig physicsSimulationConfig     = PhysicsSimulationConfig.defaultConfig;
                physicsSimulationConfig.simulate2D                  = true;
                physicsSimulationConfig.simulationTime              = settings.simulationTime;
                physicsSimulationConfig.simulationStep              = settings.simulationStep;
                physicsSimulationConfig.outOfBoundsYCoord           = settings.outOfBoundsYCoord;

                // Note: We need to restart the simulation to give each new object a chance to
                //       complete its simulation time.
                PhysicsSimulation.instance.addObject(spawnedObject);
                if (settings.instant) PhysicsSimulation.instance.performInstantSimulation(physicsSimulationConfig);
                else PhysicsSimulation.instance.start(physicsSimulationConfig);
            }
        }

        private Vector3 calcSpawnCircleCenter()
        {
            return _surface.pickPoint + settings.dropHeight * Vector3.up;
        }

        private void pickSurface()
        {
            _surface.isValid = false;

            Ray ray = PluginCamera.camera.getCursorRay();
            var sceneRayHit = PluginScene.instance.raycastClosest(ray, _raycastFilter, ObjectRaycastConfig.defaultConfig);
            if (sceneRayHit.anyHit)
            {
                if (sceneRayHit.wasObjectHit && sceneRayHit.wasGridHit)
                {
                    if (sceneRayHit.objectHit.hitEnter < sceneRayHit.gridHit.hitEnter) surfaceFromObjectHit(sceneRayHit.objectHit);
                    else surfaceFromGridHit(sceneRayHit.gridHit);
                }
                else
                if (sceneRayHit.wasObjectHit) surfaceFromObjectHit(sceneRayHit.objectHit);
                else if (sceneRayHit.wasGridHit) surfaceFromGridHit(sceneRayHit.gridHit);
            }
        }

        private void surfaceFromObjectHit(ObjectRayHit objectHit)
        {
            _surface.isValid        = true;
            _surface.surfaceObject  = objectHit.hitObject;
            _surface.pickPoint      = objectHit.hitPoint;
        }

        private void surfaceFromGridHit(GridRayHit gridHit)
        {
            _surface.isValid        = true;
            _surface.surfaceObject  = null;
            _surface.pickPoint      = gridHit.hitPoint;
        }

        private void OnEnable()
        {
            _lastUpdateTime = Time.realtimeSinceStartup;
        }
    }
}
#endif
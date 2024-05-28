#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor;

namespace GSpawn
{
    public class PropsObjectSpawn : ObjectSpawnTool
    {
        [NonSerialized]
        private ObjectSpawnGuideSettings    _spawnGuideSettings;
        [NonSerialized]
        private ObjectSurfaceSnapSettings   _surfaceSnapSettings;
        [NonSerialized]
        private ObjectDragSpawnSettings     _dragSpawnSettings;
        [NonSerialized]
        private TerrainFlattenSettings      _terrainFlattenSettings;
        [SerializeField]
        private ObjectSurfaceSnapSession    _surfaceSnapSession;
        [SerializeField]
        private ObjectMirrorGizmo           _mirrorGizmo;
        [NonSerialized]
        private ObjectMirrorGizmoSettings   _mirrorGizmoSettings;
        [NonSerialized]
        private Vector3                     _lastSurfacePickPoint;
        [NonSerialized]
        private SceneRaycastFilter          _pickPrefabRaycastFilter    = new SceneRaycastFilter();
        [NonSerialized]
        private List<OBB>                   _obbBuffer                  = new List<OBB>();
        [NonSerialized]
        private List<Vector3>               _vector3Buffer              = new List<Vector3>();
        [NonSerialized]
        private Vector3[]                   _terrainPatchCorners        = new Vector3[4];

        [SerializeField]
        private int                         _decorRuleIndex             = 0;
        [NonSerialized]
        private DecorRuleApplyResult        _prevDecorRuleApplyResult   = new DecorRuleApplyResult();
        [NonSerialized]
        private DecorRuleApplyResult        _decorRuleApplyResult       = new DecorRuleApplyResult();
        [NonSerialized]
        private GameObject                  _surfaceObjectPrefab        = null;
        [NonSerialized]
        private GameObject                  _prevSurfaceObjectPrefab    = null;

        private ObjectSurfaceSnapSession    surfaceSnapSession
        {
            get
            {
                if (_surfaceSnapSession == null)
                {
                    _surfaceSnapSession = CreateInstance<ObjectSurfaceSnapSession>();
                    _surfaceSnapSession.sharedSettings = surfaceSnapSettings;
                }
                return _surfaceSnapSession;
            }
        }

        public ObjectSurfaceSnapSettings    surfaceSnapSettings
        {
            get
            {
                if (_surfaceSnapSettings == null) _surfaceSnapSettings = AssetDbEx.loadScriptableObject<ObjectSurfaceSnapSettings>(PluginFolders.settings, typeof(PropsObjectSpawn).Name + "_" + typeof(ObjectSurfaceSnapSettings).Name);
                return _surfaceSnapSettings;
            }
        }
        public ObjectDragSpawnSettings      dragSpawnSettings
        {
            get
            {
                if (_dragSpawnSettings == null) _dragSpawnSettings = AssetDbEx.loadScriptableObject<ObjectDragSpawnSettings>(PluginFolders.settings, typeof(PropsObjectSpawn).Name + "_" + typeof(ObjectDragSpawnSettings).Name);
                return _dragSpawnSettings;
            }
        }
        public TerrainFlattenSettings       terrainFlattenSettings
        {
            get
            {
                if (_terrainFlattenSettings == null) _terrainFlattenSettings = AssetDbEx.loadScriptableObject<TerrainFlattenSettings>(PluginFolders.settings, typeof(PropsObjectSpawn).Name + "_" + typeof(TerrainFlattenSettings).Name);
                return _terrainFlattenSettings;
            }
        }
        public override ObjectSpawnGuideSettings spawnGuideSettings
        {
            get
            {
                if (_spawnGuideSettings == null) _spawnGuideSettings = AssetDbEx.loadScriptableObject<ObjectSpawnGuideSettings>(PluginFolders.settings, typeof(PropsObjectSpawn).Name + "_" + typeof(ObjectSpawnGuideSettings).Name);
                return _spawnGuideSettings;
            }
        }
        public ObjectMirrorGizmoSettings    mirrorGizmoSettings
        {
            get
            {
                if (_mirrorGizmoSettings == null) _mirrorGizmoSettings = AssetDbEx.loadScriptableObject<ObjectMirrorGizmoSettings>(PluginFolders.settings, typeof(PropsObjectSpawn).Name + "_" + typeof(ObjectMirrorGizmoSettings).Name);
                return _mirrorGizmoSettings;
            }
        }
        public override ObjectSpawnToolId   spawnToolId             { get { return ObjectSpawnToolId.Props; } }
        public override bool                requiresSpawnGuide      { get { return true; } }
        public override ObjectMirrorGizmo   mirrorGizmo             { get { return _mirrorGizmo; } }

        public PropsObjectSpawn()
        {
        }

        public override void setSpawnGuidePrefab(PluginPrefab prefab)
        {
            spawnGuide.usePrefab(prefab, surfaceSnapSession);
        }

        public override void onNoLongerActive()
        {
            spawnGuide.destroyGuide();
        }

        public void executeSurfaceSnapSessionCommand(ObjectSurfaceSnapSessionCommand command)
        {
            surfaceSnapSession.executeCommand(command);
        }

        protected override void doOnSceneGUI()
        {
            spawnGuide.onSceneGUI();
            _mirrorGizmo.onSceneGUI();

            Event e = Event.current;
            if (FixedShortcuts.enablePickSpawnGuidePrefabFromScene(e))
            {
                if (e.isLeftMouseButtonDownEvent())
                {
                    var prefabPickResult = PluginScene.instance.pickPrefab(PluginCamera.camera.getCursorRay(), _pickPrefabRaycastFilter, ObjectRaycastConfig.defaultConfig);
                    if (prefabPickResult != null)
                    {
                        setSpawnGuidePrefab(prefabPickResult.pickedPluginPrefab);
                        spawnGuide.setRotationAndScale(prefabPickResult.pickedObject.transform.rotation, prefabPickResult.pickedObject.transform.lossyScale);
                    }
                }
            }

            if (surfaceSnapSession.isActive)
            {
                if (spawnGuideSettings.applyDecorRules)
                {
                    if (FixedShortcuts.changeDecorRuleIndex(e))
                    {
                        if (e.delta.y > 0) ++_decorRuleIndex;
                        else --_decorRuleIndex;

                        e.disable();
                    }

                    if (surfaceSnapSession.surfaceObject != null)
                    {
                        _surfaceObjectPrefab = surfaceSnapSession.surfaceObject.getOutermostPrefabAsset();
                        if (_surfaceObjectPrefab != null && _prevSurfaceObjectPrefab != _surfaceObjectPrefab)
                        {
                            var decorPrefab = PrefabDecorRuleDb.instance.getDecor(spawnGuide.sourcePrefab.prefabAsset);
                            if (decorPrefab != null) _decorRuleIndex = decorPrefab.getFavPropsSpawnRule(surfaceSnapSession.surfaceObject);
                            _prevSurfaceObjectPrefab = _surfaceObjectPrefab;
                        }
                    }

                    PrefabDecorRuleDb.instance.applyDecorRule(spawnGuide.gameObject,
                                spawnGuide.sourcePrefab.prefabAsset, surfaceSnapSession.surfaceObject, _decorRuleIndex,
                                e.delta.magnitude != 0.0f, _decorRuleApplyResult);

                    _prevDecorRuleApplyResult.copy(_decorRuleApplyResult);
                    _decorRuleIndex = _decorRuleApplyResult.ruleIndex;
                }

                if (surfaceSnapSession.isSurfaceValid)
                {
                    if (e.isLeftMouseButtonDownEvent())
                    {
                        surfaceSnapSession.addObjectHierarchyIgnoredAsSurface(spawn());
                        _lastSurfacePickPoint = surfaceSnapSession.surfacePickPoint;
                        if (surfaceSnapSession.isSurfaceTerrain) surfaceSnapSession.setSurfaceLocked(true);
                    }
                    else
                    if (e.isLeftMouseButtonDragEvent())
                    {
                        float minDragDistance = dragSpawnSettings.minDragDistance;
                        if (dragSpawnSettings.useSafeDragDistance) minDragDistance = Mathf.Max(minDragDistance, spawnGuide.volumeRadius * 2.0f);
                        if ((_lastSurfacePickPoint - surfaceSnapSession.surfacePickPoint).magnitude >= minDragDistance)
                        {
                            surfaceSnapSession.addObjectHierarchyIgnoredAsSurface(spawn());
                             _lastSurfacePickPoint = surfaceSnapSession.surfacePickPoint;
                            if (surfaceSnapSession.isSurfaceTerrain) surfaceSnapSession.setSurfaceLocked(true);
                        }
                    }
                    else
                    if (FixedShortcuts.changeRadiusByScrollWheel(e))
                    {
                        if (terrainFlattenSettings.flattenTerrain)
                        {
                            e.disable();

                            int amount = (int)(e.delta.y * 0.5f);
                            if (amount == 0) amount = (int)(Mathf.Sign(e.delta.y));

                            terrainFlattenSettings.terrainQuadRadius -= amount;
                            EditorUtility.SetDirty(terrainFlattenSettings);
                        }
                    }

                    if (_mirrorGizmo.enabled)
                    {
                        _mirrorGizmo.mirrorOBB(spawnGuide.calcWorldOBB(), _obbBuffer);
                        _mirrorGizmo.drawMirroredOBBs(_obbBuffer);
                    }
                }
            }

            if (e.type == EventType.MouseUp || e.type == EventType.MouseLeaveWindow)
            {
                surfaceSnapSession.clearObjectsIgnoredAsSurface();
                surfaceSnapSession.setSurfaceLocked(false);
            }
        }

        protected override void draw()
        {
            if (surfaceSnapSession.isActive)
            {
                if (surfaceSnapSession.isSurfaceUnityTerrain && terrainFlattenSettings.flattenTerrain)
                {
                    Terrain terrain     = surfaceSnapSession.surfaceObject.getTerrain();
                    float terrainYPos   = terrain.transform.position.y;

                    spawnGuide.calcWorldOBB().calcCorners(_vector3Buffer, false);
                    AABB aabb           = new AABB(_vector3Buffer);
                    terrain.calcTerrainPatchCorners(terrainFlattenSettings.terrainQuadRadius, aabb, _terrainPatchCorners);

                    HandlesEx.saveColor();
                    Handles.color = ObjectSpawnPrefs.instance.propsSpawnTerrainFlattenAreaColor;

                    const int numSamplePoints   = 30;
                    float stepSize              = 1.0f / (float)numSamplePoints;
                    for (int lineIndex = 0; lineIndex < 4; ++lineIndex)
                    {
                        Vector3 p0      = _terrainPatchCorners[lineIndex];
                        Vector3 p1      = _terrainPatchCorners[(lineIndex + 1) % 4];
                        Vector3 dir     = (p1 - p0);
                        Vector3 prevPt  = terrain.projectPoint(terrainYPos, p0);
                        for (int i = 0; i < numSamplePoints; ++i)
                        {
                            Vector3 currentPt = terrain.projectPoint(terrainYPos, prevPt + dir * stepSize);
                            Handles.DrawLine(prevPt, currentPt);
                            prevPt = currentPt;
                        }
                    }

                    HandlesEx.restoreColor();
                }

                if (spawnGuideSettings.applyDecorRules)
                {
                    Handles.BeginGUI();
                    Vector3 labelPos            = HandlesEx.calcLabelPositionAboveOBB(spawnGuide.calcWorldOBB());
                    var labelStyle              = new GUIStyle(GUIStyleDb.instance.sceneViewInfoLabel);
                    labelStyle.normal.textColor = Color.green;
                    labelStyle.hover.textColor  = Color.green;
                    Handles.Label(labelPos, "Decor rules: " + _decorRuleApplyResult.rules.Count + "\nSelected rule: " + _decorRuleIndex, labelStyle);
                    Handles.EndGUI();
                }
            }
        }

        private GameObject spawn()
        {
            Vector3 surfaceNormal = surfaceSnapSession.surfacePickNormal;
            if (!surfaceSnapSettings.alignAxis) surfaceNormal = surfaceSnapSession.getNoAlignmentRotationAxis();

            if (spawnGuideSettings.applyDecorRules)
            {
                var decorPrefab = PrefabDecorRuleDb.instance.getDecor(spawnGuide.sourcePrefab.prefabAsset);
                if (decorPrefab != null)
                    decorPrefab.setFavPropsSpawnRule(surfaceSnapSession.surfaceObject, _decorRuleIndex);
            }

            GameObject spawnedObject;
            if (surfaceSnapSession.isSurfaceUnityTerrain && terrainFlattenSettings.flattenTerrain)
            {
                Terrain terrain                     = surfaceSnapSession.surfaceObject.getTerrain();

                TerrainFlattenConfig flattenConfig  = new TerrainFlattenConfig();
                flattenConfig.terrainQuadRadius     = terrainFlattenSettings.terrainQuadRadius;
                flattenConfig.mode                  = terrainFlattenSettings.mode;
                flattenConfig.applyFalloff          = terrainFlattenSettings.applyFalloff;
                terrain.flattenAroundOBB(spawnGuide.calcWorldOBB(), flattenConfig);

                surfaceSnapSession.projectTargetsOnSurface();
                spawnedObject = spawnGuide.spawn();
            }
            else spawnedObject = spawnGuide.spawn();

            if (_mirrorGizmo.enabled) _mirrorGizmo.mirrorObject_NoDuplicateCommand(spawnedObject, spawnGuide.sourcePrefab.prefabAsset);

            if (spawnGuideSettings.randomizePrefab)
            {
                RandomPrefab randomPrefab = spawnGuideSettings.randomPrefabProfile.pickPrefab();
                if (randomPrefab != null) setSpawnGuidePrefab(randomPrefab.pluginPrefab);
            }

            spawnGuide.randomizeTransformIfNecessary(spawnGuideSettings.transformRandomizationSettings, surfaceNormal);

            return spawnedObject;
        }

        protected override void onEnabled()
        {
            _pickPrefabRaycastFilter            = createDefaultPrefabPickRaycastFilter();
            surfaceSnapSession.sharedSettings   = surfaceSnapSettings;

            if (_mirrorGizmo == null)
            {
                _mirrorGizmo = ScriptableObject.CreateInstance<ObjectMirrorGizmo>();
                _mirrorGizmo.enabled = false;
            }

            _mirrorGizmo.sharedSettings = mirrorGizmoSettings;
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_surfaceSnapSession);
        }
    }
}
#endif
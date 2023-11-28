#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class ModularSnapObjectSpawn : ObjectSpawnTool
    {
        [NonSerialized]
        private ObjectSpawnGuideSettings            _spawnGuideSettings;
        [NonSerialized]
        private ObjectModularSnapSettings           _modularSnapSettings;
        [SerializeField]
        private ObjectModularSnapSession            _modularSnapSession;
        [SerializeField]
        private ObjectMirrorGizmo                   _mirrorGizmo;
        [NonSerialized]
        private ObjectMirrorGizmoSettings           _mirrorGizmoSettings;
        [NonSerialized]
        private SceneRaycastFilter                  _pickPrefabRaycastFilter;
        [NonSerialized]
        private List<OBB>                           _obbBuffer                  = new List<OBB>();

        private ObjectModularSnapSession            modularSnapSession
        {
            get
            {
                if (_modularSnapSession == null)
                {
                    _modularSnapSession = CreateInstance<ObjectModularSnapSession>();
                    _modularSnapSession.sharedSettings = modularSnapSettings;
                }
                return _modularSnapSession;
            }
        }

        public ObjectModularSnapSettings            modularSnapSettings
        {
            get
            {
                if (_modularSnapSettings == null) _modularSnapSettings = AssetDbEx.loadScriptableObject<ObjectModularSnapSettings>(PluginFolders.settings, typeof(ModularSnapObjectSpawn).Name + "_" + typeof(ObjectModularSnapSettings).Name);
                return _modularSnapSettings;
            }
        }
        public ObjectMirrorGizmoSettings            mirrorGizmoSettings
        {
            get
            {
                if (_mirrorGizmoSettings == null) _mirrorGizmoSettings = AssetDbEx.loadScriptableObject<ObjectMirrorGizmoSettings>(PluginFolders.settings, typeof(ModularSnapObjectSpawn).Name + "_" + typeof(ObjectMirrorGizmoSettings).Name);
                return _mirrorGizmoSettings;
            }
        }
        public override ObjectSpawnGuideSettings    spawnGuideSettings
        {
            get
            {
                if (_spawnGuideSettings == null) _spawnGuideSettings = AssetDbEx.loadScriptableObject<ObjectSpawnGuideSettings>(PluginFolders.settings, typeof(ModularSnapObjectSpawn).Name + "_" + typeof(ObjectSpawnGuideSettings).Name);
                return _spawnGuideSettings;
            }
        }
        public override ObjectSpawnToolId           spawnToolId         { get { return ObjectSpawnToolId.ModularSnap; } }
        public override bool                        requiresSpawnGuide  { get { return true; } }
        public override ObjectMirrorGizmo           mirrorGizmo         { get { return _mirrorGizmo; } }

        public ModularSnapObjectSpawn()
        {
        }

        public override void setSpawnGuidePrefab(PluginPrefab prefab)
        {
            spawnGuide.usePrefab(prefab, modularSnapSession);
        }

        public override void onNoLongerActive()
        {
            spawnGuide.destroyGuide();
        }

        public void executeModularSnapSessionCommand(ObjectModularSnapSessionCommand command)
        {
            modularSnapSession.executeCommand(command);
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

            if (modularSnapSession.isActive)
            {
                if (e.isLeftMouseButtonDownEvent())
                {
                    GameObject spawnedObject = spawnGuide.spawn();
                    if (_mirrorGizmo.enabled) _mirrorGizmo.mirrorObject_NoDuplicateCommand(spawnedObject, spawnGuide.sourcePrefab.prefabAsset);

                    if (spawnGuideSettings.randomizePrefab)
                    {
                        RandomPrefab randomPrefab = spawnGuideSettings.randomPrefabProfile.pickPrefab();
                        if (randomPrefab != null) setSpawnGuidePrefab(randomPrefab.pluginPrefab);
                    }

                    spawnGuide.randomizeTransformIfNecessary(spawnGuideSettings.transformRandomizationSettings, PluginScene.instance.grid.planeNormal);
                }

                if (_mirrorGizmo.enabled)
                {
                    _mirrorGizmo.mirrorOBB(spawnGuide.calcWorldOBB(), _obbBuffer);
                    _mirrorGizmo.drawMirroredOBBs(_obbBuffer);
                }
            }
        }

        protected override void onEnabled()
        {
            _pickPrefabRaycastFilter = createDefaultPrefabPickRaycastFilter();
            modularSnapSession.sharedSettings = modularSnapSettings;

            if (_mirrorGizmo == null)
            {
                _mirrorGizmo = ScriptableObject.CreateInstance<ObjectMirrorGizmo>();
                _mirrorGizmo.enabled = false;
            }

            _mirrorGizmo.sharedSettings = mirrorGizmoSettings;
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_modularSnapSession);
        }
    }
}
#endif
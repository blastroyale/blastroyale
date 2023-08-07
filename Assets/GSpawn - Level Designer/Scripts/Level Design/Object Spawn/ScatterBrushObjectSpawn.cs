#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

namespace GSpawn
{
    public class ScatterBrushObjectSpawn : ObjectSpawnTool
    {
        [NonSerialized]
        private ScatterBrushObjectSpawnSettings     _settings;
        [NonSerialized]
        private CircleBrush3D                       _circleBrush            = new CircleBrush3D();
        [NonSerialized]
        private ScatterBrushObjectSpawner           _spawner                = new ScatterBrushObjectSpawner();
        [NonSerialized]
        private ObjectProjectionSettings            _projectionSettings;
        [NonSerialized]
        private float                               _accumDragDistance;

        private ObjectProjectionSettings            projectionSettings
        {
            get
            {
                if (_projectionSettings == null) _projectionSettings = CreateInstance<ObjectProjectionSettings>();
                return _projectionSettings;
            }
        }

        public ScatterBrushObjectSpawnSettings      settings
        {
            get
            {
                if (_settings == null) _settings = AssetDbEx.loadScriptableObject<ScatterBrushObjectSpawnSettings>(PluginFolders.settings);
                return _settings;
            }
        }

        public override ObjectSpawnToolId           spawnToolId                 { get { return ObjectSpawnToolId.ScatterBrush; } }
        public override bool                        requiresSpawnGuide          { get { return false; } }

        protected override void doOnSceneGUI()
        {
            _circleBrush.surfaceLayers      = settings.surfaceLayers;
            _circleBrush.allowGridSurface   = settings.allowsGridSurface;
            _circleBrush.radius             = settings.brushRadius;

            GameObjectType surfaceObjectTypes   = GameObjectType.None;
            if (settings.allowsTerrainSurface)  surfaceObjectTypes |= GameObjectType.Terrain;
            if (settings.allowsMeshSurface)     surfaceObjectTypes |= GameObjectType.Mesh;
            _circleBrush.surfaceObjectTypes     = surfaceObjectTypes;

            Event e = Event.current;
            if (e.isLeftMouseButtonDownEvent()) _circleBrush.surfaceLocked = true;
            else if (e.isLeftMouseButtonUpEvent() || e.type == EventType.MouseLeaveWindow) _circleBrush.surfaceLocked = false;

            _circleBrush.updateSurface();
            if (!_circleBrush.isSurfaceValid) return;

            if (FixedShortcuts.changeRadiusByScrollWheel(e))
            {
                e.disable();
                settings.brushRadius -= 0.1f * e.delta.y;
                EditorUtility.SetDirty(settings);
            }

            if (_circleBrush.surfaceLocked)
            {
                if (!_spawner.isSpawnSessionActive) _spawner.beginSpawnSession(settings, _circleBrush, projectionSettings);

                _accumDragDistance += _circleBrush.projectedStrokeDelta;
                if (_accumDragDistance >= settings.minDragDistance)
                {
                    _spawner.spawnObjects();
                    _accumDragDistance = 0.0f;
                }
            }
            else
            {
                _spawner.endSpawnSession();
                _accumDragDistance = 0.0f;
            }
        }

        protected override void draw()
        {
            _circleBrush.draw(ObjectSpawnPrefs.instance.scatterBrushSpawnBrushBorderColor);
        }
    }
}
#endif
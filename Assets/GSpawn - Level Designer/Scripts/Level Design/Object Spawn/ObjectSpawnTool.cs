#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public enum ObjectSpawnToolId
    {
        ModularSnap = 0,
        ModularWalls,
        Segments,
        Box,
        Props,
        ScatterBrush,
        TileRules,
        Curve,
        Physics
    }

    public abstract class ObjectSpawnTool : ScriptableObject
    {
        [SerializeField]
        private ObjectSpawnGuide                _spawnGuide;

        public ObjectSpawnGuide                 spawnGuide                          { get { return _spawnGuide; } }
        public abstract ObjectSpawnToolId       spawnToolId                         { get; }
        public abstract bool                    requiresSpawnGuide                  { get; }
        public bool                             isSpawnGuideTransformSessionActive  { get { return requiresSpawnGuide && _spawnGuide.isTransformSessionActive; } }
        public bool                             isSpawnGuidePresentInScene          { get { return requiresSpawnGuide && _spawnGuide.isPresentInScene; } }
        public virtual ObjectSpawnGuideSettings spawnGuideSettings                  { get { return null; } }
        public virtual bool                     canChangeSpawnGuideTransform        { get { return true; } }
        public virtual ObjectMirrorGizmo        mirrorGizmo                         { get { return null; } }
        public PluginPrefab                     spawnGuidePrefab                    { get { return requiresSpawnGuide ? spawnGuide.sourcePrefab : null; } }

        public void onSceneGUI()
        {
            doOnSceneGUI();
            draw();
        }

        public OBB calcSpawnGuideWorldOBB()
        {
            return requiresSpawnGuide ? spawnGuide.calcWorldOBB() : OBB.getInvalid();
        }

        public void resetSpawnGuideRotationToOriginal()
        {
            if (isSpawnGuidePresentInScene && canChangeSpawnGuideTransform) spawnGuide.resetRotationToOriginal();
        }

        public void resetSpawnGuideScaleToOriginal()
        {
            if (isSpawnGuidePresentInScene && canChangeSpawnGuideTransform) spawnGuide.resetScaleToOriginal();
        }

        public void rotateSpawnGuide(Vector3 axis, float degrees)
        {
            if (isSpawnGuidePresentInScene && canChangeSpawnGuideTransform) spawnGuide.rotate(axis, degrees);
        }

        public void rotateSpawnGuide(Vector3 point, Vector3 axis, float degrees)
        {
            if (isSpawnGuidePresentInScene && canChangeSpawnGuideTransform) spawnGuide.rotate(point, axis, degrees);
        }

        public void setSpawnGuideRotation(Quaternion rotation)
        {
            if (isSpawnGuidePresentInScene && canChangeSpawnGuideTransform) spawnGuide.setRotation(rotation);
        }

        public virtual void setSpawnGuidePrefab(PluginPrefab prefab) { }
        public virtual void onNoLongerActive() { }

        protected SceneRaycastFilter createDefaultPrefabPickRaycastFilter()
        {
            SceneRaycastFilter raycastFilter    = new SceneRaycastFilter();
            raycastFilter.objectTypes           = GameObjectType.Mesh | GameObjectType.Sprite;
            raycastFilter.raycastGrid           = false;
            raycastFilter.customFilter          = (GameObject gameObject) => { return !spawnGuide.isObjectPartOfGuideHierarchy(gameObject); };
            return raycastFilter;
        }

        protected virtual void doOnSceneGUI() { }
        protected virtual void draw() { }
        protected virtual void onEnabled() { }
        protected virtual void onDisabled() { }
        protected virtual void onDestroy() { }

        private void OnEnable()
        {
            if (!PluginFolders.isDataFolderValid())
                return;

            if (requiresSpawnGuide && _spawnGuide == null) _spawnGuide = CreateInstance<ObjectSpawnGuide>();
            onEnabled();
        }

        private void OnDisable()
        {
            onDisabled();
        }

        private void OnDestroy()
        {
            onDestroy();
            if (requiresSpawnGuide && _spawnGuide != null) DestroyImmediate(_spawnGuide);
        }
    }
}
#endif
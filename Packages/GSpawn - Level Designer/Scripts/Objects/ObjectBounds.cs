#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace GSpawn
{
    public static class ObjectBounds
    {
        public struct QueryConfig
        {
            public GameObjectType   objectTypes;
            public Vector3          volumelessSize;
            public bool             includeInactive;
            public bool             includeInvisible;
            public bool             includeAddedObjectOverrides;

            public static readonly QueryConfig defaultConfig    = new QueryConfig()
            {
                objectTypes                 = GameObjectType.All,
                volumelessSize              = Vector3.zero,
                includeInactive             = false,
                includeInvisible            = false,
                includeAddedObjectOverrides = true
            };
        }

        private static List<Vector2>        _vector2Buffer      = new List<Vector2>();
        private static List<Vector3>        _vector3Buffer      = new List<Vector3>();
        private static List<GameObject>     _allChildrenBuffer  = new List<GameObject>();

        public static Rect calcScreenRect(GameObject gameObject, Camera camera, QueryConfig queryConfig)
        {
            OBB worldOBB = calcWorldOBB(gameObject, queryConfig);
            if (!worldOBB.isValid) return new Rect(0.0f, 0.0f, 0.0f, 0.0f);

            worldOBB.calcCorners(_vector3Buffer, false);
            camera.worldToScreenPoints(_vector3Buffer, _vector2Buffer);
          
            return RectEx.create(_vector2Buffer);
        }

        public static OBB calcSpriteWorldOBB(GameObject gameObject)
        {
            AABB modelAABB = calcSpriteModelAABB(gameObject);
            if (!modelAABB.isValid) return OBB.getInvalid();

            return new OBB(modelAABB, gameObject.transform);
        }

        public static AABB calcSpriteWorldAABB(GameObject gameObject)
        {
            AABB modelAABB = calcSpriteModelAABB(gameObject);
            if (!modelAABB.isValid) return modelAABB;

            modelAABB.transform(gameObject.transform.localToWorldMatrix);
            return modelAABB;
        }

        public static AABB calcSpriteModelAABB(GameObject spriteObject)
        {
            SpriteRenderer spriteRenderer = spriteObject.getSpriteRenderer();
            if (spriteRenderer == null) return AABB.getInvalid();

            return spriteRenderer.calcModelSpaceAABB();
        }

        public static OBB calcMeshWorldOBB(GameObject gameObject)
        {
            AABB modelAABB = calcMeshModelAABB(gameObject);
            if (!modelAABB.isValid) return OBB.getInvalid();

            return new OBB(modelAABB, gameObject.transform);
        }

        public static AABB calcMeshWorldAABB(GameObject gameObject)
        {
            AABB modelAABB = calcMeshModelAABB(gameObject);
            if (!modelAABB.isValid) return modelAABB;

            modelAABB.transform(gameObject.transform.localToWorldMatrix);
            return modelAABB;
        }

        public static AABB calcObjectsWorldAABB(IEnumerable<GameObject> gameObjectCollection, QueryConfig queryConfig)
        {
            AABB aabb           = AABB.getInvalid();
            foreach (var gameObject in gameObjectCollection)
            {
                AABB worldAABB  = ObjectBounds.calcWorldAABB(gameObject, queryConfig);
                if (worldAABB.isValid)
                {
                    if (aabb.isValid) aabb.encloseAABB(worldAABB);
                    else aabb   = worldAABB;
                }
            }

            return aabb;
        }

        public static AABB calcHierarchiesWorldAABB(IEnumerable<GameObject> parents, QueryConfig queryConfig)
        {
            AABB aabb               = AABB.getInvalid();
            foreach (var parent in parents)
            {
                AABB hierarchyAABB  = calcHierarchyWorldAABB(parent, queryConfig);
                if (hierarchyAABB.isValid)
                {
                    if (aabb.isValid) aabb.encloseAABB(hierarchyAABB);
                    else aabb       = hierarchyAABB;
                }
            }

            return aabb;
        }

        public static OBB calcHierarchiesWorldOBB(IEnumerable<GameObject> parents, QueryConfig queryConfig)
        {
            OBB obb                 = OBB.getInvalid();
            foreach (var parent in parents)
            {
                OBB hierarchyOBB    = calcHierarchyWorldOBB(parent, queryConfig);
                if (hierarchyOBB.isValid)
                {
                    if (obb.isValid) obb.encloseOBB(hierarchyOBB);
                    else obb        = hierarchyOBB;
                }
            }

            return obb;
        }

        public static OBB calcHierarchyWorldOBB(GameObject parent, QueryConfig queryConfig)
        {
            AABB modelAABB = calcHierarchyModelAABB(parent, queryConfig);
            if (!modelAABB.isValid) return OBB.getInvalid();

            if (parent.getTerrain() != null) return new OBB(modelAABB.center + parent.transform.position, modelAABB.size);
            else return new OBB(modelAABB, parent.transform);
        }

        public static AABB calcHierarchyWorldAABB(GameObject parent, QueryConfig queryConfig)
        {
            AABB modelAABB = calcHierarchyModelAABB(parent, queryConfig);
            if (!modelAABB.isValid) return AABB.getInvalid();

            if (parent.getTerrain() != null)
            {
                modelAABB.center += parent.transform.position;
                return modelAABB;
            }
            else
            {
                modelAABB.transform(parent.transform.localToWorldMatrix);
                return modelAABB;
            }
        }

        public static OBB calcWorldOBB(GameObject gameObject, QueryConfig queryConfig)
        {
            GameObjectType objectType   = GameObjectDataDb.instance.getGameObjectType(gameObject);
            AABB modelAABB              = ObjectBounds.calcModelAABB(gameObject, queryConfig, objectType);
            if (!modelAABB.isValid)     return OBB.getInvalid();
       
            if (objectType == GameObjectType.Terrain) return new OBB(modelAABB.center + gameObject.transform.position, modelAABB.size);
            else return new OBB(modelAABB, gameObject.transform);
        }

        public static OBB calcWorldOBB(GameObject gameObject, GameObjectType objectType, QueryConfig queryConfig)
        {
            AABB modelAABB          = ObjectBounds.calcModelAABB(gameObject, queryConfig, objectType);
            if (!modelAABB.isValid) return OBB.getInvalid();

            if (objectType == GameObjectType.Terrain) return new OBB(modelAABB.center + gameObject.transform.position, modelAABB.size);
            else return new OBB(modelAABB, gameObject.transform);
        }

        public static AABB calcWorldAABB(GameObject gameObject, QueryConfig queryConfig)
        {
            GameObjectType objectType   = GameObjectDataDb.instance.getGameObjectType(gameObject);
            AABB modAABB                = calcModelAABB(gameObject, queryConfig, objectType);
            if (!modAABB.isValid)       return modAABB;

            if (objectType == GameObjectType.Terrain)
            {
                modAABB.center += gameObject.transform.position;
                return modAABB;
            }
            else
            {
                modAABB.transform(gameObject.transform.localToWorldMatrix);
                return modAABB;
            }
        }

        public static AABB calcHierarchyModelAABB(GameObject parent, QueryConfig queryConfig)
        {
            Matrix4x4 rootTransform     = parent.transform.localToWorldMatrix;
            AABB finalAABB              = calcModelAABB(parent, queryConfig, GameObjectDataDb.instance.getGameObjectType(parent));
          
            parent.getAllChildren(queryConfig.includeInactive, queryConfig.includeInvisible, _allChildrenBuffer);
            foreach (var child in _allChildrenBuffer)
            {
                if (!queryConfig.includeAddedObjectOverrides && PrefabUtility.IsAddedGameObjectOverride(child)) continue;

                AABB modAABB            = calcModelAABB(child, queryConfig, GameObjectDataDb.instance.getGameObjectType(child));
                if (modAABB.isValid)
                {
                    Matrix4x4 rootRelativeTransform = child.transform.localToWorldMatrix.calcRelativeTransform(rootTransform);
                    modAABB.transform(rootRelativeTransform);

                    if (finalAABB.isValid) finalAABB.encloseAABB(modAABB);
                    else finalAABB      = modAABB;

                    // Note: Useful when the artist has created a hierarchy with unnecessary meshes.
                    //       Example: Root has valid mesh with renderer. Child has mesh collider and a duplicate
                    //       mesh and renderer which do not render. This kind of overlap can create rounding errors.
                    finalAABB.size = finalAABB.size.roundCorrectError(1e-4f);
                }
            }

            return finalAABB;
        }

        public static AABB calcMeshModelAABB(GameObject gameObject)
        {
            Mesh mesh = gameObject.getMesh();
            if (mesh == null) return AABB.getInvalid();

            return new AABB(mesh.bounds);
        }

        public static AABB calcModelAABB(GameObject gameObject, QueryConfig queryConfig, GameObjectType objectType)
        {
            if (gameObject.isSceneObject() && !gameObject.activeInHierarchy && !queryConfig.includeInactive) return AABB.getInvalid();
            if ((objectType & queryConfig.objectTypes) == 0) return AABB.getInvalid();

            if (objectType == GameObjectType.Mesh)
            {
                MeshFilter meshFilter = gameObject.getMeshFilter();
                if (meshFilter != null)
                {
                    Mesh mesh = meshFilter.sharedMesh;
                    if (mesh != null)
                    {
                        if (!queryConfig.includeInvisible)
                        {
                            MeshRenderer meshRenderer = gameObject.getMeshRenderer();
                            if (meshRenderer == null || !meshRenderer.enabled) return AABB.getInvalid();
                        }
                      
                        AABB aabb = new AABB(mesh.bounds);
                        aabb.size = aabb.size.roundCorrectError(1e-4f);
                        return aabb;
                    }
                }
                else
                {
                    SkinnedMeshRenderer skinnedRenderer = gameObject.getSkinnedMeshRenderer();
                    if (skinnedRenderer != null)
                    {
                        Mesh mesh = skinnedRenderer.sharedMesh;
                        if (mesh != null)
                        {
                            if (!queryConfig.includeInvisible && !skinnedRenderer.enabled) return AABB.getInvalid();

                            AABB aabb = new AABB(mesh.bounds);
                            aabb.size = aabb.size.roundCorrectError(1e-4f);
                            return aabb;
                        }
                    }
                }

                return AABB.getInvalid();
            }
            else
            if (objectType == GameObjectType.Sprite)
            {
                SpriteRenderer spriteRenderer = gameObject.getSpriteRenderer();
                if (!queryConfig.includeInvisible && !spriteRenderer.enabled) return AABB.getInvalid();

                return spriteRenderer.calcModelSpaceAABB();
            }
            else if (objectType == GameObjectType.Terrain)
            {
                Terrain terrain = gameObject.getTerrain();
                if (!queryConfig.includeInvisible && !terrain.enabled) return AABB.getInvalid();
                return terrain.calcModelAABB();
            }
            else
            {
                return new AABB(Vector3.zero, queryConfig.volumelessSize);
            }
        }
    }
}
#endif
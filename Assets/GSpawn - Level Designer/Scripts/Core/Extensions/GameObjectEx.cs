#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace GSpawn
{
    public static class GameObjectEx
    {
        // Note: Used to avoid calling GetComponent<T>. GetComponents<T> is much faster
        //       because it seems to avoid allocating memory.
        private static List<Camera>                 _cameraBuffer               = new List<Camera>();
        private static List<SpriteRenderer>         _spriteRendererBuffer       = new List<SpriteRenderer>();
        private static List<MeshFilter>             _meshFilterBuffer           = new List<MeshFilter>();
        private static List<Terrain>                _terrainBuffer              = new List<Terrain>();
        private static List<TerrainCollider>        _terrainColliderBuffer      = new List<TerrainCollider>();
        private static List<MeshRenderer>           _meshRendererBuffer         = new List<MeshRenderer>();
        private static List<SkinnedMeshRenderer>    _skinnedMeshRendererBuffer  = new List<SkinnedMeshRenderer>();
        private static List<LODGroup>               _lodGroupBuffer             = new List<LODGroup>();
        private static List<Rigidbody>              _rigidBodyBuffer            = new List<Rigidbody>();
        private static List<Rigidbody2D>            _rigidBody2DBuffer          = new List<Rigidbody2D>();
        private static List<Collider>               _colliderBuffer             = new List<Collider>();
        private static List<Collider2D>             _collider2DBuffer           = new List<Collider2D>();
        private static List<Light>                  _lightBuffer                = new List<Light>();
        private static List<ParticleSystem>         _particleSystemBuffer       = new List<ParticleSystem>();
        private static List<Tree>                   _treeBuffer                 = new List<Tree>();
        private static List<GSpawn>                 _pluginBuffer               = new List<GSpawn>();

        private static List<GameObject>             _prefabInstanceBuffer       = new List<GameObject>();
        private static List<GameObject>             _allChildrenAndSelfBuffer   = new List<GameObject>();
        private static List<Transform>              _childTransformBuffer       = new List<Transform>();
        private static HashSet<GameObject>          _prefabInstanceRootSet      = new HashSet<GameObject>();

        public static bool obbIntersectsMeshTriangles(this GameObject gameObject, OBB obb)
        {
            PluginMesh pluginMesh = PluginMeshDb.instance.getPluginMesh(gameObject.getMesh());
            if (pluginMesh == null) return false;

            return pluginMesh.trianglesOverlapBox(obb, gameObject.transform);
        }

        private static List<GameObject> _firstIntersectBuffer   = new List<GameObject>();
        private static List<GameObject> _secondIntersectBuffer  = new List<GameObject>();
        public static bool meshHierarchyIntersectsMeshTriangles(this GameObject gameObject, GameObject other, float hierarchyMeshInflate)
        {
            PluginMesh otherPluginMesh = PluginMeshDb.instance.getPluginMesh(other.getMesh());
            if (otherPluginMesh == null) return false;

            gameObject.getAllChildrenAndSelf(false, false, _firstIntersectBuffer);

            int numObjects = _firstIntersectBuffer.Count;
            for (int i = 0; i < numObjects; ++i)
            {
                PluginMesh pluginMesh = PluginMeshDb.instance.getPluginMesh(_firstIntersectBuffer[i].getMesh());
                if (pluginMesh == null) continue;

                if (pluginMesh.trianglesIntersectTriangles(_firstIntersectBuffer[i].transform, hierarchyMeshInflate, otherPluginMesh, other.transform)) return true;
            }

            return false;
        }

        public static void pingPrefabAsset(this GameObject gameObject)
        {
            UnityEditorWindows.showProjectBrowser();
            EditorGUIUtility.PingObject(gameObject);
        }

        public static bool couldBePooled(this GameObject gameObject)
        {
            return !gameObject.activeSelf && (gameObject.hideFlags & HideFlags.HideInHierarchy) != 0;
        }

        public static bool isObjectModularWallPiece(this GameObject gameObject)
        {
            GameObject prefabAsset = gameObject.getOutermostPrefabAsset();
            if (prefabAsset == null) return false;

            return ModularWallPrefabProfileDb.instance.containsWallPiecePrefabAsset(prefabAsset);
        }

        public static bool isObjectModularWallPillar(this GameObject gameObject)
        {
            GameObject prefabAsset = gameObject.getOutermostPrefabAsset();
            if (prefabAsset == null) return false;

            return ModularWallPrefabProfileDb.instance.containsPillarPrefabAsset(prefabAsset);
        }

        public static void setMeshPivotPoint(this GameObject gameObject, Mesh mesh, Vector3 pivotPoint)
        {
            Vector3[] vertexPositions = mesh.vertices;
            for (int vIndex = 0; vIndex < vertexPositions.Length; ++vIndex)
            {
                vertexPositions[vIndex] = vertexPositions[vIndex] - pivotPoint;
            }
            mesh.vertices = vertexPositions;
            mesh.RecalculateBounds();

            gameObject.transform.position = pivotPoint;
        }

        public static bool isPartOfLODGroup(this GameObject gameObject)
        {
            var parent = gameObject.transform.parent;
            if (parent == null) return false;

            var lodGroup = parent.gameObject.getLODGroup();
            if (lodGroup == null) return false;

            var lods = lodGroup.GetLODs();
            foreach(var lod in lods)
            {
                var renderers = lod.renderers;
                foreach (var r in renderers)
                    if (r.gameObject == gameObject) return true;
            }

            return false;
        }

        public static int findLODIndexAndMeshRenderer(this GameObject gameObject, out MeshRenderer renderer)
        {
            renderer = null;

            var parent = gameObject.transform.parent;
            if (parent == null) return -1;

            var lodGroup = parent.gameObject.getLODGroup();
            if (lodGroup == null) return -1;

            var lods = lodGroup.GetLODs();
            int numLODs = lods.Length;
            for (int i = 0; i < numLODs; ++i)
            {
                var renderers = lods[i].renderers;
                foreach (var r in renderers)
                {
                    if (!(r is MeshRenderer)) continue;

                    if (r.gameObject == gameObject)
                    {
                        renderer = r as MeshRenderer;
                        return i;
                    }
                }
            }

            return -1;
        }

        public static GameObject globalIdStringToGameObject(string globalObjectIdString)
        {
            GlobalObjectId globalId;
            if (GlobalObjectId.TryParse(globalObjectIdString, out globalId))
                return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) as GameObject;

            return null;
        }

        public static string getGlobalIdSlowString(this GameObject gameObject)
        {
            return GlobalObjectId.GetGlobalObjectIdSlow(gameObject).ToString();
        }

        public static bool isEditorOnly(this GameObject gameObject)
        {
            return gameObject.tag == "EditorOnly";
        }

        public static void makeEditorOnly(this GameObject gameObject)
        {
            gameObject.tag = "EditorOnly";
        }

        public static void makeUntagged(this GameObject gameObject)
        {
            gameObject.tag = "Untagged";
        }

        public static ObjectRayHit raycastUnityTerrain(this GameObject gameObject, Ray ray, TerrainRaycastConfig raycastConfig)
        {
            TerrainCollider terrainCollider = gameObject.getTerrainCollider();
            if (terrainCollider == null) return null;
;
            RaycastHit raycastHit;
            if (terrainCollider.Raycast(ray, out raycastHit, float.MaxValue))
            {
                Vector3 hitNormal = raycastHit.normal;
                if (raycastConfig.useInterpolatedNormal)
                {
                    Terrain terrain = terrainCollider.gameObject.getTerrain();
                    if (terrain != null) hitNormal = terrain.getInterpolatedNormal(raycastHit.point);
                }

                return new ObjectRayHit(ray, gameObject, hitNormal, raycastHit.distance);
            }

            return null;
        }

        public static ObjectRayHit raycastMesh(this GameObject gameObject, Ray ray, MeshRaycastConfig raycastConfig)
        {
            Mesh mesh = gameObject.getMesh();
            if (mesh == null) return null;

            PluginMesh pluginMesh = PluginMeshDb.instance.getPluginMesh(mesh);
            if (pluginMesh == null) return null;

            MeshRayHit meshRayHit;
            if (pluginMesh.raycastClosest(ray, gameObject.transform, raycastConfig, out meshRayHit))
                return new ObjectRayHit(ray, gameObject, meshRayHit);

            return null;
        }

        public static ObjectRayHit raycastSprite(this GameObject gameObject, Ray ray)
        {
            OBB worldOBB = ObjectBounds.calcSpriteWorldOBB(gameObject);
            if (worldOBB.isValid)
            {
                float t;
                if (worldOBB.raycast(ray, out t))
                {
                    Vector3 hitPt = ray.GetPoint(t);
                    Box3DFace faceClosestToPt = Box3D.findFaceClosestToPoint(hitPt, worldOBB.center, worldOBB.size, worldOBB.rotation);
                    Vector3 faceNormal = Box3D.calcFaceNormal(worldOBB.center, worldOBB.size, worldOBB.rotation, faceClosestToPt);

                    return new ObjectRayHit(ray, gameObject, faceNormal, t);
                }
            }

            return null;
        }

        public static bool isTerrain(this GameObject gameObject)
        {
            GameObjectType objectType = GameObjectDataDb.instance.getGameObjectType(gameObject);

            if (objectType == GameObjectType.Terrain) return true;
            if (objectType == GameObjectType.Mesh && gameObject.isTerrainMesh()) return true;

            return false;
        }

        public static bool isTerrainMesh(this GameObject gameObject)
        {
            return PluginObjectLayerDb.instance.getLayer(gameObject.layer).isTerrainMesh;
        }

        public static bool isSphericalMesh(this GameObject gameObject)
        {
            return PluginObjectLayerDb.instance.getLayer(gameObject.layer).isSphericalMesh;
        }

        public static bool isSceneObject(this GameObject gameObject)
        {
            return gameObject.scene.IsValid();
        }

        public static void disconnectPrefabInstances(this GameObject prefab)
        {
            _prefabInstanceBuffer.Clear();
            PluginScene.instance.findPrefabInstances(prefab, _prefabInstanceBuffer);
            foreach (var prefabInstance in _prefabInstanceBuffer)
                PrefabUtility.UnpackPrefabInstance(prefabInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        public static GameObject getPrefabAsset(this GameObject gameObject)
        {
            return PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject);
        }

        public static GameObject getOutermostPrefabAsset(this GameObject gameObject)
        {
            var outermostInstance = gameObject.getOutermostPrefabInstanceRoot();
            if (outermostInstance == null) return null;
            return outermostInstance.getPrefabAsset();
        }

        public static GameObject getOutermostPrefabInstanceRoot(this GameObject gameObject)
        {
            return PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
        }

        public static bool isPartOfPrefabInstance(this GameObject gameObject)
        {
            return PrefabUtility.IsPartOfPrefabInstance(gameObject);
        }

        public static GameObject instantiatePrefab(this GameObject prefab)
        {
            return PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        }

        public static GameObject instantiatePrefab(this GameObject prefab, Vector3 pos, Quaternion rotation, Vector3 scale)
        {
            var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            go.transform.position = pos;
            go.transform.rotation = rotation;
            go.transform.localScale = scale;

            return go;
        }

        public static GameObject instantiatePrefab(this GameObject prefab, Scene scene)
        {
            return PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        }

        public static GameObject saveAsPrefabAsset(this GameObject gameObject, string prefabPath)
        {
            return PrefabUtility.SaveAsPrefabAsset(gameObject, prefabPath);
        }

        public static void resetRotationToOriginal(this GameObject gameObject)
        {
            var prefab = gameObject.getPrefabAsset();
            if (prefab != null) gameObject.transform.rotation = prefab.transform.rotation;
            else gameObject.transform.rotation = Quaternion.identity;
        }

        public static void resetScaleToOriginal(this GameObject gameObject)
        {
            var prefab = gameObject.getPrefabAsset();
            if (prefab != null) gameObject.transform.localScale = prefab.transform.localScale;
            else gameObject.transform.localScale = Vector3.one;
        }

        public static void placeInFrontOfCamera(this GameObject gameObject, Camera camera)
        {
            OBB worldOBB = ObjectBounds.calcHierarchyWorldOBB(gameObject, ObjectBounds.QueryConfig.defaultConfig);
            if (worldOBB.isValid)
            {
                Sphere sphere = new Sphere(worldOBB);
                Vector3 targetPos = camera.calcSphereCenterInFrontOfCamera(sphere);
                gameObject.transform.position += (targetPos - sphere.center);
            }
        }

        public static void setVisible(this GameObject gameObject, bool isVisible, bool allowUndoRedo)
        {
            var meshRenderer = gameObject.getMeshOrSkinnedMeshRenderer();
            if (meshRenderer != null)
            {
                if (allowUndoRedo) UndoEx.record(meshRenderer);
                meshRenderer.enabled = isVisible;
            }

            var spriteRenderer = gameObject.getSpriteRenderer();
            if (spriteRenderer != null)
            {
                if (allowUndoRedo) UndoEx.record(spriteRenderer);
                spriteRenderer.enabled = isVisible;
            }

            var terrain = gameObject.getTerrain();
            if (terrain != null)
            {
                if (allowUndoRedo) UndoEx.record(terrain);
                terrain.enabled = isVisible;
            }
        }

        public static bool hasVolume(this GameObject gameObject)
        {
            GameObjectType gameObjectType = GameObjectDataDb.instance.getGameObjectType(gameObject);
            if (gameObjectType == GameObjectType.Mesh) return true;
            if (gameObjectType == GameObjectType.Sprite) return true;
            if (gameObjectType == GameObjectType.Terrain) return true;

            return false;
        }

        public static bool isVisible(this GameObject gameObject)
        {
            GameObjectType objectType = GameObjectDataDb.instance.getGameObjectType(gameObject);
            if (objectType == GameObjectType.Mesh) return gameObject.isMeshOrSkinnedMeshRendererEnabled();
            else if (objectType == GameObjectType.Sprite) return gameObject.isSpriteRendererEnabled();
            else if (objectType == GameObjectType.Terrain) return gameObject.isTerrainEnabled();

            return true;
        }

        public static void setStatic(this GameObject gameObject, bool isStatic, bool affectChildren)
        {
            if (!affectChildren) gameObject.isStatic = isStatic;
            else
            {
                gameObject.getAllChildrenAndSelf(true, true, _allChildrenAndSelfBuffer);
                foreach (var gameObj in _allChildrenAndSelfBuffer) gameObj.isStatic = isStatic;
            }
        }

        public static bool hierarchyHasMesh(this GameObject root, bool includeInactive, bool includeInvisible)
        {
            root.getAllChildrenAndSelf(includeInactive, includeInvisible, _allChildrenAndSelfBuffer);
            foreach (var gameObj in _allChildrenAndSelfBuffer)
            {
                if (gameObj.getMesh() != null) return true;
            }

            return false;
        }

        public static bool hierarchyHasSprite(this GameObject root, bool includeInactive, bool includeInvisible)
        {
            root.getAllChildrenAndSelf(includeInactive, includeInvisible, _allChildrenAndSelfBuffer);
            foreach (var gameObj in _allChildrenAndSelfBuffer)
            {
                if (gameObj.getSprite() != null) return true;
            }

            return false;
        }

        public static bool hierarchyHasTerrain(this GameObject root, bool includeInactive, bool includeInvisible)
        {
            root.getAllChildrenAndSelf(includeInactive, includeInvisible, _allChildrenAndSelfBuffer);
            foreach (var gameObj in _allChildrenAndSelfBuffer)
            {
                if (gameObj.getTerrain() != null) return true;
            }

            return false;
        }

        public static bool hierarchyHasMeshOrSprite(this GameObject root, bool includeInactive, bool includeInvisible)
        {
            root.getAllChildrenAndSelf(includeInactive, includeInvisible, _allChildrenAndSelfBuffer);
            foreach (var gameObj in _allChildrenAndSelfBuffer)
            {
                if (gameObj.getMesh() != null ||
                    gameObj.getSprite() != null) return true;
            }

            return false;
        }

        public static bool hierarchiesHaveMeshesOrSprites(List<GameObject> roots, bool includeInactive, bool includeInvisible)
        {
            foreach (var root in roots)
                if (root.hierarchyHasMeshOrSprite(includeInactive, includeInvisible)) return true;

            return false;
        }

        public static bool hierarchiesHaveMeshes(List<GameObject> roots, bool includeInactive, bool includeInvisible)
        {
            foreach (var root in roots)
                if (root.hierarchyHasMesh(includeInactive, includeInvisible)) return true;

            return false;
        }

        public static bool hierarchyHasObjectsOfType(this GameObject root, GameObjectType typeFlags, bool includeInactive, bool includeInvisible)
        {
            root.getAllChildrenAndSelf(includeInactive, includeInvisible, _allChildrenAndSelfBuffer);
            foreach (var gameObj in _allChildrenAndSelfBuffer)
            {
                GameObjectType objectType = GameObjectDataDb.instance.getGameObjectType(gameObj);
                if ((typeFlags & objectType) != 0) return true;
            }

            return false;
        }

        public static bool hierarchyHasOnlySprites(this GameObject root, bool includeInactive, bool includeInvisible)
        {
            root.getAllChildrenAndSelf(includeInactive, includeInvisible, _allChildrenAndSelfBuffer);
            foreach (var gameObj in _allChildrenAndSelfBuffer)
            {
                GameObjectType objectType = GameObjectDataDb.instance.getGameObjectType(gameObj);
                if (objectType != GameObjectType.Sprite) return false;
            }

            return true;
        }

        public static bool hierarchyHasTrees(this GameObject root, bool includeInactive, bool includeInvisible)
        {
            root.getAllChildrenAndSelf(includeInactive, includeInvisible, _allChildrenAndSelfBuffer);
            foreach (var go in _allChildrenAndSelfBuffer)
            {
                Tree tree = go.getTree();
                if (tree != null) return true;
            }

            return false;
        }

        public static void getMeshObjectsInHierarchy(this GameObject root, bool includeInactive, bool includeInvisible, List<GameObject> meshObjects)
        {
            meshObjects.Clear();
            root.getAllChildrenAndSelf(includeInactive, includeInvisible, _allChildrenAndSelfBuffer);
            foreach (var gameObj in _allChildrenAndSelfBuffer)
            {
                if (gameObj.getMesh() != null)
                    meshObjects.Add(gameObj);
            }
        }

        public static void getSpriteObjectsInHierarchy(this GameObject root, bool includeInactive, bool includeInvisible, List<GameObject> spriteObjects)
        {
            spriteObjects.Clear();
            root.getAllChildrenAndSelf(includeInactive, includeInvisible, _allChildrenAndSelfBuffer);
            foreach (var gameObj in _allChildrenAndSelfBuffer)
            {
                if (gameObj.getSprite() != null)
                    spriteObjects.Add(gameObj);
            }
        }

        public static void getAllObjectsInHierarchies(IEnumerable<GameObject> roots, bool includeInactive, bool includeInvisible, List<GameObject> allObjects)
        {
            allObjects.Clear();
            foreach (var root in roots)
            {
                root.getAllChildrenAndSelf(includeInactive, includeInvisible, _allChildrenAndSelfBuffer);
                allObjects.AddRange(_allChildrenAndSelfBuffer);
            }
        }

        public static void getDirectChildren(this GameObject gameObject, bool includeInactive, bool includeInvisible, List<GameObject> children)
        {
            children.Clear();
            Transform transform = gameObject.transform;

            for (int childIndex = 0; childIndex < transform.childCount; ++childIndex)
            {
                var child = transform.GetChild(childIndex).gameObject;
                if ((includeInactive || child.activeSelf) && (includeInvisible || child.gameObject.isVisible())) children.Add(child);
            }
        }

        public static void getAllChildren(this GameObject gameObject, bool includeInactive, bool includeInvisible, List<GameObject> children)
        {
            children.Clear();
            gameObject.GetComponentsInChildren<Transform>(includeInactive, _childTransformBuffer);

            if (includeInvisible)
            {
                foreach (var child in _childTransformBuffer)
                {
                    if (child.gameObject != gameObject) children.Add(child.gameObject);
                }
            }
            else
            {
                foreach (var child in _childTransformBuffer)
                {
                    if (child.gameObject != gameObject && 
                        child.gameObject.isVisible()) children.Add(child.gameObject);
                }
            }
        }

        public static void getAllChildrenAndSelf(this GameObject gameObject, bool includeInactive, bool includeInvisible, List<GameObject> childrenAndSelf)
        {
            childrenAndSelf.Clear();
            gameObject.GetComponentsInChildren(includeInactive, _childTransformBuffer);

            if (includeInvisible)
            {
                foreach (var child in _childTransformBuffer)
                    childrenAndSelf.Add(child.gameObject);
            }
            else
            {
                foreach (var child in _childTransformBuffer)
                    if (!child.gameObject.hasVolume() || child.gameObject.isVisible()) childrenAndSelf.Add(child.gameObject);
            }
        }

        public static Mesh getMesh(this GameObject gameObject)
        {
            _meshFilterBuffer.Clear();
            gameObject.GetComponents(_meshFilterBuffer);
            if (_meshFilterBuffer.Count != 0 && _meshFilterBuffer[0].sharedMesh != null) return _meshFilterBuffer[0].sharedMesh;

            _skinnedMeshRendererBuffer.Clear();
            gameObject.GetComponents(_skinnedMeshRendererBuffer);
            if (_skinnedMeshRendererBuffer.Count != 0 && _skinnedMeshRendererBuffer[0].sharedMesh != null) return _skinnedMeshRendererBuffer[0].sharedMesh;

            return null;
        }

        public static bool isTerrainEnabled(this GameObject gameObject)
        {
            Terrain terrain = gameObject.getTerrain();
            if (terrain != null) return terrain.enabled;

            return false;
        }

        public static LODGroup getLODGroup(this GameObject gameObject)
        {
            _lodGroupBuffer.Clear();
            gameObject.GetComponents(_lodGroupBuffer);
            return _lodGroupBuffer.Count != 0 ? _lodGroupBuffer[0] : null;
        }

        public static Rigidbody getRigidBody(this GameObject gameObject)
        {
            _rigidBodyBuffer.Clear();
            gameObject.GetComponents(_rigidBodyBuffer);
            return _rigidBodyBuffer.Count != 0 ? _rigidBodyBuffer[0] : null;
        }

        public static Rigidbody2D getRigidBody2D(this GameObject gameObject)
        {
            _rigidBody2DBuffer.Clear();
            gameObject.GetComponents(_rigidBody2DBuffer);
            return _rigidBody2DBuffer.Count != 0 ? _rigidBody2DBuffer[0] : null;
        }

        public static void setAllCollidersEnabled(this GameObject gameObject, bool enabled)
        {
            _colliderBuffer.Clear();
            gameObject.GetComponents(_colliderBuffer);
            foreach (var c in _colliderBuffer)
                c.enabled = enabled;
        }

        public static void getAllColliders(this GameObject gameObject, List<Collider> colliders)
        {
            colliders.Clear();
            _colliderBuffer.Clear();
            gameObject.GetComponents(_colliderBuffer);
            colliders.AddRange(_colliderBuffer);
        }

        public static Collider getCollider(this GameObject gameObject)
        {
            _colliderBuffer.Clear();
            gameObject.GetComponents(_colliderBuffer);
            return _colliderBuffer.Count != 0 ? _colliderBuffer[0] : null;
        }

        public static Collider2D getCollider2D(this GameObject gameObject)
        {
            _collider2DBuffer.Clear();
            gameObject.GetComponents(_collider2DBuffer);
            return _collider2DBuffer.Count != 0 ? _collider2DBuffer[0] : null;
        }

        public static MeshFilter getMeshFilter(this GameObject gameObject)
        {
            _meshFilterBuffer.Clear();
            gameObject.GetComponents(_meshFilterBuffer);
            return _meshFilterBuffer.Count != 0 ? _meshFilterBuffer[0] : null;
        }

        public static MeshRenderer getMeshRenderer(this GameObject gameObject)
        {
            _meshRendererBuffer.Clear();
            gameObject.GetComponents(_meshRendererBuffer);
            return _meshRendererBuffer.Count != 0 ? _meshRendererBuffer[0] : null;
        }

        public static SkinnedMeshRenderer getSkinnedMeshRenderer(this GameObject gameObject)
        {
            _skinnedMeshRendererBuffer.Clear();
            gameObject.GetComponents(_skinnedMeshRendererBuffer);
            return _skinnedMeshRendererBuffer.Count != 0 ? _skinnedMeshRendererBuffer[0] : null;
        }

        public static Renderer getMeshOrSkinnedMeshRenderer(this GameObject gameObject)
        {
            _meshRendererBuffer.Clear();
            gameObject.GetComponents(_meshRendererBuffer);
            if (_meshRendererBuffer.Count != 0) return _meshRendererBuffer[0];

            _skinnedMeshRendererBuffer.Clear();
            gameObject.GetComponents(_skinnedMeshRendererBuffer);
            return _skinnedMeshRendererBuffer.Count != 0 ? _skinnedMeshRendererBuffer[0] : null;
        }

        public static void setMeshOrSkinnedMeshRendererEnabled(this GameObject gameObject, bool enabled)
        {
            var r = gameObject.getMeshOrSkinnedMeshRenderer();
            if (r != null) r.enabled = false;
        }

        public static bool isMeshOrSkinnedMeshRendererEnabled(this GameObject gameObject)
        {
            MeshRenderer meshRenderer = gameObject.getMeshRenderer();
            if (meshRenderer != null) return meshRenderer.enabled;

            SkinnedMeshRenderer skinnedRenderer = gameObject.getSkinnedMeshRenderer();
            if (skinnedRenderer != null) return skinnedRenderer.enabled;

            return false;
        }

        public static bool isSpriteRendererEnabled(this GameObject gameObject)
        {
            SpriteRenderer spriteRenderer = gameObject.getSpriteRenderer();
            if (spriteRenderer != null) return spriteRenderer.enabled;

            return false;
        }

        public static GSpawn getPlugin(this GameObject gameObject)
        {
            _pluginBuffer.Clear();
            gameObject.GetComponents(_pluginBuffer);
            return _pluginBuffer.Count != 0 ? _pluginBuffer[0] : null;
        }

        public static Camera getCamera(this GameObject gameObject)
        {
            _cameraBuffer.Clear();
            gameObject.GetComponents(_cameraBuffer);
            return _cameraBuffer.Count != 0 ? _cameraBuffer[0] : null;
        }

        public static Light getLight(this GameObject gameObject)
        {
            _lightBuffer.Clear();
            gameObject.GetComponents(_lightBuffer);
            return _lightBuffer.Count != 0 ? _lightBuffer[0] : null;
        }

        public static ParticleSystem getParticleSystem(this GameObject gameObject)
        {
            _particleSystemBuffer.Clear();
            gameObject.GetComponents(_particleSystemBuffer);
            return _particleSystemBuffer.Count != 0 ? _particleSystemBuffer[0] : null;
        }

        public static Tree getTree(this GameObject gameObject)
        {
            _treeBuffer.Clear();
            gameObject.GetComponents(_treeBuffer);
            return _treeBuffer.Count != 0 ? _treeBuffer[0] : null;
        }

        public static Sprite getSprite(this GameObject gameObject)
        {
            // Note: It's faster to call 'GetComponents' (plural) than 'GetComponent' (singular).
            _spriteRendererBuffer.Clear();
            gameObject.GetComponents(_spriteRendererBuffer);
            return _spriteRendererBuffer.Count != 0 ? _spriteRendererBuffer[0].sprite : null;
        }

        public static SpriteRenderer getSpriteRenderer(this GameObject gameObject)
        {
            _spriteRendererBuffer.Clear();
            gameObject.GetComponents(_spriteRendererBuffer);
            return _spriteRendererBuffer.Count != 0 ? _spriteRendererBuffer[0] : null;
        }

        public static SpriteRenderer getSpriteRendererInChildren(this GameObject gameObject)
        {
            _spriteRendererBuffer.Clear();
            gameObject.GetComponentsInChildren(_spriteRendererBuffer);
            return _spriteRendererBuffer.Count != 0 ? _spriteRendererBuffer[0] : null;
        }

        public static Terrain getTerrain(this GameObject gameObject)
        {
            _terrainBuffer.Clear();
            gameObject.GetComponents(_terrainBuffer);
            return _terrainBuffer.Count != 0 ? _terrainBuffer[0] : null;
        }

        public static TerrainCollider getTerrainCollider(this GameObject gameObject)
        {
            _terrainColliderBuffer.Clear();
            gameObject.GetComponents(_terrainColliderBuffer);
            return _terrainColliderBuffer.Count != 0 ? _terrainColliderBuffer[0] : null;
        }

        public static void getGameObjects(IEnumerable<Transform> transforms, List<GameObject> gameObjects)
        {
            gameObjects.Clear();
            foreach (var t in transforms)
                gameObjects.Add(t.gameObject);
        }

        public static void getRoots(IEnumerable<GameObject> gameObjects, List<GameObject> roots)
        {
            if (gameObjects == null) return;

            roots.Clear();
            HashSet<GameObject> rootHash = new HashSet<GameObject>();
            foreach (var gameObject in gameObjects)
            {
                GameObject root = gameObject.transform.root.gameObject;
                if (!rootHash.Contains(root))
                {
                    rootHash.Add(root);
                    roots.Add(root);
                }
            }

            rootHash.Clear();
        }

        public static void getParents(IEnumerable<GameObject> gameObjects, List<GameObject> parents)
        {
            if (gameObjects == null) return;

            parents.Clear();
            foreach (var gameObject in gameObjects)
            {
                bool foundParent = false;
                Transform objectTransform = gameObject.transform;

                foreach (var possibleParent in gameObjects)
                {
                    if (possibleParent != gameObject)
                    {
                        if (objectTransform.IsChildOf(possibleParent.transform))
                        {
                            foundParent = true;
                            break;
                        }
                    }
                }

                if (!foundParent) parents.Add(gameObject);
            }
        }

        public static void getOutermostPrefabInstanceRoots(IEnumerable<GameObject> gameObjects, List<GameObject> prefabAssets, List<GameObject> prefabInstances, Func<GameObject, bool> prefabInstanceFilter)
        {
            prefabInstances.Clear();
            _prefabInstanceRootSet.Clear();
            foreach (var gameObject in gameObjects)
            {
                var prefabInstanceRoot = gameObject.getOutermostPrefabInstanceRoot();
                if (prefabInstanceRoot != null && !_prefabInstanceRootSet.Contains(prefabInstanceRoot))
                {
                    var prefab = prefabInstanceRoot.getPrefabAsset();
                    if (prefab != null && prefabAssets.Contains(prefab))
                    {
                        if (prefabInstanceFilter == null || prefabInstanceFilter(prefabInstanceRoot))
                        {
                            _prefabInstanceRootSet.Add(prefabInstanceRoot);
                            prefabInstances.Add(prefabInstanceRoot);
                        }
                    }
                }
            }
        }

        public static void getOutermostPrefabInstanceRoots(IEnumerable<GameObject> gameObjects, GameObject prefabAsset, List<GameObject> prefabInstances, Func<GameObject, bool> prefabInstanceFilter)
        {
            prefabInstances.Clear();
            _prefabInstanceRootSet.Clear();
            foreach (var gameObject in gameObjects)
            {
                var prefabInstanceRoot = gameObject.getOutermostPrefabInstanceRoot();
                if (prefabInstanceRoot != null && !_prefabInstanceRootSet.Contains(prefabInstanceRoot))
                {
                    var prefab = prefabInstanceRoot.getPrefabAsset();
                    if (prefab != null && prefabAsset == prefab)
                    {
                        if (prefabInstanceFilter == null || prefabInstanceFilter(prefabInstanceRoot))
                        {
                            _prefabInstanceRootSet.Add(prefabInstanceRoot);
                            prefabInstances.Add(prefabInstanceRoot);
                        }
                    }
                }
            }
        }

        public static void getOutermostPrefabInstanceRoots(IEnumerable<GameObject> gameObjects, List<GameObject> prefabInstances, Func<GameObject, bool> prefabInstanceFilter)
        {
            prefabInstances.Clear();
            _prefabInstanceRootSet.Clear();
            foreach (var gameObject in gameObjects)
            {
                var prefabInstanceRoot = gameObject.getOutermostPrefabInstanceRoot();
                if (prefabInstanceRoot != null && !_prefabInstanceRootSet.Contains(prefabInstanceRoot))
                {
                    var prefab = prefabInstanceRoot.getPrefabAsset();
                    if (prefab != null)
                    {
                        if (prefabInstanceFilter == null || prefabInstanceFilter(prefabInstanceRoot))
                        {
                            _prefabInstanceRootSet.Add(prefabInstanceRoot);
                            prefabInstances.Add(prefabInstanceRoot);
                        }
                    }
                }
            }
        }

        public static void getPrefabInstancesAndNonInstances(IEnumerable<GameObject> gameObjects, List<GameObject> destinationBuffer)
        {
            destinationBuffer.Clear();
            _prefabInstanceRootSet.Clear();
            foreach (var gameObject in gameObjects)
            {
                var prefabInstanceRoot = gameObject.getOutermostPrefabInstanceRoot();
                if (prefabInstanceRoot == null) destinationBuffer.Add(gameObject);
                else
                if (prefabInstanceRoot != null && !_prefabInstanceRootSet.Contains(prefabInstanceRoot))
                {
                    var prefab = prefabInstanceRoot.getPrefabAsset();
                    if (prefab != null)
                    {
                        _prefabInstanceRootSet.Add(prefabInstanceRoot);
                        destinationBuffer.Add(prefabInstanceRoot);
                    }
                }
            }
        }
    }
}
#endif
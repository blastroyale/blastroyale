#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace GSpawn
{
    public static class MeshCombiner
    {
        private class MaterialInstance
        {
            public Material             material;
            public List<MeshInstance>   meshInstances = new List<MeshInstance>();
        }

        private class MeshInstance
        {
            public Mesh                 mesh;
            public int                  subMeshIndex;
            public Transform            transform;
        }

        private class CombinedMeshData
        {
            public List<Vector3>        positions;
            public List<Color>          colors;
            public List<Vector4>        tangents;
            public List<Vector3>        normals;
            public List<Vector2>        uv1;
            public List<Vector2>        uv2;
            public List<Vector2>        uv3;
            public List<Vector2>        uv4;
            public List<int>            indices;

            public int                  currentVertexIndex;

            public CombinedMeshData()
            {
                tangents        = new List<Vector4>();
                positions       = new List<Vector3>();
                normals         = new List<Vector3>();
                uv1             = new List<Vector2>();
                uv2             = new List<Vector2>();
                uv3             = new List<Vector2>();
                uv4             = new List<Vector2>();
                colors          = new List<Color>();
                indices         = new List<int>();
            }

            public CombinedMeshData(int combinedNumVertsGuess)
            {
                tangents        = new List<Vector4>(combinedNumVertsGuess);
                positions       = new List<Vector3>(combinedNumVertsGuess);
                normals         = new List<Vector3>(combinedNumVertsGuess);
                uv1             = new List<Vector2>(combinedNumVertsGuess);
                uv2             = new List<Vector2>(combinedNumVertsGuess);
                uv3             = new List<Vector2>(combinedNumVertsGuess);
                uv4             = new List<Vector2>(combinedNumVertsGuess);
                colors          = new List<Color>(combinedNumVertsGuess);
                indices         = new List<int>(combinedNumVertsGuess / 3);
            }

            public void reset()
            {
                tangents.Clear();
                positions.Clear();
                normals.Clear();
                uv1.Clear();
                uv2.Clear();
                uv3.Clear();
                uv4.Clear();
                colors.Clear();
                indices.Clear();
                currentVertexIndex = 0;
            }

            public void addCurrentVertIndex()
            {
                // Note: Assumes that vertices are stored in the combined mesh buffers in the same
                //       way as they are encountered when reading the vertex data using indices
                //       from the source mesh.
                indices.Add(currentVertexIndex++);
            }

            public void reverseWindingOrderForLastTriangle()
            {
                int lastIndexPtr            = indices.Count - 1;
                int tempIndex               = indices[lastIndexPtr];
                indices[lastIndexPtr]       = indices[lastIndexPtr - 2];
                indices[lastIndexPtr - 2]   = tempIndex;
            }
        }

        private static List<Mesh>           _combinedMeshes = new List<Mesh>();
        private static MeshCombineSettings  _settings;

        private static List<GameObject>     _parentsBuffer  = new List<GameObject>();

        public static void combine(List<GameObject> sourceObjects, GameObject destinationParent, MeshCombineSettings settings)
        {
            if (sourceObjects.Count == 0)
            {
                EditorUtility.DisplayDialog("Missing Data", "No source objects available.", "Ok");
                return;
            }
            if (destinationParent == null)
            {
                EditorUtility.DisplayDialog("Missing Data", "You must specify a destination parent that will hold all meshes that result from the mesh combine process.", "Ok");
                return;
            }
            if (!settings.combineStaticMeshes && !settings.combineDynamicMeshes)
            {
                EditorUtility.DisplayDialog("Invalid Data", "You have specified that neither static nor dynamic meshes should be combined. " +
                    "At least one of these (i.e. static or dynamic) should be allowed.", "Ok");
                return;
            }

            _combinedMeshes.Clear();
            _settings = settings;

            GameObjectEx.getParents(sourceObjects, _parentsBuffer);
            List<GameObject> meshObjects = new List<GameObject>();
            collectMeshObjects(_parentsBuffer, meshObjects);
            if (meshObjects.Count == 0)
            {
                EditorUtility.DisplayDialog("Nothing Combined", "There were no meshes combined.", "Ok");
                return;
            }

            combineMeshObjects(meshObjects, destinationParent);
        }

        public static void combineChildren(GameObject sourceParent, GameObject destinationParent, MeshCombineSettings settings)
        {
            if (sourceParent == null)
            {
                EditorUtility.DisplayDialog("Missing Data", "You must specify a source parent whose child meshes must be combined.", "Ok");
                return;
            }
            if (destinationParent == null)
            {
                EditorUtility.DisplayDialog("Missing Data", "You must specify a destination parent that will hold all meshes that result from the mesh combine process.", "Ok");
                return;
            }
            if (!settings.combineStaticMeshes && !settings.combineDynamicMeshes)
            {
                EditorUtility.DisplayDialog("Invalid Data", "You have specified that neither static nor dynamic meshes should be combined. " + 
                    "At least one of these (i.e. static or dynamic) should be allowed.", "Ok");
                return;
            }

            _combinedMeshes.Clear();
            _settings = settings;

            List<GameObject> meshObjects = new List<GameObject>();
            collectMeshObjects(sourceParent, meshObjects, false);
            if (meshObjects.Count == 0)
            {
                EditorUtility.DisplayDialog("Nothing Combined", "There were no meshes combined.", "Ok");
                return;
            }

            combineMeshObjects(meshObjects, destinationParent);
        }

        private static void combineMeshObjects(List<GameObject> meshObjects, GameObject destinationParent)
        {
            List<MaterialInstance> materialInstances = new List<MaterialInstance>();
            collectMaterialInstances(meshObjects, materialInstances);

            // Note: Create the combined mesh folder if not present.
            if (!FileSystem.folderExists(_settings.combinedMeshFolder)) AssetDbEx.createAssetFolder(_settings.combinedMeshFolder);

            UndoEx.saveEnabledState();
            UndoEx.enabled = false;
            int numMaterialInstances = materialInstances.Count;
            for (int i = 0; i < numMaterialInstances; ++i)
                combine(materialInstances[i], destinationParent);

            int numMeshObjects = meshObjects.Count;
            PluginProgressDialog.begin("Post-Processing Mesh Objects");
            if (_settings.disableSourceRenderers)
            {
                for (int i = 0; i < numMeshObjects; ++i)
                {
                    PluginProgressDialog.updateProgress(meshObjects[i].name, (i + 1) / (float)numMeshObjects);
                    MeshRenderer r = meshObjects[i].getMeshRenderer();
                    if (r != null) r.enabled = false;
                }
            }
            PluginProgressDialog.end();

            int numCombinedMeshes = _combinedMeshes.Count;
            if (numCombinedMeshes != 0)
            {
                PluginProgressDialog.begin("Saving Mesh Assets");
                for (int i = 0; i < numCombinedMeshes; ++i)
                {
                    Mesh mesh = _combinedMeshes[i];
                    PluginProgressDialog.updateProgress(mesh.name, (i + 1) / (float)numCombinedMeshes);
                    AssetDatabase.CreateAsset(mesh, _settings.combinedMeshFolder + "/" + mesh.name + ".asset");
                }
                AssetDatabase.SaveAssets();
                PluginProgressDialog.end();
            }

            UndoEx.restoreEnabledState();
        }

        private static void collectMeshObjects(List<GameObject> parentObjects, List<GameObject> meshObjects)
        {
            meshObjects.Clear();

            PluginProgressDialog.begin("Collecting Mesh Objects");
            int numParentObjects = parentObjects.Count;
            for (int i = 0; i < numParentObjects; ++i)
            {
                var parent = parentObjects[i];
                collectMeshObjects(parent, meshObjects, true);

                PluginProgressDialog.updateProgress(parent.name, (i + 1) / (float)numParentObjects);
            }
            PluginProgressDialog.end();
        }

        private static void collectMeshObjects(GameObject sourceParent, List<GameObject> meshObjects, bool append)
        {
            if (!append)
            {
                meshObjects.Clear();

                PluginProgressDialog.begin("Collecting Mesh Objects");
                var meshRenderers = sourceParent.GetComponentsInChildren<MeshRenderer>(false);
                int numRenderers = meshRenderers.Length;
                for (int i = 0; i < numRenderers; ++i)
                {
                    var r = meshRenderers[i];
                    PluginProgressDialog.updateProgress(r.gameObject.name, (i + 1) / (float)numRenderers);
                    if (canMeshObjectBeCombined(r, sourceParent))
                        meshObjects.Add(r.gameObject);
                }
                PluginProgressDialog.end();
            }
            else
            {
                var meshRenderers = sourceParent.GetComponentsInChildren<MeshRenderer>(false);
                int numRenderers = meshRenderers.Length;
                for (int i = 0; i < numRenderers; ++i)
                {
                    var r = meshRenderers[i];
                    if (canMeshObjectBeCombined(r, sourceParent))
                        meshObjects.Add(r.gameObject);
                }
            }
        }

        private static void collectMaterialInstances(List<GameObject> meshObjects, List<MaterialInstance> materialInstances)
        {
            materialInstances.Clear();
            var materialInstanceMap = new Dictionary<Material, MaterialInstance>();

            PluginProgressDialog.begin("Collecting Material Instances");
            int numMeshObjects = meshObjects.Count;
            for (int i = 0; i < numMeshObjects; ++i)
            {
                var meshObject = meshObjects[i];
                PluginProgressDialog.updateProgress(meshObject.name, (i + 1) / (float)numMeshObjects);

                MeshFilter meshFilter = meshObject.getMeshFilter();
                MeshRenderer meshRenderer;

                if (_settings.combineLODs)
                {
                    int lodIndex = meshObject.findLODIndexAndMeshRenderer(out meshRenderer);
                    if (lodIndex < 0) meshRenderer = meshObject.getMeshRenderer();
                    else if (lodIndex != _settings.lodIndex) continue;
                }
                else meshRenderer = meshObject.getMeshRenderer();

                Mesh sharedMesh         = meshFilter.sharedMesh;
                int numSharedMaterials  = meshRenderer.sharedMaterials.Length;
                for (int subMeshIndex = 0; subMeshIndex < sharedMesh.subMeshCount; ++subMeshIndex)
                {
                    // Note: How can this happen?
                    if (subMeshIndex >= numSharedMaterials) break;

                    // Note: Only accepts triangle meshes.
                    if (sharedMesh.GetTopology(subMeshIndex) != MeshTopology.Triangles) continue;

                    Material material = meshRenderer.sharedMaterials[subMeshIndex];

                    MaterialInstance materialInstance;
                    if (!materialInstanceMap.ContainsKey(material))
                    {
                        materialInstance            = new MaterialInstance();
                        materialInstance.material   = material;

                        materialInstanceMap.Add(material, materialInstance);
                        materialInstances.Add(materialInstance);
                    }
                    else materialInstance = materialInstanceMap[material];

                    var meshInstance            = new MeshInstance();
                    meshInstance.mesh           = sharedMesh;
                    meshInstance.subMeshIndex   = subMeshIndex;
                    meshInstance.transform      = meshObject.transform;
                    materialInstance.meshInstances.Add(meshInstance);
                }

            }
            PluginProgressDialog.end();
        }

        private static bool canMeshObjectBeCombined(Renderer renderer, GameObject sourceParent)
        {
            if (!renderer.enabled) return false;

            GameObject gameObject = renderer.gameObject;
            if (!gameObject.activeInHierarchy) return false;

            if (!_settings.combineStaticMeshes && gameObject.isStatic) return false;
            if (!_settings.combineDynamicMeshes && !gameObject.isStatic) return false;

            MeshFilter meshFilter = gameObject.getMeshFilter();
            if (meshFilter == null || meshFilter.sharedMesh == null) return false;

            if (_settings.ignoreMultiLevelHierarchies)
            {
                if (sourceParent != null)
                {
                    if ((gameObject.transform.parent != null && gameObject.transform.parent.gameObject != sourceParent) ||
                        gameObject.transform.childCount != 0) return false;
                }
                else if (gameObject.transform.parent != null || gameObject.transform.childCount != 0) return false;
            }

            if (!_settings.combineLODs)
            {
                if (gameObject.isPartOfLODGroup()) return false;
            }

            return true;
        }

        private static int getMaxNumberOfMeshVerts()
        {
            if (_settings.combinedIndexFormat == MeshCombineIndexFormat.UInt16)
            {
                if (_settings.generateLightmapUVs) return 32000;
                return 65000;
            }
            else
            {
                if (_settings.generateLightmapUVs) return 4000000;
                return 8000000;

                // Note: These values seem to be too large and cause Unity to crash
                //       when saving the meshes as assets.
                /*
                if (_settings.generateLightmapUVs) return 1000000000;
                return 2000000000;*/
            }
        }

        private static void combine(MaterialInstance materialInstance, GameObject destinationParent)
        {
            List<MeshInstance> meshInstances = materialInstance.meshInstances;
            if (meshInstances.Count == 0) return;

            int maxNumMeshVerts     = getMaxNumberOfMeshVerts();
            var combinedMeshData    = new CombinedMeshData();

            PluginProgressDialog.begin("Combining Meshes for Material: " + materialInstance.material.name);
            List<GameObject> combinedMeshObjects = new List<GameObject>();
            for (int meshInstanceIndex = 0; meshInstanceIndex < meshInstances.Count; ++meshInstanceIndex)
            {
                MeshInstance meshInstance   = meshInstances[meshInstanceIndex];
                Mesh mesh                   = meshInstance.mesh;
                if (mesh.vertexCount == 0) continue;

                PluginProgressDialog.updateProgress("Mesh: " + meshInstance.mesh.name, (meshInstanceIndex + 1) / (float)meshInstances.Count);

                Matrix4x4 worldMatrix           = meshInstance.transform.localToWorldMatrix;
                Matrix4x4 worldInverseTranspose = worldMatrix.inverse.transpose;

                Vector3 worldScale              = meshInstance.transform.lossyScale;
                bool reverseVertexWindingOrder  = (worldScale.countNegative() % 2 != 0);

                int[] subMeshVertIndices        = mesh.GetTriangles(meshInstance.subMeshIndex);
                if (subMeshVertIndices.Length == 0) continue;

                Vector3[] positions             = mesh.vertices;
                Color[] colors                  = mesh.colors;
                Vector4[] tangents              = mesh.tangents;
                Vector3[] normals               = mesh.normals;
                Vector2[] uv1                   = mesh.uv;
                Vector2[] uv2                   = mesh.uv2;
                Vector2[] uv3                   = mesh.uv3;
                Vector2[] uv4                   = mesh.uv4;

                foreach (var vertIndex in subMeshVertIndices)
                {
                    if (tangents.Length != 0)
                    {
                        Vector3 transformedTangent = new Vector3(tangents[vertIndex].x, tangents[vertIndex].y, tangents[vertIndex].z);
                        transformedTangent = worldInverseTranspose.MultiplyVector(transformedTangent);
                        transformedTangent.Normalize();

                        combinedMeshData.tangents.Add(new Vector4(transformedTangent.x, transformedTangent.y, transformedTangent.z, tangents[vertIndex].w));
                    }

                    if (normals.Length != 0)
                    {
                        Vector3 transformedNormal = worldInverseTranspose.MultiplyVector(normals[vertIndex]);
                        transformedNormal.Normalize();

                        combinedMeshData.normals.Add(transformedNormal);
                    }

                    if (positions.Length != 0)  combinedMeshData.positions.Add(worldMatrix.MultiplyPoint(positions[vertIndex]));
                    if (colors.Length != 0)     combinedMeshData.colors.Add(colors[vertIndex]);
                    if (uv1.Length != 0)        combinedMeshData.uv1.Add(uv1[vertIndex]);
                    if (uv3.Length != 0)        combinedMeshData.uv2.Add(uv3[vertIndex]);
                    if (uv4.Length != 0)        combinedMeshData.uv3.Add(uv4[vertIndex]);
                    if (uv2.Length != 0 && !_settings.generateLightmapUVs) combinedMeshData.uv2.Add(uv2[vertIndex]);

                    combinedMeshData.addCurrentVertIndex();

                    int numIndices = combinedMeshData.indices.Count;
                    if (reverseVertexWindingOrder && numIndices % 3 == 0) combinedMeshData.reverseWindingOrderForLastTriangle();

                    int numMeshVerts = combinedMeshData.positions.Count;
                    if (combinedMeshData.indices.Count % 3 == 0 && (maxNumMeshVerts - numMeshVerts) < 3)
                    {
                        combinedMeshObjects.Add(createCombinedMeshObject(combinedMeshData, materialInstance, destinationParent));
                        combinedMeshData.reset();
                    }
                }
            }
            PluginProgressDialog.end();
            combinedMeshObjects.Add(createCombinedMeshObject(combinedMeshData, materialInstance, destinationParent));
        }

        private static GameObject createCombinedMeshObject(CombinedMeshData combinedMeshData, MaterialInstance materialInstance, GameObject destinationParent)
        {
            Mesh combinedMesh = createCombinedMesh(combinedMeshData, materialInstance);

            string baseName = _settings.combinedMeshObjectBaseName;
            if (baseName == null) baseName = string.Empty;

            GameObject combinedMeshObject       = new GameObject(baseName + materialInstance.material.name);
            combinedMeshObject.transform.parent = destinationParent.transform;
            combinedMeshObject.isStatic         = _settings.combineAsStatic ? true : false;

            MeshFilter meshFilter               = combinedMeshObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh               = combinedMesh;

            MeshRenderer meshRenderer           = combinedMeshObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial         = materialInstance.material;

            var boundsQConfig = ObjectBounds.QueryConfig.defaultConfig;
            boundsQConfig.objectTypes = GameObjectType.Mesh;
            AABB combinedAABB = ObjectBounds.calcWorldAABB(combinedMeshObject, boundsQConfig);
        
            Vector3 meshPivotPt;
            if (_settings.combinedMeshPivot == MeshCombinePivot.Center)             meshPivotPt = combinedAABB.center;
            else if (_settings.combinedMeshPivot == MeshCombinePivot.BackCenter)    meshPivotPt = Box3D.calcFaceCenter(combinedAABB.center, combinedAABB.size, Box3DFace.Back);
            else if (_settings.combinedMeshPivot == MeshCombinePivot.FrontCenter)   meshPivotPt = Box3D.calcFaceCenter(combinedAABB.center, combinedAABB.size, Box3DFace.Front);
            else if (_settings.combinedMeshPivot == MeshCombinePivot.BottomCenter)  meshPivotPt = Box3D.calcFaceCenter(combinedAABB.center, combinedAABB.size, Box3DFace.Bottom);
            else if (_settings.combinedMeshPivot == MeshCombinePivot.TopCenter)     meshPivotPt = Box3D.calcFaceCenter(combinedAABB.center, combinedAABB.size, Box3DFace.Top);
            else if (_settings.combinedMeshPivot == MeshCombinePivot.LeftCenter)    meshPivotPt = Box3D.calcFaceCenter(combinedAABB.center, combinedAABB.size, Box3DFace.Left);
            else meshPivotPt = Box3D.calcFaceCenter(combinedAABB.center, combinedAABB.size, Box3DFace.Right);

            combinedMeshObject.setMeshPivotPoint(combinedMesh, meshPivotPt);
            return combinedMeshObject;
        }

        private static Mesh createCombinedMesh(CombinedMeshData combinedMeshData, MaterialInstance materialInstance)
        {
            Mesh combinedMesh           = new Mesh();
            combinedMesh.name           = _settings.combinedMeshBaseName + materialInstance.material.name + "_" + combinedMesh.GetHashCode();
            combinedMesh.indexFormat    = _settings.combinedIndexFormat == MeshCombineIndexFormat.UInt32 ? IndexFormat.UInt32 : IndexFormat.UInt16;

            combinedMesh.vertices       = combinedMeshData.positions.ToArray();
            if (combinedMeshData.tangents.Count != 0)   combinedMesh.tangents = combinedMeshData.tangents.ToArray();
            if (combinedMeshData.normals.Count != 0)    combinedMesh.normals = combinedMeshData.normals.ToArray();
            if (combinedMeshData.uv1.Count != 0)        combinedMesh.uv = combinedMeshData.uv1.ToArray();
            if (combinedMeshData.uv3.Count != 0)        combinedMesh.uv3 = combinedMeshData.uv3.ToArray();
            if (combinedMeshData.uv4.Count != 0)        combinedMesh.uv4 = combinedMeshData.uv4.ToArray();
            combinedMesh.SetIndices(combinedMeshData.indices.ToArray(), MeshTopology.Triangles, 0);

            if (_settings.generateLightmapUVs) Unwrapping.GenerateSecondaryUVSet(combinedMesh);
            else if (combinedMeshData.uv2.Count != 0) combinedMesh.uv2 = combinedMeshData.uv2.ToArray();

            combinedMesh.UploadMeshData(!_settings.combinedMeshesAreReadable);
            _combinedMeshes.Add(combinedMesh);

            return combinedMesh;
        }
    }
}
#endif
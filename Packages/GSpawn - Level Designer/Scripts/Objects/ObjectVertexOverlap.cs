#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public static class ObjectVertexOverlap
    {
        private static List<Vector3>        _vertBuffer_overlapHierarchiesWorldVerts    = new List<Vector3>();
        private static List<Vector3>        _vertBuffer_overlapHierarchyWorldVerts      = new List<Vector3>();
        private static List<GameObject>     _meshObjectBuffer                           = new List<GameObject>();
        private static List<GameObject>     _spriteObjectBuffer                         = new List<GameObject>();

        // Note: Used to be 0.01f but doesn't work for some prefabs that contain irregularities.
        public static float                 safeMinOverlapSize                          { get { return 0.055f; } }    
        public static float                 safeMinOverlapEps                           { get { return 1e-4f; } }

        public static void overlapSpriteModelVerts(Sprite sprite, AABB modelABB, List<Vector3> verts)
        {
            verts.Clear();
            modelABB.inflate(safeMinOverlapEps);
            var spriteModelVerts = sprite.vertices;

            foreach (var vertPos in spriteModelVerts)
            {
                if (modelABB.containsPoint(vertPos))
                    verts.Add(vertPos);
            }
        }

        public static void overlapSpriteWorldVerts(Sprite sprite, Transform spriteTransform, OBB worldOBB, List<Vector3> verts)
        {
            verts.Clear();
            worldOBB.inflate(safeMinOverlapEps);
            var spriteWorldVerts = new List<Vector3>();
            sprite.getWorldVerts(spriteTransform, spriteWorldVerts);

            foreach (var vertPos in spriteWorldVerts)
            {
                if (worldOBB.containsPoint(vertPos))
                    verts.Add(vertPos);
            }
        }

        public static void overlapMeshModelVerts(GameObject gameObject, OBB modelOBB, List<Vector3> verts)
        {
            verts.Clear();
            modelOBB.inflate(safeMinOverlapEps);

            Mesh objectMesh         = gameObject.getMesh();
            PluginMesh pluginMesh   = PluginMeshDb.instance.getPluginMesh(objectMesh);
            if (pluginMesh != null) pluginMesh.modelVertsOverlapBox(modelOBB, verts);
        }

        public static void overlapMeshModelVerts(GameObject gameObject, AABB modelAABB, List<Vector3> verts)
        {
            verts.Clear();
            modelAABB.inflate(safeMinOverlapEps);

            Mesh objectMesh         = gameObject.getMesh();
            PluginMesh pluginMesh   = PluginMeshDb.instance.getPluginMesh(objectMesh);
            if (pluginMesh != null) pluginMesh.modelVertsOverlapBox(modelAABB, verts);
        }

        public static void overlapMeshWorldVerts(GameObject gameObject, OBB worldOBB, List<Vector3> verts)
        {
            verts.Clear();
            worldOBB.inflate(safeMinOverlapEps);

            Mesh objectMesh         = gameObject.getMesh();
            PluginMesh pluginMesh   = PluginMeshDb.instance.getPluginMesh(objectMesh);
            if (pluginMesh != null) pluginMesh.vertsOverlapBox(worldOBB, gameObject.transform, verts);
        }

        public static void overlapHierarchiesWorldVerts(List<GameObject> parents, OBB worldOBB, Box3DFace overlapStartFace, float overlapSize, List<Vector3> verts)
        {
            verts.Clear();
            foreach (var parent in parents)
            {
                overlapHierarchyWorldVerts(parent, worldOBB, overlapStartFace, overlapSize, _vertBuffer_overlapHierarchiesWorldVerts);
                if (_vertBuffer_overlapHierarchiesWorldVerts.Count != 0) verts.AddRange(_vertBuffer_overlapHierarchiesWorldVerts);
            }
        }

        public static void overlapHierarchyWorldVerts(GameObject parent, OBB worldOBB, Box3DFace overlapStartFace, float overlapSize, List<Vector3> verts)
        {
            verts.Clear();
            parent.getMeshObjectsInHierarchy(false, false, _meshObjectBuffer);
            parent.getSpriteObjectsInHierarchy(false, false, _spriteObjectBuffer);
            if (_meshObjectBuffer.Count == 0 && _spriteObjectBuffer.Count == 0) return;

            OBB overlapOBB = worldOBB.calcInwardFaceExtrusion(overlapStartFace, overlapSize);
            overlapOBB.inflate(safeMinOverlapEps);

            foreach (var meshObject in _meshObjectBuffer)
            {
                Mesh mesh               = meshObject.getMesh();
                PluginMesh pluginMesh   = PluginMeshDb.instance.getPluginMesh(mesh);
                if (pluginMesh == null) continue;

                pluginMesh.vertsOverlapBox(overlapOBB, meshObject.transform, _vertBuffer_overlapHierarchyWorldVerts);
                if (_vertBuffer_overlapHierarchyWorldVerts.Count != 0) verts.AddRange(_vertBuffer_overlapHierarchyWorldVerts);
            }

            foreach (var spriteObject in _spriteObjectBuffer)
            {
                overlapSpriteWorldVerts(spriteObject.getSprite(), spriteObject.transform, overlapOBB, _vertBuffer_overlapHierarchyWorldVerts);
                if (_vertBuffer_overlapHierarchyWorldVerts.Count != 0) verts.AddRange(verts);
            }
        }
    }
}
#endif
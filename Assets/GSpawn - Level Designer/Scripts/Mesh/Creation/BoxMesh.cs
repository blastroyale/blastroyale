#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public static class BoxMesh
    {
        public static Mesh create(float width, float height, float depth, Color color)
        {
            if (width < 1e-4f) return null;
            if (height < 1e-4f) return null;
            if (depth < 1e-4f) return null;

            float halfWidth     = width * 0.5f;
            float halfHeight    = height * 0.5f;
            float halfDepth     = depth * 0.5f;

            Vector3[] positions = new Vector3[]
            {
                // Front face
                new Vector3(-halfWidth, -halfHeight, -halfDepth),
                new Vector3(-halfWidth, halfHeight, -halfDepth),
                new Vector3(halfWidth, halfHeight, -halfDepth),
                new Vector3(halfWidth, -halfHeight, -halfDepth),

                // Back face
                new Vector3(halfWidth, -halfHeight, halfDepth),
                new Vector3(halfWidth, halfHeight, halfDepth),
                new Vector3(-halfWidth, halfHeight, halfDepth),
                new Vector3(-halfWidth, -halfHeight, halfDepth),

                // Top face
                new Vector3(-halfWidth, halfHeight, -halfDepth),
                new Vector3(-halfWidth, halfHeight, halfDepth),
                new Vector3(halfWidth, halfHeight, halfDepth),
                new Vector3(halfWidth, halfHeight, -halfDepth),

                // Bottom face
                new Vector3(halfWidth, -halfHeight, -halfDepth),
                new Vector3(halfWidth, -halfHeight, halfDepth),
                new Vector3(-halfWidth, -halfHeight, halfDepth),
                new Vector3(-halfWidth, -halfHeight, -halfDepth),

                // Left face
                new Vector3(-halfWidth, -halfHeight, halfDepth),
                new Vector3(-halfWidth, halfHeight, halfDepth),
                new Vector3(-halfWidth, halfHeight, -halfDepth),
                new Vector3(-halfWidth, -halfHeight, -halfDepth),

                // Right face
                new Vector3(halfWidth, -halfHeight, -halfDepth),
                new Vector3(halfWidth, halfHeight, -halfDepth),
                new Vector3(halfWidth, halfHeight, halfDepth),
                new Vector3(halfWidth, -halfHeight, halfDepth)
            };

            Vector3[] normals = new Vector3[]
            {
                // Front face
                -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward,
                
                // Back face
                Vector3.forward,  Vector3.forward, Vector3.forward, Vector3.forward,

                // Top face
                Vector3.up,  Vector3.up,  Vector3.up, Vector3.up,

                // Bottom face
                -Vector3.up, -Vector3.up, -Vector3.up, -Vector3.up,

                // Left face
                -Vector3.right, -Vector3.right,  -Vector3.right, -Vector3.right,

                // Right face
                Vector3.right, Vector3.right, Vector3.right, Vector3.right
            };

            int[] indices = new int[] 
            {
                // Front face
                0, 1, 2, 2, 3, 0,   

                // Back face
                4, 5, 6, 6, 7, 4,
                
                // Top face
                8, 9, 10, 10, 11, 8,

                // Bottom face
                12, 13, 14, 14, 15, 12,

                // Left face
                16, 17, 18, 18, 19, 16,

                // Right face
                20, 21, 22, 22, 23, 20
            };

            Mesh mesh       = new Mesh();
            mesh.vertices   = positions;
            mesh.normals    = normals;
            mesh.colors     = ColorEx.createFilledColorArray(24, color);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.UploadMeshData(false);

            return mesh;
        }

        public static Mesh createWire(float width, float height, float depth, Color color)
        {
            if (width < 1e-4f) return null;
            if (height < 1e-4f) return null;
            if (depth < 1e-4f) return null;

            float halfWidth     = width * 0.5f;
            float halfHeight    = height * 0.5f;
            float halfDepth     = depth * 0.5f;

            Vector3[] positions = new Vector3[]
            {
                // Front face
                new Vector3(-halfWidth, -halfHeight, -halfDepth),
                new Vector3(-halfWidth, halfHeight, -halfDepth),
                new Vector3(halfWidth, halfHeight, -halfDepth),
                new Vector3(halfWidth, -halfHeight, -halfDepth),

                // Back face
                new Vector3(halfWidth, -halfHeight, halfDepth),
                new Vector3(halfWidth, halfHeight, halfDepth),
                new Vector3(-halfWidth, halfHeight, halfDepth),
                new Vector3(-halfWidth, -halfHeight, halfDepth),

                // Top face
                new Vector3(-halfWidth, halfHeight, -halfDepth),
                new Vector3(-halfWidth, halfHeight, halfDepth),
                new Vector3(halfWidth, halfHeight, halfDepth),
                new Vector3(halfWidth, halfHeight, -halfDepth),

                // Bottom face
                new Vector3(halfWidth, -halfHeight, -halfDepth),
                new Vector3(halfWidth, -halfHeight, halfDepth),
                new Vector3(-halfWidth, -halfHeight, halfDepth),
                new Vector3(-halfWidth, -halfHeight, -halfDepth),

                // Left face
                new Vector3(-halfWidth, -halfHeight, halfDepth),
                new Vector3(-halfWidth, halfHeight, halfDepth),
                new Vector3(-halfWidth, halfHeight, -halfDepth),
                new Vector3(-halfWidth, -halfHeight, -halfDepth),

                // Right face
                new Vector3(halfWidth, -halfHeight, -halfDepth),
                new Vector3(halfWidth, halfHeight, -halfDepth),
                new Vector3(halfWidth, halfHeight, halfDepth),
                new Vector3(halfWidth, -halfHeight, halfDepth)
            };

            Vector3[] normals = new Vector3[]
            {
                // Front face
                -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward,
                
                // Back face
                Vector3.forward,  Vector3.forward, Vector3.forward, Vector3.forward,

                // Top face
                Vector3.up,  Vector3.up,  Vector3.up, Vector3.up,

                // Bottom face
                -Vector3.up, -Vector3.up, -Vector3.up, -Vector3.up,

                // Left face
                -Vector3.right, -Vector3.right,  -Vector3.right, -Vector3.right,

                // Right face
                Vector3.right, Vector3.right, Vector3.right, Vector3.right
            };

            int[] indices = new int[] 
            {
                // Front face
                0, 1, 1, 2, 2, 3, 3, 0,  

                // Back face
                4, 5, 5, 6, 6, 7, 7, 4,
                
                // Top face
                8, 9, 9, 10, 10, 11, 11, 8,

                // Bottom face
                12, 13, 13, 14, 14, 15, 15, 12,

                // Left face
                16, 17, 17, 18, 18, 19, 19, 16,

                // Right face
                20, 21, 21, 22, 22, 23, 23, 20
            };

            Mesh mesh       = new Mesh();
            mesh.vertices   = positions;
            mesh.normals    = normals;
            mesh.colors     = ColorEx.createFilledColorArray(24, color);
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            mesh.UploadMeshData(false);

            return mesh;
        }
    }
}
#endif
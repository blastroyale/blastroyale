#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public static class QuadMesh
    {
        public static Mesh createWireXY(float width, float height, Color color)
        {
            if (width < 1e-4f) return null;
            if (height < 1e-4f) return null;

            float halfWidth     = width * 0.5f;
            float halfHeight    = height * 0.5f;

            Vector3[] positions = new Vector3[]
            {
                -Vector3.right * halfWidth - Vector3.up * halfHeight,
                -Vector3.right * halfWidth + Vector3.up * halfHeight,
                 Vector3.right * halfWidth + Vector3.up * halfHeight,
                 Vector3.right * halfWidth - Vector3.up * halfHeight
            };

            Mesh mesh       = new Mesh();
            mesh.vertices   = positions;
            mesh.colors     = ColorEx.createFilledColorArray(4, color);
            mesh.SetIndices(new int[] { 0, 1, 1, 2, 2, 3, 3, 0 }, MeshTopology.Lines, 0);
            mesh.UploadMeshData(false);

            return mesh;
        }

        public static Mesh createXY(float width, float height, Color color)
        {
            if (width < 1e-4f) return null;
            if (height < 1e-4f) return null;

            float halfWidth     = width * 0.5f;
            float halfHeight    = height * 0.5f;

            Vector3[] positions = new Vector3[]
            {
                -Vector3.right * halfWidth - Vector3.up * halfHeight,
                -Vector3.right * halfWidth + Vector3.up * halfHeight,
                 Vector3.right * halfWidth + Vector3.up * halfHeight,
                 Vector3.right * halfWidth - Vector3.up * halfHeight
            };

            Vector3[] normals   = new Vector3[]
            {
                -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward
            };

            Vector2[] uvs = new Vector2[]
            {
                Vector2.zero, new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 0.0f)
            };

            Mesh mesh       = new Mesh();
            mesh.vertices   = positions;
            mesh.normals    = normals;
            mesh.uv         = uvs;
            mesh.colors     = ColorEx.createFilledColorArray(4, color);
            mesh.SetIndices(new int[] { 0, 1, 2, 2, 3, 0 }, MeshTopology.Triangles, 0);
            mesh.UploadMeshData(false);

            return mesh;
        }

        public static Mesh createXZ(float width, float depth, Color color)
        {
            if (width < 1e-4f) return null;
            if (depth < 1e-4f) return null;

            float halfWidth = width * 0.5f;
            float halfDepth = depth * 0.5f;

            Vector3[] positions = new Vector3[]
            {
                -Vector3.right * halfWidth - Vector3.forward * halfDepth,
                -Vector3.right * halfWidth + Vector3.forward * halfDepth,
                 Vector3.right * halfWidth + Vector3.forward * halfDepth,
                 Vector3.right * halfWidth - Vector3.forward * halfDepth
            };

            Vector3[] normals = new Vector3[]
            {
                Vector3.up, Vector3.up, Vector3.up, Vector3.up
            };

            Vector2[] uvs = new Vector2[]
            {
                Vector2.zero, new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 0.0f)
            };

            Mesh mesh       = new Mesh();
            mesh.vertices   = positions;
            mesh.normals    = normals;
            mesh.uv         = uvs;
            mesh.colors     = ColorEx.createFilledColorArray(4, color);
            mesh.SetIndices(new int[] { 0, 1, 2, 2, 3, 0 }, MeshTopology.Triangles, 0);
            mesh.UploadMeshData(false);

            return mesh;
        }
    }
}
#endif
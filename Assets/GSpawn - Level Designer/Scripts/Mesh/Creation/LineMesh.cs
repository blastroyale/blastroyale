#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public static class LineMesh
    {
        public static Mesh createCoordSystemAxes(float axisLength, Color color)
        {
            if (axisLength < 1e-4f) return null;

            Vector3[] positions = new Vector3[]
            {
                Vector3.zero, 
                Vector3.right * axisLength,
                Vector3.up * axisLength,
                Vector3.forward * axisLength
            };

            int[] indices = new int[] 
            {
                0, 1,
                0, 2,
                0, 3
            };

            Mesh mesh       = new Mesh();
            mesh.vertices   = positions;
            mesh.colors     = ColorEx.createFilledColorArray(4, color);
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            mesh.UploadMeshData(false);

            return mesh;
        }

        public static Mesh create(Vector3 startPoint, Vector3 endPoint, Color color)
        {
            Mesh mesh       = new Mesh();
            mesh.vertices   = new Vector3[] { startPoint, endPoint };
            mesh.colors     = ColorEx.createFilledColorArray(2, color);
            mesh.SetIndices(new int[] { 0, 1 }, MeshTopology.Lines, 0);
            mesh.UploadMeshData(false);

            return mesh;
        }

        public static Mesh createXAxis(Vector3 origin, float length, Color color)
        {
            if (length < 1e-5f) return null;

            Vector3[] positions = new Vector3[]
            {
                origin, origin + Vector3.right * length
            };

            Mesh mesh       = new Mesh();
            mesh.vertices   = positions;
            mesh.colors     = ColorEx.createFilledColorArray(2, color);
            mesh.SetIndices(new int[] { 0, 1 }, MeshTopology.Lines, 0);
            mesh.UploadMeshData(false);

            return mesh;
        }
    }
}
#endif
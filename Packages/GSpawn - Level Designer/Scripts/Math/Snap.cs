#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public static class Snap
    {
        public struct GridSnapConfig
        {
            public Vector3 cellSize;
            public Vector3 origin;
            public Vector3 right;
            public Vector3 up;
            public Vector3 look;
        }

        public static float snap(float value, float snap)
        {
            return Mathf.Round(value / snap) * snap;
        }

        public static int snapToInt(float value, float snap)
        {
            return (int)(Mathf.Round(value / snap) * snap);
        }

        public static Vector3 gridSnapAxis(Vector3 point, GridSnapConfig snapConfig, int axisIndex)
        {
            Vector3 toPoint = point - snapConfig.origin;
            float dotX      = Vector3.Dot(toPoint, snapConfig.right);
            float dotY      = Vector3.Dot(toPoint, snapConfig.up);
            float dotZ      = Vector3.Dot(toPoint, snapConfig.look);

            if (axisIndex == 0) dotX = snap(dotX, snapConfig.cellSize.x);
            else if (axisIndex == 1) dotY = snap(dotY, snapConfig.cellSize.y);
            else dotZ = snap(dotZ, snapConfig.cellSize.z);

            return snapConfig.origin + snapConfig.right * dotX + snapConfig.up * dotY + snapConfig.look * dotZ;
        }

        public static Vector3 gridSnapAxes(Vector3 point, GridSnapConfig snapConfig, Vector3Int axes)
        {
            Vector3 toPoint = point - snapConfig.origin;
            float dotX      = Vector3.Dot(toPoint, snapConfig.right);
            float dotY      = Vector3.Dot(toPoint, snapConfig.up);
            float dotZ      = Vector3.Dot(toPoint, snapConfig.look);

            if (axes.x != 0) dotX = snap(dotX, snapConfig.cellSize.x);
            if (axes.y != 0) dotY = snap(dotY, snapConfig.cellSize.y);
            if (axes.z != 0) dotZ = snap(dotZ, snapConfig.cellSize.z);

            return snapConfig.origin + snapConfig.right * dotX + snapConfig.up * dotY + snapConfig.look * dotZ;
        }

        public static Vector3 gridSnapAllAxes(Vector3 point, GridSnapConfig snapConfig)
        {
            Vector3 toPoint = point - snapConfig.origin;
            Vector3 result  = snapConfig.origin;

            float dot       = Vector3.Dot(toPoint, snapConfig.right);
            result          += snapConfig.right * snap(dot, snapConfig.cellSize.x);

            dot             = Vector3.Dot(toPoint, snapConfig.up);
            result          += snapConfig.up * snap(dot, snapConfig.cellSize.y);

            dot             = Vector3.Dot(toPoint, snapConfig.look);
            result          += snapConfig.look * snap(dot, snapConfig.cellSize.z);

            return result;
        }
    }
}
#endif
#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public struct GridCell
    {
        public int x;
        public int y;
        public int z;
    }

    public class PluginGrid : ScriptableObject
    {
        [NonSerialized]
        private List<Vector3>       _vector3Buffer  = new List<Vector3>();

        public GridSettingsProfile  activeSettings  { get { return GridSettingsProfileDb.instance.activeProfile; } }
        public Quaternion           rotation        { get { return activeSettings.getOrientationRotation(); } }
        public Vector3              right           { get { return rotation * Vector3.right; } }
        public Vector3              up              { get { return rotation * Vector3.up; } }
        public Vector3              look            { get { return rotation * Vector3.forward; } }
        public Vector3              origin          { get { return up * activeSettings.localOriginYOffset; } }
        public Matrix4x4            transformMatrix { get { return Matrix4x4.TRS(origin, rotation, Vector3.one); } }
        public Plane                plane           { get { return new Plane(planeNormal, origin); } }
        public Vector3              planeNormal     { get { return up; } }

        public Vector3 getAxis(Axis axis, AxisSign axisSign)
        {
            Vector3 gridAxis                            = right;
            if (axis == Axis.Y) gridAxis                = up;
            else if (axis == Axis.Z) gridAxis           = look;
            if (axisSign == AxisSign.Negative) gridAxis = -gridAxis;

            return gridAxis;
        }

        public void matchCellSizeToOBBSize(OBB obb)
        {
            if (!obb.isValid) return;

            Vector3 projectedCenter = plane.projectPoint(obb.center);
            obb.calcCorners(_vector3Buffer, false);
            plane.projectPoints(_vector3Buffer);

            float radiusX = float.MinValue;
            float radiusZ = float.MinValue;

            foreach (var pt in _vector3Buffer)
            {
                Vector3 dir = pt - projectedCenter;

                float prj = Vector3Ex.absDot(dir, right);
                if (prj > radiusX) radiusX = prj;

                prj = Vector3Ex.absDot(dir, look);
                if (prj > radiusZ) radiusZ = prj;
            }

            activeSettings.cellSizeX = radiusX * 2.0f;
            activeSettings.cellSizeZ = radiusZ * 2.0f;
        }

        public void snapToPoint(Vector3 point)
        {
            float d                 = plane.GetDistanceToPoint(point);
            Vector3 newOriginOffset = activeSettings.localOriginOffset;
            newOriginOffset.y       += d;
            activeSettings.localOriginYOffset = newOriginOffset.y;
        }

        public Vector3 snapAllAxes(Vector3 point)
        {
            return Snap.gridSnapAllAxes(point, getGridSnapConfig());
        }

        public void snapAllAxes(GameObject gameObject)
        {
            gameObject.transform.position = Snap.gridSnapAllAxes(gameObject.transform.position, getGridSnapConfig());
        }

        public Vector3 snapAxes(Vector3 point, Vector3Int axes)
        {
            return Snap.gridSnapAxes(point, getGridSnapConfig(), axes);
        }

        public Vector3 snapAxis(Vector3 point, int axisIndex)
        {
            return Snap.gridSnapAxis(point, getGridSnapConfig(), axisIndex);
        }

        public void snapObjectAllAxes(GameObject gameObject)
        {
            var snapConfig = getGridSnapConfig();
            gameObject.transform.position = Snap.gridSnapAllAxes(gameObject.transform.position, snapConfig);
        }

        public void snapObjectsAllAxes(IEnumerable<GameObject> gameObjects)
        {
            var snapConfig = getGridSnapConfig();
            foreach (var go in gameObjects)
                go.transform.position = Snap.gridSnapAllAxes(go.transform.position, snapConfig);
        }

        public void snapObjectAxes(GameObject gameObject, Vector3Int axes)
        {
            var snapConfig = getGridSnapConfig();
            gameObject.transform.position = Snap.gridSnapAxes(gameObject.transform.position, snapConfig, axes);
        }

        public void snapObjectsAxes(IEnumerable<GameObject> gameObjects, Vector3Int axes)
        {
            var snapConfig = getGridSnapConfig();
            foreach (var go in gameObjects)
                go.transform.position = Snap.gridSnapAxes(go.transform.position, snapConfig, axes);
        }

        public void snapTransformsAllAxes(IEnumerable<Transform> transforms)
        {
            var snapConfig = getGridSnapConfig();
            foreach(var transform in transforms)
                transform.position = Snap.gridSnapAllAxes(transform.position, snapConfig);
        }

        public void snapTransformsAxis(IEnumerable<Transform> transforms, int axisIndex)
        {
            var snapConfig = getGridSnapConfig();
            foreach (var transform in transforms)
                transform.position = Snap.gridSnapAxis(transform.position, snapConfig, axisIndex);
        }

        public GridCell getCellFromPoint(Vector3 point)
        {
            Vector3 modelPoint = transformMatrix.inverse.MultiplyPoint(point);
            return new GridCell() 
            { 
                x = Mathf.FloorToInt(modelPoint.x / activeSettings.cellSizeX), 
                y = Mathf.FloorToInt(modelPoint.y / activeSettings.cellSizeY), 
                z = Mathf.FloorToInt(modelPoint.z / activeSettings.cellSizeZ) 
            };
        }

        public void calcCellCenterAndCorners(GridCell cell, bool ignoreY, List<Vector3> centerAndCorners)
        {
            centerAndCorners.Clear();

            Vector3 cellMin = origin + right * cell.x * activeSettings.cellSizeX + 
                              look * cell.z * activeSettings.cellSizeZ;
            if (!ignoreY) cellMin += up * cell.y * activeSettings.cellSizeY;

            Vector3 cellMax = cellMin + right * activeSettings.cellSizeX +
                              look * activeSettings.cellSizeZ;
            if (!ignoreY) cellMax += up * activeSettings.cellSizeY;

            centerAndCorners.Add((cellMin + cellMax) * 0.5f);
            centerAndCorners.Add(cellMin);
            centerAndCorners.Add(cellMin + look * activeSettings.cellSizeZ);
            centerAndCorners.Add(cellMax);
            centerAndCorners.Add(cellMin + right * activeSettings.cellSizeX);
        }

        private Snap.GridSnapConfig getGridSnapConfig()
        {
            Snap.GridSnapConfig config  = new Snap.GridSnapConfig();
            config.cellSize             = activeSettings.cellSize;
            config.origin               = origin;
            config.right                = right;
            config.up                   = up;
            config.look                 = look;

            return config;
        }
    }
}
#endif
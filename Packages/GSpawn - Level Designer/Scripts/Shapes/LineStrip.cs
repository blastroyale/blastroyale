#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GSpawn
{
    public class LineStrip
    {
        private Color           _tickColor      = Color.green;
        private float           _tickSize       = 10.0f;
        private Color           _segmentColor   = Color.white;

        private List<Vector3>   _points         = new List<Vector3>();

        public int              numPoints       { get { return _points.Count; } }
        public int              numSegments     { get { return numPoints - 1; } }
        public Color            tickColor       { get { return _tickColor; } set { _tickColor = value; } }
        public float            tickSize        { get { return _tickSize; } set { _tickSize = Mathf.Max(0.0f, value); } }
        public Color            segmentColor    { get { return _segmentColor; } set { _segmentColor = value; } }
        
        public Vector3 getPoint(int index)
        {
            return _points[index];
        }

        public void duplicateLastPoint()
        {
            if (numPoints != 0)
                _points.Add(_points[_points.Count - 1]);
        }

        public void clearKeepLastPoint()
        {
            if (numPoints != 0)
            {
                Vector3 lastPt = _points[_points.Count - 1];
                _points.Clear();
                _points.Add(lastPt);
            }
        }

        public void replaceLastPointOrAdd(Vector3 newPoint)
        {
            if (numPoints == 0) _points.Add(newPoint);
            else _points[_points.Count - 1] = newPoint;
        }

        public void removeLastPoint()
        {
            if (numPoints != 0)
                _points.RemoveAt(_points.Count - 1);
        }

        public void draw()
        {
            if (numPoints == 0) return;

            HandlesEx.saveColor();
            for (int ptIndex = 0; ptIndex < _points.Count; ++ptIndex)
            {
                Vector3 point = _points[ptIndex];
                if (ptIndex != _points.Count - 1)
                {
                    Handles.color = _segmentColor;
                    Handles.DrawLine(point, _points[ptIndex + 1]);
                }

                Handles.color = _tickColor;
                Handles.DotHandleCap(0, point, Quaternion.identity, HandleUtility.GetHandleSize(point) * _tickSize, EventType.Repaint);
            }
            HandlesEx.restoreColor();
        }
    }
}
#endif
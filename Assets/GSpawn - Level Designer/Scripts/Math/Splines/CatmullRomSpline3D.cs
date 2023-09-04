#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    [Serializable]
    public class CatmullRomSpline3D : ISpline3D
    {
        private struct ControlPointIndices
        {
            public int p0;
            public int p1;
            public int p2;
            public int p3;
        }

        private struct ControlPointWeights
        {
            public float w0;
            public float w1;
            public float w2;
            public float w3;
        }

        [SerializeField]
        private List<Vector3>       _controlPoints      = new List<Vector3>();

        public int                  numControlPoints    { get { return _controlPoints.Count; } }
        public int                  numSegments         { get { return numControlPoints - 1; } }

        public Vector3 calcControlPointCenter(bool excludeFirstAndLast)
        {
            int numCtrlPoints   = numControlPoints;
            Vector3 center      = Vector3.zero;
            if (numCtrlPoints == 0) return center;

            if (excludeFirstAndLast)
            {
                for (int i = 1; i < numCtrlPoints - 1; ++i)
                    center += _controlPoints[i];

                center /= (float)(numCtrlPoints - 2);
            }
            else
            {
                for (int i = 0; i < numCtrlPoints; ++i)
                    center += _controlPoints[i];

                center /= (float)numCtrlPoints;
            }

            return center;
        }

        public void clear()
        {
            _controlPoints.Clear();
        }

        public void copy(ISpline3D src)
        {
            if (src == this) return;

            _controlPoints.Clear();
            int numCtrlPoints = src.numControlPoints;
            for (int i = 0; i < numCtrlPoints; ++i)
                addControlPoint(src.getControlPoint(i));
        }

        public void setControlPoint(int index, Vector3 controlPt)
        {
            _controlPoints[index] = controlPt;
        }

        public void moveControlPoint(int index, Vector3 offset)
        {
            _controlPoints[index] += offset;
        }

        public void moveControlPoints(Vector3 offset)
        {
            for (int i = 0; i < numControlPoints; ++i)
                _controlPoints[i] += offset;
        }

        public void addControlPoint(Vector3 controlPt)
        {
            _controlPoints.Add(controlPt);
        }

        public void insertControlPoint(int index, Vector3 controlPt)
        {
            _controlPoints.Insert(index, controlPt);
        }

        public void removeControlPoint(int index)
        {
            _controlPoints.RemoveAt(index);
        }

        public void removeLastControlPoint()
        {
            if (_controlPoints.Count != 0)
            {
                _controlPoints.RemoveAt(_controlPoints.Count - 1);
            }
        }

        public Vector3 evalPosition(float t)
        {
            if (numControlPoints < 3) return Vector3.zero;
            if (t < 0.0f) return _controlPoints[0];
            if ((int)t > numControlPoints - 4) return _controlPoints[numControlPoints - 1];

            ControlPointIndices i = calcControlPointIndices(t);
            ControlPointWeights w = calcControlPointWeights(t);

            return new Vector3(_controlPoints[i.p0].x * w.w0 + _controlPoints[i.p1].x * w.w1 + _controlPoints[i.p2].x * w.w2 + _controlPoints[i.p3].x * w.w3,
                            _controlPoints[i.p0].y * w.w0 + _controlPoints[i.p1].y * w.w1 + _controlPoints[i.p2].y * w.w2 + _controlPoints[i.p3].y * w.w3,
                            _controlPoints[i.p0].z * w.w0 + _controlPoints[i.p1].z * w.w1 + _controlPoints[i.p2].z * w.w2 + _controlPoints[i.p3].z * w.w3);
        }

        public void evalPositions(List<Vector3> positions, float step)
        {
            positions.Clear();
            float lastT = numControlPoints - 1.0f;
            for (float t = 0.0f; t <= lastT; t += step)
                positions.Add(evalPosition(t));
        }

        public Vector3 getControlPoint(int index)
        {
            return _controlPoints[index];
        }

        public Vector3 getSegmentStart(int segmentIndex)
        {
            return _controlPoints[segmentIndex];
        }

        public Vector3 getSegmentEnd(int segmentIndex)
        {
            return _controlPoints[segmentIndex + 1];
        }

        private ControlPointIndices calcControlPointIndices(float t)
        {
            ControlPointIndices indices = new ControlPointIndices();

            indices.p1 = (int)t + 1;
            indices.p0 = indices.p1 - 1;
            indices.p2 = indices.p1 + 1;
            indices.p3 = indices.p2 + 1;

            return indices;
        }

        private ControlPointWeights calcControlPointWeights(float t)
        {
            t           = t - (int)t;
            float tSQ   = t * t;
            float tCB   = tSQ * t;

            return new ControlPointWeights()
            {
                w0 = 0.5f * (-tCB + 2.0f * tSQ - t),
                w1 = 0.5f * (3.0f * tCB - 5.0f * tSQ + 2.0f),
                w2 = 0.5f * (-3.0f * tCB + 4.0f * tSQ + t),
                w3 = 0.5f * (tCB - tSQ)
            };
        }
    }
}
#endif
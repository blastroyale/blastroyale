#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using System;

namespace GSpawn
{
    public enum ObjectSpawnCurveGizmoId
    {
        Move = 0,
        Rotate,
        Scale
    }

    public class ObjectSpawnCurveDb : ScriptableObject
    {
        [SerializeField]
        private List<ObjectSpawnCurve>      _curves             = new List<ObjectSpawnCurve>();
        [NonSerialized]
        private List<ObjectSpawnCurve>      _curveBuffer        = new List<ObjectSpawnCurve>();
        [NonSerialized]
        private List<string>                _stringBuffer       = new List<string>();

        public int                          numCurves           { get { return _curves.Count; } }

        public static ObjectSpawnCurveDb    instance            { get { return GSpawn.active.objectSpawnCurveDb; } }

        public void frameSelectedCurves()
        {
            if (numCurves == 0) return;

            AABB aabb = AABB.getInvalid();
            foreach(var curve in _curves)
            {
                if (curve.uiSelected)
                {
                    AABB curveAABB = curve.calcWorldAABB();
                    if (aabb.isValid)
                    {
                        if (curveAABB.isValid) aabb.encloseAABB(curveAABB);
                    }
                    else aabb = curveAABB;
                }
            }

            if (!aabb.isValid) return;
            SceneViewEx.frame(aabb.toBounds(), false);
        }

        public ObjectSpawnCurve createCurve(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            getCurveNames(_stringBuffer, null);
            name = UniqueNameGen.generate(name, _stringBuffer);

            UndoEx.saveEnabledState();
            UndoEx.enabled = false;
            UndoEx.record(this);
            var curve       = UndoEx.createScriptableObject<ObjectSpawnCurve>();
            curve.curveName = name;
            curve.name      = name;

            _curves.Add(curve);

             UndoEx.restoreEnabledState();
            return curve;
        }

        public ObjectSpawnCurve cloneCurve(ObjectSpawnCurve curve)
        {
            UndoEx.saveEnabledState();
            UndoEx.enabled  = false;

            var clonedCurve = createCurve(curve.curveName);
            clonedCurve.copy(curve);

            UndoEx.restoreEnabledState();
            return clonedCurve;
        }

        public void renameCurve(ObjectSpawnCurve curve, string newName)
        {
            if (!string.IsNullOrEmpty(newName) &&
                containsCurve(curve) && curve.curveName != newName)
            {
                getCurveNames(_stringBuffer, curve.curveName);
                UndoEx.record(this);
                curve.curveName     = UniqueNameGen.generate(newName, _stringBuffer);
                curve.name          = curve.curveName;
            }
        }

        public void deleteCurve(ObjectSpawnCurve curve)
        {
            UndoEx.saveEnabledState();
            UndoEx.enabled = false;
            if (curve != null)
            {
                if (containsCurve(curve))
                {
                    UndoEx.record(this);
                    _curves.Remove(curve);
                    UndoEx.destroyObjectImmediate(curve);
                }
            }
            UndoEx.restoreEnabledState();
        }

        public void deleteCurves(List<ObjectSpawnCurve> curves)
        {
            UndoEx.saveEnabledState();
            UndoEx.enabled = false;
            if (curves.Count != 0)
            {
                UndoEx.record(this);
                _curveBuffer.Clear();
                foreach (var curve in curves)
                {
                    if (containsCurve(curve))
                    {
                        _curves.Remove(curve);
                        _curveBuffer.Add(curve);
                    }
                }

                foreach (var curve in _curveBuffer)
                    UndoEx.destroyObjectImmediate(curve);
            }
            UndoEx.restoreEnabledState();
        }

        public void deleteAllCurves()
        {
            UndoEx.saveEnabledState();
            UndoEx.enabled = false;
            if (_curves.Count != 0)
            {
                UndoEx.record(this);

                _curveBuffer.Clear();
                _curveBuffer.AddRange(_curves);
                _curves.Clear();

                foreach (var curve in _curveBuffer)
                    UndoEx.destroyObjectImmediate(curve);
            }
            UndoEx.restoreEnabledState();
        }

        public bool containsCurve(ObjectSpawnCurve curve)
        {
            return _curves.Contains(curve);
        }

        public ObjectSpawnCurve getCurve(int index)
        {
            return _curves[index];
        }

        public void getCurves(List<ObjectSpawnCurve> curves)
        {
            curves.Clear();
            curves.AddRange(_curves);
        }

        public void getCurveNames(List<string> names, string ignoredName)
        {
            names.Clear();
            foreach (var curve in _curves)
            {
                if (curve.curveName != ignoredName)
                    names.Add(curve.curveName);
            }
        }

        private void OnDestroy()
        {
            deleteAllCurves();
        }
    }
}
#endif
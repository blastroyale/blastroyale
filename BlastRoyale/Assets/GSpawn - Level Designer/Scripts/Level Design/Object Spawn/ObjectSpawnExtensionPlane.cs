#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    [Serializable]
    public class ObjectSpawnExtensionPlane
    {
        private OBB                 _refOBB;
        [SerializeField]
        private Box3DFace           _refOBBFace         = Box3DFace.Bottom;
        private Vector3             _right;
        private Vector3             _look;
        private Plane               _plane;

        private Vector3[]           _drawVerts          = new Vector3[4];
        private float               _drawInflateAmount;

        [NonSerialized]
        private SceneRaycastFilter  _facePickRaycastFilter = new SceneRaycastFilter()
        {
            objectTypes = GameObjectType.Mesh | GameObjectType.Sprite
        };

        public OBB                  refOBB              { get { return _refOBB; } }
        public Box3DFace            refOBBFace          { get { return _refOBBFace; } }
        public Color                borderColor         { get; set; }
        public Color                fillColor           { get; set; }
        public Plane                plane               { get { return _plane; } }
        public Vector3              right               { get { return _right; } }
        public Vector3              look                { get { return _look; } }
        public Vector3              planeNormal         { get { return _plane.normal; } }
        public float                drawInflateAmount   { get { return _drawInflateAmount; } }

        public void pickRefOBBFaceWithCursor(List<GameObject> ignoredObjects)
        {
            _facePickRaycastFilter.setIgnoredObjects(ignoredObjects);
            var rayHit = PluginScene.instance.raycastClosest(PluginCamera.camera.getCursorRay(), _facePickRaycastFilter, ObjectRaycastConfig.defaultConfig);
            if (rayHit.anyHit)
            {
                Plane hitPlane                          = new Plane();
                if (rayHit.wasObjectHit) hitPlane       = rayHit.objectHit.hitPlane;
                else if (rayHit.wasGridHit) hitPlane    = rayHit.gridHit.hitPlane;

                Box3DFace boxFace = Box3D.findMostAlignedFace(_refOBB.center, _refOBB.size, _refOBB.rotation, -hitPlane.normal);
                setRefOBBFace(boxFace);
            }
        }

        public void setRefOBBFace(Box3DFace refOBBFace)
        {
            _refOBBFace             = refOBBFace;

            Box3DFaceDesc faceDesc  = Box3D.getFaceDesc(_refOBB.center, _refOBB.size, _refOBB.rotation, _refOBBFace);
            _plane                  = faceDesc.plane.invertNormal();
            _right                  = faceDesc.right;
            _look                   = faceDesc.look;

            createDrawData();
        }

        public void set(OBB refOBB, Box3DFace refOBBFace, float drawInflateAmount)
        {
            _refOBB                 = refOBB;
            _drawInflateAmount      = drawInflateAmount;
            setRefOBBFace(refOBBFace);
        }

        public bool raycast(Ray ray, out Vector3 intersectPt)
        {
            intersectPt = Vector3.zero;
            float t;
            if (_plane.Raycast(ray, out t))
            {
                intersectPt = ray.GetPoint(t);
                return true;
            }

            return false;
        }

        public bool cursorRaycast(out Vector3 intersectPt)
        {
            intersectPt = Vector3.zero;

            float t;
            Ray ray = PluginCamera.camera.getCursorRay();
            if (_plane.Raycast(ray, out t))
            {
                intersectPt = ray.GetPoint(t);
                return true;
            }

            return false;
        }

        public bool cursorRaycast()
        {
            float t;
            Ray ray = PluginCamera.camera.getCursorRay();
            if (_plane.Raycast(ray, out t)) return true;

            return false;
        }

        public void draw()
        {
            HandlesEx.saveMatrix();
            HandlesEx.saveColor();
            HandlesEx.saveZTest();

            Handles.zTest = CompareFunction.LessEqual;
            Handles.DrawSolidRectangleWithOutline(_drawVerts, fillColor, borderColor);

            HandlesEx.restoreZTest();
            HandlesEx.restoreColor();
            HandlesEx.restoreMatrix();
        }

        private bool ensureNonZeroAreaOBBFace()
        {
            float size0 = Vector3Ex.getSizeAlongAxis(_refOBB.size, _refOBB.rotation, right);
            float size1 = Vector3Ex.getSizeAlongAxis(_refOBB.size, _refOBB.rotation, look);

            if (size0 < 1e-4f || size1 < 1e-4f)
            {
                Box3DFace correctedFace;
                if (Box3D.firstNonZeroAreaFace(_refOBB.size, out correctedFace))
                    _refOBBFace = correctedFace;

                return true;
            }

            return false;
        }

        private void createDrawData()
        {
            Box3D.calcFaceCorners(_refOBB.center, Vector3Ex.add(_refOBB.size, _drawInflateAmount, Box3D.getFaceAxisMask(_refOBBFace)),
                _refOBB.rotation, _refOBBFace, _drawVerts);

            for (int i = 0; i < 4; ++i)
                _drawVerts[i] += _plane.normal * 0.03f;
        }
    }
}
#endif
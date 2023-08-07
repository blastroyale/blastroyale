#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.Collections.Generic;

namespace GSpawn
{
    public static class HandlesEx
    {
        private static Vector3[]                _unitCubeVerts      = new Vector3[]
        {
            // Back
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),

            // Front
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f)
        };

        private static Stack<Color>             _colorStack         = new Stack<Color>();
        private static Stack<Matrix4x4>         _matrixStack        = new Stack<Matrix4x4>();
        private static Stack<CompareFunction>   _zTestFuncStack     = new Stack<CompareFunction>();
        private static Stack<bool>              _litStack           = new Stack<bool>();
        private static Vector3[]                _triangleVerts      = new Vector3[4];
        private static List<Vector3>            _spriteVerts        = new List<Vector3>();

        public static void drawMeshWireTriangles(Mesh mesh, Transform transform, Color color)
        {
            PluginMesh pluginMesh = PluginMeshDb.instance.getPluginMesh(mesh);
            if (pluginMesh == null) return;

            HandlesEx.saveColor();
            HandlesEx.saveMatrix();
            HandlesEx.saveZTest();
            HandlesEx.saveLit();

            Handles.color       = color;
            Handles.lighting    = false;
            Handles.matrix      = transform.localToWorldMatrix;
            Handles.zTest       = CompareFunction.Disabled;

            int numTriangles = pluginMesh.numTriangles;
            for (int i = 0; i < numTriangles; ++i)
            {
                pluginMesh.getTriangleVerts(i, _triangleVerts);
                _triangleVerts[3] = _triangleVerts[0];
                Handles.DrawPolyLine(_triangleVerts);                
            }

            HandlesEx.restoreLit();
            HandlesEx.restoreZTest();
            HandlesEx.restoreMatrix();
            HandlesEx.restoreColor();
        }

        public static void drawSpriteWireTriangles(Sprite sprite, Transform transform, Color color)
        {
            HandlesEx.saveColor();
            HandlesEx.saveMatrix();
            HandlesEx.saveZTest();
            HandlesEx.saveLit();

            Handles.color       = color;
            Handles.lighting    = false;
            Handles.matrix      = transform.localToWorldMatrix;
            Handles.zTest       = CompareFunction.Disabled;

            var vertIndices     = sprite.triangles;
            sprite.getModelVerts(_spriteVerts);
            int numTriangles    = vertIndices.Length / 3;
            for (int i = 0; i < numTriangles; ++i)
            {
                _triangleVerts[0] = _spriteVerts[vertIndices[i * 3]];
                _triangleVerts[1] = _spriteVerts[vertIndices[i * 3 + 1]];
                _triangleVerts[2] = _spriteVerts[vertIndices[i * 3 + 2]];
                _triangleVerts[3] = _triangleVerts[0];
                Handles.DrawPolyLine(_triangleVerts);       
            }

            HandlesEx.restoreLit();
            HandlesEx.restoreZTest();
            HandlesEx.restoreMatrix();
            HandlesEx.restoreColor();
        }

        // Note: Needed because Handles.DrawWireCube seems to be broken as it doesn't take 
        //       the Handles.color property into account.
        public static void drawUnitWireCube()
        {
            // Back
            Handles.DrawLine(_unitCubeVerts[0], _unitCubeVerts[1]);
            Handles.DrawLine(_unitCubeVerts[1], _unitCubeVerts[2]);
            Handles.DrawLine(_unitCubeVerts[2], _unitCubeVerts[3]);
            Handles.DrawLine(_unitCubeVerts[3], _unitCubeVerts[0]);

            // Front
            Handles.DrawLine(_unitCubeVerts[4], _unitCubeVerts[5]);
            Handles.DrawLine(_unitCubeVerts[5], _unitCubeVerts[6]);
            Handles.DrawLine(_unitCubeVerts[6], _unitCubeVerts[7]);
            Handles.DrawLine(_unitCubeVerts[7], _unitCubeVerts[4]);

            // Top
            Handles.DrawLine(_unitCubeVerts[1], _unitCubeVerts[6]);
            Handles.DrawLine(_unitCubeVerts[2], _unitCubeVerts[5]);

            // Bottom
            Handles.DrawLine(_unitCubeVerts[0], _unitCubeVerts[7]);
            Handles.DrawLine(_unitCubeVerts[3], _unitCubeVerts[4]);
        }

        public static void saveLit()
        {
            _litStack.Push(Handles.lighting);
        }

        public static void restoreLit()
        {
            if (_litStack.Count != 0) Handles.lighting = _litStack.Pop();
        }

        public static void saveColor()
        {
            _colorStack.Push(Handles.color);
        }

        public static void restoreColor()
        {
            if (_colorStack.Count != 0) Handles.color = _colorStack.Pop();
        }

        public static void saveZTest()
        {
            _zTestFuncStack.Push(Handles.zTest);
        }

        public static void restoreZTest()
        {
            if (_zTestFuncStack.Count != 0) Handles.zTest = _zTestFuncStack.Pop();
        }

        public static void saveMatrix()
        {
            _matrixStack.Push(Handles.matrix);
        }

        public static void restoreMatrix()
        {
            if (_matrixStack.Count != 0) Handles.matrix = _matrixStack.Pop();
        }

        public static Vector3 calcLabelPositionBelowOBB(OBB obb)
        {
            float offset    = HandleUtility.GetHandleSize(obb.center) * 0.7f;
            Box3DFace face  = Box3D.findMostAlignedFace(obb.center, obb.size, obb.rotation, PluginCamera.camera.transform.up);
            var faceCenter  = Box3D.calcFaceCenter(obb.center, obb.size, obb.rotation, face);

            return faceCenter - PluginCamera.camera.transform.up * offset;
        }

        public static Vector3 calcLabelPositionAboveOBB(OBB obb)
        {
            float offset = HandleUtility.GetHandleSize(obb.center) * 0.7f;
            Box3DFace face = Box3D.findMostAlignedFace(obb.center, obb.size, obb.rotation, PluginCamera.camera.transform.up);
            var faceCenter = Box3D.calcFaceCenter(obb.center, obb.size, obb.rotation, face);

            return faceCenter + PluginCamera.camera.transform.up * offset;
        }
    }
}
#endif
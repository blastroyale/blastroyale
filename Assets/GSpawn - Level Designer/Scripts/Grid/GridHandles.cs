#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public static class GridHandles
    {
        public struct DrawConfig
        {
            public Color        wireColor;
            public Color        fillColor;
            public Vector3      origin;
            public Vector3      right;
            public Vector3      planeNormal;
            public Vector3      look;
            public float        cellSizeX;
            public float        cellSizeZ;

            public int          numCellsX;
            public int          numCellsZ;

            public bool         drawCoordSystem;
            public bool         infiniteXAxis;
            public bool         infiniteYAxis;
            public bool         infiniteZAxis;
            public float        finiteAxisLength;
            public Color        xAxisColor;
            public Color        yAxisColor;
            public Color        zAxisColor;

            public Quaternion calcRotation()
            {
                return Quaternion.LookRotation(look, planeNormal);
            }
        }

        public static void drawFinite(DrawConfig drawConfig, Camera camera)
        {
            GL.Viewport(camera.pixelRect);

            float gridSizeX     = drawConfig.numCellsX * drawConfig.cellSizeX;
            float gridSizeZ     = drawConfig.numCellsZ * drawConfig.cellSizeZ;
            Material material   = MaterialPool.instance.xzGrid;

            Vector3 origin      = drawConfig.origin;
            Vector3 drawPos     = drawConfig.origin;
            if (drawConfig.numCellsX % 2 != 0) origin -= drawConfig.right * drawConfig.cellSizeX * 0.5f;
            if (drawConfig.numCellsZ % 2 != 0) origin -= drawConfig.look * drawConfig.cellSizeZ * 0.5f;

            material.SetColor           ("_WireColor",      drawConfig.wireColor);
            material.SetColor           ("_FillColor",      drawConfig.fillColor);
            material.SetFloat           ("_CellSizeX",      drawConfig.cellSizeX);
            material.SetFloat           ("_CellSizeZ",      drawConfig.cellSizeZ);
            material.SetFloat           ("_FarPlaneDist",   camera.farClipPlane);
            material.SetVector          ("_CameraPos",      camera.transform.position);
            material.SetVector          ("_Origin",         origin);
            material.SetVector          ("_Right",          drawConfig.right);
            material.SetVector          ("_Look",           drawConfig.look);
            material.setZTestEnabled    (true);

            Matrix4x4 transformMtx = Matrix4x4.TRS(drawPos, drawConfig.calcRotation(), new Vector3(gridSizeX, 1.0f, gridSizeZ));

            int numPasses = material.passCount;
            for (int passIndex = 0; passIndex < numPasses; ++passIndex)
            {
                material.SetPass(passIndex);
                Graphics.DrawMeshNow(MeshPool.instance.unitQuadXZ, transformMtx);
            }

            if (drawConfig.drawCoordSystem) drawCoordSystem(drawConfig, camera);
        }

        public static void drawInfinite(DrawConfig drawConfig, Camera camera)
        {
            // Note: In newer versions of Unity, using Graphics.DrawMeshNow seems to behave in
            //       an unexpected manner where it draws inside the scene view (OK) but also
            //       over the scene view tab sitting at the top of the scene view (NOT OK). It
            //       seems the problem goes away by explicitly setting the viewport. There still
            //       is a small artifact to the left of the scene view area where it seems that
            //       the grid's coordinate system lines are being drawn, but it's minor and hardly
            //       noticeable. For the moment, this will have to do.
            GL.Viewport(camera.pixelRect);

            Vector3 offsetOrigin = drawConfig.origin + drawConfig.planeNormal * 0.0035f;

            Material material = MaterialPool.instance.xzGrid;
            material.SetColor           ("_WireColor",      drawConfig.wireColor);
            material.SetColor           ("_FillColor",      drawConfig.fillColor);
            material.SetFloat           ("_CellSizeX",      drawConfig.cellSizeX);
            material.SetFloat           ("_CellSizeZ",      drawConfig.cellSizeZ);
            material.SetFloat           ("_FarPlaneDist",   camera.farClipPlane);
            material.SetVector          ("_CameraPos",      camera.transform.position);
            material.SetVector          ("_Origin",         offsetOrigin);
            material.SetVector          ("_Right",          drawConfig.right);
            material.SetVector          ("_Look",           drawConfig.look);
            material.setZTestEnabled    (true);

            AABB camVolumeAABB      = camera.calcVolumeAABB(0.5f);
            camVolumeAABB.enclosePoint(camera.transform.position);

            // Note: Calculating the size from the camera view volume seems to cause the
            //       grid to flicker and shiver.
            //const float sizeAdd     = 30.0f;
            //float gridSize          = camVolumeAABB.size.magnitude + sizeAdd;
            float gridSize          = camera.farClipPlane * 0.5f;
            Quaternion rotation     = drawConfig.calcRotation();
            Plane plane             = new Plane(drawConfig.planeNormal, offsetOrigin);
            Matrix4x4 transformMtx  = Matrix4x4.TRS(plane.projectPoint(camera.transform.position), rotation, new Vector3(gridSize, 1.0f, gridSize));

            int numPasses           = material.passCount;
            for (int passIndex = 0; passIndex < numPasses; ++passIndex)
            {
                material.SetPass(passIndex);
                Graphics.DrawMeshNow(MeshPool.instance.unitQuadXZ, transformMtx);
            }

            if (drawConfig.drawCoordSystem) drawCoordSystem(drawConfig, camera);
        }
        
        private static void drawCoordSystem(DrawConfig drawConfig, Camera camera)
        {
            Material material       = MaterialPool.instance.xzGridCoordSystemLine;
            material.SetFloat   ("_FarPlaneDist",   camera.farClipPlane);
            material.SetVector  ("_CameraPos",      camera.transform.position);
            material.SetColor   ("_Color",          drawConfig.xAxisColor);
            material.SetPass(0);

            Vector3 offsetOrigin    = drawConfig.origin + drawConfig.planeNormal * 0.0035f;

            Quaternion rotation     = drawConfig.calcRotation();
            const float infiniteLength = 9999999.0f;
            Vector3 scale           = new Vector3(drawConfig.infiniteXAxis ? infiniteLength : drawConfig.finiteAxisLength, 1.0f, 1.0f);
            Matrix4x4 transformMtx  = Matrix4x4.TRS(offsetOrigin, rotation, scale);
            Graphics.DrawMeshNow(MeshPool.instance.unitXAxis, transformMtx);
            Graphics.DrawMeshNow(MeshPool.instance.unitXAxis, transformMtx * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(-1.0f, 1.0f, 1.0f)));

            material.SetColor("_Color", drawConfig.yAxisColor);
            material.SetPass(0);

            scale                   = new Vector3(drawConfig.infiniteYAxis ? infiniteLength : drawConfig.finiteAxisLength, 1.0f, 1.0f);
            transformMtx            = Matrix4x4.TRS(offsetOrigin, rotation * Quaternion.AngleAxis(90.0f, Vector3.forward), scale);
            Graphics.DrawMeshNow(MeshPool.instance.unitXAxis, transformMtx);
            Graphics.DrawMeshNow(MeshPool.instance.unitXAxis, transformMtx * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(-1.0f, 1.0f, 1.0f)));

            material.SetColor("_Color", drawConfig.zAxisColor);
            material.SetPass(0);

            scale                   = new Vector3(drawConfig.infiniteZAxis ? infiniteLength : drawConfig.finiteAxisLength, 1.0f, 1.0f);
            transformMtx            = Matrix4x4.TRS(offsetOrigin, rotation * Quaternion.AngleAxis(-90.0f, Vector3.up), scale);
            Graphics.DrawMeshNow(MeshPool.instance.unitXAxis, transformMtx);
            Graphics.DrawMeshNow(MeshPool.instance.unitXAxis, transformMtx * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(-1.0f, 1.0f, 1.0f)));
        }
    }
}
#endif
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GSpawn
{
    public static class CameraEx
    {
        public static void worldToScreenPoints(this Camera camera, List<Vector3> worldPoints, List<Vector2> screenPoints)
        {
            screenPoints.Clear();
            foreach(var worldPt in worldPoints)
                screenPoints.Add(camera.worldToScreenPointEx(worldPt));
        }

        public static Vector2 worldToScreenPointEx(this Camera camera, Vector3 worldPt)
        {
            Vector2 screenPt    = HandleUtility.WorldToGUIPoint(worldPt);
            screenPt.y          = camera.pixelRect.height - screenPt.y;

            return screenPt;
        }

        public static Ray screenPointToRay(this Camera camera, Vector3 screenPt)
        {
            return HandleUtility.GUIPointToWorldRay(screenPt);
        }

        public static Vector3 screenPointToWorldPoint(this Camera camera, Vector3 screenPt)
        {
            return camera.ScreenToWorldPoint(screenPt);
        }

        public static Ray getCursorRay(this Camera camera)
        {
            return HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        }

        public static Matrix4x4 calcXYCircleOnNearPlaneTransform(this Camera camera, Vector2 circleCenter, float circleRadius, float offsetFromNearPlane)
        {
            Vector3 screenPos           = new Vector3(circleCenter.x, circleCenter.y, camera.nearClipPlane + offsetFromNearPlane);
            Vector3 circlePos           = camera.screenPointToWorldPoint(screenPos);
            screenPos.x                 += circleRadius;
            Vector3 circleRightExtent   = camera.screenPointToWorldPoint(screenPos);
            return Matrix4x4.TRS(circlePos, camera.transform.rotation, Vector3.one * (circleRightExtent - circlePos).magnitude);
        }

        public static Vector3 calcSphereCenterInFrontOfCamera(this Camera camera, Sphere sphere)
        {
            float distance = camera.calcFrustumDistance(sphere.radius);
            if (distance < camera.nearClipPlane + sphere.radius)
                distance += (camera.nearClipPlane + sphere.radius);

            return camera.transform.position + camera.transform.forward * distance;
        }

        public static float calcFrustumDistance(this Camera camera, float frustumHeight)
        {
            return (frustumHeight * 0.5f) / Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        }

        public static float calcFOV(this Camera camera, float frustumHeight, float distance)
        {
            return 2.0f * Mathf.Atan2(frustumHeight * 0.5f, distance) * Mathf.Rad2Deg;
        }

        public static float calcFrustumWidth(this Camera camera, float distFromCamera)
        {
            return Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5f) * distFromCamera * 2.0f * camera.aspect;
        }

        public static float calcFrustumHeight(this Camera camera, float distFromCamera)
        {
            return Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5f) * distFromCamera * 2.0f;
        }

        public static AABB calcVolumeAABB(this Camera camera, float farPlaneScale)
        {
            if (camera.orthographic) return camera.calcOrthoAABB(farPlaneScale);
            return camera.calcFrustumAABB(farPlaneScale);
        }

        public static OBB calcVolumeOBB(this Camera camera, float farPlaneScale)
        {
            if (camera.orthographic) return camera.calcOrthoOBB(farPlaneScale);
            return camera.calcFrustumOBB(farPlaneScale);
        }

        public static OBB calcFrustumOBB(this Camera camera, float farPlaneScale)
        {
            float farPlane          = camera.farClipPlane * farPlaneScale;
            float frustumWidth      = camera.calcFrustumWidth(farPlane);
            float frustumHeight     = camera.calcFrustumHeight(farPlane);

            Transform camTransform  = camera.transform;
            Vector3 camPos          = camTransform.position;
            Vector3 camLook         = camTransform.forward;

            OBB obb                 = new OBB();
            obb.center              = camPos + camLook * (camera.nearClipPlane + (farPlane - camera.nearClipPlane) * 0.5f);
            obb.size                = new Vector3(frustumWidth, frustumHeight, (farPlane - camera.nearClipPlane));

            return obb;
        }

        public static OBB calcOrthoOBB(this Camera camera, float farPlaneScale)
        {
            float farPlane          = camera.farClipPlane * farPlaneScale;
            float orthoHeight       = camera.orthographicSize * 2.0f;
            float orthoWidth        = orthoHeight * camera.aspect;

            Transform camTransform  = camera.transform;
            Vector3 camPos          = camTransform.position;
            Vector3 camLook         = camTransform.forward;

            OBB obb                 = new OBB();
            obb.center              = camPos + camLook * (camera.nearClipPlane + (farPlane - camera.nearClipPlane) * 0.5f);
            obb.size                = new Vector3(orthoWidth, orthoHeight, (farPlane - camera.nearClipPlane));
            return obb;
        }

        public static AABB calcFrustumAABB(this Camera camera, float farPlaneScale)
        {
            float frustumWidth      = camera.calcFrustumWidth(camera.farClipPlane);
            float frustumHeight     = camera.calcFrustumHeight(camera.farClipPlane);

            Transform camTransform  = camera.transform;
            Vector3 camPos          = camTransform.position;
            Vector3 camRight        = camTransform.right;
            Vector3 camUp           = camTransform.up;
            Vector3 camLook         = camTransform.forward;
            Vector3 midFar          = camPos + camLook * camera.farClipPlane * farPlaneScale;

            return new AABB(new Vector3[]
            {
                camPos, 
                midFar + camRight * frustumWidth * 0.5f,
                midFar - camUp * frustumHeight * 0.5f,
                midFar - camRight * frustumWidth * 0.5f,
                midFar + camUp * frustumHeight * 0.5f
            });
        }

        public static AABB calcOrthoAABB(this Camera camera, float farPlaneScale)
        {
            float orthoHeight       = camera.orthographicSize * 2.0f;
            float orthoWidth        = orthoHeight * camera.aspect;

            Transform camTransform  = camera.transform;
            Vector3 camPos          = camTransform.position;
            Vector3 camRight        = camTransform.right;
            Vector3 camUp           = camTransform.up;
            Vector3 camLook         = camTransform.forward;

            Vector3 midNear         = camPos + camLook * camera.nearClipPlane;
            Vector3 nearTopLeft     = midNear - camRight * orthoWidth * 0.5f + camUp * orthoHeight * 0.5f;
            Vector3 nearTopRight    = nearTopLeft + camRight * orthoWidth;
            Vector3 nearBottomRight = nearTopRight - camUp * orthoHeight;
            Vector3 nearBottomLeft  = nearBottomRight - camRight * orthoWidth;

            Vector3 midFar          = camPos + camLook * camera.farClipPlane * farPlaneScale;
            Vector3 farTopLeft      = midFar - camRight * orthoWidth * 0.5f + camUp * orthoHeight * 0.5f;
            Vector3 farTopRight     = farTopLeft + camRight * orthoWidth;
            Vector3 farBottomRight  = farTopRight - camUp * orthoHeight;
            Vector3 farBottomLeft   = farBottomRight - camRight * orthoWidth;

            return new AABB(new Vector3[] { nearTopLeft, nearTopRight, nearBottomRight, nearBottomLeft,
                                            farTopLeft, farTopRight, farBottomRight, farBottomLeft});
        }

        public static bool isPointFacingCamera(this Camera camera, Vector3 point, Vector3 pointNormal)
        {
            Vector3 lookRay = point - camera.transform.position;
            if (camera.orthographic) lookRay = camera.transform.forward;
            return Vector3.Dot(lookRay, pointNormal) < 0.0f;
        }

        public static bool isPointBehindNearPlane(this Camera camera, Vector3 point)
        {
            Plane plane = new Plane(camera.transform.forward, camera.transform.position + camera.transform.forward * camera.nearClipPlane);
            return plane.GetDistanceToPoint(point) < 0.0f;
        }

        public static Rect worldPointToScreenRect(this Camera camera, Vector3 point, Vector2 rectSize)
        {
            var screenCenter = camera.worldToScreenPointEx(point);
            return RectEx.create(screenCenter, rectSize);
        }
    }
}
#endif
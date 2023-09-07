#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public static class GLEx
    {
        private static List<Vector2> _rectCorners = new List<Vector2>();

        public static void drawQuads2D(List<Vector2> quadPoints, Camera camera)
        {
            int numQuads = quadPoints.Count / 4;
            if (numQuads < 1) return;

            GL.PushMatrix();
            GL.LoadOrtho();

            GL.Begin(GL.QUADS);
            for (int quadIndex = 0; quadIndex < numQuads; ++quadIndex)
            {
                int basePtIndex = quadIndex * 4;
                GL.Vertex(camera.ScreenToViewportPoint(quadPoints[basePtIndex]));
                GL.Vertex(camera.ScreenToViewportPoint(quadPoints[basePtIndex + 1]));
                GL.Vertex(camera.ScreenToViewportPoint(quadPoints[basePtIndex + 2]));
                GL.Vertex(camera.ScreenToViewportPoint(quadPoints[basePtIndex + 3]));
            }

            GL.End();
            GL.PopMatrix();
        }

        public static void drawLineLoop2D(List<Vector2> linePoints, Camera camera)
        {
            if (linePoints.Count < 2) return;

            GL.PushMatrix();
            GL.LoadOrtho();

            GL.Begin(GL.LINES);
            for (int pointIndex = 0; pointIndex < linePoints.Count; ++pointIndex)
            {
                Vector3 firstPoint  = linePoints[pointIndex];
                Vector3 secondPoint = linePoints[(pointIndex + 1) % linePoints.Count];

                firstPoint          = camera.ScreenToViewportPoint(firstPoint);
                secondPoint         = camera.ScreenToViewportPoint(secondPoint);

                GL.Vertex(new Vector3(firstPoint.x, firstPoint.y, 0.0f));
                GL.Vertex(new Vector3(secondPoint.x, secondPoint.y, 0.0f));
            }

            GL.End();
            GL.PopMatrix();
        }

        public static void drawLineLoop2D(List<Vector2> linePoints, Vector2 translation, Vector2 scale, Camera camera)
        {
            if (linePoints.Count < 2) return;

            GL.PushMatrix();
            GL.LoadOrtho();

            GL.Begin(GL.LINES);
            for (int pointIndex = 0; pointIndex < linePoints.Count; ++pointIndex)
            {
                Vector3 firstPoint  = Vector2.Scale(linePoints[pointIndex], scale) + translation;
                Vector3 secondPoint = Vector2.Scale(linePoints[(pointIndex + 1) % linePoints.Count], scale) + translation;

                firstPoint          = camera.ScreenToViewportPoint(firstPoint);
                secondPoint         = camera.ScreenToViewportPoint(secondPoint);

                GL.Vertex(new Vector3(firstPoint.x, firstPoint.y, 0.0f));
                GL.Vertex(new Vector3(secondPoint.x, secondPoint.y, 0.0f));
            }

            GL.End();
            GL.PopMatrix();
        }

        public static void drawLines2D(List<Vector2> linePoints, Camera camera)
        {
            if (linePoints.Count < 2) return;

            GL.PushMatrix();
            GL.LoadOrtho();

            GL.Begin(GL.LINES);
            for (int pointIndex = 0; pointIndex < linePoints.Count - 1; ++pointIndex)
            {
                Vector3 firstPoint  = linePoints[pointIndex];
                Vector3 secondPoint = linePoints[pointIndex + 1];

                firstPoint          = camera.ScreenToViewportPoint(firstPoint);
                secondPoint         = camera.ScreenToViewportPoint(secondPoint);

                GL.Vertex(new Vector3(firstPoint.x, firstPoint.y, 0.0f));
                GL.Vertex(new Vector3(secondPoint.x, secondPoint.y, 0.0f));
            }

            GL.End();
            GL.PopMatrix();
        }

        public static void drawLines2D(List<Vector2> linePoints, Vector2 translation, Vector2 scale, Camera camera)
        {
            if (linePoints.Count < 2) return;

            GL.PushMatrix();
            GL.LoadOrtho();

            GL.Begin(GL.LINES);
            for (int pointIndex = 0; pointIndex < linePoints.Count - 1; ++pointIndex)
            {
                Vector3 firstPoint  = Vector2.Scale(linePoints[pointIndex], scale) + translation;
                Vector3 secondPoint = Vector2.Scale(linePoints[pointIndex + 1], scale) + translation;

                firstPoint          = camera.ScreenToViewportPoint(firstPoint);
                secondPoint         = camera.ScreenToViewportPoint(secondPoint);

                GL.Vertex(new Vector3(firstPoint.x, firstPoint.y, 0.0f));
                GL.Vertex(new Vector3(secondPoint.x, secondPoint.y, 0.0f));
            }

            GL.End();
            GL.PopMatrix();
        }

        public static void drawLine2D(Vector2 startPoint, Vector2 endPoint, Camera camera)
        {
            GL.PushMatrix();
            GL.LoadOrtho();

            GL.Begin(GL.LINES);
            GL.Vertex(camera.ScreenToViewportPoint(startPoint));
            GL.Vertex(camera.ScreenToViewportPoint(endPoint));
            GL.End();

            GL.PopMatrix();
        }

        public static void drawLine3D(Vector3 startPoint, Vector3 endPoint)
        {
            GL.Begin(GL.LINES);
            GL.Vertex(startPoint);
            GL.Vertex(endPoint);
            GL.End();
        }

        public static void drawLines3D(List<Vector3> linePoints)
        {
            if (linePoints.Count < 2) return;

            GL.Begin(GL.LINES);
            for (int pointIndex = 0; pointIndex < linePoints.Count - 1; ++pointIndex)
            {
                Vector3 firstPoint  = linePoints[pointIndex];
                Vector3 secondPoint = linePoints[pointIndex + 1];

                GL.Vertex(firstPoint);
                GL.Vertex(secondPoint);
            }
            GL.End();
        }

        public static void drawLineLoop3D(List<Vector3> linePoints)
        {
            if (linePoints.Count < 2) return;

            GL.Begin(GL.LINES);
            for (int pointIndex = 0; pointIndex < linePoints.Count; ++pointIndex)
            {
                Vector3 firstPoint  = linePoints[pointIndex];
                Vector3 secondPoint = linePoints[(pointIndex + 1) % linePoints.Count];

                GL.Vertex(firstPoint);
                GL.Vertex(secondPoint);
            }

            GL.End();
        }

        public static void drawLineStrip3D(List<Vector3> linePoints)
        {
            if (linePoints.Count < 2) return;

            GL.Begin(GL.LINES);
            for (int pointIndex = 0; pointIndex < linePoints.Count - 1; ++pointIndex)
            {
                GL.Vertex(linePoints[pointIndex]);
                GL.Vertex(linePoints[pointIndex + 1]);
            }

            GL.End();
        }

        public static void drawLineLoop3D(List<Vector3> linePoints, Vector3 pointOffset)
        {
            if (linePoints.Count < 2) return;

            GL.Begin(GL.LINES);
            for (int pointIndex = 0; pointIndex < linePoints.Count; ++pointIndex)
            {
                Vector3 firstPoint  = linePoints[pointIndex] + pointOffset;
                Vector3 secondPoint = linePoints[(pointIndex + 1) % linePoints.Count] + pointOffset;

                GL.Vertex(firstPoint);
                GL.Vertex(secondPoint);
            }

            GL.End();
        }

        public static void drawLinePairs3D(List<Vector3> pairPoints)
        {
            if (pairPoints.Count < 2 || pairPoints.Count % 2 != 0) return;

            GL.Begin(GL.LINES);
            for (int pointIndex = 0; pointIndex < pairPoints.Count; pointIndex += 2)
            {
                Vector3 firstPoint  = pairPoints[pointIndex];
                Vector3 secondPoint = pairPoints[(pointIndex + 1)];

                GL.Vertex(firstPoint);
                GL.Vertex(secondPoint);
            }

            GL.End();
        }

        public static void drawRectBorder2D(Rect rect, Camera camera)
        {
            rect.calcCorners(_rectCorners);
            drawLineLoop2D(_rectCorners, camera);
        }

        public static void drawRect2D(Rect rect, Camera camera)
        {
            GL.PushMatrix();
            GL.LoadOrtho();

            GL.Begin(GL.QUADS);

            rect.calcCorners(_rectCorners);
            _rectCorners[0] = camera.ScreenToViewportPoint(_rectCorners[0]);
            _rectCorners[1] = camera.ScreenToViewportPoint(_rectCorners[1]);
            _rectCorners[2] = camera.ScreenToViewportPoint(_rectCorners[2]);
            _rectCorners[3] = camera.ScreenToViewportPoint(_rectCorners[3]);

            GL.Vertex(_rectCorners[0]);
            GL.Vertex(_rectCorners[1]);
            GL.Vertex(_rectCorners[2]);
            GL.Vertex(_rectCorners[3]);

            GL.End();
            GL.PopMatrix();
        }

        public static void drawTriangleFan2D(Vector2 origin, List<Vector2> points, Vector2 translation, Vector2 scale, Camera camera)
        {
            int numTriangles = points.Count - 1;
            if (numTriangles < 1) return;

            GL.PushMatrix();
            GL.LoadOrtho();

            GL.Begin(GL.TRIANGLES);

            origin = camera.ScreenToViewportPoint(Vector2.Scale(origin, scale) + translation);
            for (int triangleIndex = 0; triangleIndex < numTriangles; ++triangleIndex)
            {
                GL.Vertex(origin);
                GL.Vertex(camera.ScreenToViewportPoint(Vector2.Scale(points[triangleIndex], scale) + translation));
                GL.Vertex(camera.ScreenToViewportPoint(Vector2.Scale(points[triangleIndex + 1], scale) + translation));
            }

            GL.End();
            GL.PopMatrix();
        }

        public static void drawTriangleFan2D(Vector2 origin, List<Vector2> points, Camera camera)
        {
            int numTriangles = points.Count - 1;
            if (numTriangles < 1) return;

            GL.PushMatrix();
            GL.LoadOrtho();

            GL.Begin(GL.TRIANGLES);

            origin = camera.ScreenToViewportPoint(origin);
            for (int triangleIndex = 0; triangleIndex < numTriangles; ++triangleIndex)
            {
                GL.Vertex(origin);
                GL.Vertex(camera.ScreenToViewportPoint(points[triangleIndex]));
                GL.Vertex(camera.ScreenToViewportPoint(points[triangleIndex + 1]));
            }

            GL.End();
            GL.PopMatrix();
        }

        public static void drawTriangleFan3D(Vector3 origin, List<Vector3> points, Vector3 translation, Vector3 scale)
        {
            int numTriangles = points.Count - 1;
            if (numTriangles < 1) return;

            GL.Begin(GL.TRIANGLES);

            origin = Vector3.Scale(origin, scale) + translation;
            for (int triangleIndex = 0; triangleIndex < numTriangles; ++triangleIndex)
            {
                GL.Vertex(origin);
                GL.Vertex(Vector3.Scale(points[triangleIndex], scale) + translation);
                GL.Vertex(Vector3.Scale(points[triangleIndex + 1], scale) + translation);
            }

            GL.End();
        }

        public static void drawTriangleFan3D(Vector3 origin, List<Vector3> points)
        {
            int numTriangles = points.Count - 1;
            if (numTriangles < 1) return;

            GL.Begin(GL.TRIANGLES);

            for (int triangleIndex = 0; triangleIndex < numTriangles; ++triangleIndex)
            {
                GL.Vertex(origin);
                GL.Vertex(points[triangleIndex]);
                GL.Vertex(points[triangleIndex + 1]);
            }

            GL.End();
        }
    }
}
#endif
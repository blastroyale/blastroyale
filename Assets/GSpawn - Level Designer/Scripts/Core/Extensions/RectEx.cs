#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public enum RectCornerPoint
    {
        TopLeft = 0,
        TopRight,
        BottomRight,
        BottomLeft
    }

    public static class RectEx
    {
        public static Rect createBelowCenterHrz(this Rect rect, Rect other)
        {
            float centerX = other.center.x;
            float centerY = other.center.y - other.size.y * 0.5f - rect.size.y * 0.5f;

            return create(new Vector2(centerX, centerY), rect.size);
        }

        public static Rect create(Vector2 center, Vector2 size)
        {
            return new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
        }

        public static Rect create(IEnumerable<Vector2> points)
        {
            Vector2 minPt = Vector2Ex.create(float.MaxValue), maxPt = Vector2Ex.create(float.MinValue);
            foreach (var pt in points)
            {
                minPt = Vector2.Min(pt, minPt);
                maxPt = Vector2.Max(pt, maxPt);
            }

            return Rect.MinMaxRect(minPt.x, minPt.y, maxPt.x, maxPt.y);
        }

        public static Rect create(Texture2D texture2D)
        {
            return new Rect(0.0f, 0.0f, texture2D.width, texture2D.height);
        }

        public static Rect createInflated(this Rect rect, float inflateAmount)
        {
            float sizeAdd   = inflateAmount;
            float newSizeX  = rect.size.x >= 0.0f ? rect.size.x + sizeAdd : rect.size.x - sizeAdd;
            float newSizeY  = rect.size.y >= 0.0f ? rect.size.y + sizeAdd : rect.size.y - sizeAdd;

            return create(rect.center, new Vector2(newSizeX, newSizeY));
        }

        public static Rect createInvertedYCoords(this Rect rect, Camera camera)
        {
            Rect newRect        = rect;
            newRect.yMin        = camera.pixelHeight - newRect.yMin;
            newRect.yMax        = camera.pixelHeight - newRect.yMax;

            if (newRect.yMax < newRect.yMin)
            {
                float temp      = newRect.yMax;
                newRect.yMax    = newRect.yMin;
                newRect.yMin    = temp;
            }

            if (newRect.xMax < newRect.xMin)
            {
                float temp      = newRect.xMax;
                newRect.xMax    = newRect.xMin;
                newRect.xMin    = temp;
            }

            return newRect;
        }

        public static void calcCorners(this Rect rect, List<Vector2> corners)
        {
            corners.Clear();
            corners.Add(new Vector2(rect.xMin, rect.yMax)); // TopLeft
            corners.Add(new Vector2(rect.xMax, rect.yMax)); // TopRight
            corners.Add(new Vector2(rect.xMax, rect.yMin)); // BottomRight
            corners.Add(new Vector2(rect.xMin, rect.yMin)); // BottomLeft
        }

        public static bool containsPoints(this Rect rect, IEnumerable<Vector2> points)
        {
            foreach(var pt in points)
                if (!rect.Contains(pt, true)) return false;

            return true;
        }
        
        public static float absArea(this Rect rect)
        {
            return Mathf.Abs(rect.width * rect.height);
        }
    }
}
#endif
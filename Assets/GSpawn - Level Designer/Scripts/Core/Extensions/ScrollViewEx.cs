#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace GSpawn
{
    public static class ScrollViewEx
    {
        public static T findClosestOutOfBounds<T>(this ScrollView scrollView, List<T> items) where T : VisualElement
        {
            T closestItem       = null;
            float minDist       = float.MaxValue;
            Rect scrollViewRect = scrollView.localBound;
            var itemCorners     = new List<Vector2>();

            foreach(var item in items)
            {
                Rect itemRect = item.localBound;
                itemRect.calcCorners(itemCorners);

                if (!scrollViewRect.containsPoints(itemCorners))
                {
                    float d = (itemRect.center - scrollViewRect.center).magnitude;
                    if (d < minDist)
                    {
                        minDist = d;
                        closestItem = item;
                    }
                }
            }

            return closestItem;
        }
    }
}
#endif
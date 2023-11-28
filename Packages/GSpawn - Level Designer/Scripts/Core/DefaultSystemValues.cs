#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public static class DefaultSystemValues
    {
        public static Color     xAxisColor              { get { return ColorEx.create(219, 62, 29, 237); } }
        public static Color     yAxisColor              { get { return ColorEx.create(154, 243, 72, 237); } }
        public static Color     zAxisColor              { get { return ColorEx.create(58, 122, 248, 237); } }
        public static float     tickSize                { get { return 0.04f; } }
        public static Color     parentHighlightColor    { get { return ColorEx.create(255, 102, 0, 255); } }
        public static Color     childHighlightColor     { get { return ColorEx.create(94, 119, 155, 255); } }
        public static float     minGridCellSize         { get { return 0.01f; } }

    }
}
#endif
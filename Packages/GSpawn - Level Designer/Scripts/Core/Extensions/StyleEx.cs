#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public static class StyleEx
    {
        public static void setBackgroundImage(this IStyle style, Texture2D image, bool syncSize)
        {
            style.backgroundImage           = image;
            if (syncSize)
            {
                style.width                 = image.width;
                style.height                = image.height;
            }
        }

        public static void setBorderWidth(this IStyle style, float borderWidth)
        {
            style.borderLeftWidth           = borderWidth;
            style.borderRightWidth          = borderWidth;
            style.borderTopWidth            = borderWidth;
            style.borderBottomWidth         = borderWidth;
        }

        public static void setBorderRadius(this IStyle style, float borderRadius)
        {
            style.borderBottomLeftRadius    = borderRadius;
            style.borderBottomRightRadius   = borderRadius;
            style.borderTopLeftRadius       = borderRadius;
            style.borderTopRightRadius      = borderRadius;
        }

        public static void setBorderColor(this IStyle style, Color color)
        {
            style.borderLeftColor           = color;
            style.borderTopColor            = color;
            style.borderRightColor          = color;
            style.borderBottomColor         = color;
        }

        public static void setMargins(this IStyle style, float margin)
        {
            style.marginLeft                = margin;
            style.marginRight               = margin;
            style.marginTop                 = margin;
            style.marginBottom              = margin;
        }
    }
}
#endif
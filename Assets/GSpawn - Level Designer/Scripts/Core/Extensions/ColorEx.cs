#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public static class ColorEx
    {
        public static Color coral       { get { return create(255, 127, 80, 255); } }
        public static Color orange      { get { return create(255, 165, 0, 255); } }
        public static Color darkOrange  { get { return create(255, 140, 0, 255); } }

        public static Color create(byte r, byte g, byte b, byte a)
        {
            return new Color(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f);
        }

        public static Color create(byte r, byte g, byte b)
        {
            return new Color(r / 255.0f, g / 255.0f, b / 255.0f, 1.0f);
        }

        public static Color createNewAlpha(this Color color, float newAlpha)
        {
            return new Color(color.r, color.g, color.b, newAlpha);
        }

        public static Color[] createFilledColorArray(int arrayLength, Color fillValue)
        {
            Color[] colorArray = new Color[arrayLength];
            for (int colorIndex = 0; colorIndex < arrayLength; ++colorIndex)
                colorArray[colorIndex] = fillValue;

            return colorArray;
        }

        public static Color getGrayscale(float val, float alpha)
        {
            return new Color(val, val, val, alpha);
        }
    }
}
#endif
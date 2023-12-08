#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public static class MathEx
    {
        public static float roundCorrectError(float value, float eps)
        {
            // Round to whole
            float iVal  = Mathf.Floor(value);
            if (Mathf.Abs(value - iVal) < eps) return iVal;

            float up    = Mathf.Ceil(value);
            if (Mathf.Abs(value - up) < eps) return up;

            // Round to 0.75
            float sign  = Mathf.Sign(value);
            iVal        = (float)((int)value);
            float frac  = (value - iVal);
            if (Mathf.Abs(frac - 0.75f * sign) < eps) return iVal + 0.75f * sign;
      
            // Round to 0.5
            if (Mathf.Abs(frac - 0.5f * sign) < eps) return iVal + 0.5f * sign;
       
            // Round to 0.25
            if (Mathf.Abs(frac - 0.25f * sign) < eps) return iVal + 0.25f * sign;
            
            return value;
        }

        public static float randomAngle()
        {
            return UnityEngine.Random.Range(0.0f, 360.0f);
        }

        public static int getNumDigits(int number)
        {
            return number == 0 ? 1 : Mathf.FloorToInt(Mathf.Log10(Mathf.Abs(number)) + 1);
        }

        public static float safeAcos(float cosine)
        {
            cosine = Mathf.Max(-1.0f, Mathf.Min(1.0f, cosine));
            return Mathf.Acos(cosine);
        }

        // Note: It's simply faster than Mathf.Abs.
        public static float fastAbs(float val)
        {
            return val < 0.0f ? -val : val;
        }

        public static bool solveQuadratic(float a, float b, float c, out float t1, out float t2)
        {
            t1 = t2 = 0.0f;

            float delta = b * b - 4.0f * a * c;
            if (delta < 0.0f) return false;

            float _2TimesA = 2.0f * a;
            if (_2TimesA == 0.0f) return false;

            if (delta == 0.0f)
            {
                t1 = t2 = -b / _2TimesA;
                return true;
            }
            else
            {
                float sqrtDelta = Mathf.Sqrt(delta);

                t1 = (-b + sqrtDelta) / _2TimesA;
                t2 = (-b - sqrtDelta) / _2TimesA;

                if (t1 > t2)
                {
                    float tSwap = t1;
                    t1 = t2;
                    t2 = tSwap;
                }

                return true;
            }
        }
    }
}
#endif
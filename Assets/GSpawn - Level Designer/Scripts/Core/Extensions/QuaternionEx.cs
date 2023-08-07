#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public static class QuaternionEx
    {
        public static Quaternion create(float angle, Axis axis)
        {
            Vector3 rotAxis                     = Vector3.right;
            if (axis == Axis.Y) rotAxis         = Vector3.up;
            else if (axis == Axis.Z) rotAxis    = Vector3.forward;

            return Quaternion.AngleAxis(angle, rotAxis);
        }

        public static Quaternion create(Matrix4x4 mtx)
        {
            return Quaternion.LookRotation(mtx.getNormalizedAxis(2), mtx.getNormalizedAxis(1));
        }

        public static Quaternion create(Vector3 from, Vector3 to, Vector3 perp180)
        {
            from    = from.normalized;
            to      = to.normalized;

            float dot = Vector3.Dot(from, to);
            if (1.0f - dot < 1e-5f) return Quaternion.identity;
            if (1.0f + dot < 1e-5f) return Quaternion.AngleAxis(180.0f, perp180);

            float angle = MathEx.safeAcos(dot) * Mathf.Rad2Deg;
            Vector3 rotationAxis = Vector3.Cross(from, to).normalized;
            return Quaternion.AngleAxis(angle, rotationAxis);
        }

        public static Quaternion createRelativeRotation(Quaternion from, Quaternion to)
        {
            return Quaternion.Inverse(from) * to;
        }

        public static Quaternion roundCorrectError(this Quaternion val, float eps)
        {
            Vector3 euler = val.eulerAngles;
            return Quaternion.Euler(euler.roundCorrectError(eps)).normalized;
        }
    }
}
#endif
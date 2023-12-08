#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public static class GizmosEx
    {
        private static Stack<Color>         _colorStack     = new Stack<Color>();
        private static Stack<Matrix4x4>     _matrixStack    = new Stack<Matrix4x4>();

        public static void saveColor()
        {
            _colorStack.Push(Gizmos.color);
        }

        public static void restoreColor()
        {
            if (_colorStack.Count != 0) Gizmos.color = _colorStack.Pop();
        }

        public static void saveMatrix()
        {
            _matrixStack.Push(Gizmos.matrix);
        }

        public static void restoreMatrix()
        {
            if (_matrixStack.Count != 0) Gizmos.matrix = _matrixStack.Pop();
        }
    }
}
#endif
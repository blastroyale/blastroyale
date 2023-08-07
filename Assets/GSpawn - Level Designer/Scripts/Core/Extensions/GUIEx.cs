#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public static class GUIEx
    {
        static Stack<Color>         _colorStack         = new Stack<Color>();
        static Stack<Color>         _contentColorStack  = new Stack<Color>();
        static Stack<Matrix4x4>     _matrixStack        = new Stack<Matrix4x4>();

        public static void saveColor()
        {
            _colorStack.Push(GUI.color);
        }

        public static void restoreColor()
        {
            if (_colorStack.Count != 0) GUI.color = _colorStack.Pop();
        }

        public static void saveContentColor()
        {
            _contentColorStack.Push(GUI.contentColor);
        }

        public static void restoreContentColor()
        {
            if (_contentColorStack.Count != 0) GUI.contentColor = _contentColorStack.Pop();
        }

        public static void saveMatrix()
        {
            _matrixStack.Push(Gizmos.matrix);
        }

        public static void restoreMatrix()
        {
            if (_matrixStack.Count != 0) GUI.matrix = _matrixStack.Pop();
        }
    }
}
#endif
#if UNITY_EDITOR
using UnityEditor;

namespace GSpawn
{
    public class ObjectUniversalGizmo : ObjectTransformGizmo
    {
        protected override Channels draw()
        {
            _newPosition    = position;
            _newRotation    = rotation;
            _newScale       = scale;

            EditorGUI.BeginChangeCheck();
            Handles.TransformHandle(ref _newPosition, ref _newRotation, ref _newScale);
            if (EditorGUI.EndChangeCheck())
            {
                if (_newPosition != position)   return Channels.Position;
                else if (_newScale != scale)    return Channels.Scale;
                else return Channels.Rotation;
            }

            return Channels.None;
        }
    }
}
#endif
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

namespace GSpawn
{
    [Serializable]
    public class ObjectScaleGizmo : ObjectTransformGizmo
    {
        public override void refreshRotation()
        {
            if (calcNumTargetParents() == 1 && _pivotObject != null) rotation = _pivotObject.transform.rotation;
            else rotation = Quaternion.identity;
        }

        protected override Channels draw()
        {
            EditorGUI.BeginChangeCheck();
            _newScale = Handles.ScaleHandle(scale, position, rotation, HandleUtility.GetHandleSize(position));
            if (EditorGUI.EndChangeCheck()) return Channels.Scale;

            return Channels.None;
        }
    }
}
#endif
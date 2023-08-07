#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

namespace GSpawn
{
    public static class SerializedPropertyEx
    {
        public static void addElement(this SerializedProperty list, object element)
        {
            list.arraySize += 1;
            list.GetArrayElementAtIndex(list.arraySize - 1).setValue(element);
        }

        // Source: https://forum.unity.com/threads/how-to-correct-the-size-of-a-serialized-array-when-removing-objects-via-editor-script.479859/
        public static void removeElementAtIndex(this SerializedProperty list, int index)
        {
            if (!list.isArray)
                throw new ArgumentException("Property is not an array");

            if (index < 0 || index >= list.arraySize)
                throw new IndexOutOfRangeException();

            list.GetArrayElementAtIndex(index).setValue(null);
            list.DeleteArrayElementAtIndex(index);
            list.serializedObject.ApplyModifiedProperties();
        }

        // Source: https://forum.unity.com/threads/how-to-correct-the-size-of-a-serialized-array-when-removing-objects-via-editor-script.479859/
        public static void setValue(this SerializedProperty property, object value)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Generic:

                    // ?
                    break;

                case SerializedPropertyType.Integer:

                    property.intValue = Convert.ToInt32(value);
                    break;

                case SerializedPropertyType.Boolean:

                    property.boolValue = Convert.ToBoolean(value);
                    break;

                case SerializedPropertyType.Float:

                    property.floatValue = Convert.ToSingle(value);
                    break;

                case SerializedPropertyType.String:

                    property.stringValue = value as string;
                    break;

                case SerializedPropertyType.Color:

                    property.colorValue = (value == null) ? new Color() : (Color)value;
                    break;

                case SerializedPropertyType.ObjectReference:

                    property.objectReferenceValue = value as UnityEngine.Object;
                    break;

                case SerializedPropertyType.LayerMask:

                    property.intValue = (value is LayerMask) ? ((LayerMask)value).value : Convert.ToInt32(value);
                    break;

                case SerializedPropertyType.Enum:

                    property.enumValueIndex = (value == null) ? -1 : Convert.ToInt32(value);
                    break;

                case SerializedPropertyType.Vector2:

                    property.vector2Value = (value == null) ? Vector2.zero : (Vector2)value;
                    break;

                case SerializedPropertyType.Vector3:

                    property.vector3Value = (value == null) ? Vector3.zero : (Vector3)value;
                    break;

                case SerializedPropertyType.Vector4:

                    property.vector4Value = (value == null) ? Vector4.zero : (Vector4)value;
                    break;

                case SerializedPropertyType.Rect:

                    property.rectValue = (value == null) ? new Rect() : (Rect)value;
                    break;

                case SerializedPropertyType.ArraySize:

                    property.intValue = Convert.ToInt32(value);
                    break;

                case SerializedPropertyType.Character:

                    property.intValue = Convert.ToInt32(value);
                    break;

                case SerializedPropertyType.AnimationCurve:

                    property.animationCurveValue = value as AnimationCurve;
                    break;

                case SerializedPropertyType.Bounds:

                    property.boundsValue = (value == null) ? new Bounds() : (Bounds)value;
                    break;

                case SerializedPropertyType.Gradient:

                    // ?
                    break;

                case SerializedPropertyType.Quaternion:

                    property.quaternionValue = (value == null) ? Quaternion.identity : (Quaternion)value;
                    break;

                case SerializedPropertyType.ExposedReference:

                    property.exposedReferenceValue = value as UnityEngine.Object;
                    break;

                case SerializedPropertyType.FixedBufferSize:

                    // Read-only.
                    break;

                case SerializedPropertyType.Vector2Int:

                    property.vector2IntValue = (value == null) ? Vector2Int.zero : (Vector2Int)value;
                    break;

                case SerializedPropertyType.Vector3Int:

                    property.vector3IntValue = (value == null) ? Vector3Int.zero : (Vector3Int)value;
                    break;

                case SerializedPropertyType.RectInt:

                    property.rectIntValue = (value == null) ? new RectInt() : (RectInt)value;
                    break;

                case SerializedPropertyType.BoundsInt:

                    property.boundsIntValue = value == null ? new BoundsInt() : (BoundsInt)value;
                    break;

                case SerializedPropertyType.ManagedReference:

                    property.managedReferenceValue = value;
                    break;

                case SerializedPropertyType.Hash128:

                    property.hash128Value = (value == null) ? new Hash128() : (Hash128)value;
                    break;
            }
        }
    }
}
#endif
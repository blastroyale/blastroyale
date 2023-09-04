#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
#if UNITY_2021
using UnityEditor.UIElements;
#endif

namespace GSpawn
{
    public static class FloatFieldEx
    {
        public static void bindMinValueProperty(this FloatField floatField, string propertyName, string minPropertyName, SerializedObject serializedObject)
        {
            var property    = serializedObject.FindProperty(propertyName);
            var minProperty = serializedObject.FindProperty(minPropertyName);
            floatField.RegisterValueChangedCallback(p =>
            {
                if (property.floatValue < minProperty.floatValue)
                {
                    minProperty.floatValue = property.floatValue;
                    serializedObject.ApplyModifiedProperties();
                }
            });
        }

        public static void bindMinValueProperty(this FloatField floatField, string propertyName, string minPropertyName, float minValue, SerializedObject serializedObject)
        {
            var property    = serializedObject.FindProperty(propertyName);
            var minProperty = serializedObject.FindProperty(minPropertyName);
            floatField.RegisterValueChangedCallback(p =>
            {
                property.floatValue = Mathf.Max(property.floatValue, minValue);
                if (property.floatValue < minProperty.floatValue)
                {
                    minProperty.floatValue = property.floatValue;
                    serializedObject.ApplyModifiedProperties();
                }
            });
        }

        public static void bindMaxValueProperty(this FloatField floatField, string propertyName, string maxPropertyName, SerializedObject serializedObject)
        {
            var property    = serializedObject.FindProperty(propertyName);
            var maxProperty = serializedObject.FindProperty(maxPropertyName);
            floatField.RegisterValueChangedCallback(p =>
             {
                 if (property.floatValue > maxProperty.floatValue)
                 {
                     maxProperty.floatValue = property.floatValue;
                     serializedObject.ApplyModifiedProperties();
                 }
             });
        }

        public static void bindMaxValueProperty(this FloatField floatField, string propertyName, string maxPropertyName, float minValue, SerializedObject serializedObject)
        {
            var property    = serializedObject.FindProperty(propertyName);
            var maxProperty = serializedObject.FindProperty(maxPropertyName);
            floatField.RegisterValueChangedCallback(p =>
            {
                property.floatValue = Mathf.Max(property.floatValue, minValue);
                if (property.floatValue > maxProperty.floatValue)
                {
                    maxProperty.floatValue = property.floatValue;
                    serializedObject.ApplyModifiedProperties();
                }
            });
        }
    }
}
#endif
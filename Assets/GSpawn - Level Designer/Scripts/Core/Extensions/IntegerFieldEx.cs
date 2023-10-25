#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
#if UNITY_2021
using UnityEditor.UIElements;
#endif

namespace GSpawn
{
    public static class IntegerFieldEx
    {
        public static void bindMinValueProperty(this IntegerField intField, string propertyName, string minPropertyName, SerializedObject serializedObject)
        {
            var property        = serializedObject.FindProperty(propertyName);
            var minProperty     = serializedObject.FindProperty(minPropertyName);
            intField.RegisterValueChangedCallback(p =>
            {
                if (property.intValue < minProperty.intValue)
                {
                    minProperty.intValue = property.intValue;
                    serializedObject.ApplyModifiedProperties();
                }
            });
        }

        public static void bindMaxValueProperty(this IntegerField intField, string propertyName, string maxPropertyName, SerializedObject serializedObject)
        {
            var property        = serializedObject.FindProperty(propertyName);
            var maxProperty     = serializedObject.FindProperty(maxPropertyName);
            intField.RegisterValueChangedCallback(p =>
            {
                if (property.intValue > maxProperty.intValue)
                {
                    maxProperty.intValue = property.intValue;
                    serializedObject.ApplyModifiedProperties();
                }
            });
        }
    }
}
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor
{
	/// <summary>
	/// Implement this property drawer with your own custom EnumSelectorPropertyDrawer implementation for the given
	/// enum of type <typeparamref name="T"/>
	/// 
	/// Ex:
	/// [CustomPropertyDrawer(typeof(EnumSelectorExample))]
	/// public class EnumSelectorExamplePropertyDrawer : EnumSelectorPropertyDrawer{EnumExample}
	/// {
	/// }
	/// </summary>
	public abstract class EnumSelectorPropertyDrawer<T> : PropertyDrawer 
		where T : Enum
	{
		private static readonly Dictionary<Type, GUIContent[]> _sortedEnums = new Dictionary<Type, GUIContent[]>();
	 
		private bool _errorFound;
	 
		/// <inheritdoc />
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			var enumType = typeof(T);
			var enumValues = GetSortedEnumConstants(enumType);
			var selectionProperty = property.FindPropertyRelative("_selection");
			var currentString = selectionProperty.stringValue;
			var currentIndex = string.IsNullOrWhiteSpace(currentString) ? 0 : Array.FindIndex(enumValues, s => s.text == currentString);
			
			if (currentIndex != -1)
			{
				selectionProperty.stringValue = enumValues[EditorGUI.Popup(position, label, currentIndex, enumValues)].text;
	 
				_errorFound = false;
			}
			else
			{
				// The string is not a valid enum constant, because it was renamed or removed
				if (!_errorFound)
				{
					var targetObject = selectionProperty.serializedObject.targetObject;
					
					Debug.LogError($"Invalid enum constant: {enumType.Name}.{currentString} in object {targetObject.name} of type: {targetObject.GetType().Name}");
					
					_errorFound = true;
				}
	 
				var color = GUI.contentColor;
				var finalArray = new [] { new GUIContent("Invalid: " + currentString) }.Concat(enumValues).ToArray();
	 
				GUI.contentColor = Color.red;
				var newSelection = EditorGUI.Popup(position, label, 0, finalArray);
				GUI.contentColor = color;
				
				if (newSelection > 0)
				{
					selectionProperty.stringValue = finalArray[newSelection].text;
				}
			}
	 
			EditorGUI.EndProperty();
		}
	 
		private GUIContent[] GetSortedEnumConstants(Type enumType)
		{
			if (!_sortedEnums.TryGetValue(enumType, out var content))
			{
				var values = Enum.GetNames(enumType);

				content = new GUIContent[values.Length];
				
				Array.Sort(values);

				for (var i = 0; i < values.Length; i++)
				{
					content[i] = new GUIContent(values[i]);
				}
				
				_sortedEnums.Add(enumType, content);
			}
			return content;
		}
	}
}
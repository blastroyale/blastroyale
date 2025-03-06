/*using System;
using FirstLight.FLogger;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UI.Attributes
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class SelectIconAttribute : Attribute
	{
	}

	[CustomPropertyDrawer(typeof(SelectIconAttribute))]
	public class SelectIconAttributePropertyDrawer : PropertyDrawer
	{
		static string ICONS_PATH = "Assets/Art/UI/Sprites/Elements/Icons";

		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			var a = new ObjectField();
			var row = new VisualElement {style = {flexDirection = FlexDirection.Row}};
			var textField = new TextField("My Text") {style = {flexGrow = 1}};
			row.Add(textField);
			return row;
		}

		[MenuItem("FLG/FODAAAAAAAAAAAAAAAAAAAAA")]
		public static void Fodaaaaaaaa()
		{
			var assets = AssetDatabase.FindAssets("t:Sprite", new[] {ICONS_PATH});
			foreach (var asset in assets)
			{
				Debug.Log(asset);
			}
		}
	}
}*/
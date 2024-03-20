using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Editor.EditorTools;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace FirstLight.Editor.Inspector
{
	[InitializeOnLoad]
	public class SpriteClassAttributeProcessor : OdinAttributeProcessor
	{
		public static ValueDropdownList<string> cachedClasses;


		static SpriteClassAttributeProcessor()
		{
			cachedClasses = new ValueDropdownList<string>();
			foreach (var kv in EditorShortcuts.GetAllGeneratedClassNames())
			{
				cachedClasses.Add(kv.Key, kv.Value);
			}
		}
		
		public override bool CanProcessSelfAttributes(InspectorProperty property)
		{
			return property.GetAttribute<SpriteClassAttribute>() != null;
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			attributes.Add(new ValueDropdownAttribute($"@{nameof(SpriteClassAttributeProcessor)}.{nameof(GetSprites)}()"));
			attributes.Add(new InfoBoxAttribute($"@{nameof(SpriteClassAttributeProcessor)}.GetClassName($property)"));
			base.ProcessSelfAttributes(property, attributes);
		}

		private static string GetClassName(InspectorProperty property)
		{
			return property.BaseValueEntry.WeakSmartValue.ToString();
		}

		private static IEnumerable GetSprites()
		{
			return cachedClasses;
		}
	}
	
}
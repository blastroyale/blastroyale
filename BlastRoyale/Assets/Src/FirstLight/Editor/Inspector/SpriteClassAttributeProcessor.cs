using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Editor.EditorTools;
using FirstLight.Game.Utils.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace FirstLight.Editor.Inspector
{
	public class SpriteClassAttributeProcessor : OdinAttributeProcessor
	{
		private static ValueDropdownList<string> _cachedClasses;

		private static ValueDropdownList<string> GetClasses()
		{
			if (_cachedClasses == null || _cachedClasses.Count == 0)
			{
				_cachedClasses = new ValueDropdownList<string>();
				foreach (var kv in EditorShortcuts.GetAllGeneratedClassNames())
				{
					_cachedClasses.Add(kv.Key, kv.Value);
				}
			}

			return _cachedClasses;
		}

		public override bool CanProcessSelfAttributes(InspectorProperty property)
		{
			return property.GetAttribute<SpriteClassAttribute>() != null;
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			attributes.Add(new ValueDropdownAttribute($"@{nameof(SpriteClassAttributeProcessor)}.{nameof(GetSprites)}()"));
			attributes.Add(new InfoBoxAttribute($"@{nameof(SpriteClassAttributeProcessor)}.{nameof(GetClassName)}($property)"));
			base.ProcessSelfAttributes(property, attributes);
		}

		private static string GetClassName(InspectorProperty property)
		{
			return property?.BaseValueEntry?.WeakSmartValue?.ToString() ?? "None";
		}

		private static IEnumerable GetSprites()
		{
			return GetClasses();
		}
	}
}
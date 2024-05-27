using System;
using System.Collections.Generic;
using I2.Loc;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace FirstLight.Editor.Inspector
{
	public class LocalizationTermAttributeProcessor : OdinAttributeProcessor<string>
	{
		public override bool CanProcessSelfAttributes(InspectorProperty property)
		{
			return property.GetAttribute<LocalizationTermAttribute>() != null;
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			attributes.Add(new ValueDropdownAttribute($"@{nameof(LocalizationTermAttributeProcessor)}.{nameof(GetLocalizationTerms)}()"));
			base.ProcessSelfAttributes(property, attributes);
		}
		

		private static IEnumerable<string> GetLocalizationTerms()
		{
			return LocalizationManager.GetTermsList();
		}
	}
}
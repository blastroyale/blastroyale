using System.Collections.Generic;
using I2.Loc;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A button that has it's text set from a I2 Localization key.
	/// </summary>
	public class LocalizedDropDown : DropdownField
	{
		protected string localizationKey { get; set; }
		
		public LocalizedDropDown() : this(string.Empty)
		{
		}

		public LocalizedDropDown(string key)
		{
			Localize(key);
		}

		/// <summary>
		/// Sets the text to the localized string from <paramref name="key"/>.
		/// </summary>
		public void Localize(string key)
		{
			localizationKey = key;
			label = LocalizationManager.TryGetTranslation(key, out var translation) ? translation : $"#{key}#";
		}

		public new class UxmlFactory : UxmlFactory<LocalizedDropDown, UxmlTraits>
		{
		}

		public new class UxmlTraits : DropdownField.UxmlTraits
		{
			UxmlStringAttributeDescription _localizationKeyAttribute = new()
			{
				name = "localization-key",
				use = UxmlAttributeDescription.Use.Required
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				((LocalizedDropDown) ve).Localize(_localizationKeyAttribute.GetValueFromBag(bag, cc));
			}
		}
	}
}
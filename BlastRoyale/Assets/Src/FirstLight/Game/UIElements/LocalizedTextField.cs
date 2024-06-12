using I2.Loc;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A button that has it's text set from a I2 Localization key.
	/// </summary>
	public class LocalizedTextField : TextField
	{
		private string labelLocalizationKey { get; set; }

		public LocalizedTextField() : this(string.Empty)
		{
		}

		public LocalizedTextField(string labelKey)
		{
			LocalizeLabel(labelKey);
		}

		/// <summary>
		/// Sets the label text to the localized string from <paramref name="key"/>.
		/// </summary>
		public void LocalizeLabel(string labelKey)
		{
			labelLocalizationKey = labelKey;
			label = LocalizationManager.TryGetTranslation(labelKey, out var translation) ? translation : $"#{labelKey}#";
		}

		public new class UxmlFactory : UxmlFactory<LocalizedTextField, UxmlTraits>
		{
		}

		public new class UxmlTraits : TextField.UxmlTraits
		{
			UxmlStringAttributeDescription _labelLocalizationKeyAttribute = new ()
			{
				name = "label-localization-key",
				use = UxmlAttributeDescription.Use.Required
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				((LocalizedTextField) ve).LocalizeLabel(_labelLocalizationKeyAttribute.GetValueFromBag(bag, cc));
			}
		}
	}
}
using I2.Loc;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A button that has it's text set from a I2 Localization key.
	/// </summary>
	public sealed class LocalizedTextField : TextField
	{
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
		public void LocalizeLabel(string key)
		{
			label = LocalizationManager.TryGetTranslation(key, out var translation) ? translation : $"#{key}#";
		}

		public new class UxmlFactory : UxmlFactory<LocalizedTextField, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			UxmlStringAttributeDescription _labelLocalizationKeyAttribute = new()
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
using I2.Loc;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	public class LocalizedRadioButton : RadioButton
	{
		protected string localizationKey { get; set; }

		public LocalizedRadioButton() : this(string.Empty)
		{
		}

		public LocalizedRadioButton(string key)
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

		public new class UxmlFactory : UxmlFactory<LocalizedRadioButton, UxmlTraits>
		{
		}

		public new class UxmlTraits : RadioButton.UxmlTraits
		{
			UxmlStringAttributeDescription _localizationKeyAttribute = new()
			{
				name = "localization-key",
				use = UxmlAttributeDescription.Use.Required
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				((LocalizedRadioButton) ve).Localize(_localizationKeyAttribute.GetValueFromBag(bag, cc));
			}
		}
	}
}
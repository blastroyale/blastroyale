using I2.Loc;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A label that has it's text set from a I2 Localization key.
	/// </summary>
	public sealed class LocalizedLabel : TextElement
	{
		public LocalizedLabel() : this(string.Empty)
		{
		}

		public LocalizedLabel(string key)
		{
			Localize(key);
		}

		/// <summary>
		/// Sets the text to the localized string from <paramref name="key"/>.
		/// </summary>
		public void Localize(string key)
		{
			text = LocalizationManager.TryGetTranslation(key, out var translation) ? translation : $"#{key}#";
		}

		public new class UxmlFactory : UxmlFactory<LocalizedLabel, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			UxmlStringAttributeDescription _localizationKeyAttribute = new()
			{
				name = "localization-key",
				use = UxmlAttributeDescription.Use.Required
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				((LocalizedLabel) ve).Localize(_localizationKeyAttribute.GetValueFromBag(bag, cc));
			}
		}
	}
}
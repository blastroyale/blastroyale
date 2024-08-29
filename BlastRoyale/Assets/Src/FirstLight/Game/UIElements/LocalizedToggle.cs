using I2.Loc;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	public class LocalizedToggle : Toggle
	{
		protected string localizationKey { get; set; }
		private Label _label;

		public LocalizedToggle() : this(string.Empty)
		{
		}

		public LocalizedToggle(string key)
		{
			_label = new LabelOutlined("##");
			_label.AddToClassList("localized-toggle__label");
			Insert(0, _label);
			Localize(key);
		}

		/// <summary>
		/// Sets the text to the localized string from <paramref name="key"/>.
		/// </summary>
		public void Localize(string key)
		{
			localizationKey = key;
			_label.text = LocalizationManager.TryGetTranslation(key, out var translation) ? translation : $"#{key}#";
		}

		public new class UxmlFactory : UxmlFactory<LocalizedToggle, UxmlTraits>
		{
		}

		public new class UxmlTraits : Toggle.UxmlTraits
		{
			UxmlStringAttributeDescription _localizationKeyAttribute = new ()
			{
				name = "localization-key",
				use = UxmlAttributeDescription.Use.Required
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				((LocalizedToggle) ve).Localize(_localizationKeyAttribute.GetValueFromBag(bag, cc));
			}
		}
	}
}
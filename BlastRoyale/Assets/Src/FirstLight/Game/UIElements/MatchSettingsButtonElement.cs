using System;
using I2.Loc;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	public class MatchSettingsButtonElement : VisualElement
	{
		private const string USS_BLOCK = "match-settings-button";
		private const string USS_TITLE = USS_BLOCK + "__title";
		private const string USS_BUTTON = USS_BLOCK + "__button";
		private const string USS_BUTTON_BACKGROUND = USS_BLOCK + "__button-background";
		private const string USS_BUTTON_LABEL = USS_BLOCK + "__button-label";

		private const string USS_BUTTON_NUMERICAL = USS_BUTTON + "--numerical";

		public ImageButton Button => _button;

		private bool Numerical { get; set; }
		private string TitleLocalizationKey { get; set; }

		private readonly Label _title;
		private readonly ImageButton _button;
		private readonly VisualElement _buttonBackground;
		private readonly Label _buttonLabel;

		public MatchSettingsButtonElement()
		{
			AddToClassList(USS_BLOCK);

			Add(_title = new Label("MODE") {name = "title"});
			_title.AddToClassList(USS_TITLE);

			Add(_button = new ImageButton {name = "button"});
			_button.AddToClassList(USS_BUTTON);
			{
				_button.Add(_buttonBackground = new VisualElement {name = "button-background"});
				_buttonBackground.AddToClassList(USS_BUTTON_BACKGROUND);

				_button.Add(_buttonLabel = new Label("BATTLE ROYALE") {name = "button-label"});
				_buttonLabel.AddToClassList(USS_BUTTON_LABEL);
			}
		}

		public void SetValue(string value)
		{
			_buttonLabel.text = value;
		}

		public new class UxmlFactory : UxmlFactory<MatchSettingsButtonElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			private readonly UxmlBoolAttributeDescription _numericalAttribute = new ()
			{
				name = "numerical",
				use = UxmlAttributeDescription.Use.Optional,
				defaultValue = false
			};

			private readonly UxmlStringAttributeDescription _titleLocalizationKeyAttribute = new ()
			{
				name = "title-localization-key",
				use = UxmlAttributeDescription.Use.Required
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);

				var msbe = ((MatchSettingsButtonElement) ve);

				var isNumerical = _numericalAttribute.GetValueFromBag(bag, cc);
				msbe.Numerical = isNumerical;
				msbe._button.EnableInClassList(USS_BUTTON_NUMERICAL, isNumerical);

				var titleKey = _titleLocalizationKeyAttribute.GetValueFromBag(bag, cc);
				msbe.TitleLocalizationKey = titleKey;
				msbe._title.text = LocalizationManager.TryGetTranslation(titleKey, out var translation) ? translation : $"#{titleKey}#";
			}
		}
	}
}
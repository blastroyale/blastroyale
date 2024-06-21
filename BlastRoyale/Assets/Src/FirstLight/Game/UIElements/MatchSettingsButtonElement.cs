using I2.Loc;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	public class MatchSettingsButtonElement : ImageButton
	{
		private const string USS_BLOCK = "match-settings-button";
		private const string USS_TITLE = USS_BLOCK + "__title";
		private const string USS_VALUE = USS_BLOCK + "__value";
		private const string USS_EDIT_ICON = USS_BLOCK + "__edit-icon";

		private const string USS_BUTTON_NUMERICAL = USS_BLOCK + "--numerical";

		private bool Numerical { get; set; }
		private string TitleLocalizationKey { get; set; }

		private readonly Label _title;
		private readonly Label _value;

		public MatchSettingsButtonElement()
		{
			AddToClassList(USS_BLOCK);

			Add(_title = new Label("MODE") {name = "title"});
			_title.AddToClassList(USS_TITLE);

			Add(_value = new Label("BATTLE ROYALE") {name = "value"});
			_value.AddToClassList(USS_VALUE);

			var editIcon = new VisualElement {name = "edit-icon"};
			Add(editIcon);
			editIcon.AddToClassList(USS_EDIT_ICON);
		}

		public void SetValue(string value)
		{
			_value.text = value;
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

				var msbe = (MatchSettingsButtonElement) ve;

				var isNumerical = _numericalAttribute.GetValueFromBag(bag, cc);
				msbe.Numerical = isNumerical;
				msbe.EnableInClassList(USS_BUTTON_NUMERICAL, isNumerical);

				var titleKey = _titleLocalizationKeyAttribute.GetValueFromBag(bag, cc);
				msbe.TitleLocalizationKey = titleKey;
				msbe._title.text = LocalizationManager.TryGetTranslation(titleKey, out var translation) ? translation : $"#{titleKey}#";

				// For editor preview
				if (!Application.isPlaying && isNumerical)
				{
					msbe._value.text = "1";
				}
			}
		}
	}
}
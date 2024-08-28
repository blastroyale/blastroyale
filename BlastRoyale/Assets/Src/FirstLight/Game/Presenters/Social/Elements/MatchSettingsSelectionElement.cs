using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// An element to show a map / gun / mutator...
	/// </summary>
	public class MatchSettingsSelectionElement : ImageButton
	{
		private const string USS_BLOCK = "match-settings-selection";
		private const string USS_TITLE = USS_BLOCK + "__title";
		private const string USS_SUBTITLE = USS_BLOCK + "__subtitle";
		private const string USS_TEXT_CONTAINER = USS_BLOCK + "__text-container";
		private const string USS_IMAGE = USS_BLOCK + "__image";

		public string titleLocalizationKey { get; set; }
		public string subtitleLocalizationKey { get; set; }

		private LocalizedLabel _title;
		private LocalizedLabel _subtitle;
		private VisualElement _image;

		public MatchSettingsSelectionElement() : this("DISTRICT DASH", "Small map, shorter\nmatches.")
		{
		}

		public MatchSettingsSelectionElement(string titleKey, string subtitleKey)
		{
			AddToClassList(USS_BLOCK);

			var textContainer = new VisualElement {name = "text-container"};
			Add(textContainer);
			textContainer.AddToClassList(USS_TEXT_CONTAINER);
			{
				textContainer.Add(_title = new LocalizedLabel(titleKey) {name = "title"});
				_title.AddToClassList(USS_TITLE);

				if (!string.IsNullOrEmpty(subtitleKey))
				{
					textContainer.Add(_subtitle = new LocalizedLabel(subtitleKey) {name = "subtitle"});
					_subtitle.AddToClassList(USS_SUBTITLE);	
				}

				
			}

			Add(_image = new VisualElement {name = "image"});
			_image.AddToClassList(USS_IMAGE);
		}

		public void SetImage(Sprite sprite)
		{
			_image.style.backgroundImage = new StyleBackground(sprite);
		}

		public new class UxmlFactory : UxmlFactory<MatchSettingsSelectionElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : ImageButton.UxmlTraits
		{
			private readonly UxmlStringAttributeDescription _titleLocalizationKeyAttribute = new ()
			{
				name = "title-localization-key",
				use = UxmlAttributeDescription.Use.Required
			};

			private readonly UxmlStringAttributeDescription _subtitleLocalizationKeyAttribute = new ()
			{
				name = "subtitle-localization-key",
				use = UxmlAttributeDescription.Use.Required
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);

				var msse = (MatchSettingsSelectionElement) ve;

				msse.titleLocalizationKey = _titleLocalizationKeyAttribute.GetValueFromBag(bag, cc);
				msse._title.Localize(msse.titleLocalizationKey);

				msse.subtitleLocalizationKey = _subtitleLocalizationKeyAttribute.GetValueFromBag(bag, cc);
				msse._subtitle.Localize(msse.subtitleLocalizationKey);
			}
		}
	}
}
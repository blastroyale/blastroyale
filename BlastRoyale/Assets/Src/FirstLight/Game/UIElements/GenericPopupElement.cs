using System;
using I2.Loc;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	public class GenericPopupElement : VisualElement
	{
		private const string USS_BLOCK = "generic-popup";
		private const string USS_HEADER = USS_BLOCK + "__header";
		private const string USS_TITLE = USS_BLOCK + "__title";
		private const string USS_CONTENT = USS_BLOCK + "__content";
		private const string USS_CLOSE_BUTTON = USS_BLOCK + "__close-button";

		private string TitleLocalizationKey { get; set; }

		private readonly VisualElement _header;
		private readonly Label _title;
		private readonly ImageButton _closeButton;
		private readonly VisualElement _content;

		public event Action CloseClicked;

		public override VisualElement contentContainer { get; }

		public GenericPopupElement()
		{
			contentContainer = this;

			AddToClassList(USS_BLOCK);

			// Header
			Add(_header = new VisualElement {name = "header"});
			_header.AddToClassList(USS_HEADER);
			{
				_header.Add(_title = new Label("Title") {name = "title"});
				_title.AddToClassList(USS_TITLE);
				_header.Add(_closeButton = new ImageButton {name = "close-button"});
				_closeButton.AddToClassList(USS_CLOSE_BUTTON);
				_closeButton.clicked += () => CloseClicked?.Invoke();
			}

			Add(_content = new VisualElement {name = "content"});
			_content.AddToClassList(USS_CONTENT);

			contentContainer = _content;
		}

		public void LocalizeTitle(string labelKey)
		{
			TitleLocalizationKey = labelKey;
			_title.text = LocalizationManager.TryGetTranslation(labelKey, out var translation)
				? translation
				: $"#{labelKey}#";
		}

		public new class UxmlFactory : UxmlFactory<GenericPopupElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			private readonly UxmlStringAttributeDescription _titleLocalizationKeyAttribute = new ()
			{
				name = "title-localization-key",
				use = UxmlAttributeDescription.Use.Required
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				((GenericPopupElement) ve).LocalizeTitle(_titleLocalizationKeyAttribute.GetValueFromBag(bag, cc));
			}
		}
	}
}
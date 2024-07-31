using System;
using FirstLight.Game.Utils;
using I2.Loc;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// The default popup element for the game.
	/// </summary>
	public class GenericPopupElement : VisualElement
	{
		private const string USS_BLOCK = "generic-popup";
		private const string USS_NO_PADDING_MODIFIER = USS_BLOCK + "--no-padding";
		private const string USS_HEADER = USS_BLOCK + "__header";
		private const string USS_TITLE = USS_BLOCK + "__title";
		private const string USS_CONTENT = USS_BLOCK + "__content";
		private const string USS_GLOW_HOLDER = USS_CONTENT + "__glow-holder";
		private const string USS_GLOW = USS_CONTENT + "__glow";
		private const string USS_CLOSE_BUTTON = USS_BLOCK + "__close-button";
		private const string USS_CLOSE_BUTTON_CONTAINER = USS_CLOSE_BUTTON + "-container";
		private string TitleLocalizationKey { get; set; }

		private readonly VisualElement _header;
		private readonly Label _title;
		private readonly ImageButton _closeButton;
		private readonly VisualElement _content;
		private readonly VisualElement _glowHolder;

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
				_header.Add(_title = new LabelOutlined("Title") {name = "title"});
				_title.AddToClassList(USS_TITLE);
				_header.Add(_closeButton = new ImageButton {name = "close-button-container"});
				_closeButton.AddToClassList(USS_CLOSE_BUTTON_CONTAINER);
				var icon = new VisualElement() {name = "close-button"};
				icon.AddToClassList(USS_CLOSE_BUTTON);
				_closeButton.Add(icon);
				_closeButton.clicked += () => CloseClicked?.Invoke();
			}

			Add(_content = new VisualElement {name = "content"});
			_content.AddToClassList(USS_CONTENT);
			_glowHolder = new VisualElement {name = "glow-holder"};
			_glowHolder.AddToClassList(USS_GLOW_HOLDER);
			_glowHolder.SetDisplay(false);
			var glow = new VisualElement() {name = "glow"};
			glow.AddToClassList(USS_GLOW);
			_glowHolder.Add(glow);
			_content.Add(_glowHolder);

			contentContainer = _content;
		}

		public void Configure(bool padding, bool glowBackground)
		{
			EnableInClassList(USS_NO_PADDING_MODIFIER, !padding);
			_glowHolder.SetDisplay(glowBackground);
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
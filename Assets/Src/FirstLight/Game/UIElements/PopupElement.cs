using I2.Loc;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays the base popup
	/// </summary>
	public class PopupElement : VisualElement
	{
		private string titleLocalizationKey { get; set; }

		private const string UssPopup = "popup-element";
		private const string UssPopupFront = UssPopup + "__front";
		private const string UssPopupHeader = UssPopup + "__header";
		private const string UssPopupHeaderBackground = UssPopupHeader + "__background";
		private const string UssPopupHeaderTitle = UssPopupHeader + "__title";
		private const string UssPopupContainer = UssPopup + "__container";
		private const string UssPopupBlocker = UssPopup + "__blocker";


		private VisualElement _container;
		public override VisualElement contentContainer => _container;
		private LocalizedLabel _title;

		public PopupElement()
		{
			AddToClassList(UssPopup);
			AddToClassList("anim-delay-0");
			AddToClassList("anim-translate");
			AddToClassList("anim-translate--down-xxxl");

			VisualElement frontPopup = new VisualElement();
			frontPopup.AddToClassList(UssPopupFront);
			hierarchy.Add(frontPopup);

			VisualElement header = new VisualElement();
			header.AddToClassList(UssPopupHeader);
			frontPopup.Add(header);

			VisualElement dotsBg = new VisualElement();
			header.Add(dotsBg);
			dotsBg.AddToClassList(UssPopupHeaderBackground);

			_title = new LocalizedLabel();
			header.Add(_title);
			_title.AddToClassList(UssPopupHeaderTitle);

			_container = new VisualElement();
			_container.AddToClassList(UssPopupContainer);
			frontPopup.Add(_container);

			VisualElement blocker = new VisualElement();
			blocker.name = "Blocker";
			blocker.AddToClassList(UssPopupBlocker);
			frontPopup.Add(blocker);
		}

		public void Localize(string key)
		{
			titleLocalizationKey = key;
			_title.text = LocalizationManager.TryGetTranslation(key, out var translation) ? translation : $"#{key}#";
		}

		public new class UxmlFactory : UxmlFactory<PopupElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			UxmlStringAttributeDescription _localizationKeyAttribute = new()
			{
				name = "title-localization-key",
				use = UxmlAttributeDescription.Use.Required
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				((PopupElement)ve).Localize(_localizationKeyAttribute.GetValueFromBag(bag, cc));
			}
		}
	}
}
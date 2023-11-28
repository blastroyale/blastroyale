using System;
using FirstLight.Game.Utils;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays the common header element with a title and a subtitle.
	/// </summary>
	public class ScreenHeaderElement : VisualElement
	{
		private const string UssBlock = "screen-header";

		private const string UssSafeAreaHolder = UssBlock + "__safe-area-holder";
		private const string UssTitle = UssBlock + "__title";
		private const string UssSubTitle = UssBlock + "__subtitle";
		private const string UssHome = UssBlock + "__home";
		private const string UssBack = UssBlock + "__back";
		private const string UssSeparator = UssBlock + "__separator";

		/// <summary>
		/// Triggered when the home button is clicked.
		/// </summary>
		public event Action homeClicked;

		/// <summary>
		/// Triggered when the back button is clicked.
		/// </summary>
		public event Action backClicked;

		private string titleKey { get; set; }
		private string subtitleKey { get; set; }

		private readonly Label _title;
		private readonly Label _subTitle;
		private readonly ImageButton _back;
		private readonly ImageButton _home;

		public ScreenHeaderElement()
		{
			AddToClassList(UssBlock);
			AddToClassList("anim-delay-0");
			AddToClassList("anim-fade");

			// This doesn't seem to work at the moment - picking mode has to be manually set to Ignore
			// on the Header element in UXML if you want to have interactive elements behind it.
			pickingMode = PickingMode.Ignore;

			var safeAreaContainer = new SafeAreaElement(false, false, false, true);
			safeAreaContainer.AddToClassList(UssSafeAreaHolder);
			Add(safeAreaContainer);

			safeAreaContainer.Add(_back = new ImageButton {name = "back"});
			_back.AddToClassList(UssBack);
			_back.AddToClassList(UIConstants.SFX_CLICK_BACKWARDS);
			_back.clicked += () => backClicked?.Invoke();

			safeAreaContainer.Add(_title = new Label("TITLE") {name = "title"});
			_title.AddToClassList(UssTitle);

			safeAreaContainer.Add(_subTitle = new Label("SUBTITLE") {name = "subtitle"});
			_subTitle.AddToClassList(UssSubTitle);

			var centerContent = new VisualElement {name = "separator", pickingMode = PickingMode.Ignore};
			centerContent.AddToClassList(UssSeparator);
			safeAreaContainer.Add(centerContent);

			safeAreaContainer.Add(_home = new ImageButton {name = "home"});
			_home.AddToClassList(UssHome);
			_home.AddToClassList(UIConstants.SFX_CLICK_BACKWARDS);
			_home.clicked += () => homeClicked?.Invoke();
		}

		/// <summary>
		/// Sets the home button visible or invisible.
		/// </summary>
		public void SetHomeVisible(bool vis)
		{
			_home.SetVisibility(vis);
		}

		/// <summary>
		/// Sets the title and optional subtitle of the header element (should be already localized).
		/// </summary>
		public void SetTitle(string title, string subtitle = "")
		{
			_title.text = title;
			SetSubtitle(subtitle);
		}

		/// <summary>
		/// Sets the subtitle of the header element (should be localized)
		/// </summary>
		public void SetSubtitle(string subtitle)
		{
			if (string.IsNullOrWhiteSpace(subtitle))
			{
				_subTitle.style.display = DisplayStyle.None;
			}
			else
			{
				_subTitle.style.display = DisplayStyle.Flex;
				_subTitle.text = subtitle;
			}
		}

		public new class UxmlFactory : UxmlFactory<ScreenHeaderElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : ImageButton.UxmlTraits
		{
			private readonly UxmlStringAttributeDescription _titleKeyAttribute = new()
			{
				name = "title-key",
				use = UxmlAttributeDescription.Use.Required
			};

			private readonly UxmlStringAttributeDescription _subTitleKeyAttribute = new()
			{
				name = "subtitle-key",
				use = UxmlAttributeDescription.Use.Optional
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);

				var she = (ScreenHeaderElement) ve;
				she.titleKey = _titleKeyAttribute.GetValueFromBag(bag, cc);
				she.subtitleKey = _subTitleKeyAttribute.GetValueFromBag(bag, cc);

				she.SetTitle(string.IsNullOrWhiteSpace(she.titleKey) ? "" : she.titleKey.LocalizeKey(),
					string.IsNullOrWhiteSpace(she.subtitleKey) ? "" : she.subtitleKey.LocalizeKey());
			}
		}
	}
}
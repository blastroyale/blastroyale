using FirstLight.Game.Utils;
using I2.Loc;
using Quantum.Prototypes;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays the common header element with a title and a subtitle.
	/// </summary>
	public class ScreenHeaderElement : ImageButton
	{
		private const string UssBlock = "screen-header";

		private const string UssSafeAreaHolder = UssBlock + "__safe-area-holder";
		private const string UssTitle = UssBlock + "__title";
		private const string UssSubtitle = UssBlock + "__subtitle";
		private const string UssBackIcon = UssBlock + "__back-icon";

		public string titleKey { get; set; }
		public string subtitleKey { get; set; }
		public bool subtitleBack { get; set; }

		private readonly Label _title;
		private readonly Label _subtitle;
		private readonly VisualElement _backIcon;

		public ScreenHeaderElement()
		{
			AddToClassList(UssBlock);

			var safeAreaContainer = new SafeAreaElement(true, false, true, false);
			safeAreaContainer.AddToClassList(UssSafeAreaHolder);
			Add(safeAreaContainer);

			safeAreaContainer.Add(_title = new Label("TITLE") {name = "title"});
			_title.AddToClassList(UssTitle);

			safeAreaContainer.Add(_subtitle = new Label("SUBTITLE") {name = "subtitle"});
			_subtitle.AddToClassList(UssSubtitle);

			_subtitle.Add(_backIcon = new VisualElement {name = "back-icon"});
			_backIcon.AddToClassList(UssBackIcon);
		}

		/// <summary>
		/// Sets the title of the header element (should be already localized).
		/// </summary>
		public void SetTitle(string title)
		{
			_title.text = title;
		}

		/// <summary>
		/// Sets the subtitle of the header element (should be already localized). If
		/// <paramref name="back"/> is true it will append "BACK TO" and show a back icon.
		/// </summary>
		public void SetSubtitle(string subtitle, bool back = true)
		{
			if (string.IsNullOrEmpty(subtitle))
			{
				_subtitle.SetDisplayActive(false);
			}
			else
			{
				_subtitle.SetDisplayActive(true);

				if (back)
				{
					_subtitle.text = string.Format(ScriptLocalization.UITShared.back_to, subtitle);
					_backIcon.SetDisplayActive(true);
				}
				else
				{
					_subtitle.text = subtitle;
					_backIcon.SetDisplayActive(false);
				}
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

			private readonly UxmlStringAttributeDescription _subtitleKeyAttribute = new()
			{
				name = "subtitle-key",
				use = UxmlAttributeDescription.Use.Optional,
				defaultValue = ""
			};

			private readonly UxmlBoolAttributeDescription _subtitleBackAttribute = new()
			{
				name = "subtitle-back",
				use = UxmlAttributeDescription.Use.Optional,
				defaultValue = true
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);

				var she = (ScreenHeaderElement) ve;
				she.titleKey = _titleKeyAttribute.GetValueFromBag(bag, cc);
				she.subtitleKey = _subtitleKeyAttribute.GetValueFromBag(bag, cc);
				she.subtitleBack = _subtitleBackAttribute.GetValueFromBag(bag, cc);

				she.SetTitle(she.titleKey.LocalizeKey());
				she.SetSubtitle(string.IsNullOrEmpty(she.subtitleKey) ? null : she.subtitleKey.LocalizeKey());
			}
		}
	}
}
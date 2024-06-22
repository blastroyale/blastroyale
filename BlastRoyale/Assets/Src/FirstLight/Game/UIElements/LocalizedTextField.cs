using FirstLight.Game.Utils;
using I2.Loc;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A button that has it's text set from a I2 Localization key.
	/// </summary>
	public class LocalizedTextField : TextField
	{
		private string labelLocalizationKey { get; set; }
		private bool showCopyButton { get; set; }

		private ImageButton _copyButton;

		public LocalizedTextField() : this(string.Empty)
		{
		}

		public LocalizedTextField(string labelKey)
		{
			LocalizeLabel(labelKey);
		}

		/// <summary>
		/// Sets the label text to the localized string from <paramref name="key"/>.
		/// </summary>
		public void LocalizeLabel(string labelKey)
		{
			labelLocalizationKey = labelKey;
			label = string.IsNullOrEmpty(labelKey) ? string.Empty :
				LocalizationManager.TryGetTranslation(labelKey, out var translation) ? translation : $"#{labelKey}#";
		}

		/// <summary>
		/// Enables or disables the "copy to clipboard" button.
		/// </summary>
		public void EnableCopyButton(bool enable)
		{
			showCopyButton = enable;
			if (enable && _copyButton == null)
			{
				_copyButton = new ImageButton {name = "copy-button"};
				Add(_copyButton);
				_copyButton.AddToClassList("unity-text-field__copy-button");
				
				_copyButton.clicked += () => UIUtils.SaveToClipboard(value);
			}
			else if (!enable && _copyButton != null)
			{
				_copyButton.RemoveFromHierarchy();
				_copyButton = null;
			}
		}

		public new class UxmlFactory : UxmlFactory<LocalizedTextField, UxmlTraits>
		{
		}

		public new class UxmlTraits : TextField.UxmlTraits
		{
			private readonly UxmlStringAttributeDescription _labelLocalizationKeyAttribute = new ()
			{
				name = "label-localization-key",
				use = UxmlAttributeDescription.Use.Required
			};

			private readonly UxmlBoolAttributeDescription _showCopyButton = new ()
			{
				name = "show-copy-button",
				use = UxmlAttributeDescription.Use.Optional,
				defaultValue = false
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);

				var ltf = (LocalizedTextField) ve;

				ltf.LocalizeLabel(_labelLocalizationKeyAttribute.GetValueFromBag(bag, cc));
				ltf.EnableCopyButton(_showCopyButton.GetValueFromBag(bag, cc));
			}
		}
	}
}
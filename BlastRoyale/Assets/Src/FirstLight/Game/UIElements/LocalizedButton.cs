using System;
using I2.Loc;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A button that has it's text set from a I2 Localization key.
	/// </summary>
	public sealed class LocalizedButton : ButtonOutlined
	{
		private string _localizationKey;

		public string LocalizationKey
		{
			get => _localizationKey;
			set
			{
				_localizationKey = value;
				if (string.IsNullOrWhiteSpace(_localizationKey)) return;
				if (!LocalizationManager.TryGetTranslation(_localizationKey, out var translation))
				{
					translation = _localizationKey;
					Debug.LogWarning($"Could not find translation for key {_localizationKey} in element " + name);
				}

				text = translation;
			}
		}

		[Obsolete("Do not use default constructor")]
		public LocalizedButton()
		{
		}

		public LocalizedButton(string localizationKey = null, Action action = null) : base(localizationKey, action)
		{
			LocalizationKey = localizationKey;
		}

		public new class UxmlFactory : UxmlFactory<LocalizedButton, UxmlTraits>
		{
		}

		public new class UxmlTraits : ButtonOutlined.UxmlTraits
		{
			UxmlStringAttributeDescription _localizationKeyAttribute = new ()
			{
				name = "localization-key",
				use = UxmlAttributeDescription.Use.Optional
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				var labelOutlined = (LocalizedButton) ve;
				labelOutlined.LocalizationKey = _localizationKeyAttribute.GetValueFromBag(bag, cc);
			}
		}
	}
}
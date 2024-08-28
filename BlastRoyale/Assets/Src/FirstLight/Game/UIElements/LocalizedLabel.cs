using System;
using I2.Loc;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A label that has it's text set from a I2 Localization key.
	/// </summary>
	public class LocalizedLabel : LabelOutlined
	{
		protected string _localizationKey;

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
		public LocalizedLabel()
		{
		}

		public LocalizedLabel(string localizationKey) : base($"#{localizationKey}#")
		{
			LocalizationKey = localizationKey;
		}

		/// <summary>
		/// Sets the text to the localized string from <paramref name="key"/>.
		/// </summary>
		public void Localize(string key)
		{
			LocalizationKey = key;
		}

		public new class UxmlFactory : UxmlFactory<LocalizedLabel, UxmlTraits>
		{
		}

		public new class UxmlTraits : LabelOutlined.UxmlTraits
		{
			UxmlStringAttributeDescription _localizationKeyAttribute = new ()
			{
				name = "localization-key",
				use = UxmlAttributeDescription.Use.Optional
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				var labelOutlined = (LocalizedLabel) ve;
				labelOutlined.LocalizationKey = _localizationKeyAttribute.GetValueFromBag(bag, cc);
			}
		}
	}
}
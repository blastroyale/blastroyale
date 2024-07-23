using System;
using I2.Loc;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A label that has it's text set from a I2 Localization key.
	/// </summary>
	public sealed class LocalizedLabel : LabelOutlined
	{
		public new class UxmlFactory : UxmlFactory<LocalizedLabel, UxmlTraits>
		{
		}

		[Obsolete("Do not use default constructor")]
		public LocalizedLabel()
		{
		}

		public LocalizedLabel(string localizationKey, string elementName, bool outlineHack = false) : base(elementName, outlineHack)
		{
			LocalizationKey = localizationKey;
		}

		public LocalizedLabel(string localizationKey) : base("localized-label")
		{
			LocalizationKey = localizationKey;
		}
	}
}
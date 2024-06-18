using System;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK.Popups
{
	public class SelectNumberPopupView : UIView
	{
		private readonly Action<int> _onConfirm;
		private readonly string _subtitleKey;
		private readonly int _min;
		private readonly int _max;
		private readonly int _currentValue;

		public SelectNumberPopupView(Action<int> onConfirm, string subtitleKey, int min, int max, int currentValue)
		{
			_onConfirm = onConfirm;
			_subtitleKey = subtitleKey;
			_min = min;
			_max = max;
			_currentValue = currentValue;
		}

		protected override void Attached()
		{
			Element.Q<LocalizedLabel>("Subtitle").Localize(_subtitleKey);

			var slider = Element.Q<SliderIntWithButtons>("Slider");
			slider.lowValue = _min;
			slider.highValue = _max;
			slider.value = _currentValue;

			Element.Q<LocalizedButton>("ConfirmButton").Required().clicked += () =>
			{
				_onConfirm.Invoke(slider.value);
			};
		}
	}
}
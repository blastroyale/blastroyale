using System;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using QuickEye.UIToolkit;

namespace FirstLight.Game.Views.UITK.Popups
{
	/// <summary>
	/// Allows the user to select a number within a range.
	/// </summary>
	public class SelectNumberPopupView : UIView
	{
		[Q("Subtitle")] private LocalizedLabel _subtitle;
		[Q("Slider")] private SliderIntWithButtons _slider;
		[Q("ConfirmButton")] private LocalizedButton _confirmButton;

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
			_subtitle.Localize(_subtitleKey);

			_slider.lowValue = _min;
			_slider.highValue = _max;
			_slider.value = _currentValue;

			_confirmButton.Required().clicked += () => _onConfirm.Invoke(_slider.value);
			;
		}
	}
}
using System;
using FirstLight.UiService;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	public class LoadingSpinnerPresenter : UiPresenter
	{
		[SerializeField] private RectTransform _spinnerImage;
		[SerializeField] private float _anglePerSecond;

		private void Update()
		{
			_spinnerImage.Rotate(0f, 0f, _anglePerSecond * Time.deltaTime);
		}
	}
}
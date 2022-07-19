using System;
using FirstLight.UiService;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Controls the rotation of the loading spinner
	/// </summary>
	public class LoadingSpinnerPresenter : UiPresenter
	{
		[SerializeField] private RectTransform _spinnerImage;
		[SerializeField] private float _anglePerSecond;
		[SerializeField] private RectTransform _darkOverlay;

		private float _startTime;
		private bool _started;

		private void Start()
		{
			_darkOverlay.gameObject.SetActive(false);
			_spinnerImage.gameObject.SetActive(false);
			_startTime = Time.time;
		}
		private void Update()
		{
			if (!_started && !(Time.time - _startTime > 0.3f)) return;

			if (!_started)
			{
				_started = true;
				_darkOverlay.gameObject.SetActive(true);
				_spinnerImage.gameObject.SetActive(true);		
			}
			
			_spinnerImage.Rotate(0f, 0f, _anglePerSecond * Time.deltaTime);
		}
	}
}
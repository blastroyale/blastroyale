using System;
using System.Collections;
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
		
		private bool _started;

		private void OnEnable()
		{
			_darkOverlay.gameObject.SetActive(false);
			_spinnerImage.gameObject.SetActive(false);
		}

		private void Update()
		{
			if (!_started) return;

			_spinnerImage.Rotate(0f, 0f, _anglePerSecond * Time.deltaTime);
		}

		private IEnumerator ActivateSpinner()
		{
			yield return new WaitForSeconds(0.3f);
			
			_started = true;
			_darkOverlay.gameObject.SetActive(true);
			_spinnerImage.gameObject.SetActive(true);
		} 
	}
}
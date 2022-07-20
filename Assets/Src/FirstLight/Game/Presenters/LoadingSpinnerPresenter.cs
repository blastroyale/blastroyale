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
		private const float MIN_DELAY_SHOW_TIME = 0.3f;
		[SerializeField] private RectTransform _spinnerImage;
		[SerializeField] private float _anglePerSecond;
		[SerializeField] private RectTransform _darkOverlay;

		private void OnEnable()
		{
			StartCoroutine(ActivateSpinner());
		}
		
		private void Update()
		{
			_spinnerImage.Rotate(0f, 0f, _anglePerSecond * Time.deltaTime);
		}

		private IEnumerator ActivateSpinner()
		{
			_darkOverlay.gameObject.SetActive(false);
			_spinnerImage.gameObject.SetActive(false);
			
			yield return new WaitForSeconds(MIN_DELAY_SHOW_TIME);

			_darkOverlay.gameObject.SetActive(true);
			_spinnerImage.gameObject.SetActive(true);
		} 
	}
}
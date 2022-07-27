using System.Collections;
using FirstLight.Game.Views;
using FirstLight.UiService;
using UnityEngine;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Controls the rotation of the loading spinner
	/// </summary>
	public class LoadingSpinnerScreenPresenter : UiPresenter
	{
		private const float MIN_DELAY_SHOW_TIME = 0.3f;

		[SerializeField] private LoadingSpinnerView _spinnerView;

		private void OnEnable()
		{
			StartCoroutine(ActivateSpinner());
		}

		private IEnumerator ActivateSpinner()
		{
			_spinnerView.gameObject.SetActive(false);

			yield return new WaitForSeconds(MIN_DELAY_SHOW_TIME);

			_spinnerView.gameObject.SetActive(true);
		}
	}
}
using UnityEngine;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// Responsible for animating a loading spinner.
	/// </summary>
	public class LoadingSpinnerView : MonoBehaviour
	{
		[SerializeField] private RectTransform _spinnerImage;
		[SerializeField] private float _anglePerSecond = 360;

		private void Update()
		{
			_spinnerImage.Rotate(0f, 0f, _anglePerSecond * Time.deltaTime);
		}
	}
}
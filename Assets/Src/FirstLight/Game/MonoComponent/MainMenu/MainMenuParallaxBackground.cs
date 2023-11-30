using UnityEngine;

namespace FirstLight.Game.MonoComponent.MainMenu
{
	public class MainMenuParallaxBackground : MonoBehaviour
	{
		[SerializeField] private Transform _centerTriangle;
		[SerializeField] private float _centerTriangleShift = 0.1f;
		[SerializeField] private Transform _innerBolts;
		[SerializeField] private float _innerBoltsShift = 0.2f;
		[SerializeField] private Transform _outerBolts;
		[SerializeField] private float _outerBoltsShift = 0.3f;

		[SerializeField] private float _dampingSpeed = 1f;

		private Gyroscope _gyro;

		private Vector3 _dampenedRotationRate;

		private void Start()
		{
			_gyro = UnityEngine.Input.gyro;
			_gyro.enabled = true;
		}

		private void Update()
		{
			_dampenedRotationRate = Vector3.Lerp(_dampenedRotationRate, _gyro.rotationRateUnbiased, Time.deltaTime * _dampingSpeed);

			var positionOffset = new Vector3(-_dampenedRotationRate.y, -_dampenedRotationRate.x, 0f);

			Debug.Log($"Dampened: {_dampenedRotationRate}, Raw: {_gyro.rotationRateUnbiased}, Offset: {positionOffset}");

			_centerTriangle.localPosition = positionOffset * _centerTriangleShift;
			_innerBolts.localPosition = positionOffset * _innerBoltsShift;
			_outerBolts.localPosition = positionOffset * _outerBoltsShift;
		}
	}
}
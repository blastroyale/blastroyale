using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// Put this on a Canvas to make it always rotate against the main camera.
	/// </summary>
	[RequireComponent(typeof(Canvas))]
	public class LookAtCameraView : MonoBehaviour
	{
		[InfoBox("We fetch Camera.main in Start() if this isn't assigned.", InfoMessageType.Info, "@_mainCamera == null")]
		[SerializeField] private Camera _mainCamera;

		[SerializeField] private float _distance;
		[SerializeField] private float _height;

		private Transform _mainCameraTransform;
		private Transform _transform;

		private void Start()
		{
			if (_mainCamera == null)
			{
				_mainCamera = Camera.main;
			}

			_mainCameraTransform = _mainCamera.transform;
			_transform = transform;
		}

		private void Update()
		{
			if (_distance > 0f || _height > 0f)
			{
				var direction = (_mainCameraTransform.position - _transform.parent.position).normalized;
				direction.y = 0;
				_transform.localPosition = direction * _distance + Vector3.up * _height;
			}

			_transform.LookAt(_transform.position + _mainCamera.transform.rotation * Vector3.forward,
			                  _mainCameraTransform.rotation * Vector3.up);
		}
	}
}
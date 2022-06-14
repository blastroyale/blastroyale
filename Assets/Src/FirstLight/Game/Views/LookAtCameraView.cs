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

		private Transform _mainCameraTransform;

		private void Start()
		{
			if (_mainCamera == null)
			{
				_mainCamera = Camera.main;
			}

			_mainCameraTransform = _mainCamera.transform;
		}

		private void Update()
		{
			transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
			                 _mainCameraTransform.rotation * Vector3.up);
		}
	}
}
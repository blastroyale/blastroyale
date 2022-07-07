using System;
using FirstLight.Services;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// This View handles the world elements that need an Overlay in the UI:
	/// - Showing the current object in the UI canvas following a world object
	/// </summary>
	[RequireComponent(typeof(RectTransform))]
	public class OverlayWorldView : MonoBehaviour, IPoolEntityDespawn
	{
		[SerializeField, Required] private RectTransform _rectTransform;
		[SerializeField, Required] private Vector2 _screenOffset;

		private bool _isTargetFollow;
		private Transform _target;
		private Vector3 _position;
		private Camera _camera;
			
		private void OnValidate()
		{
			_rectTransform = _rectTransform ? _rectTransform : GetComponent<RectTransform>();
		}

		private void LateUpdate()
		{
			var position = _isTargetFollow ? _target.position : _position;
			
			if(_camera != null)
			transform.position = _camera.WorldToScreenPoint(position) + (Vector3) _screenOffset;
		}

		/// <inheritdoc />
		public void OnDespawn()
		{
			_target = null;
			_isTargetFollow = false;
		}

		/// <summary>
		/// Marks the overlay to follow the given world <paramref name="target"/>
		/// </summary>
		/// <exception cref="ArgumentNullException">
		/// Thrown if the given world <paramref name="target"/> is null
		/// </exception>
		public void Follow(Transform target)
		{
			Follow(target, _screenOffset);
		}

		/// <summary>
		/// Marks the overlay to follow the given world <paramref name="target"/> with the given screen <paramref name="offset"/>
		/// </summary>
		/// <exception cref="ArgumentNullException">
		/// Thrown if the given world <paramref name="target"/> is null
		/// </exception>
		public void Follow(Transform target, Vector2 offset)
		{
			_target = target;
			_isTargetFollow = true;
			_screenOffset = offset;
			_camera = Camera.main;
		}

		/// <summary>
		/// Marks the overlay to follow the given world <paramref name="target"/>
		/// </summary>
		public void Follow(Vector3 target)
		{
			Follow(target, _screenOffset);
		}

		/// <summary>
		/// Marks the overlay to follow the given world <paramref name="target"/> with the given screen <paramref name="offset"/>
		/// </summary>
		public void Follow(Vector3 target, Vector2 offset)
		{
			_position = target;
			_isTargetFollow = false;
			_screenOffset = offset;
			_camera = Camera.main;
		}
	}
}
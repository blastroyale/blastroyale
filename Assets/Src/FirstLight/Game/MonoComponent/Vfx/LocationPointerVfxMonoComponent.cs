using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Vfx
{
	public class LocationPointerVfxMonoComponent : VfxMonoComponent
	{
		[SerializeField, Required]
		private LineRenderer _lineRenderer;
		[SerializeField, Required]
		private float _arrowAnimationSpeed;
		[SerializeField]
		private Transform _followedTransform;
		
		private Vector2 _offSet;
		
		public void SetFollowedObject(Transform transform)
		{
			_followedTransform = transform;
		}

		private void Update()
		{
			var startPosition = _followedTransform == null
				? transform.position + Vector3.forward * 3 // we just put a position that shows some of the line
				: _followedTransform.position;

			var followedDirection = (startPosition - transform.position).normalized;
			// We move the position a bit out of the center of the circle (doesn't look good)
			var endPosition = transform.position + followedDirection * 1.5f;
			
			_lineRenderer.SetPosition(0, startPosition);
			_lineRenderer.SetPosition(1, endPosition);
			_offSet.x -= _arrowAnimationSpeed*Time.deltaTime;
			_lineRenderer.material.mainTextureOffset = _offSet;
		}
	}
}
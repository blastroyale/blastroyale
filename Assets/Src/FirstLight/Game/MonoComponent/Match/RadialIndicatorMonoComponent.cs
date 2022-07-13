using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.VFX;

namespace FirstLight.Game.MonoComponent.Match
{
	/// <summary>
	/// Shows the indicator for the local player's attack with radial area of damage
	/// </summary>
	public class RadialIndicatorMonoComponent : MonoBehaviour, ITransformIndicator
	{
		[SerializeField, Required] protected GameObject _indicator;
		[SerializeField] protected float _localHeight = 0.025f;
		[SerializeField] protected bool _initiallyEnabled = false;
		
		protected Transform _playerTransform;
		protected Vector3 _position;
		protected float _maxRange;

		/// <inheritdoc />
		public bool VisualState => _indicator.activeSelf;
		
		protected virtual void Awake()
		{
			_indicator.SetActive(_initiallyEnabled);
			_playerTransform = transform;
		}

		protected void LateUpdate()
		{
			transform.position = _playerTransform.position + _position;
		}

		/// <inheritdoc />
		public void SetVisualState(bool isVisible, bool isEmphasized = false)
		{
			_indicator.SetActive(isVisible);
		}

		/// <inheritdoc />
		public void SetVisualProperties(float size, float minRange, float maxRange)
		{
			var cacheTransform = transform.transform;
			
			_maxRange = maxRange;
			cacheTransform.localScale = new Vector3(size, cacheTransform.localScale.y, size);
		}

		/// <inheritdoc />
		public void Init(EntityView playerEntityView)
		{
			_playerTransform = playerEntityView.transform;
			
			transform.SetParent(_playerTransform);
		}

		/// <inheritdoc />
		public void SetTransformState(Vector2 direction)
		{
			var move = direction * _maxRange;
			var move3 = new Vector3(move.x, 0, move.y);
			var position = _playerTransform.position;
			position.y += 0.1f;
			
			// Testing if we have a blocker in the way
			var directionRay = new Ray(position, move3);
			if (Physics.Raycast(directionRay, out var directionRestrain, _maxRange))
			{
				var distanceToObstacle = (directionRestrain.point - position).magnitude;
				if (distanceToObstacle < _maxRange)
				{
					move = direction * distanceToObstacle;
				}
			}

			// Getting the position the special is going to drop on
			position += new Vector3(move.x, 10f, move.y);
			var ray = new Ray(position, Vector3.down);
			if (Physics.Raycast(ray, out var raycastHit))
			{
				position = new Vector3(raycastHit.point.x, raycastHit.point.y, raycastHit.point.z);
			}
			else
			{
				position = new Vector3(position.x, _playerTransform.position.y, position.z);
			}
			
			_position = position-_playerTransform.position;
		}
	}
}
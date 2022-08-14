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
			var position = _playerTransform.position + new Vector3(move.x, 10f, move.y);

			if (Physics.Raycast(new Ray(position, Vector3.down), out var raycastHit))
			{
				position = new Vector3(raycastHit.point.x, raycastHit.point.y, raycastHit.point.z);
			}
			else
			{
				// If there's no surface to drop the special on, we just define the y of the player
				position = new Vector3(position.x, _playerTransform.position.y, position.z);
			}
			
			_position = position - _playerTransform.position;
		}
	}
}
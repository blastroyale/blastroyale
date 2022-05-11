using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.VFX;

namespace FirstLight.Game.MonoComponent.Match
{
	/// <summary>
	/// Shows the indicator to where the local player's is moving towards to
	/// </summary>
	public class MovementIndicatorMonoComponent: MonoBehaviour, ITransformIndicator
	{
		[SerializeField, Required] private VisualEffect _indicator;
		[SerializeField] private float _playerDistance = 2f;
		[SerializeField] private float _localHeight = 0.025f;

		private Transform _playerTransform;
		private Vector3 _position;

		/// <inheritdoc />
		public bool VisualState => _indicator.enabled;
		
		private void Awake()
		{
			_indicator.enabled = false;
			_playerTransform ??= transform;
		}

		private void LateUpdate()
		{
			transform.position = _playerTransform.position + _position;
		}

		/// <inheritdoc />
		public void SetVisualState(bool isVisible, bool isEmphasized = false)
		{
			_indicator.enabled = isVisible;
		}

		/// <inheritdoc />
		public void SetVisualProperties(float size, float minRange, float maxRange)
		{
			// Do nothing
		}

		/// <inheritdoc />
		public void Init(EntityView playerEntityView)
		{
			_playerTransform = playerEntityView.transform;
			
			transform.SetParent(_playerTransform);
		}

		/// <inheritdoc />
		public void SetTransformState(Vector2 position)
		{
			var move = position * _playerDistance;
			
			_position = new Vector3(move.x, _localHeight, move.y);
			transform.position = _playerTransform.position + _position;
		}
	}
}
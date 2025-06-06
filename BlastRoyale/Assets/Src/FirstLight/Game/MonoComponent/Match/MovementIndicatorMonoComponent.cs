using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Match
{
	/// <summary>
	/// Shows the indicator to where the local player's is moving towards to
	/// </summary>
	public class MovementIndicatorMonoComponent: MonoBehaviour, IIndicator
	{
		[SerializeField, Required] private GameObject _indicator;
		[SerializeField] private float _playerDistance = 2f;
		[SerializeField] private float _localHeight = 0.025f;

		private Transform _playerTransform;
		private Vector3 _position;

		/// <inheritdoc />
		public bool VisualState => _indicator.activeSelf;
		/// <inheritdoc />
		public IndicatorVfxId IndicatorVfxId => IndicatorVfxId.Movement;
		
		private void Awake()
		{
			_indicator.SetActive(false);
			_playerTransform ??= transform;
		}

		private void LateUpdate()
		{
			if (_playerTransform.IsDestroyed()) return;
			transform.position = _playerTransform.position + _position;
		}

		/// <inheritdoc />
		public void SetVisualState(bool isVisible, bool isEmphasized = false)
		{
			_indicator.SetActive(isVisible);;
		}

		/// <inheritdoc />
		public void SetVisualProperties(float size, float minRange, float maxRange)
		{
			transform.localScale = new Vector3(size, size, size);
		}

		/// <inheritdoc />
		public void Init(EntityView playerEntityView)
		{
			_playerTransform = playerEntityView.transform;
		}

		/// <inheritdoc />
		public void SetTransformState(Vector2 position)
		{
			if (_playerTransform.IsDestroyed()) return;
			var move = position * _playerDistance;
			_position = new Vector3(move.x, _localHeight, move.y);
			var playerPos = _playerTransform.position;
			var localTransform = transform;
			localTransform.position = playerPos + _position;
			var myPos = localTransform.position;
			localTransform.rotation = Quaternion.LookRotation(myPos - playerPos);
		}
		
		/// <inheritdoc />
		public void SetColor(Color c)
		{
			// Not implemented
		}
		
		/// <inheritdoc />
		public void ResetColor()
		{
			// Not implemented
		}
	}
}
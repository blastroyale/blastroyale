using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Match
{
	/// <summary>
	/// Shows the indicator for the local player's attack with radial area of damage
	/// </summary>
	public class RadialIndicatorMonoComponent : MonoBehaviour, IIndicator
	{
		private static readonly int _color = Shader.PropertyToID("_Color");
		
		[SerializeField, Required] protected GameObject _indicator;
		[SerializeField] protected float _localHeight = 0.025f;
		[SerializeField] protected bool _initiallyEnabled = false;
		
		private Transform _playerTransform;
		private Vector3 _position;
		private float _maxRange;
		private Color _originalColor;

		/// <inheritdoc />
		public bool VisualState => _indicator.activeSelf;
		/// <inheritdoc />
		public IndicatorVfxId IndicatorVfxId => IndicatorVfxId.Radial;
		
		private void Awake()
		{
			_indicator.SetActive(_initiallyEnabled);
			_playerTransform = transform;
			_originalColor = _indicator.GetComponent<MeshRenderer>().material.GetColor(_color);
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
			var cacheTransform = transform;
			
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
			
			_position.x = move.x;
			_position.z = move.y;
		}
		
		/// <inheritdoc />
		public void SetColor(Color c)
		{
			_indicator.GetComponent<MeshRenderer>().material.SetColor(_color, c);
		}
		
		/// <inheritdoc />
		public void ResetColor()
		{
			if (_indicator.GetComponent<MeshRenderer>().material.color != _originalColor)
			{
				_indicator.GetComponent<MeshRenderer>().material.SetColor(_color, _originalColor);
			}
		}
	}
}
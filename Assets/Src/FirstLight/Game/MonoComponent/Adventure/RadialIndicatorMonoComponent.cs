using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

namespace FirstLight.Game.MonoComponent.Adventure
{
	/// <summary>
	/// Shows the indicator for the local player's attack with radial area of damage
	/// </summary>
	public class RadialIndicatorMonoComponent : MonoBehaviour, ITransformIndicator
	{
		protected static readonly int _color = Shader.PropertyToID("Color");
		
		[SerializeField] protected VisualEffect _indicator;
		[SerializeField] protected float _localHeight = 0.025f;
		[SerializeField] protected bool _initiallyEnabled = false;
		
		protected Transform _playerTransform;
		protected Vector3 _position;
		protected float _maxRange;

		/// <inheritdoc />
		public bool VisualState => _indicator.enabled;
		
		protected virtual void Awake()
		{
			_indicator.enabled = _initiallyEnabled;
			_playerTransform = transform;
		}

		protected void LateUpdate()
		{
			transform.position = _playerTransform.position + _position;
		}

		/// <inheritdoc />
		public void SetVisualState(bool isVisible, bool isEmphasized = false)
		{
			_indicator.enabled = isVisible;

			var color = isEmphasized ? Color.red : Color.yellow;
			_indicator.SetVector4(_color, new Vector4(color.r, color.g, color.b, color.a));
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
		public void SetTransformState(Vector2 position)
		{
			var move = position * _maxRange;
			
			_position = new Vector3(move.x, _localHeight, move.y);;
			transform.position = _playerTransform.position + _position;
		}
	}
}
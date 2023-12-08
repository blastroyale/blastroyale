using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Match
{
	/// <summary>
	/// Shows the indicator for the local player's max target range
	/// </summary>
	public class RangeIndicatorMonoComponent : MonoBehaviour, IIndicator
	{
		[SerializeField] private float _localHeight = 0.25f;
		[SerializeField, Required] private MeshRenderer _indicator;
		private Color _originalColor;
		private static readonly int _color = Shader.PropertyToID("_Color");

		/// <inheritdoc />
		public bool VisualState => _indicator.enabled;
		/// <inheritdoc />
		public IndicatorVfxId IndicatorVfxId => IndicatorVfxId.Range;
		
		private void Awake()
		{
			_indicator.enabled = false;
			_originalColor = _indicator.material.GetColor(_color);
		}

		public void SetColor(Color c)
		{
			_indicator.material.SetColor(_color, c);
		}

		public void ResetColor()
		{
			if (_indicator.material.color != _originalColor)
			{
				_indicator.material.SetColor(_color, _originalColor);
			}
		}

		/// <inheritdoc />
		public void SetVisualState(bool isVisible, bool isEmphasized = false)
		{
			_indicator.enabled = isVisible;
		}

		/// <inheritdoc />
		public void SetVisualProperties(float size, float minRange, float maxRange)
		{
			transform.localScale = Vector3.one * (maxRange * GameConstants.Visuals.RADIUS_TO_SCALE_CONVERSION_VALUE);
		}

		/// <inheritdoc />
		public void SetTransformState(Vector2 position)
		{
			// DO Nothing
		}

		/// <inheritdoc />
		public void Init(EntityView playerEntityView)
		{
			var cacheTransform = transform;
			if (gameObject == null || cacheTransform == null) return;
			
			cacheTransform.SetParent(playerEntityView.transform);
			
			cacheTransform.localPosition = new Vector3(0, _localHeight, 0);
		}
	}
}
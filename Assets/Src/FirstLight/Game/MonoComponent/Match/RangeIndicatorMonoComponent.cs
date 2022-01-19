using FirstLight.Game.Utils;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Match
{
	/// <summary>
	/// Shows the indicator for the local player's max target range
	/// </summary>
	public class RangeIndicatorMonoComponent : MonoBehaviour, IIndicator
	{
		[SerializeField] private float _localHeight = 0.25f;
		[SerializeField] private MeshRenderer _indicator;

		/// <inheritdoc />
		public bool VisualState => _indicator.enabled;
		
		private void Awake()
		{
			_indicator.enabled = false;
		}

		/// <inheritdoc />
		public void SetVisualState(bool isVisible, bool isEmphasized = false)
		{
			_indicator.enabled = isVisible;
		}

		/// <inheritdoc />
		public void SetVisualProperties(float size, float minRange, float maxRange)
		{
			transform.localScale = Vector3.one * (maxRange * GameConstants.RadiusToScaleConversionValue);
		}

		/// <inheritdoc />
		public void Init(EntityView playerEntityView)
		{
			var cacheTransform = transform;
			
			cacheTransform.SetParent(playerEntityView.transform);
			
			cacheTransform.localPosition = new Vector3(0, _localHeight, 0);
		}
	}
}
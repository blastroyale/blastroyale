using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Match
{
	/// <summary>
	/// Shows the indicator for the <see cref="IndicatorVfxId"/> visual type
	/// </summary>
	public class NoneIndicatorMonoComponent : MonoBehaviour, IIndicator
	{
		/// <inheritdoc />
		public bool VisualState => false;

		/// <inheritdoc />
		public IndicatorVfxId IndicatorVfxId => IndicatorVfxId.Radial;

		/// <inheritdoc />
		public void SetVisualState(bool isVisible, bool isEmphasized = false)
		{
			// nothing
		}

		/// <inheritdoc />
		public void SetVisualProperties(float size, float minRange, float maxRange)
		{
			var cacheTransform = transform;

			cacheTransform.localScale = new Vector3(size, cacheTransform.localScale.y, size);
		}

		/// <inheritdoc />
		public void Init(EntityView playerEntityView)
		{
			transform.SetParent(playerEntityView.transform);
		}

		/// <inheritdoc />
		public void SetTransformState(Vector2 direction)
		{
			transform.localPosition = new Vector3(direction.x, 10f, direction.y);
		}
	}
}
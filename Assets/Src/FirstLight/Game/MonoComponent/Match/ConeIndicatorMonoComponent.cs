using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Match
{
	/// <summary>
	/// Shows the indicator for the local player's attack in a cone of damage.
	/// </summary>
	public class ConeIndicatorMonoComponent : MonoBehaviour, IIndicator
	{
		private static readonly int _color = Shader.PropertyToID("_Color");
		
		[SerializeField] private Color _reloadColor = new Color(255, 64, 118);
		[SerializeField, Required] private MeshRenderer _indicator;
		[SerializeField] private float _localHeight = 0.25f;

		private Quaternion _rotation;

		/// <inheritdoc />
		public bool VisualState => _indicator.enabled;
		/// <inheritdoc />
		public IndicatorVfxId IndicatorVfxId => IndicatorVfxId.Cone;

		private void Awake()
		{
			_indicator.enabled = false;
		}

		private void LateUpdate()
		{
			transform.rotation = _rotation;
		}

		/// <inheritdoc />
		public void SetVisualState(bool isVisible, bool isEmphasized = false)
		{
			_indicator.enabled = isVisible;
			_indicator.material.SetColor(_color, isEmphasized ? _reloadColor : Color.white);
		}

		/// <inheritdoc />
		public void SetVisualProperties(float size, float minRange, float maxRange)
		{
			transform.localScale = new Vector3(size, 1f, maxRange);
		}

		/// <inheritdoc />
		public void Init(EntityView playerEntityView)
		{
			var cacheTransform = transform;

			cacheTransform.SetParent(playerEntityView.transform);

			cacheTransform.localRotation = Quaternion.identity;
			cacheTransform.localPosition = new Vector3(0, _localHeight, 0);
		}

		/// <inheritdoc />
		public void SetTransformState(Vector2 position)
		{
			if (position.sqrMagnitude < Mathf.Epsilon)
			{
				return;
			}
			
			_rotation = Quaternion.LookRotation(new Vector3(position.x, 0f, position.y), Vector3.up);
			transform.rotation = _rotation;
		}
	}
}
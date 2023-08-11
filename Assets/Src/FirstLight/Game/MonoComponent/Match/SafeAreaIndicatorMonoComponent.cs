using FirstLight.FLogger;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Match
{
	public class SafeAreaIndicatorMonoComponent : MonoBehaviour, IIndicator
	{
		[SerializeField, Required] private GameObject _indicator;

		public bool VisualState => _indicator.activeSelf;

		public IndicatorVfxId IndicatorVfxId => IndicatorVfxId.SafeArea;

		private Vector3 _safeAreaCenter;
		private float _safeAreaRadius = -1f;
		private int _shrinkingStartTime;

		public void Init(EntityView playerEntityView)
		{
			var ct = transform;

			ct.SetParent(playerEntityView.transform, false);
			ct.localPosition = Vector3.zero;
			
			_indicator.SetActive(false);
		}

		private void Update()
		{
			if (_safeAreaRadius <= 0) return;

			var t = transform;
			var pos = t.position;

			var dist = Vector3.Distance(pos, _safeAreaCenter);
			if (dist > _safeAreaRadius && (QuantumRunner.Default?.Game?.Frames?.Predicted?.Time.AsFloat ?? 0) > _shrinkingStartTime)
			{
				// Outside safe area
				_indicator.SetActive(true);
				t.rotation = Quaternion.LookRotation((_safeAreaCenter - pos).normalized);
			}
			else
			{
				// Inside
				_indicator.SetActive(false);
			}
		}

		public void SetSafeArea(Vector2 center, float radius, int shrinkingStartTime)
		{
			_safeAreaCenter = new Vector3(center.x, 0, center.y);
			_safeAreaRadius = radius;
			_shrinkingStartTime = shrinkingStartTime;
		}

		public void SetVisualState(bool isVisible, bool isEmphasized = false)
		{
		}

		public void SetVisualProperties(float size, float minRange, float maxRange)
		{
		}

		public void SetTransformState(Vector2 position)
		{
		}

		public void SetColor(Color c)
		{
		}

		public void ResetColor()
		{
		}
	}
}
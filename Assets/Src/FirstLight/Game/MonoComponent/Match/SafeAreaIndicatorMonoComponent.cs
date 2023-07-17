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

		private Vector2 _safeAreaCenter;
		private float _safeAreaRadius = -1f;

		public void Init(EntityView playerEntityView)
		{
			var ct = transform;

			ct.SetParent(playerEntityView.transform, false);
			ct.localPosition = Vector3.zero;
		}

		private void Update()
		{
			if (_safeAreaRadius <= 0) return;

			var t = transform;
			var pos = t.position;

			var dist = Vector2.Distance(pos, _safeAreaCenter);
			if (dist > _safeAreaRadius)
			{
				// Outside safe area
				_indicator.SetActive(true);
				t.rotation = Quaternion.LookRotation((_safeAreaCenter - new Vector2(pos.x, pos.z)).normalized);
			}
			else
			{
				// Inside
				_indicator.SetActive(false);
			}
		}

		public void SetSafeArea(Vector2 center, float radius)
		{
			_safeAreaCenter = center;
			_safeAreaRadius = radius;
			FLog.Info("PACO", $"Set safe area: {center} - {radius}");
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
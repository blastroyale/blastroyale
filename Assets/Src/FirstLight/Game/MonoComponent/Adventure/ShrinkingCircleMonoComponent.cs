using FirstLight.Game.Views;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Adventure
{
	/// <summary>
	/// This Mono Component controls shrinking circle visuals behaviour
	/// </summary>
	public class ShrinkingCircleMonoComponent : MonoBehaviour
	{
		[SerializeField] private CircleLineRenderer _shrinkingCircleLinerRenderer;
		[SerializeField] private CircleLineRenderer _safeAreaCircleLinerRenderer;
		
		private void Awake()
		{
			QuantumCallback.Subscribe<CallbackUpdateView>(this, HandleUpdateView);
		}

		private void HandleUpdateView(CallbackUpdateView callback)
		{
			var frame = callback.Game.Frames.Verified;
			var circle = frame.GetSingleton<ShrinkingCircle>();

			var targetCircleCenter = circle.TargetCircleCenter.ToUnityVector2();
			var targetRadius = circle.TargetRadius.AsFloat;
			var lerp = Mathf.Max(0, (frame.Time.AsFloat - circle.ShrinkingStartTime.AsFloat) / circle.ShrinkingDurationTime.AsFloat);
			var radius = Mathf.Lerp(circle.CurrentRadius.AsFloat, targetRadius, lerp);
			var center = Vector2.Lerp(circle.CurrentCircleCenter.ToUnityVector2(), targetCircleCenter, lerp);
			
			var cachedShrinkingCircleLineTransform = _shrinkingCircleLinerRenderer.transform;
			var cachedSafeAreaCircleLine = _safeAreaCircleLinerRenderer.transform;
			
			var targetCenter = new Vector3(center.x, cachedShrinkingCircleLineTransform.position.y, center.y);
			
			
			cachedShrinkingCircleLineTransform.position = targetCenter;
			cachedShrinkingCircleLineTransform.localScale = new Vector3(radius, radius, 1f);
			_shrinkingCircleLinerRenderer.WidthMultiplier = 1f / radius;
			
			cachedSafeAreaCircleLine.position = new Vector3(targetCircleCenter.x, cachedSafeAreaCircleLine.position.y, targetCircleCenter.y);
			cachedSafeAreaCircleLine.localScale = new Vector3(targetRadius, targetRadius, 1f);
			_safeAreaCircleLinerRenderer.WidthMultiplier = 1f / (targetRadius);
		}
	}
}
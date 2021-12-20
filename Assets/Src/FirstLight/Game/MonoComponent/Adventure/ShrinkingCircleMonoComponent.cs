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
		[SerializeField] private Transform _damageZoneTransform;
		
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

			var cachedTransform = _damageZoneTransform;
			
			var targetCenter = new Vector3(center.x, _shrinkingCircleLinerRenderer.transform.position.y, center.y);
			
			cachedTransform.position = targetCenter;
			cachedTransform.localScale = new Vector3(radius * 2f, cachedTransform.localScale.y, radius * 2f);
			
			_shrinkingCircleLinerRenderer.Draw(targetCenter, radius);
			_safeAreaCircleLinerRenderer.Draw(new Vector3(targetCircleCenter.x, _safeAreaCircleLinerRenderer.transform.position.y, targetCircleCenter.y), targetRadius);
		}
	}
}
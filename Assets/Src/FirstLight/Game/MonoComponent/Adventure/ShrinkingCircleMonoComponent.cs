using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Adventure
{
	/// <summary>
	/// This Mono Component controls shrinking circle visuals behaviour
	/// </summary>
	public class ShrinkingCircleMonoComponent : MonoBehaviour
	{
		[SerializeField] private Transform _shrinkingCircleTransform;
		[SerializeField] private Transform _safeAreaTransform;
		
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
			var diameter = Mathf.Lerp(circle.CurrentRadius.AsFloat, targetRadius, lerp) * 2f;
			var center = Vector2.Lerp(circle.CurrentCircleCenter.ToUnityVector2(), targetCircleCenter, lerp);
			
			var cachedTransform = _shrinkingCircleTransform;
			
			cachedTransform.localScale = new Vector3(diameter, 1f, diameter);
			cachedTransform.position = new Vector3(center.x, cachedTransform.position.y, center.y);
			
			_safeAreaTransform.position = new Vector3(targetCircleCenter.x, _safeAreaTransform.position.y, targetCircleCenter.y);
			_safeAreaTransform.localScale = new Vector3(targetRadius * 2f, 1f, targetRadius * 2f);
		}
	}
}
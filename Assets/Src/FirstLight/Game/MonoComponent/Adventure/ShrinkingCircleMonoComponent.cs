using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Adventure
{
	/// <summary>
	/// This Mono Component controls shrinking circle visuals behaviour
	/// </summary>
	public class ShrinkingCircleMonoComponent : MonoBehaviour
	{
		private void Awake()
		{
			QuantumCallback.Subscribe<CallbackUpdateView>(this, HandleUpdateView);
		}

		private void HandleUpdateView(CallbackUpdateView callback)
		{
			var frame = callback.Game.Frames.Verified;
			var circle = frame.GetSingleton<ShrinkingCircle>();
			

			var lerp = Mathf.Max(0, (frame.Time.AsFloat - circle.ShrinkingStartTime.AsFloat) / circle.ShrinkingDurationTime.AsFloat);
			var diameter = Mathf.Lerp(circle.CurrentRadius.AsFloat, circle.TargetRadius.AsFloat, lerp) * 2f;
			var center = Vector2.Lerp(circle.CurrentCircleCenter.ToUnityVector2(), circle.TargetCircleCenter.ToUnityVector2(), lerp);
			
			//Debug.Log($"ShrinkingCircleMonoComponent -> HandleUpdateView {lerp} {diameter} {center}");
			
			transform.localScale = new Vector3(diameter, 1f, diameter);
			transform.position = new Vector3(center.x, transform.position.y, center.y);
		}
	}
}
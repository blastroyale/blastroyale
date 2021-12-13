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
			var shrinkingCircle = frame.GetSingleton<ShrinkingCircle>();

			var size = shrinkingCircle.CurrentRadius.AsFloat * 2f;
			transform.localScale = new Vector3(size, 1f, size);
		}
	}
}
using System;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.MonoComponent.Vfx
{
	/// <summary>
	/// Logic for updating collectable indicator vfx.
	/// </summary>
	public class CollectableIndicatorVfxMonoComponent : VfxMonoComponent
	{
		[SerializeField, Required] private Image _progressIndicator;

		private float _startTime;
		private float _endTime;

		private void OnEnable()
		{
			QuantumCallback.Subscribe<CallbackUpdateView>(this, UpdateView);
		}

		private void OnDisable()
		{
			QuantumCallback.UnsubscribeListener(this);
		}

		/// <summary>
		/// Initializes this VFX with the given <paramref name="entity"/>
		/// </summary>
		public void SetTime(float startTime, float endTime)
		{
			_startTime = startTime;
			_endTime = endTime;

			UpdateIndicator(QuantumRunner.Default.Game.Frames.Predicted.Time.AsFloat);
		}

		private void UpdateView(CallbackUpdateView callback)
		{
			UpdateIndicator(callback.Game.Frames.Predicted.Time.AsFloat);
		}

		private void UpdateIndicator(float currentTime)
		{
			_progressIndicator.fillAmount = Mathf.Lerp(0, 1, (currentTime - _startTime) / (_endTime - _startTime));
		}
	}
}
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
		private EntityRef _entity;

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
		public void SetTime(float startTime, float endTime, EntityRef entity)
		{
			_startTime = startTime;
			_endTime = endTime;
			_entity = entity;
			UpdateIndicator(QuantumRunner.Default.Game.Frames.Predicted.Time.AsFloat);
		}

		private void UpdateView(CallbackUpdateView callback)
		{
			if (callback.Game.Frames.Verified.Culled(_entity))
			{
				return;
			}
			
			UpdateIndicator(callback.Game.Frames.Predicted.Time.AsFloat);
		}

		private void UpdateIndicator(float currentTime)
		{
			_progressIndicator.fillAmount = Mathf.Lerp(0, 1, (currentTime - _startTime) / (_endTime - _startTime));
		}
	}
}
using DG.Tweening;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <summary>
	/// Responsible for triggering animation and effects for this weapon platform spawner entity view.
	/// </summary>
	public class CollectablePlatformSpawnerViewMonoComponent : EntityMainViewBase
	{
		[SerializeField] private TextMeshPro _text;
		[SerializeField] private Image _progressIndicator;
		
		protected override void OnInit()
		{
			base.OnInit();
			
			_text.text = "";
			_progressIndicator.fillAmount = 0f;
			
			EntityView.OnEntityDestroyed.AddListener(OnEntityDestroyed);
			QuantumCallback.Subscribe<CallbackUpdateView>(this, OnUpdateView);
		}

		private void OnEntityDestroyed(QuantumGame game)
		{
			QuantumCallback.UnsubscribeListener(this);
		}
		
		private void OnUpdateView(CallbackUpdateView callback)
		{
			var frame = callback.Game.Frames.Verified;
			var spawner = frame.Get<CollectablePlatformSpawner>(EntityRef);

			var remaining = spawner.NextSpawnTime.AsFloat - frame.Time.AsFloat;

			if (remaining > 0)
			{
				var intervalTime = spawner.IntervalTime.AsFloat;
				var normalizedValue = remaining / intervalTime;
				
				var sec = intervalTime * normalizedValue;
				
				_text.text = ((int)sec).ToString();	
				
				_progressIndicator.fillAmount = normalizedValue;
			}
			else
			{
				_text.text = "";
				_progressIndicator.fillAmount = 0f;
			}
		}
	}
}
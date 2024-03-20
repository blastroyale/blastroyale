using Quantum;
using Sirenix.OdinInspector;
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
		[SerializeField, Required] private TextMeshPro _text;
		[SerializeField, Required] private Image _progressIndicator;
	
		private Canvas _canvas;
		
		protected override void OnInit(QuantumGame game)
		{
			base.OnInit(game);
			_text.text = "";
			_progressIndicator.fillAmount = 0f;
			_canvas = GetComponentInChildren<Canvas>();
			QuantumCallback.Subscribe<CallbackUpdateView>(this, OnUpdateView, onlyIfActiveAndEnabled: true);
		}

		public override void SetCulled(bool culled)
		{
			if (culled)
			{
				_canvas?.gameObject?.SetActive(false);
			}

			base.SetCulled(culled);
		}

		private unsafe new void OnUpdateView(CallbackUpdateView callback)
		{
			var frame = callback.Game.Frames.Verified;

			if (Culled)
			{
				return;
			}

			var spawner = frame.Unsafe.GetPointer<CollectablePlatformSpawner>(EntityRef);
			var remaining = spawner->NextSpawnTime.AsFloat - frame.Time.AsFloat;

			if (remaining > 0 && !spawner->Disabled)
			{
				_canvas?.gameObject.SetActive(true);
				var intervalTime = spawner->IntervalTime.AsFloat;
				var normalizedValue = remaining / intervalTime;
				var sec = intervalTime * normalizedValue;

				_text.text = ((int) sec).ToString();
				_progressIndicator.fillAmount = normalizedValue;
			}
			else
			{
				_canvas?.gameObject?.SetActive(false);
				_text.text = "";
				_progressIndicator.fillAmount = 0f;
			}
		}
	}
}
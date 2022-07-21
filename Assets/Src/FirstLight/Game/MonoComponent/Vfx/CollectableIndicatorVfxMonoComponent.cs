using System.Collections;
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
		
		private EntityRef _entity;
		private Coroutine _coroutine;
		
		/// <summary>
		/// Initializes this VFX with the given <paramref name="entity"/>
		/// </summary>
		public void Init(EntityRef entity, float totalCollectionTime)
		{
			if(_coroutine != null)
			{
				StopCoroutine(_coroutine);
				_coroutine = null;
			}
			
			_entity = entity;
			_coroutine = StartCoroutine(ProgressBar(totalCollectionTime));
			
			QuantumEvent.Subscribe<EventOnLocalStoppedCollecting>(this, OnLocalStoppedCollecting);
			QuantumEvent.Subscribe<EventOnCollectableCollected>(this, OnCollectableCollected);
		}

		protected override void OnSpawned()
		{
			_progressIndicator.fillAmount = 0f;
		}

		protected override void OnDespawned()
		{
			if(_coroutine != null)
			{
				StopCoroutine(_coroutine);
				_coroutine = null;
			}

			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnCollectableCollected(EventOnCollectableCollected callback)
		{
			if (callback.CollectableEntity != _entity)
			{
				return;
			}
			
			_progressIndicator.fillAmount = 1f;
			
			Despawn();
		}

		private void OnLocalStoppedCollecting(EventOnLocalStoppedCollecting callback)
		{
			if (callback.CollectableEntity != _entity)
			{
				return;
			}
			
			Despawn();
		}

		private IEnumerator ProgressBar(float totalTime)
		{
			var startTime = Time.time;

			while (Time.time < startTime + totalTime)
			{
				_progressIndicator.fillAmount = Mathf.Lerp(0, 1, (Time.time - startTime) / totalTime);

				yield return null;
			}

			_progressIndicator.fillAmount = 1f;
			_coroutine = null;
		}
	}
}
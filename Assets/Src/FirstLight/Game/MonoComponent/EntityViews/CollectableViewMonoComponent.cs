using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Ids;
using FirstLight.Game.MonoComponent.Vfx;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <summary>
	/// Responsible for triggering animation and effects for this collectable pickup entity view.
	/// </summary>
	public class CollectableViewMonoComponent : EntityMainViewBase
	{
		[SerializeField, Required] private Transform _collectableIndicatorAnchor;
		[SerializeField, Required] private Animation _animation;
		[SerializeField, Required] private AnimationClip _spawnClip;
		[SerializeField, Required] private AnimationClip _idleClip;
		[SerializeField, Required] private AnimationClip _collectClip;

		private IMatchServices _matchServices;

		private readonly Dictionary<EntityRef, CollectingData> _collectors = new();
		private EntityRef _displayedCollector;
		private CollectableIndicatorVfxMonoComponent _collectingVfx;

		private void OnValidate()
		{
			_animation = _animation ? _animation : GetComponent<Animation>();
		}

		protected override void OnAwake()
		{
			_matchServices = MainInstaller.Resolve<IMatchServices>();

			QuantumEvent.Subscribe<EventOnStartedCollecting>(this, OnStartedCollecting);
			QuantumEvent.Subscribe<EventOnStoppedCollecting>(this, OnStoppedCollecting);
			QuantumEvent.Subscribe<EventOnCollectableCollected>(this, OnCollectableCollected);

			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);
		}

		protected override void OnInit(QuantumGame game)
		{
			base.OnInit(game);

			EntityView.OnEntityDestroyed.AddListener(OnEntityDestroyed);
			PlayAnimation(_spawnClip);

			this.LateCoroutineCall(_animation.clip.length, () => PlayAnimation(_idleClip));
		}


		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			RefreshVfx(next);
		}

		private void OnStartedCollecting(EventOnStartedCollecting callback)
		{
			if (EntityView.EntityRef != callback.CollectableEntity) return;

			var startTime = callback.Game.Frames.Predicted.Time.AsFloat;
			var endTime = callback.Collectable.CollectorsEndTime[callback.Player].AsFloat;

			_collectors.Add(callback.PlayerEntity, new CollectingData(startTime, endTime));

			RefreshVfx(_matchServices.SpectateService.SpectatedPlayer.Value);
		}

		private void OnStoppedCollecting(EventOnStoppedCollecting callback)
		{
			if (EntityView.EntityRef != callback.CollectableEntity) return;

			_collectors.Remove(callback.PlayerEntity);
			RefreshVfx(_matchServices.SpectateService.SpectatedPlayer.Value);
		}

		private void OnCollectableCollected(EventOnCollectableCollected callback)
		{
			if (EntityView.EntityRef != callback.CollectableEntity) return;

			_collectors.Remove(callback.PlayerEntity);
			RefreshVfx(_matchServices.SpectateService.SpectatedPlayer.Value);
		}

		private void RefreshVfx(SpectatedPlayer spectatedPlayer)
		{
			var hasVfx = _displayedCollector != EntityRef.None;
			_displayedCollector = _collectors.TryGetValue(spectatedPlayer.Entity, out var collectingData)
				                      ? spectatedPlayer.Entity
				                      : EntityRef.None;


			if (_displayedCollector == EntityRef.None && hasVfx)
			{
				_collectingVfx.Despawn();
				_collectingVfx = null;
			}
			else if (_displayedCollector != EntityRef.None && !hasVfx)
			{
				_collectingVfx =
					(CollectableIndicatorVfxMonoComponent) Services.VfxService.Spawn(VfxId.CollectableIndicator);
				var collectablePosition = _collectableIndicatorAnchor.position;
				var position = new Vector3(collectablePosition.x,
				                           spectatedPlayer.Transform.position.y +
				                           GameConstants.Visuals.RADIAL_LOCAL_POS_OFFSET, collectablePosition.z);

				_collectingVfx.transform.SetPositionAndRotation(position, Quaternion.identity);
				_collectingVfx.SetTime(collectingData.StartTime, collectingData.EndTime);
			}

			if (_displayedCollector != EntityRef.None)
			{
				_collectingVfx!.SetTime(collectingData.StartTime, collectingData.EndTime);
			}
		}

		private void OnEntityDestroyed(QuantumGame game)
		{
			if (_collectingVfx != null)
			{
				_collectingVfx.Despawn();
			}
			
			transform.parent = null;

			QuantumEvent.UnsubscribeListener(this);
			_matchServices?.SpectateService.SpectatedPlayer.StopObserving(OnSpectatedPlayerChanged);
			PlayAnimation(_collectClip);

			this.LateCoroutineCall(_animation.clip.length, () => { Destroy(gameObject); });
		}

		private void PlayAnimation(AnimationClip clip)
		{
			_animation.clip = clip;

			_animation.Rewind();
			_animation.Play();
		}

		private struct CollectingData
		{
			public float StartTime;
			public float EndTime;

			public CollectingData(float startTime, float endTime)
			{
				StartTime = startTime;
				EndTime = endTime;
			}
		}
	}
}

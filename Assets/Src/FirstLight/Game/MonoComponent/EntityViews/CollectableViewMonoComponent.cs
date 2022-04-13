using System;
using System.Collections;
using System.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Game.MonoComponent.Vfx;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <summary>
	/// Responsible for triggering animation and effects for this collectable pickup entity view.
	/// </summary>
	public class CollectableViewMonoComponent : EntityMainViewBase
	{
		[SerializeField] private Transform _collectableIndicatorAnchor;
		[SerializeField] private AudioId _collectSfxId;
		[SerializeField] private Animation _animation;
		[SerializeField] private AnimationClip _spawnClip;
		[SerializeField] private AnimationClip _idleClip;
		[SerializeField] private AnimationClip _collectClip;

		private void OnValidate()
		{
			_animation = _animation ? _animation : GetComponent<Animation>();
		}

		protected override void OnAwake()
		{
			QuantumEvent.Subscribe<EventOnLocalStartedCollecting>(this, OnLocalStartedCollecting);
			QuantumEvent.Subscribe<EventOnLocalCollectableCollected>(this, OnLocalCollectableCollected);
		}

		protected override void OnInit(QuantumGame game)
		{
			base.OnInit(game);
			
			EntityView.OnEntityDestroyed.AddListener(OnEntityDestroyed);
			PlayAnimation(_spawnClip);

			this.LateCoroutineCall(_animation.clip.length, () => PlayAnimation(_idleClip));
		}

		private void OnLocalCollectableCollected(EventOnLocalCollectableCollected callback)
		{
			if (EntityView.EntityRef != callback.CollectableEntity)
			{
				return;
			}
			
			Services.AudioFxService.PlayClip3D(_collectSfxId, transform.position);
		}

		private void OnLocalStartedCollecting(EventOnLocalStartedCollecting callback)
		{
			if (EntityView.EntityRef != callback.CollectableEntity)
			{
				return;
			}
			
			
			var entityView = EntityViewUpdaterService.GetManualView(callback.PlayerEntity);
			var vfx = Services.VfxService.Spawn(VfxId.CollectableIndicator) as CollectableIndicatorVfxMonoComponent;
			var collectablePosition = _collectableIndicatorAnchor.position;
			var position = new Vector3(collectablePosition.x,entityView.transform.position.y + GameConstants.RADIAL_LOCAL_POS_OFFSET, collectablePosition.z);
			var frame = callback.Game.Frames.Verified;
			var totalTime = callback.Collectable.CollectorsEndTime[callback.Player].AsFloat - frame.Time.AsFloat;
			
			vfx.transform.SetPositionAndRotation(position, Quaternion.identity);
			vfx.Init(EntityView.EntityRef, totalTime);
		}
		
		private void OnEntityDestroyed(QuantumGame game)
		{
			transform.parent = null;
			
			QuantumEvent.UnsubscribeListener(this);
			PlayAnimation(_collectClip);
			
			this.LateCoroutineCall(_animation.clip.length, () => { Destroy(gameObject); });
		}
		
		private void PlayAnimation(AnimationClip clip)
		{
			_animation.clip = clip;
			
			_animation.Rewind();
			_animation.Play();
		}
		
		
	}
}
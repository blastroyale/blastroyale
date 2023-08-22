using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.MonoComponent.Vfx;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Collections;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <summary>
	/// Responsible for triggering animation and effects for this collectable pickup entity view.
	/// </summary>
	public class CollectableViewMonoComponent : EntityMainViewBase
	{
		//private const string CLIP_SPAWN = "spawn";
		private const string CLIP_IDLE = "idle";
		private const string CLIP_COLLECT = "collect";

		/// <summary>
		/// We need this correction because we attempt to size the quantum collider with the size of the indicator.
		/// Both are using the same measurement but the indicator might be rendering a bit more/less so this correction
		/// is to ensure its precise.
		/// </summary>
		private const float RADIUS_CORRECTION = 0.9f;
		
		[SerializeField, Required] private Transform _collectableIndicatorAnchor;
		[SerializeField, Required] private Animation _animation;

		//[SerializeField, Required] private AnimationClip _spawnClip;
		[SerializeField, Required] private AnimationClip _idleClip;
		[SerializeField] private AnimationClip _collectClip;
		[SerializeField] private Transform _pickupCircle;
		[SerializeField] private bool _spawnAnim = true;

		private readonly Dictionary<EntityRef, CollectingData> _collectors = new();
		private EntityRef _displayedCollector;
		private CollectableIndicatorVfxMonoComponent _collectingVfx;

		/// <summary>
		/// Sets the visibility of the static Pickup Circle to provided value
		/// </summary>
		public void SetPickupCircleVisibility(bool value)
		{
			_pickupCircle.gameObject.SetActive(value);
		}

		private void OnValidate()
		{
			_animation = _animation ? _animation : GetComponent<Animation>();
		}

		private void OnDestroy()
		{
			MatchServices?.SpectateService?.SpectatedPlayer?.StopObserving(OnSpectatedPlayerChanged);
		}

		protected override void OnAwake()
		{
			MatchServices = MainInstaller.Resolve<IMatchServices>();

			//_animation.AddClip(_spawnClip, CLIP_SPAWN);
			_animation.AddClip(_idleClip, CLIP_IDLE);
			if (_collectClip != null)
			{
				_animation.AddClip(_collectClip, CLIP_COLLECT);
			}

			QuantumEvent.Subscribe<EventOnStartedCollecting>(this, OnStartedCollecting);
			QuantumEvent.Subscribe<EventOnStoppedCollecting>(this, OnStoppedCollecting);
			QuantumEvent.Subscribe<EventOnCollectableCollected>(this, OnCollectableCollected);

			MatchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);
			//disable mesh renderers before the object has been properly placed
			foreach (var ren in GetComponentsInChildren<MeshRenderer>())
			{
				ren.enabled = false;
			}
		}

		protected override void OnInit(QuantumGame game)
		{
			base.OnInit(game);


			var frame = game.Frames.Verified;
#if DEBUG_BOTS
			AddDebugCylinder(frame);
#endif
			//re enable renderers after the object has been properly placed
			foreach (var ren in GetComponentsInChildren<MeshRenderer>())
			{
				ren.enabled = true;
			}

			if (frame.TryGet<Collectable>(EntityView.EntityRef, out var collectable) &&
				collectable.PickupRadius > FP._0)
			{
				var radiusCorrected = collectable.PickupRadius.AsFloat * RADIUS_CORRECTION;
				_pickupCircle.localScale =
					new Vector3(radiusCorrected, radiusCorrected, 1f);
				_pickupCircle.position = _collectableIndicatorAnchor.position +
					new Vector3(0f, GameConstants.Visuals.RADIAL_LOCAL_POS_OFFSET, 0f);

				//animates between the spawning position to the display position if they are different
				var originPos = collectable.OriginPosition.ToUnityVector3();
				var displayPos = frame.Get<Transform3D>(EntityView.EntityRef).Position.ToUnityVector3();

				if (originPos != displayPos && _spawnAnim)
				{
					StartCoroutine(GoToPoint(Constants.CONSUMABLE_POPOUT_DURATION.AsFloat, originPos, displayPos));
				}
			}

			if (frame.TryGet<Consumable>(EntityView.EntityRef, out var consumable))
			{
				RefreshIndicator(frame, MatchServices.GetSpectatedPlayer().Entity, consumable.ConsumableType);
				switch (consumable.ConsumableType)
				{
					case ConsumableType.Health:
						QuantumEvent.Subscribe<EventOnHealthChanged>(this,
							c => RefreshIndicator(c.Game.Frames.Verified, c.Entity, ConsumableType.Health));
						break;
					case ConsumableType.Ammo:
						QuantumEvent.Subscribe<EventOnPlayerAmmoChanged>(this,
							c => RefreshIndicator(c.Game.Frames.Verified, c.Entity, ConsumableType.Ammo));
						break;
					case ConsumableType.Shield:
						QuantumEvent.Subscribe<EventOnShieldChanged>(this,
							c => RefreshIndicator(c.Game.Frames.Verified, c.Entity, ConsumableType.Shield));
						break;
				}
			}

			// Animation of a spawning of collectable is disabled. We can enable it again if we need it
			//_animation.Play(CLIP_SPAWN);
			//_animation.PlayQueued(CLIP_IDLE, QueueMode.CompleteOthers, PlayMode.StopAll);

			_animation.Play(CLIP_IDLE);
		}

		private void RefreshIndicator(Frame f, EntityRef entity, ConsumableType type)
		{
			if (!entity.IsValid || !f.Exists(entity) || !MatchServices.IsSpectatingPlayer(entity)) return;

			var stats = f.Get<Stats>(entity);
			_pickupCircle.gameObject.SetActive(!stats.IsConsumableStatFilled(type));
		}

		private IEnumerator GoToPoint(float moveTime, Vector3 startPos, Vector3 endPos)
		{
			if (Culled)
			{
				transform.position = endPos;
				yield break;
			}

			var startTime = Time.time;
			var startScale = transform.localScale;
			while (Time.time <= startTime + moveTime)
			{
				var progress = (Time.time - startTime) / moveTime;
				var scale = Vector3.Lerp(Vector3.zero, startScale,
					progress * 2); // scale should finish twice as fast as position
				var pos = Vector3.Lerp(startPos, endPos, progress);
				pos.y += Mathf.Sin(Mathf.PI * progress) * GameConstants.Visuals.CHEST_CONSUMABLE_POPOUT_HEIGHT;

				transform.position = pos;
				transform.localScale = scale;
				yield return new WaitForEndOfFrame();
			}

			transform.position = endPos;
			transform.localScale = startScale;
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			RefreshVfx(next);

			var f = QuantumRunner.Default.Game.Frames.Verified;
			if (f.TryGet<Consumable>(EntityView.EntityRef, out var consumable))
			{
				RefreshIndicator(f, next.Entity, consumable.ConsumableType);
			}
		}

		private void OnStartedCollecting(EventOnStartedCollecting callback)
		{
			if (EntityView.EntityRef != callback.CollectableEntity) return;

			if (Culled)
			{
				return;
			}

			var startTime = callback.Game.Frames.Predicted.Time.AsFloat;
			var endTime = callback.Collectable.CollectorsEndTime[callback.Player].AsFloat;
			var isLargeCollectable = callback.Collectable.PickupRadius > FP._1_25;

			_collectors[callback.PlayerEntity] = new CollectingData(startTime, endTime, isLargeCollectable);

			RefreshVfx(MatchServices.SpectateService.SpectatedPlayer.Value);
		}

		private void OnStoppedCollecting(EventOnStoppedCollecting callback)
		{
			if (EntityView.EntityRef != callback.CollectableEntity) return;

			if (Culled)
			{
				return;
			}

			_collectors.Remove(callback.PlayerEntity);
			RefreshVfx(MatchServices.SpectateService.SpectatedPlayer.Value);
		}

		private void OnCollectableCollected(EventOnCollectableCollected callback)
		{
			if (EntityView.EntityRef != callback.CollectableEntity) return;

			_collectors.Remove(callback.PlayerEntity);

			if (_collectingVfx != null)
			{
				_collectingVfx.Despawn();
			}

			transform.parent = null;

			QuantumEvent.UnsubscribeListener(this);
			MatchServices.SpectateService.SpectatedPlayer.StopObserving(OnSpectatedPlayerChanged);

			// Animation of a collected collectable is disabled. We can enable it again if we need it
			// Enabling the animation again because something is disabling it and we couldn't find what. For time sake we keep this quick fix.
			if (_collectClip != null)
			{
				_animation.enabled = true;
				_animation.Play(CLIP_COLLECT, PlayMode.StopAll);
				this.LateCoroutineCall(_collectClip.length, () => { Destroy(gameObject); });
			}
			else
			{
				Destroy(gameObject);
			}
		}

		private void RefreshVfx(SpectatedPlayer spectatedPlayer)
		{
			// Upon reconnections, if somebody is collecting an item, OnStartedCollecting event can fire
			// before SpectateService.OnMatchStarted fires, so for a frame the player might be invalid.
			if (spectatedPlayer.Transform == null) return;

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
				var vfxId = collectingData.IsLargeCollectable
					? VfxId.CollectableIndicatorLarge
					: VfxId.CollectableIndicator;
				_collectingVfx = (CollectableIndicatorVfxMonoComponent) Services.VfxService.Spawn(vfxId);
				var position = _collectableIndicatorAnchor.position +
					new Vector3(0f, GameConstants.Visuals.RADIAL_LOCAL_POS_OFFSET, 0f);

				_collectingVfx.transform.SetPositionAndRotation(position, Quaternion.AngleAxis(145, Vector3.up));
				_collectingVfx.transform.localScale = new Vector3(_pickupCircle.localScale.x * 2.5f, 1f,
					_pickupCircle.localScale.y * 2.5f);
				_collectingVfx.SetTime(collectingData.StartTime, collectingData.EndTime, EntityRef);
			}

			if (_displayedCollector != EntityRef.None)
			{
				_collectingVfx!.SetTime(collectingData.StartTime, collectingData.EndTime, EntityRef);
			}
		}

		private void AddDebugCylinder(Frame f)
		{
			var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
			Destroy(obj.GetComponent<CapsuleCollider>());
			obj.transform.parent = gameObject.transform;
			obj.transform.localScale = new Vector3(1, 10, 1);
			obj.transform.localPosition = new Vector3(0, 5, 0);

			var rend = obj.GetComponent<Renderer>();
			rend.material = new Material(Shader.Find("Unlit/Color"));
			var color = new Color(0.39f, 0.39f, 0.39f);
			if (f.TryGet<Collectable>(EntityView.EntityRef, out var collectable))
			{
				if (collectable.GameId.IsInGroup(GameIdGroup.Equipment))
				{
					color = new Color(0f, 0.42f, 0.09f);
				}
				else if (collectable.GameId == GameId.Health)
				{
					color = new Color(0.42f, 0.02f, 0.02f);
				}
				else if (collectable.GameId.IsInGroup(GameIdGroup.Ammo))
				{
					color = new Color(0.54f, 0.47f, 0.01f);
				}
			}

			rend.material.SetColor("_Color", color);
		}

		public override void SetCulled(bool culled)
		{
			_animation.enabled = !culled;
			base.SetCulled(culled);
		}

		private struct CollectingData
		{
			public float StartTime;
			public float EndTime;
			public bool IsLargeCollectable;

			public CollectingData(float startTime, float endTime, bool isLargeCollectable)
			{
				StartTime = startTime;
				EndTime = endTime;
				IsLargeCollectable = isLargeCollectable;
			}
		}
	}
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Game.MonoComponent.Match;
using FirstLight.Game.MonoComponent.Vfx;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using UnityEngine;
using LayerMask = UnityEngine.LayerMask;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <inheritdoc/>
	/// <remarks>
	/// Responsible to play and act on Player's visual feedback. From animations to triggered VFX
	/// </remarks>
	public class PlayerCharacterViewMonoComponent : AvatarViewBase
	{

		[SerializeField] private MatchCharacterViewMonoComponent _characterView;
		[SerializeField] private AdventureVfxSpawnerMonoComponent[] _footstepVfxSpawners;
		
		public Transform RootTransform;
		
		private Vector3 _lastPosition;
		private Collider[] _colliders;

		private Coroutine _attackHideRendererCoroutine;
		
		public HashSet<VisibilityVolumeMonoComponent> CollidingVisibilityVolumes { get; private set; }

		/// <summary>
		/// Indicates if this is the local player
		/// </summary>
		public bool IsLocalPlayer
		{
			get;
			private set;
		}

		/// <summary>
		/// Requests the <see cref="PlayerRef"/> of this player
		/// </summary>
		public PlayerRef PlayerRef
		{
			get;
			private set;
		}

		private static class PlayerFloats
		{
			public static readonly AnimatorWrapper.Float DirX = new("DirX");
			public static readonly AnimatorWrapper.Float DirY = new("DirY");
		}

		protected override void OnAwake()
		{
			base.OnAwake();

			CollidingVisibilityVolumes = new HashSet<VisibilityVolumeMonoComponent>();
			QuantumEvent.Subscribe<EventOnPlayerAlive>(this, HandleOnPlayerAlive);
			QuantumEvent.Subscribe<EventOnPlayerAttack>(this, HandleOnPlayerAttack);
			QuantumEvent.Subscribe<EventOnPlayerSpecialUsed>(this, HandleOnPlayerSpecialUsed);
			QuantumEvent.Subscribe<EventOnAirstrikeUsed>(this, HandleOnAirstrikeUsed);
			QuantumEvent.Subscribe<EventOnPlayerSpawned>(this, HandleOnPlayerSpawned);
			QuantumEvent.Subscribe<EventOnCollectableCollected>(this, HandleOnCollectableCollected);
			QuantumEvent.Subscribe<EventOnStunGrenadeUsed>(this, HandleOnStunGrenadeUsed);
			QuantumEvent.Subscribe<EventOnGrenadeUsed>(this, HandleOnGrenadeUsed);
			QuantumEvent.Subscribe<EventOnSkyBeamUsed>(this, HandleOnSkyBeamUsed);
			QuantumEvent.Subscribe<EventOnShieldedChargeUsed>(this, HandleOnShieldedChargeUsed);
			QuantumEvent.Subscribe<EventOnGameEnded>(this, HandleOnGameEnded);
			QuantumEvent.Subscribe<EventOnPlayerWeaponChanged>(this, HandlePlayerWeaponChanged);
			QuantumEvent.Subscribe<EventOnPlayerGearChanged>(this, HandlePlayerGearChanged);
			QuantumEvent.Subscribe<EventOnPlayerSkydiveLand>(this, HandlePlayerSkydiveLand);
			QuantumEvent.Subscribe<EventOnPlayerSkydivePLF>(this, HandlePlayerSkydivePLF);
			QuantumCallback.Subscribe<CallbackUpdateView>(this, HandleUpdateView);
			QuantumEvent.Subscribe<EventOnRadarUsed>(this, HandleOnRadarUsed);
		}

		private void OnDestroy()
		{
			if (_attackHideRendererCoroutine != null)
			{
				Services.CoroutineService.StopCoroutine(_attackHideRendererCoroutine);
			}
			
			Services.MessageBrokerService.UnsubscribeAll(this);
		}
		
		public override void SetCulled(bool culled)
		{
			if (culled)
			{
				foreach (var col in _colliders)
				{
					col.enabled = false;
				}
			} else
			{
				foreach (var col in _colliders)
				{
					col.enabled = true;
				}
			}
			base.SetCulled(culled);
		}
		
		/// <inheritdoc />
		public override void SetRenderContainerVisible(bool active)
		{
			if (!active && Visible)
			{
				_characterView.HideAllEquipment();
			} else if (active && !Visible)
			{
				_characterView.ShowAllEquipment();
			}
			
			base.SetRenderContainerVisible(active);
			
			for (int i = 0; i < _footstepVfxSpawners.Length; i++)
			{
				_footstepVfxSpawners[i].CanSpawnVfx = active;
			}
		}

		public void SetPlayerSilhouetteVisible(bool visible)
		{
			RenderersContainerProxy.SetRenderersLayer(LayerMask.NameToLayer(visible ? "Default Silhouette" : "Default"));

			for (int i = 0; i < _footstepVfxSpawners.Length; i++)
			{
				_footstepVfxSpawners[i].CanSpawnVfx = visible;
			}
		}
		
		/// <summary>
		/// Set's the player animation moving state based on the given <paramref name="isAiming"/> state
		/// </summary>
		public void SetMovingState(bool isAiming)
		{
			AnimatorWrapper.SetBool(Bools.Aim, isAiming);
		}

		protected override void OnInit(QuantumGame game)
		{
			base.OnInit(game);

			var frame = game.Frames.Verified;
			
			PlayerRef = frame.Get<PlayerCharacter>(EntityRef).Player;
			IsLocalPlayer = game.PlayerIsLocal(PlayerRef);
			
			if (Services.NetworkService.IsJoiningNewMatch)
			{
				var isSkydiving = frame.Get<AIBlackboardComponent>(EntityView.EntityRef).GetBoolean(frame, Constants.IsSkydiving);

				if (isSkydiving)
				{
					AnimatorWrapper.SetBool(Bools.Flying, frame.Context.GameModeConfig.SkydiveSpawn);
				}
				else
				{
					AnimatorWrapper.SetTrigger(EntityView.EntityRef.IsAlive(frame) ? Triggers.Spawn : Triggers.Die);	
				}
			}
			else
			{
				AnimatorWrapper.SetBool(Bools.Flying, false);
				
				if (!EntityView.EntityRef.IsAlive(frame))
				{
					AnimatorWrapper.SetTrigger(Triggers.Die);
				}
			}

			_colliders = GetComponentsInChildren<Collider>();
		}
		
		protected override void OnAvatarEliminated(QuantumGame game)
		{
			if (_attackHideRendererCoroutine != null)
			{
				Services.CoroutineService.StopCoroutine(_attackHideRendererCoroutine);
			}
			
			base.OnAvatarEliminated(game);
		}

		private void TryStartAttackWithinVisVolume()
		{
			if (EntityRef == MatchServices.SpectateService.SpectatedPlayer.Value.Entity || 
			    CollidingVisibilityVolumes.Count == 0)
			{
				return;
			}
			
			if (_attackHideRendererCoroutine != null)
			{
				Services.CoroutineService.StopCoroutine(_attackHideRendererCoroutine);
			}
				
			_attackHideRendererCoroutine = Services.CoroutineService.StartCoroutine(AttackWithinVisVolumeCoroutine());
		}

		private IEnumerator AttackWithinVisVolumeCoroutine()
		{
			SetPlayerSilhouetteVisible(true);

			yield return new WaitForSeconds(GameConstants.Visuals.GAMEPLAY_POST_ATTACK_HIDE_DURATION);
			
			if (CollidingVisibilityVolumes.Count > 0)
			{
				var visVolumeHasSpectatedPlayer = CollidingVisibilityVolumes.Any(visVolume => visVolume.VolumeHasSpectatedPlayer());
				SetPlayerSilhouetteVisible(visVolumeHasSpectatedPlayer);
			}
		}

		private void HandleOnStunGrenadeUsed(EventOnStunGrenadeUsed callback)
		{
			if (callback.HazardData.Attacker != EntityView.EntityRef)
			{
				return;
			}

			var time = callback.Game.Frames.Verified.Time;
			var targetPosition = callback.TargetPosition.ToUnityVector3();

			HandleParabolicUsed(callback.HazardData.EndTime,
			                    time, targetPosition, VfxId.GrenadeStunParabolic, VfxId.ImpactGrenadeStun);

			var vfx = Services.VfxService.Spawn(VfxId.SpecialReticule) as SpecialReticuleVfxMonoComponent;
			
			var vfxTime = Mathf.Max(0, (callback.HazardData.EndTime - time).AsFloat);

			vfx.SetTarget(targetPosition, callback.HazardData.Radius.AsFloat,vfxTime);
		}

		private void HandleOnGrenadeUsed(EventOnGrenadeUsed callback)
		{
			if (callback.HazardData.Attacker != EntityView.EntityRef)
			{
				return;
			}

			var time = callback.Game.Frames.Verified.Time;
			var targetPosition = callback.TargetPosition.ToUnityVector3();

			HandleParabolicUsed(callback.HazardData.EndTime,
			                    time, targetPosition, VfxId.GrenadeParabolic, VfxId.ImpactGrenade);

			var vfx = Services.VfxService.Spawn(VfxId.SpecialReticule) as SpecialReticuleVfxMonoComponent;

			var vfxTime = Mathf.Max(0, (callback.HazardData.EndTime - time).AsFloat);

			vfx.SetTarget(targetPosition, callback.HazardData.Radius.AsFloat,vfxTime);
		}

		private async void HandleParabolicUsed(FP launchTime, FP frameTime, Vector3 targetPosition,
		                                       VfxId parabolicVfxId, VfxId impactVfxId)
		{
			var flyTime = (launchTime - frameTime).AsFloat;

			if (flyTime < 0)
			{
				return;
			}

			var parabolic = Services.VfxService.Spawn(parabolicVfxId) as ParabolicVfxMonoComponent;

			parabolic.transform.position = transform.position;

			parabolic.StartParabolic(targetPosition, flyTime);

			await Task.Delay((int) (flyTime * 1000));

			if (parabolic.IsDestroyed())
			{
				return;
			}

			Services.VfxService.Spawn(impactVfxId).transform.position = targetPosition;
		}

		private void HandleOnCollectableCollected(EventOnCollectableCollected callback)
		{
			if (EntityView.EntityRef != callback.PlayerEntity || callback.CollectableId != GameId.Health)
			{
				return;
			}

			var vfx = Services.VfxService.Spawn(VfxId.StatusFxHeal).transform;

			vfx.SetParent(transform);
			vfx.localPosition = Vector3.zero;
			vfx.localScale = Vector3.one;
			vfx.localRotation = Quaternion.identity;
		}

		private void HandleOnPlayerAlive(EventOnPlayerAlive callback)
		{
			if (callback.Entity != EntityView.EntityRef)
			{
				return;
			}

			AnimatorWrapper.Enabled = true;

			AnimatorWrapper.SetTrigger(Triggers.Revive);
			RenderersContainerProxy.SetRendererState(true);
		}

		private void HandleOnPlayerSpawned(EventOnPlayerSpawned callback)
		{
			if (callback.Entity != EntityView.EntityRef)
			{
				return;
			}

			if (callback.HasRespawned)
			{
				AnimatorWrapper.SetTrigger(Triggers.Revive);
			}
			
			RenderersContainerProxy.SetRendererState(false);
			RigidbodyContainerMonoComponent.SetState(false);
		}

		private void HandleOnPlayerAttack(EventOnPlayerAttack callback)
		{
			if (callback.PlayerEntity != EntityRef)
			{
				return;
			}

			AnimatorWrapper.SetTrigger(Triggers.Shoot);
			TryStartAttackWithinVisVolume();
		}

		private void HandleOnPlayerSpecialUsed(EventOnPlayerSpecialUsed callback)
		{
			if (callback.Entity != EntityView.EntityRef)
			{
				return;
			}
	
			AnimatorWrapper.SetTrigger(Triggers.Special);
			TryStartAttackWithinVisVolume();
		}

		private void HandleOnGameEnded(EventOnGameEnded callback)
		{
			if (EntityView.EntityRef != callback.EntityLeader)
			{
				AnimatorWrapper.SetBool(Bools.Stun, true);
			}
			else
			{
				AnimatorWrapper.SetBool(Bools.Aim, false);
				AnimatorWrapper.SetTrigger(Triggers.Victory);
			}
		}

		private async void HandlePlayerWeaponChanged(EventOnPlayerWeaponChanged callback)
		{
			if (EntityView.EntityRef != callback.Entity)
			{
				return;
			}

			var weapons = await _characterView.EquipWeapon(callback.Weapon.GameId);
			
			var f = callback.Game.Frames.Verified;
			if (!f.Exists(EntityView.EntityRef))
			{
				return;
			}

			for (var i = 0; i < weapons.Count; i++)
			{
				var components = weapons[i].GetComponents<EntityViewBase>();

				foreach (var entityViewBase in components)
				{
					entityViewBase.SetEntityView(callback.Game, EntityView);
				}
			}
		}

		private async void HandlePlayerGearChanged(EventOnPlayerGearChanged callback)
		{
			if (callback.Entity != EntityView.EntityRef)
			{
				return;
			}

			await _characterView.EquipItem(callback.Gear.GameId);
		}

		private void HandleOnAirstrikeUsed(EventOnAirstrikeUsed callback)
		{
			if (callback.HazardData.Attacker != EntityView.EntityRef)
			{
				return;
			}

			var vfx = Services.VfxService.Spawn(VfxId.SpecialReticule) as SpecialReticuleVfxMonoComponent;
			var time = callback.Game.Frames.Verified.Time;
			var targetPosition = callback.TargetPosition.ToUnityVector3();

			vfx.SetTarget(targetPosition, callback.HazardData.Radius.AsFloat,
			              (callback.HazardData.EndTime - time).AsFloat);

			Services.VfxService.Spawn(VfxId.Airstrike).transform.position = targetPosition;

			HandleDelayedFX(callback.HazardData.Interval, targetPosition, VfxId.ImpactAirStrike);
		}

		private async void HandleDelayedFX(FP delayTime, Vector3 targetPosition, VfxId explosionVfxId)
		{
			await Task.Delay((int) (delayTime * 1000));

			Services.VfxService.Spawn(explosionVfxId).transform.position = targetPosition;
		}

		private void HandleOnShieldedChargeUsed(EventOnShieldedChargeUsed callback)
		{
			if (callback.Attacker != EntityView.EntityRef)
			{
				return;
			}

			var vfx = Services.VfxService.Spawn(VfxId.EnergyShield) as MutableTimeVfxMonoComponent;
			var vfxTransform = vfx.transform;
			vfxTransform.SetParent(transform);
			vfxTransform.localPosition = Vector3.zero;
			vfxTransform.localScale = Vector3.one;
			vfxTransform.localRotation = Quaternion.identity;

			vfx.StartDespawnTimer(callback.ChargeDuration.AsFloat);
		}

		private void HandleOnSkyBeamUsed(EventOnSkyBeamUsed callback)
		{
			if (callback.HazardData.Attacker != EntityView.EntityRef)
			{
				return;
			}

			var vfx = Services.VfxService.Spawn(VfxId.SpecialReticule) as SpecialReticuleVfxMonoComponent;
			var time = callback.Game.Frames.Verified.Time;
			var targetPosition = callback.TargetPosition.ToUnityVector3();

			vfx.SetTarget(targetPosition, callback.HazardData.Radius.AsFloat,
			              (callback.HazardData.EndTime - time).AsFloat);

			HandleDelayedFX(callback.HazardData.Interval - FP._0_50, targetPosition, VfxId.Skybeam);
		}
		
		private void HandleOnRadarUsed(EventOnRadarUsed callback)
		{
			if (callback.Player != PlayerRef)
			{
				return;
			}

			Services.VfxService.Spawn(VfxId.Radar).transform.position = transform.position;
		}

		private void HandleUpdateView(CallbackUpdateView callback)	
		{	
			const float speedThreshold = 0.5f; // unity units per second	
			var f = callback.Game.Frames.Predicted;
			if (!f.TryGet<AIBlackboardComponent>(EntityRef, out var bb))
			{
				return;
			}

			if (Culled)
			{
				return;
			}
				
			var currentPosition = transform.position;
			var deltaPosition = currentPosition - _lastPosition;
			deltaPosition.y = 0f; // falling doesn't count
			var sqrSpeed = (deltaPosition / f.DeltaTime.AsFloat).sqrMagnitude;
			var isMoving = sqrSpeed > speedThreshold * speedThreshold;
			var isAiming = bb.GetBoolean(f, Constants.IsAimPressedKey);
			AnimatorWrapper.SetBool(Bools.Move, isMoving);
			
			if (isMoving)
			{
				deltaPosition.Normalize();
				var localDeltaPosition = transform.InverseTransformDirection(deltaPosition);
				AnimatorWrapper.SetFloat(PlayerFloats.DirX, localDeltaPosition.x);
				AnimatorWrapper.SetFloat(PlayerFloats.DirY, localDeltaPosition.z);
			}
			else
			{
				AnimatorWrapper.SetFloat(PlayerFloats.DirX, 0f);
				AnimatorWrapper.SetFloat(PlayerFloats.DirY, 0f);
			}
			
			AnimatorWrapper.SetBool(Bools.Aim, isAiming);
			_lastPosition = currentPosition;
		}

		private void HandlePlayerSkydivePLF(EventOnPlayerSkydivePLF callback)
		{
			if (EntityView.EntityRef != callback.Entity)
			{
				return;
			}

			AnimatorWrapper.SetTrigger(Triggers.PLF);
		}

		private void HandlePlayerSkydiveLand(EventOnPlayerSkydiveLand callback)
		{
			if (EntityView.EntityRef != callback.Entity)
			{
				return;
			}

			AnimatorWrapper.SetBool(Bools.Flying, false);
			
			_characterView.DestroyItem(GameIdGroup.Glider);
		}
	}
}
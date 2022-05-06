using System.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Game.MonoComponent.Vfx;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using UnityEngine;
using UnityEngine.Serialization;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <inheritdoc/>
	/// <remarks>
	/// Responsible to play and act on Player's visual feedback. From animations to triggered VFX
	/// </remarks>
	public class PlayerCharacterViewMonoComponent : AvatarViewBase
	{
		[FormerlySerializedAs("_adventureCharacterView")] [SerializeField] private MatchCharacterViewMonoComponent _characterView;

		public Transform RootTransform;

		private Vector3 _lastPosition;

		/// <summary>
		/// Indicates if this is the local player
		/// </summary>
		public bool IsLocalPlayer
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

			QuantumEvent.Subscribe<EventOnPlayerAlive>(this, HandleOnPlayerAlive);
			QuantumEvent.Subscribe<EventOnPlayerAttack>(this, HandleOnPlayerAttack);
			QuantumEvent.Subscribe<EventOnSpecialUsed>(this, HandleOnSpecialUsed);
			QuantumEvent.Subscribe<EventOnAirstrikeUsed>(this, HandleOnAirstrikeUsed);
			QuantumEvent.Subscribe<EventOnPlayerSpawned>(this, HandleOnPlayerSpawned);
			QuantumEvent.Subscribe<EventOnConsumablePicked>(this, HandleOnConsumablePicked);
			QuantumEvent.Subscribe<EventOnStunGrenadeUsed>(this, HandleOnStunGrenadeUsed);
			QuantumEvent.Subscribe<EventOnGrenadeUsed>(this, HandleOnGrenadeUsed);
			QuantumEvent.Subscribe<EventOnSkyBeamUsed>(this, HandleOnSkyBeamUsed);
			QuantumEvent.Subscribe<EventOnShieldedChargeUsed>(this, HandleOnShieldedChargeUsed);
			QuantumEvent.Subscribe<EventOnGameEnded>(this, HandleOnGameEnded);
			QuantumEvent.Subscribe<EventOnPlayerWeaponChanged>(this, HandlePlayerWeaponChanged);
			QuantumEvent.Subscribe<EventOnPlayerSkydiveLand>(this, HandlePlayerSkydiveLand);
			QuantumEvent.Subscribe<EventOnPlayerSkydivePLF>(this, HandlePlayerSkydivePLF);
			QuantumCallback.Subscribe<CallbackUpdateView>(this, HandleUpdateView);
		}

		protected override void OnInit(QuantumGame game)
		{
			base.OnInit(game);

			var frame = game.Frames.Verified;

			AnimatorWrapper.SetBool(Bools.Flying, frame.RuntimeConfig.GameMode == GameMode.BattleRoyale);
			AnimatorWrapper.SetTrigger(frame.Has<DeadPlayerCharacter>(EntityView.EntityRef)
				                           ? Triggers.Die
				                           : Triggers.Spawn);
			IsLocalPlayer = frame.Context.IsLocalPlayer(frame.Get<PlayerCharacter>(EntityRef).Player);
		}

		/// <summary>
		/// Set's the player animation moving state based on the given <paramref name="isAiming"/> state
		/// </summary>
		public void SetMovingState(bool isAiming)
		{
			AnimatorWrapper.SetBool(Bools.Aim, isAiming);
		}

		protected override void OnAvatarEliminated(QuantumGame game)
		{
			base.OnAvatarEliminated(game);

			Services.AudioFxService.PlayClip3D(AudioId.ActorDeath01, transform.position);
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

			vfx.SetTarget(targetPosition, callback.HazardData.Radius.AsFloat,
			              (callback.HazardData.EndTime - time).AsFloat);
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

			vfx.SetTarget(targetPosition, callback.HazardData.Radius.AsFloat,
			              (callback.HazardData.EndTime - time).AsFloat);
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

		private void HandleOnConsumablePicked(EventOnConsumablePicked callback)
		{
			if (EntityView.EntityRef != callback.PlayerEntity ||
			    callback.Consumable.ConsumableType != ConsumableType.Health)
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
			Services.AudioFxService.PlayClip3D(AudioId.ActorSpawnEnd1, transform.position);
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

			Services.AudioFxService.PlayClip3D(AudioId.ActorSpawnStart1, transform.position);
			RenderersContainerProxy.SetRendererState(false);
			RigidbodyContainerMonoComponent.SetState(false);
		}

		private void HandleOnPlayerAttack(EventOnPlayerAttack evnt)
		{
			if (evnt.PlayerEntity != EntityRef)
			{
				return;
			}

			Services.AudioFxService.PlayClip3D(AudioId.ProjectileFired01, transform.position);
			AnimatorWrapper.SetTrigger(Triggers.Shoot);
		}

		private void HandleOnSpecialUsed(EventOnSpecialUsed evnt)
		{
			if (evnt.Entity != EntityView.EntityRef)
			{
				return;
			}

			AnimatorWrapper.SetTrigger(Triggers.Special);
		}

		private void HandleOnGameEnded(EventOnGameEnded callback)
		{
			if (EntityView.EntityRef != callback.EntityLeader)
			{
				AnimatorWrapper.SetBool(Bools.Stun, true);
			}
			else
			{
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

			for (var i = 0; i < weapons.Count; i++)
			{
				var components = weapons[i].GetComponents<EntityViewBase>();

				foreach (var entityViewBase in components)
				{
					entityViewBase.SetEntityView(callback.Game, EntityView);
				}
			}
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

		private void HandleUpdateView(CallbackUpdateView callback)
		{
			const float speedThreshold = 0.5f; // unity units per second

			var currentPosition = transform.position;
			var deltaPosition = currentPosition - _lastPosition;
			deltaPosition.y = 0f; // falling doesn't count
			var sqrSpeed = (deltaPosition / Time.deltaTime).sqrMagnitude;
			var isMoving = sqrSpeed > speedThreshold * speedThreshold;

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
		}
	}
}
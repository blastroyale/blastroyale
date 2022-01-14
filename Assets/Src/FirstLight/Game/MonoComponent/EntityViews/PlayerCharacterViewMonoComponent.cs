using System.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Game.MonoComponent.Vfx;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <inheritdoc/>
	/// <remarks>
	/// Responsible to play and act on Player's visual feedback. From animations to triggered VFX
	/// </remarks>
	public class PlayerCharacterViewMonoComponent : AvatarViewBase
	{
		[SerializeField] private AdventureCharacterViewMonoComponent _adventureCharacterView;
		[SerializeField] private bool _isDebugMode;
		
		public Transform RootTransform;
		
		private Vector3 _lastPosition;

		private static class PlayerFloats
		{
			public static readonly AnimatorWrapper.Float DirX = new AnimatorWrapper.Float("DirX");
			public static readonly AnimatorWrapper.Float DirY = new AnimatorWrapper.Float("DirY");
		}
		
		protected override void OnAwake()
		{
			base.OnAwake();
			
			QuantumEvent.Subscribe<EventOnPlayerAlive>(this, HandleOnPlayerAlive);
			QuantumEvent.Subscribe<EventOnPlayerDead>(this, HandleOnPlayerDead);
			QuantumEvent.Subscribe<EventOnAirstrikeUsed>(this, HandleOnAirstrikeUsed);
			QuantumEvent.Subscribe<EventOnPlayerSpawned>(this, HandleOnPlayerSpawned);
			QuantumEvent.Subscribe<EventOnConsumablePicked>(this, HandleOnConsumablePicked);
			QuantumEvent.Subscribe<EventOnStunGrenadeUsed>(this, HandleOnStunGrenadeUsed);
			QuantumEvent.Subscribe<EventOnGrenadeUsed>(this, HandleOnGrenadeUsed);
			QuantumEvent.Subscribe<EventOnSkyBeamUsed>(this, HandleOnSkyBeamUsed);
			QuantumEvent.Subscribe<EventOnShieldedChargeUsed>(this, HandleOnShieldedChargeUsed);
			QuantumEvent.Subscribe<EventOnGameEnded>(this, HandleOnGameEnded);
			QuantumEvent.Subscribe<EventOnPlayerWeaponChanged>(this, HandlePlayerWeaponChanged);
			QuantumCallback.Subscribe<CallbackUpdateView>(this, HandleUpdateView);
		}

		protected override void OnInit()
		{
			base.OnInit();

			var frame = QuantumRunner.Default.Game.Frames.Verified;
			
			AnimatorWrapper.SetTrigger(frame.Has<DeadPlayerCharacter>(EntityView.EntityRef) ? Triggers.Die : Triggers.Spawn);
			RenderersContainerProxy.SetRendererState(!frame.Has<SpawnPlayerCharacter>(EntityView.EntityRef));
		}

		/// <summary>
		/// Set's the player animation moving state based on the given <paramref name="isAiming"/> state
		/// </summary>
		public void SetMovingState(bool isAiming)
		{
			AnimatorWrapper.SetBool(Bools.Aim, isAiming);
		}

		protected override void OnPlayerDead(QuantumGame game)
		{
			base.OnPlayerDead(game);
			
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

			HandleParabolicUsed(callback.HazardData.EndTime - callback.HazardData.Interval, 
			                    time, targetPosition, VfxId.GrenadeStunParabolic, VfxId.ImpactGrenadeStun);
			
			var vfx = Services.VfxService.Spawn(VfxId.SpecialReticule) as SpecialReticuleVfxMonoComponent;
			
			vfx.SetTarget(targetPosition, callback.HazardData.Radius.AsFloat, (callback.HazardData.EndTime - time).AsFloat);
		}

		private void HandleOnGrenadeUsed(EventOnGrenadeUsed callback)
		{
			if (callback.HazardData.Attacker != EntityView.EntityRef)
			{
				return;
			}

			var time = callback.Game.Frames.Verified.Time;
			var targetPosition = callback.TargetPosition.ToUnityVector3();

			HandleParabolicUsed(callback.HazardData.EndTime - callback.HazardData.Interval, 
			                    time, targetPosition, VfxId.GrenadeParabolic, VfxId.ImpactGrenade);
			
			var vfx = Services.VfxService.Spawn(VfxId.SpecialReticule) as SpecialReticuleVfxMonoComponent;
			
			vfx.SetTarget(targetPosition, callback.HazardData.Radius.AsFloat, (callback.HazardData.EndTime - time).AsFloat);
		}

		private async void HandleParabolicUsed(FP launchTime, FP frameTime, Vector3 targetPosition, VfxId parabolicVfxId, VfxId impactVfxId)
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
			if (EntityView.EntityRef != callback.PlayerEntity || callback.Consumable.ConsumableType != ConsumableType.Health)
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

		private void HandleOnPlayerDead(EventOnPlayerDead callback)
		{
			if (callback.Entity != EntityView.EntityRef)
			{
				return;
			}
			
			OnPlayerDead(callback.Game);
		}

		private void HandleOnGameEnded(EventOnGameEnded callback)
		{
			if (EntityView.EntityRef != callback.WinnerMatchData.Data.Entity)
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
			
			var weapons = await _adventureCharacterView.EquipWeapon(callback.WeaponGameId);

			foreach (var weapon in weapons)
			{
				weapon.GetComponent<WeaponViewMonoComponent>().SetEntityView(EntityView);
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
			
			vfx.SetTarget(callback.TargetPosition.ToUnityVector3(), callback.HazardData.Radius.AsFloat, 
			              (callback.HazardData.EndTime - time).AsFloat);
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
			
			vfx.SetTarget(callback.TargetPosition.ToUnityVector3(), callback.HazardData.Radius.AsFloat, 
			              (callback.HazardData.EndTime - time).AsFloat);
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

			if (!_isDebugMode)
			{
				//return;
			}

			DebugAttackGizmos(callback.Game);
		}

		private void DebugAttackGizmos(QuantumGame game)
		{
#if UNITY_EDITOR
			var f = game.Frames.Verified;

			if (!f.TryGet<Weapon>(EntityRef, out var weapon))
			{
				return;
			}
			
			var fpPosition = _lastPosition.ToFPVector3() + FPVector3.Up;
			var angleCount = FPMath.FloorToInt(weapon.AttackAngle / (FP._1 * 10)) + 1;
			var angle = -weapon.AttackAngle / FP._2;
			var angleStep = weapon.AttackAngle / FPMath.Max(FP._1, angleCount - 1);
			var aimingDirection = f.Get<AIBlackboardComponent>(EntityRef).GetVector2(f, Constants.AimDirectionKey).Normalized * 
			                      weapon.AttackRange;
			
			Draw.Line(fpPosition, fpPosition + aimingDirection.XOY, ColorRGBA.Red);

			for (var i = 0; i < angleCount; i++)
			{
				var direction = FPVector2.Rotate(aimingDirection, angle * FP.Deg2Rad);
				
				Draw.Line(fpPosition, fpPosition + direction.XOY);

				angle += angleStep;
			}
#endif
		}
	}
}
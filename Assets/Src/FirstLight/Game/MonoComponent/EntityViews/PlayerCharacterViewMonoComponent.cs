using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
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

		private static readonly int _playerPos = Shader.PropertyToID("_PlayerPos");
		private const float SPEED_THRESHOLD_SQUARED = 0.45f * 0.45f; // unity units per second	
		private bool _moveSpeedControl = false;
		public Transform RootTransform;

		/// <summary>
		/// Deprecated, should be removed.
		/// This is only used for buildings.
		/// </summary>
		[System.Obsolete] public PlayerBuildingVisibility BuildingVisibility;

		private Vector3 _lastPosition;
		private Collider[] _colliders;

		private Coroutine _attackHideRendererCoroutine;
		private IGameServices _services;
		private IMatchServices _matchServices;
		private bool _playerFullyGrounded;

		/// <summary>
		/// Indicates if this is the local player
		/// </summary>
		public bool IsLocalPlayer { get; private set; }

		/// <summary>
		/// Requests the <see cref="PlayerRef"/> of this player
		/// </summary>
		public PlayerRef PlayerRef { get; private set; }
		
		private static class PlayerFloats
		{
			public static readonly AnimatorWrapper.Float DirX = new("DirX");
			public static readonly AnimatorWrapper.Float DirY = new("DirY");
		}

		protected override void OnAwake()
		{
			base.OnAwake();

			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_services = MainInstaller.ResolveServices();
			
			BuildingVisibility = new();
			QuantumEvent.Subscribe<EventOnHealthChanged>(this, HandleOnHealthChanged);
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
			QuantumEvent.Subscribe<EventOnPlayerSkydiveFullyGrounded>(this, HandlePlayerSkydiveFullyGrounded);
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
				if (_playerFullyGrounded)
				{
					AnimatorWrapper.Enabled = false;
				}

				foreach (var col in _colliders)
				{
					col.enabled = false;
				}
			}
			else
			{
				AnimatorWrapper.Enabled = true;
				foreach (var col in _colliders)
				{
					col.enabled = true;
				}
			}

			_characterView.PrintFootsteps = !culled;

			base.SetCulled(culled);
		}

		/// <inheritdoc />
		public override void SetRenderContainerVisible(bool active)
		{
			if (!active && Visible)
			{
				_characterView.HideAllEquipment();
			}
			else if (active && !Visible)
			{
				_characterView.ShowAllEquipment();
			}

			base.SetRenderContainerVisible(active);

			_characterView.PrintFootsteps = active;
		}
		
		private void HandleOnHealthChanged(EventOnHealthChanged evnt)
		{
			if (Culled || evnt.Entity != EntityView.EntityRef || evnt.PreviousHealth <= evnt.CurrentHealth)
			{
				return;
			}

			AnimatorWrapper.SetTrigger(Triggers.Hit);
			
			if (_matchServices.SpectateService.SpectatedPlayer?.Value == null)
			{
				return;
			}
			
			var localPlayer = _matchServices.SpectateService.SpectatedPlayer.Value.Entity;
			if (evnt.Entity != localPlayer)
			{
				return;
			}
			
			if (!_matchServices.EntityViewUpdaterService.TryGetView(evnt.Entity, out var attackerView))
			{
				return;
			}
			
			UpdateColor(GameConstants.Visuals.HIT_COLOR, 0.2f);
		}

		/// <summary>
		/// Set's the player animation moving state based on the given <paramref name="isAiming"/> state
		/// </summary>
		public void SetMovingState(bool isAiming)
		{
			AnimatorWrapper.SetBool(Bools.Aim, isAiming);
		}

		public bool IsBeingSpectated => EntityRef == MatchServices.SpectateService.SpectatedPlayer.Value.Entity;

		public PlayerCharacter CharacterComponent => QuantumRunner.Default.Game.Frames.Predicted.Get<PlayerCharacter>(EntityRef);

		public bool IsEntityDestroyed() => !QuantumRunner.Default.PredictedFrame().Exists(EntityView.EntityRef);
		
		public bool IsSkydiving
		{
			get
			{
				var f = QuantumRunner.Default.PredictedFrame();
				return f.Get<AIBlackboardComponent>(EntityView.EntityRef).GetBoolean(f, Constants.IsSkydiving);
			}
		}

		protected override void OnInit(QuantumGame game)
		{
			base.OnInit(game);
			var frame = game.Frames.Verified;

			PlayerRef = frame.Get<PlayerCharacter>(EntityRef).Player;
			IsLocalPlayer = game.PlayerIsLocal(PlayerRef);

			if (IsLocalPlayer)
			{
				_moveSpeedControl = MainInstaller.Resolve<IGameDataProvider>().AppDataProvider.MovespeedControl;
			}

			if (!Services.NetworkService.JoinSource.HasResync())
			{
				if (IsSkydiving)
				{
					_playerFullyGrounded = false;
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

		public bool IsInInvisibilityArea()
		{
			return MatchServices.EntityVisibilityService.IsInInvisibilityArea(EntityRef) || BuildingVisibility.CollidingVisibilityVolumes.Count > 0;
		}

		private void TryStartAttackWithinVisVolume()
		{
			if (IsBeingSpectated || !IsInInvisibilityArea())
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
			SetRenderContainerVisible(true);

			yield return new WaitForSeconds(GameConstants.Visuals.GAMEPLAY_BUSH_ATTACK_REVEAL_SECONDS);

			if (IsInInvisibilityArea())
			{
				SetRenderContainerVisible(MatchServices.EntityVisibilityService.CanSpectatedPlayerSee(EntityRef) && BuildingVisibility.CanSee());
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

			var vfx = (SpecialReticuleVfxMonoComponent)Services.VfxService.Spawn(VfxId.SpecialReticule);

			var vfxTime = Mathf.Max(0, (callback.HazardData.EndTime - time).AsFloat);

			vfx.SetTarget(targetPosition, callback.HazardData.Radius.AsFloat, vfxTime);
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

			var vfx = (SpecialReticuleVfxMonoComponent)Services.VfxService.Spawn(VfxId.SpecialReticule);

			var vfxTime = Mathf.Max(0, (callback.HazardData.EndTime - time).AsFloat);

			vfx.SetTarget(targetPosition, callback.HazardData.Radius.AsFloat, vfxTime);
		}

		private async void HandleParabolicUsed(FP launchTime, FP frameTime, Vector3 targetPosition,
											   VfxId parabolicVfxId, VfxId impactVfxId)
		{
			var flyTime = (launchTime - frameTime).AsFloat;

			if (flyTime < 0)
			{
				return;
			}

			var parabolic = (ParabolicVfxMonoComponent)Services.VfxService.Spawn(parabolicVfxId);

			parabolic.transform.position = transform.position;

			parabolic.StartParabolic(targetPosition, flyTime);

			await Task.Delay((int)(flyTime * 1000));

			if (parabolic.IsDestroyed())
			{
				return;
			}

			Services.VfxService.Spawn(impactVfxId).transform.position = targetPosition;
		}

		private void HandleOnCollectableCollected(EventOnCollectableCollected callback)
		{
			if (Culled || EntityView.EntityRef != callback.PlayerEntity || callback.CollectableId != GameId.Health)
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
#if DEBUG_BOTS
			AddDebugCylinder(callback.Game.Frames.Verified);
#endif

			AnimatorWrapper.Enabled = true;

			AnimatorWrapper.SetTrigger(Triggers.Revive);
			RenderersContainerProxy.SetEnabled(true);
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

			RenderersContainerProxy.SetEnabled(false);
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

			//TODO: bespoke animation for each special 
			//AnimatorWrapper.SetTrigger(Triggers.Special);
			TryStartAttackWithinVisVolume();
		}

		private void HandleOnGameEnded(EventOnGameEnded callback)
		{
			var localPlayerRef = callback.Game.GetLocalPlayerRef();
			
			if (EntityView.EntityRef == callback.EntityLeader ||
				(localPlayerRef != PlayerRef.None && callback.PlayersMatchData[localPlayerRef].TeamId == callback.LeaderTeam))
			{
				AnimatorWrapper.SetBool(Bools.Aim, false);
				AnimatorWrapper.SetTrigger(Triggers.Victory);
			}
			else
			{
				AnimatorWrapper.SetBool(Bools.Stun, true);
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

			var vfx = (SpecialReticuleVfxMonoComponent)Services.VfxService.Spawn(VfxId.SpecialReticule);
			var time = callback.Game.Frames.Verified.Time;
			var targetPosition = callback.TargetPosition.ToUnityVector3();

			vfx.SetTarget(targetPosition, callback.HazardData.Radius.AsFloat,
				(callback.HazardData.EndTime - time).AsFloat);

			Services.VfxService.Spawn(VfxId.Airstrike).transform.position = targetPosition;

			HandleDelayedFX(callback.HazardData.Interval, targetPosition, VfxId.ImpactAirStrike);
		}

		private async void HandleDelayedFX(FP delayTime, Vector3 targetPosition, VfxId explosionVfxId)
		{
			await Task.Delay((int)(delayTime * 1000));

			Services.VfxService.Spawn(explosionVfxId).transform.position = targetPosition;
		}

		private void HandleOnShieldedChargeUsed(EventOnShieldedChargeUsed callback)
		{
			if (callback.Attacker != EntityView.EntityRef)
			{
				return;
			}

			var vfx = (MutableTimeVfxMonoComponent)Services.VfxService.Spawn(VfxId.EnergyShield);
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

			var vfx = (SpecialReticuleVfxMonoComponent)Services.VfxService.Spawn(VfxId.SpecialReticule);
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
			var isMoving = sqrSpeed > SPEED_THRESHOLD_SQUARED;
			var isAiming = bb.GetBoolean(f, Constants.IsAimPressedKey);

			AnimatorWrapper.SetBool(Bools.Move, isMoving);
			_characterView.PrintFootsteps = isMoving;
			if (isMoving)
			{
				deltaPosition.Normalize();
				var localDeltaPosition = transform.InverseTransformDirection(deltaPosition);
				AnimatorWrapper.SetFloat(PlayerFloats.DirX, localDeltaPosition.x);
				AnimatorWrapper.SetFloat(PlayerFloats.DirY, localDeltaPosition.z);
				if (_moveSpeedControl) AnimatorWrapper.Speed = isAiming ? 1 : sqrSpeed / 3.5f;
			}
			else
			{
				if (_moveSpeedControl) AnimatorWrapper.Speed = 1;
				AnimatorWrapper.SetFloat(PlayerFloats.DirX, 0f);
				AnimatorWrapper.SetFloat(PlayerFloats.DirY, 0f);
			}

			AnimatorWrapper.SetBool(Bools.Aim, isAiming);
			_lastPosition = currentPosition;

			if (_matchServices.SpectateService.SpectatedPlayer.Value.Entity == EntityRef)
			{
				Shader.SetGlobalVector(_playerPos, transform.position);
			}
		}

		private void HandlePlayerSkydivePLF(EventOnPlayerSkydivePLF callback)
		{
			if (EntityView.EntityRef != callback.Entity)
			{
				return;
			}

			AnimatorWrapper.SetTrigger(Triggers.PLF);
		}

		private void HandlePlayerSkydiveFullyGrounded (EventOnPlayerSkydiveFullyGrounded callback)
		{
			if (EntityView.EntityRef != callback.Entity)
			{
				return;
			}
			
			_playerFullyGrounded = true;
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

		private void AddDebugCylinder(Frame frame)
		{
			var playerCharacter = frame.Get<PlayerCharacter>(EntityRef);
			var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			DestroyImmediate(obj.GetComponent<CapsuleCollider>());
			obj.transform.parent = gameObject.transform;
			obj.transform.localScale = new Vector3(2, 30, 2);
			obj.transform.localPosition = new Vector3(0, 30, 0);

			var rend = obj.GetComponent<Renderer>();
			rend.material = new Material(Shader.Find("Unlit/Color"));
			if (playerCharacter.TeamId > 0)
			{
				var playersByTeam = TeamHelpers.GetPlayersByTeam(frame);
				float teams = playersByTeam.Count;
				float myTeam = 0;
				foreach (var entityRefs in playersByTeam.Values)
				{
					if (entityRefs.Contains(EntityRef))
					{
						break;
					}

					myTeam++;
				}

				var h = Mathf.Lerp(0, 0.92f, myTeam / teams);
				rend.material.SetColor("_Color", Color.HSVToRGB(h, 0.75f, Random.Range(0.60f, 1f)));
				return;
			}
			

			rend.material.SetColor("_Color", Random.ColorHSV(0f, 1, 1, 1, 0.5f, 1));
		}
	}
}
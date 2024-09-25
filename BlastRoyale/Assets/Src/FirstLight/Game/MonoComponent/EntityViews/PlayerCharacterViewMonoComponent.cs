using System;
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.MonoComponent.Match;
using FirstLight.Game.MonoComponent.Vfx;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using Quantum.Systems;
using UnityEngine;
using Debug = System.Diagnostics.Debug;
using Random = UnityEngine.Random;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <inheritdoc/>
	/// <remarks>
	/// Responsible to play and act on Player's visual feedback. From animations to triggered VFX
	/// </remarks>
	public class PlayerCharacterViewMonoComponent : AvatarViewBase
	{
		private static readonly float TURN_RATE = 0.4f;
		
		[SerializeField] private MatchCharacterViewMonoComponent _characterView;

		private static readonly int _playerPos = Shader.PropertyToID("_PlayerPos");
		private const float SPEED_THRESHOLD_SQUARED = 0.45f * 0.45f; // unity units per second	
		private const float KNOCKED_OUT_SPEED_THRESHOLD_SQUARED = 0.1f * 0.1f; // unity units per second	
		private Vector3 _clientPredictionDirection;
		
		/// <summary>
		/// Deprecated, should be removed.
		/// This is only used for buildings.
		/// </summary>
		[System.Obsolete] public PlayerBuildingVisibility BuildingVisibility;

		private Vector3 _lastPosition;
		private DateTime _lastMoving;
		private bool _wasMoving;
		private Coroutine _attackHideRendererCoroutine;
		private IMatchServices _matchServices;

		// TODO mihak: Probably remove this
#pragma warning disable CS0414 // Field is assigned but its value is never used
		private bool _playerFullyGrounded;
#pragma warning restore CS0414 // Field is assigned but its value is never used

		/// <summary>
		/// Indicates if this is the local player
		/// </summary>
		public bool IsLocalPlayer { get; private set; }

		/// <summary>
		/// Holds where the view should be looking at
		/// </summary>
		public FPVector2 PredictedAimDirection { get; private set; }

		/// <summary>
		/// Requests the <see cref="PlayerRef"/> of this player
		/// </summary>
		public PlayerRef PlayerRef { get; private set; }

		protected override void OnAwake()
		{
			base.OnAwake();

			_matchServices = MainInstaller.Resolve<IMatchServices>();
			if (_characterView == null)
			{
				_characterView = GetComponent<MatchCharacterViewMonoComponent>();
			}

#pragma warning disable CS0612 // Type or member is obsolete
			BuildingVisibility = new ();
#pragma warning restore CS0612 // Type or member is obsolete
			QuantumEvent.Subscribe<EventOnHealthChangedPredicted>(this, HandleOnHealthChanged);
			QuantumEvent.Subscribe<EventOnPlayerAlive>(this, HandleOnPlayerAlive);
			QuantumEvent.Subscribe<EventOnPlayerAttack>(this, HandleOnPlayerAttack);
			QuantumEvent.Subscribe<EventOnPlayerSpecialUsed>(this, HandleOnPlayerSpecialUsed);
			QuantumEvent.Subscribe<EventOnPlayerSpawned>(this, HandleOnPlayerSpawned);
			QuantumEvent.Subscribe<EventOnCollectableCollected>(this, HandleOnCollectableCollected);
			QuantumEvent.Subscribe<EventOnStunGrenadeUsed>(this, HandleOnStunGrenadeUsed);
			QuantumEvent.Subscribe<EventOnGrenadeUsed>(this, HandleOnGrenadeUsed);
			QuantumEvent.Subscribe<EventOnSkyBeamUsed>(this, HandleOnSkyBeamUsed);
			QuantumEvent.Subscribe<EventOnShieldedChargeUsed>(this, HandleOnShieldedChargeUsed);
			QuantumEvent.Subscribe<EventOnGameEnded>(this, HandleOnGameEnded);
			QuantumEvent.Subscribe<EventOnPlayerWeaponChanged>(this, HandlePlayerWeaponChanged);
			QuantumEvent.Subscribe<EventOnPlayerWeaponAdded>(this, HandlePlayerWeaponAdded);
			QuantumEvent.Subscribe<EventOnPlayerSkydiveLand>(this, HandlePlayerSkydiveLand);
			QuantumEvent.Subscribe<EventOnPlayerSkydivePLF>(this, HandlePlayerSkydivePLF);
			QuantumEvent.Subscribe<EventOnPlayerSkydiveFullyGrounded>(this, HandlePlayerSkydiveFullyGrounded);
			QuantumCallback.Subscribe<CallbackUpdateView>(this, HandleUpdateView);
			QuantumEvent.Subscribe<EventOnRadarUsed>(this, HandleOnRadarUsed);
		}
		
		/// <summary>
		/// Handles player rotation. This is done 100% on the client side
		/// </summary>
		unsafe void LateUpdate()
		{
			if (!QuantumRunner.Default.IsDefinedAndRunning()) return;
			var f = QuantumRunner.Default.Game.Frames.Predicted;
			if (Culled)
			{
				return;
			}
			
			if (!f.Unsafe.TryGetPointer<AIBlackboardComponent>(EntityRef, out var bb)) return;

			var aim = bb->GetVector2(f, Constants.AIM_DIRECTION_KEY);
			var lastShotAt = bb->GetFP(f, Constants.LAST_SHOT_AT);
			var aiming = bb->GetBoolean(f, Constants.IS_AIM_PRESSED_KEY);
			
			var dir = EntityRef.GetKccMoveDirection(f);
			var kcc = f.Unsafe.GetPointer<TopDownController>(EntityRef);
			
			if (f.TryGet<BotCharacter>(EntityRef, out var bot))
			{
				if (bot.BehaviourType != BotBehaviourType.Static)
				{
					dir = _clientPredictionDirection.ToFPVector2();
				}
				else
				{
					dir = aim;
				}
			}
			
			// Wait to rotate a bit after attack for smoother animation
			var attackOnCooldown = lastShotAt > 0 && f.Time < lastShotAt + FP._0_25;
		
			if (attackOnCooldown && !aiming)
			{
				return;
			}
			
			PredictedAimDirection = aiming ? aim : dir;
			if (PredictedAimDirection == FPVector2.Zero)
			{
				return;
			}
			var targetRotation = Quaternion.LookRotation(PredictedAimDirection.ToUnityVector3());
			if (PredictedAimDirection != FPVector2.Zero)
			{
				transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, TURN_RATE);
			}
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
			if (_characterView == null || this.IsDestroyed()) return;
			
			if (culled)
			{
			 	if (_playerFullyGrounded)
			 	{
			 		_skin.AnimationEnabled = false;
			 	}
			}
			else
			{ 
				_skin.AnimationEnabled = true;
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

		private void HandleOnHealthChanged(EventOnHealthChangedPredicted evnt)
		{
			if (Culled || evnt.Entity != EntityView.EntityRef || evnt.PreviousValue <= evnt.CurrentValue)
			{
				return;
			}

			// Do not trigger hit when player is knockedout
			if (!ReviveSystem.IsKnockedOut(evnt.Game.Frames.Predicted, evnt.Entity))
			{
				_skin.TriggerHit();
			}

			if (evnt.SpellType == Spell.KnockedOut) return;

			if (_matchServices.SpectateService.SpectatedPlayer?.Value == null)
			{
				return;
			}

			if (!_matchServices.EntityViewUpdaterService.TryGetView(evnt.Entity, out var attackerView))
			{
				return;
			}

			UpdateAdditiveColor(GameConstants.Visuals.HIT_COLOR, 0.2f);
		}

		public bool IsBeingSpectated => EntityRef == MatchServices.SpectateService.SpectatedPlayer.Value.Entity;

		public PlayerCharacter CharacterComponent => QuantumRunner.Default.Game.Frames.Predicted.Get<PlayerCharacter>(EntityRef);

		public bool IsEntityDestroyed() => !QuantumRunner.Default.PredictedFrame().Exists(EntityView.EntityRef);

		public bool IsSkydiving
		{
			get
			{
				var f = QuantumRunner.Default.PredictedFrame();
				return f.Get<AIBlackboardComponent>(EntityView.EntityRef).GetBoolean(f, Constants.IS_SKYDIVING);
			}
		}

		protected override void OnInit(QuantumGame game)
		{
			base.OnInit(game);
			var frame = game.Frames.Verified;

			PlayerRef = frame.Get<PlayerCharacter>(EntityRef).Player;
			IsLocalPlayer = game.PlayerIsLocal(PlayerRef);

			if (!Services.NetworkService.JoinSource.HasResync())
			{
				if (IsSkydiving)
				{
					_playerFullyGrounded = false;
					if (frame.Context.GameModeConfig.SkydiveSpawn)
					{
						_skin.TriggerSkydive();
						SkydiveAnimationHack().Forget();
					}
				}
				else
				{
					// TODO: No spawn animation - probably not needed
					//AnimatorWrapper.SetTrigger(EntityView.EntityRef.IsAlive(frame) ? Triggers.Spawn : Triggers.Die);
				}
			}
			else
			{
				//AnimatorWrapper.SetBool(Bools.Flying, false);

				if (!EntityView.EntityRef.IsAlive(frame))
				{
					_skin.TriggerDie();
				}
			}
		}

		/// <summary>
		/// Non intrusive way of doing skydive animation.
		/// Needa to wait a frame because it needs quantum to set the object position first
		/// </summary>
		private async UniTaskVoid SkydiveAnimationHack()
		{
			if (IsLocalPlayer)
			{
				var camera = _matchServices.MatchCameraService.GetCamera();
				await UniTask.NextFrame();
				
				// Magical camera effect hack
				var follow = camera.Follow;
				var blendTime = FLGCamera.Instance.CinemachineBrain.m_DefaultBlend.m_Time;
				FLGCamera.Instance.CinemachineBrain.m_DefaultBlend.m_Time = 0;
				camera.Follow = null;
				camera.LookAt = null;
				await Task.Delay(TimeSpan.FromSeconds(1));
				camera.transform.position += Vector3.up * 20;
				FLGCamera.Instance.CinemachineBrain.m_DefaultBlend.m_Time = blendTime;
				camera.Follow = follow;
				camera.LookAt = follow;
				
				// Position hack
				var endPosition = _skin.transform.localPosition;
				var startPosition = endPosition + new Vector3(0, 2, 0);
				await UniTask.NextFrame();
				_skin.transform.localPosition = startPosition;
				await UniTask.NextFrame();
				_skin.transform.DOLocalMove(endPosition, 5f);
			}
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
			return MatchServices.EntityVisibilityService.IsInInvisibilityArea(EntityRef);
		}

		private void TryStartAttackWithinVisVolume()
		{
			if (!FeatureFlags.ALWAYS_TOGGLE_INVISIBILITY_AREAS && (IsBeingSpectated || !IsInInvisibilityArea()))
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
				SetRenderContainerVisible(MatchServices.EntityVisibilityService.CanSpectatedPlayerSee(EntityRef));
			}

			//Old system needs to burn in fire
#pragma warning disable CS0612 // Type or member is obsolete
			else if (BuildingVisibility.IsInLegacyVisibilityVolume())
			{
				SetRenderContainerVisible(BuildingVisibility.IsInSameLegacyVolumeAsSpectator());
			}
#pragma warning restore CS0612 // Type or member is obsolete
		}

		private void HandleOnStunGrenadeUsed(EventOnStunGrenadeUsed callback)
		{
			if (callback.HazardData.Attacker != EntityView.EntityRef) return;

			var time = callback.Game.Frames.Verified.Time;
			var targetPosition = callback.TargetPosition.ToUnityVector3();

			HandleParabolicUsed(callback.HazardData.EndTime,
				time, targetPosition, VfxId.GrenadeStunParabolic, VfxId.ImpactGrenadeStun).Forget();

			var vfx = (SpecialReticuleVfxMonoComponent) Services.VfxService.Spawn(VfxId.SpecialReticule);

			var vfxTime = Mathf.Max(0, (callback.HazardData.EndTime - time).AsFloat);

			vfx.SetTarget(targetPosition, callback.HazardData.Radius.AsFloat, vfxTime);
		}

		private void HandleOnGrenadeUsed(EventOnGrenadeUsed callback)
		{
			if (callback.HazardData.Attacker != EntityView.EntityRef) return;

			var time = callback.Game.Frames.Verified.Time;
			var targetPosition = callback.TargetPosition.ToUnityVector3();

			HandleParabolicUsed(callback.HazardData.EndTime,
				time, targetPosition, VfxId.GrenadeParabolic, VfxId.Explosion).Forget();

			var vfx = (SpecialReticuleVfxMonoComponent) Services.VfxService.Spawn(VfxId.SpecialReticule);

			var vfxTime = Mathf.Max(0, (callback.HazardData.EndTime - time).AsFloat);

			vfx.SetTarget(targetPosition, callback.HazardData.Radius.AsFloat, vfxTime);
		}

		private async UniTaskVoid HandleParabolicUsed(FP launchTime, FP frameTime, Vector3 targetPosition,
													  VfxId parabolicVfxId, VfxId impactVfxId)
		{
			var flyTime = (launchTime - frameTime).AsFloat;

			if (flyTime < 0)
			{
				return;
			}

			var parabolic = (ParabolicVfxMonoComponent) Services.VfxService.Spawn(parabolicVfxId);

			parabolic.transform.position = transform.position;
			parabolic.GetComponent<Rigidbody>().position = transform.position;

			parabolic.StartParabolic(targetPosition, flyTime);
			await UniTask.NextFrame();
			parabolic.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Extrapolate;
			await UniTask.Delay((int) (flyTime * 1000));

			if (parabolic.IsDestroyed())
			{
				return;
			}

			Services.VfxService.Spawn(impactVfxId).transform.position = targetPosition;
		}

		private void PlayCollectionVfx(VfxId id, EventOnCollectableCollected ev)
		{
			var vfx = Services.VfxService.Spawn(id).transform;
			vfx.position = ev.CollectablePosition.ToUnityVector3();
		}

		private void HandleOnCollectableCollected(EventOnCollectableCollected callback)
		{
			if (Culled || EntityView.EntityRef != callback.CollectorEntity) return;

			switch (callback.CollectableId)
			{
				case GameId.Health:
					// If we want the VFX on the character, add this back
					// var vfx = Services.VfxService.Spawn(VfxId.StatusFxHeal).transform;
					// vfx.SetParent(transform);
					// vfx.localPosition = Vector3.zero;
					// vfx.localScale = Vector3.one;
					// vfx.localRotation = Quaternion.identity;
					PlayCollectionVfx(VfxId.HealthPickupFx, callback);
					return;
				case GameId.ShieldLarge:
				case GameId.ShieldSmall:
				case GameId.NOOB:
				case GameId.NOOBRainbow:
				case GameId.NOOBGolden:
				case GameId.PartnerANCIENT8:
				case GameId.PartnerAPECOIN:
				case GameId.PartnerBEAM:
				case GameId.PartnerBLOCKLORDS:
				case GameId.PartnerBLOODLOOP:
				case GameId.PartnerCROSSTHEAGES:
				case GameId.PartnerFARCANA:
				case GameId.PartnerGAM3SGG:
				case GameId.PartnerIMMUTABLE:
				case GameId.PartnerMOCAVERSE:
				case GameId.PartnerNYANHEROES:
				case GameId.PartnerPIRATENATION:
				case GameId.PartnerPIXELMON:
				case GameId.PartnerPLANETMOJO:
				case GameId.PartnerSEEDIFY:
				case GameId.PartnerWILDERWORLD:
				case GameId.PartnerXBORG:
					PlayCollectionVfx(VfxId.ShieldPickupFx, callback);
					return;
				case GameId.COIN:
					PlayCollectionVfx(VfxId.CoinPickupFx, callback);
					return;
				case GameId.BPP:
					PlayCollectionVfx(VfxId.BppPickupFx, callback);
					return;
				case GameId.AmmoLarge:
				case GameId.AmmoSmall:
					PlayCollectionVfx(VfxId.AmmoPickupFx, callback);
					return;
				case GameId.ChestEquipment:
				case GameId.ChestConsumable:
					PlayCollectionVfx(VfxId.ChestPickupFx, callback);
					return;
				case GameId.ChestLegendary:
					PlayCollectionVfx(VfxId.AirdropPickupFx, callback);
					return;
			}

			if (callback.CollectableId.IsInGroup(GameIdGroup.Special) || callback.CollectableId.IsInGroup(GameIdGroup.Weapon))
			{
				var chestPickupVfx = Services.VfxService.Spawn(VfxId.SpecialAndWeaponPickupFx).transform;
				chestPickupVfx.position = callback.CollectablePosition.ToUnityVector3();
			}
		}

		private void HandleOnPlayerAlive(EventOnPlayerAlive callback)
		{
			if (EntityView.EntityRef != callback.Entity) return;

			AddDebugCylinder(callback.Game.Frames.Verified);

			RenderersContainerProxy.SetEnabled(true);
		}

		private void HandleOnPlayerSpawned(EventOnPlayerSpawned callback)
		{
			if (EntityView.EntityRef != callback.Entity) return;

			RenderersContainerProxy.SetEnabled(false);
		}

		private void HandleOnPlayerAttack(EventOnPlayerAttack callback)
		{
			if (EntityView.EntityRef != callback.PlayerEntity) return;


			_skin.TriggerAttack();
			TryStartAttackWithinVisVolume();
		}

		private void HandleOnPlayerSpecialUsed(EventOnPlayerSpecialUsed callback)
		{
			if (EntityView.EntityRef != callback.Entity) return;


			//TODO: bespoke animation for each special 
			//AnimatorWrapper.SetTrigger(Triggers.Special);
			TryStartAttackWithinVisVolume();
		}

		private void HandleOnGameEnded(EventOnGameEnded callback)
		{
			var localPlayerRef = callback.Game.GetLocalPlayerRef();
			_lastMoving = DateTime.MinValue;
			_skin.Moving = false;
			_skin.Aiming = false;
			if (EntityView.EntityRef == callback.EntityLeader ||
				(localPlayerRef != PlayerRef.None && callback.PlayersMatchData[localPlayerRef].TeamId == callback.LeaderTeam))
			{
				// TODO mihak: AnimatorWrapper.SetBool(Bools.Aim, false);
				_skin.TriggerVictory();
			}
			else
			{
				_skin.TriggerStun();
			}
		}

		private void HandlePlayerWeaponChanged(EventOnPlayerWeaponChanged callback)
		{
			if (EntityView.EntityRef != callback.Entity) return;

			_characterView.EquipWeapon(callback.Weapon.GameId);
		}

		private void HandlePlayerWeaponAdded(EventOnPlayerWeaponAdded callback)
		{
			if (EntityView.EntityRef != callback.Entity) return;

			_characterView.InstantiateWeapon(callback.Weapon)
				.ContinueWith(weapon =>
				{
					var f = callback.Game.Frames.Verified;
					if (!f.Exists(EntityView.EntityRef))
					{
						return;
					}

					if (weapon == null) return;
					var components = weapon.GetComponentsInChildren<EntityViewBase>();

					foreach (var entityViewBase in components)
					{
						entityViewBase.SetEntityView(callback.Game, EntityView);
					}
				}).Forget();
		}
		

		private async UniTaskVoid HandleDelayedFX(FP delayTime, Vector3 targetPosition, VfxId explosionVfxId)
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

			var vfx = (MutableTimeVfxMonoComponent) Services.VfxService.Spawn(VfxId.EnergyShield);
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

			var vfx = (SpecialReticuleVfxMonoComponent) Services.VfxService.Spawn(VfxId.SpecialReticule);
			var time = callback.Game.Frames.Verified.Time;
			var targetPosition = callback.TargetPosition.ToUnityVector3();

			vfx.SetTarget(targetPosition, callback.HazardData.Radius.AsFloat,
				(callback.HazardData.EndTime - time).AsFloat);

			HandleDelayedFX(callback.HazardData.Interval - FP._0_50, targetPosition, VfxId.Skybeam).Forget();
		}

		private void HandleOnRadarUsed(EventOnRadarUsed callback)
		{
			if (callback.Player != PlayerRef)
			{
				return;
			}

			Services.VfxService.Spawn(VfxId.Radar).transform.position = transform.position;
		}

		private unsafe void HandleUpdateView(CallbackUpdateView callback)
		{
			var f = callback.Game.Frames.Predicted;
			if (!f.Unsafe.TryGetPointer<AIBlackboardComponent>(EntityRef, out var bb))
			{
				return;
			}

			if (f.Unsafe.GetPointerSingleton<GameContainer>()->IsGameCompleted || f.Unsafe.GetPointerSingleton<GameContainer>()->IsGameOver)
			{
				return;
			}

			if (!f.Unsafe.TryGetPointer<TopDownController>(EntityRef, out var characterController3D))
			{
				return;
			}

			if (Culled)
			{
				return;
			}
			
			var knockedOut = ReviveSystem.IsKnockedOut(f, EntityRef);
			var currentPosition = transform.position;
			var deltaPosition = currentPosition - _lastPosition;
			_clientPredictionDirection = deltaPosition.normalized;
			var sqrSpeed = (deltaPosition / f.DeltaTime.AsFloat).sqrMagnitude;
			var moveDir = EntityRef.GetKccMoveDirection(f);
			var minSpeed = (ReviveSystem.IsKnockedOut(f, EntityRef) ? KNOCKED_OUT_SPEED_THRESHOLD_SQUARED : SPEED_THRESHOLD_SQUARED);
			var isMoving = sqrSpeed > minSpeed;
			if (!f.Has<BotCharacter>(EntityRef) && !isMoving)
			{
				isMoving = moveDir != FPVector2.Zero;
			}
			var isAiming = bb->GetBoolean(f, Constants.IS_AIM_PRESSED_KEY) && !knockedOut;
			
			_skin.Moving = isMoving || _lastMoving > DateTime.UtcNow - TimeSpan.FromSeconds(0.1);
			_wasMoving = isMoving;
			if (isMoving)
			{
				_lastMoving = DateTime.UtcNow;
			}
			_characterView.PrintFootsteps = isMoving && !knockedOut;
			
			_skin.Aiming = isAiming;
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

			_skin.TriggerPLF();
		}

		private void HandlePlayerSkydiveFullyGrounded(EventOnPlayerSkydiveFullyGrounded callback)
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

			// TODO mihak: ???
			//AnimatorWrapper.SetBool(Bools.Flying, false);

			_characterView.DestroyGlider();
		}

		[Conditional("DEBUG_BOTS")]
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
				var playersByTeam = TeamSystem.GetPlayersByTeam(frame);
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
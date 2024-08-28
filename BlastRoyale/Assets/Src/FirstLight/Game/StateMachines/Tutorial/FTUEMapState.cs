using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.Vfx;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Tutorial;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using I2.Loc;
using Photon.Deterministic;
using Quantum;
using Quantum.Commands;
using UnityEngine;
using UnityEngine.UIElements;


namespace FirstLight.Game.StateMachines
{
	public class FirstGameTutorialState
	{
		struct GameplayProceedEventData
		{
			public Type EventType;
			public string EventMetaId;
			public short EventMetaAmount;
			public Func<Frame, bool> Completed;
		}

		public static readonly IStatechartEvent ProceedTutorialEvent = new StatechartEvent("TUTORIAL - Proceed tutorial event");
		public static readonly IStatechartEvent SkipTutorialEvent = new StatechartEvent("TUTORIAL - SkipTutorialEvent");

		private readonly IGameServices _services;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		private IMatchServices _matchServices;
		private TutorialOverlayPresenter _tutorialOverlay;
		private HUDScreenPresenter _hud;
		private Dictionary<string, GameObject> _tutorialObjectRefs = new ();
		private List<LocationPointerVfxMonoComponent> _activeLocationPointers = new ();
		private MetaTutorialSequence _sequence;
		private GameplayProceedEventData _currentGameplayProceedData;


		public FirstGameTutorialState(IGameServices services, Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_statechartTrigger = statechartTrigger;
			_sequence = new MetaTutorialSequence(services, TutorialSection.FIRST_GUIDE_MATCH);
		}

		/// <summary>
		/// Setups the audio state - root state, and then per gamemode type nested states
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var createTutorialRoom = stateFactory.State("Create tutorial room");
			var waitSimulationStart = stateFactory.State("Waiting for match start");
			var startedSimulation = stateFactory.State("Playing tutorial match");
			var moveJoystick = stateFactory.State("Move joystick");
			var firstMove = stateFactory.State("First Move");
			var destroyBarrier = stateFactory.State("Destroy barrier");
			var pickupWeapon = stateFactory.State("Pickup Weapon");
			var moveToDummyArea = stateFactory.State("Move to dummy area");
			var kill2Bots = stateFactory.State("Kill 2 bots");
			var pickupSpecial = stateFactory.State("Pickup Special");
			var kill1BotSpecial = stateFactory.State("Kill 1 bot special");
			var checkGrenadeKill = stateFactory.Choice("Check Grenade Kill");
			var moveToGateArea = stateFactory.State("Proceed through iron gate");
			var moveToChestArea = stateFactory.State("Move to chest area");
			var openBox = stateFactory.State("Open box");
			var killFinalBot = stateFactory.State("Kill final bot");
			var waitMatchFinish = stateFactory.State("Wait simulation finish");

			void SkipTutorialHook(params IStateEvent[] states)
			{
				foreach (var stateEvent in states)
				{
					stateEvent.Event(SkipTutorialEvent)
						.OnTransition(KillTutorialBots)
						.Target(waitMatchFinish);
				}
			}

			initial.Transition().Target(createTutorialRoom);
			initial.OnExit(SubscribeMessages);
			initial.OnExit(GetTutorialUiRefs);

			createTutorialRoom.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.CreateTutorialRoom); });
			createTutorialRoom.OnEnter(StartFirstTutorialMatch);
			createTutorialRoom.Event(NetworkState.JoinedRoomEvent).Target(waitSimulationStart);

			waitSimulationStart.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.WaitSimulationStart); });
			waitSimulationStart.Event(GameSimulationState.SimulationStartedEvent).Target(startedSimulation);
			waitSimulationStart.OnExit(BindMatchServices);

			startedSimulation.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.Spawn); });
			startedSimulation.OnEnter(GetGroundIndicatorRefs);
			startedSimulation.OnEnter(OnEnterStartedSimulation);
			startedSimulation.Event(ProceedTutorialEvent).Target(moveJoystick);

			moveJoystick.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.MoveJoystick); });
			moveJoystick.OnEnter(OnEnterMoveJoystick);
			moveJoystick.Event(MatchState.MatchUnloadedEvent).Target(final);
			moveJoystick.Event(ProceedTutorialEvent).Target(firstMove);
			moveJoystick.OnExit(OnExitMoveJoystick);

			firstMove.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.FirstMove); });
			firstMove.OnEnter(OnEnterFirstMove);
			firstMove.Event(MatchState.MatchUnloadedEvent).Target(final);
			firstMove.Event(ProceedTutorialEvent).Target(destroyBarrier);

			destroyBarrier.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.DestroyBarrier); });
			destroyBarrier.OnEnter(OnEnterDestroyBarrier);
			destroyBarrier.Event(MatchState.MatchUnloadedEvent).Target(final);
			destroyBarrier.Event(ProceedTutorialEvent).Target(pickupWeapon);

			SkipTutorialHook(moveJoystick, firstMove, destroyBarrier);
			pickupWeapon.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.PickUpWeapon); });
			pickupWeapon.OnEnter(OnEnterPickupWeapon);
			pickupWeapon.Event(MatchState.MatchUnloadedEvent).Target(final);
			pickupWeapon.Event(ProceedTutorialEvent).Target(moveToDummyArea);

			moveToDummyArea.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.MoveToDummyArea); });
			moveToDummyArea.OnEnter(OnEnterMoveToDummyArea);
			moveToDummyArea.Event(MatchState.MatchUnloadedEvent).Target(final);
			moveToDummyArea.Event(ProceedTutorialEvent).Target(kill2Bots);

			kill2Bots.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.Kill2Bots); });
			kill2Bots.OnEnter(OnEnterKill2Bots);
			kill2Bots.Event(MatchState.MatchUnloadedEvent).Target(final);
			kill2Bots.Event(ProceedTutorialEvent).Target(pickupSpecial);


			pickupSpecial.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.PickupSpecial); });
			pickupSpecial.OnEnter(OnEnterPickupSpecial);
			pickupSpecial.Event(MatchState.MatchUnloadedEvent).Target(final);
			pickupSpecial.Event(ProceedTutorialEvent).Target(kill1BotSpecial);

			kill1BotSpecial.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.Kill1BotSpecial); });
			kill1BotSpecial.OnEnter(OnEnterKill1BotSpecial);
			kill1BotSpecial.Event(MatchState.MatchUnloadedEvent).Target(final);
			kill1BotSpecial.Event(ProceedTutorialEvent).Target(checkGrenadeKill);

			checkGrenadeKill.Transition().Condition(() => IsGrenadeBotDead(QuantumRunner.Default.Game.Frames.Verified)).Target(moveToGateArea);
			checkGrenadeKill.Transition().Condition(() => HasSpecial(QuantumRunner.Default.Game.Frames.Verified)).Target(kill1BotSpecial);
			checkGrenadeKill.Transition().Target(pickupSpecial);

			moveToGateArea.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.MoveToGateArea); });
			moveToGateArea.OnEnter(OnEnterMoveToGateArea);
			moveToGateArea.Event(MatchState.MatchUnloadedEvent).Target(final);
			moveToGateArea.Event(ProceedTutorialEvent).Target(openBox);

			openBox.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.OpenBox); });
			openBox.OnEnter(OnEnterOpenBox);
			openBox.Event(MatchState.MatchUnloadedEvent).Target(final);
			openBox.Event(ProceedTutorialEvent).Target(killFinalBot);

			killFinalBot.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.KillFinalBot); });
			killFinalBot.OnEnter(OnEnterKillFinalBot);
			killFinalBot.Event(ProceedTutorialEvent).Target(waitMatchFinish);

			waitMatchFinish.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.MatchEnded); });
			waitMatchFinish.OnEnter(OnEnterWaitMatchFinish);
			waitMatchFinish.Event(MatchState.MatchUnloadedEvent).Target(final);
			waitMatchFinish.OnExit(() => { _sequence.EnterStep(TutorialClientStep.TutorialFinish); });

			final.OnEnter(CloseTutorialUi);
			final.OnEnter(_sequence.SendCurrentStepCompletedAnalytics);
			final.OnEnter(UnsubscribeMessages);
		}


		private void OnUpdateSimulation(CallbackUpdateView callback)
		{
			var f = callback.Game.Frames.Verified;
			CheckProgressWithCompleteFunction(f);
		}

		private void CheckProgressWithCompleteFunction(Frame f)
		{
			var current = _currentGameplayProceedData;
			if (current.Completed == null)
			{
				return;
			}

			if (!current.Completed(f)) return;
			_statechartTrigger(ProceedTutorialEvent);
		}

		private void KillTutorialBots()
		{
			QuantumRunner.Default.Game.SendCommand(new CheatKillAllTutorialBots {BehaviourType = BotBehaviourType.Static});
			QuantumRunner.Default.Game.SendCommand(new CheatKillAllTutorialBots {BehaviourType = BotBehaviourType.WanderAndShoot});
		}

		private void GetTutorialUiRefs()
		{
			_tutorialOverlay = _services.UIService.GetScreen<TutorialOverlayPresenter>();
			//_services.UIService.OpenScreen<SwipeTransitionScreenPresenter>().Forget();
		}

		private void GetGroundIndicatorRefs()
		{
			foreach (var indicator in _services.TutorialService.FindTutorialObjects(GameConstants.Tutorial.TAG_INDICATORS))
			{
				_tutorialObjectRefs.Add(indicator.name, indicator);
			}
		}

		private void CloseTutorialUi()
		{
			_tutorialOverlay.Dialog.HideDialog(CharacterType.Female);
			_tutorialOverlay.HideGuideHand();
		}

		private EntityView GetLocalPlayerView()
		{
			_matchServices.EntityViewUpdaterService.TryGetView(_matchServices.SpectateService.GetSpectatedEntity(), out var entityView);
			return entityView;
		}


		private void SubscribeMessages()
		{
			QuantumEvent.SubscribeManual<EventOnLocalPlayerAlive>(this, OnLocalPlayerAlive);
			QuantumEvent.SubscribeManual<EventOnHazardLand>(this, OnHazardLand);
			QuantumCallback.SubscribeManual<CallbackUpdateView>(this, OnUpdateSimulation);
			_services.MessageBrokerService.Subscribe<PlayerEnteredMessageVolume>(OnPlayerEnteredMessageVolume);
		}


		private void DespawnPointers()
		{
			foreach (var activePointer in _activeLocationPointers.ToList())
			{
				activePointer.Despawn();
			}
		}

		private void SpawnNewPointer(Vector3 spawnLocation, Transform followTransform)
		{
			var pointerFx = _services.VfxService.Spawn(VfxId.LocationPointer) as LocationPointerVfxMonoComponent;
			pointerFx.SetFollowedObject(followTransform);
			pointerFx.transform.position = spawnLocation;
			_activeLocationPointers.Add(pointerFx);
		}

		private void OnPlayerEnteredMessageVolume(PlayerEnteredMessageVolume msg)
		{
			CheckGameplayProceedConditions(typeof(PlayerEnteredMessageVolume), msg.VolumeId);
		}

		private void BindMatchServices()
		{
			_matchServices = MainInstaller.Resolve<IMatchServices>();
		}

		private async void OnLocalPlayerAlive(EventOnLocalPlayerAlive callback)
		{
			await Task.Yield();
			CheckGameplayProceedConditions(typeof(EventOnLocalPlayerAlive));
		}


		private async void OnHazardLand(EventOnHazardLand callback)
		{
			await Task.Yield();

			if (callback.sourceId == GameId.SkipTutorial)
			{
				_statechartTrigger(SkipTutorialEvent);
				return;
			}
		}


		private void UnsubscribeMessages()
		{
			QuantumEvent.UnsubscribeListener(this);
			QuantumCallback.UnsubscribeListener(this);
			_services.MessageBrokerService.UnsubscribeAll(this);
		}

		private void StartFirstTutorialMatch()
		{
			_services.TutorialService.CreateJoinFirstTutorialRoom();
		}

		private void CheckGameplayProceedConditions(Type eventType, string metaId = "", short metaAmount = 0)
		{
			if (_currentGameplayProceedData.EventType != eventType) return;

			if (!string.IsNullOrEmpty(_currentGameplayProceedData.EventMetaId) &&
				_currentGameplayProceedData.EventMetaId != metaId) return;

			if (_currentGameplayProceedData.EventMetaAmount != 0 &&
				_currentGameplayProceedData.EventMetaAmount > metaAmount) return;

			_statechartTrigger(ProceedTutorialEvent);
		}

		private void OnEnterStartedSimulation()
		{
			_tutorialOverlay.Dialog.ShowDialog(ScriptLocalization.UITTutorial.welcome_to_wastelands, CharacterType.Female, CharacterDialogMoodType.Happy,
				CharacterDialogPosition.TopLeft);

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnLocalPlayerAlive)
			};
		}

		private void SetFingerPosition(VisualElement element, float angle = 45)
		{
			var root = _hud.Root;
			var elementPosition = element.GetPositionOnScreen(root);
			_tutorialOverlay.SetGuideHandScreenPosition(elementPosition, angle);
		}

		private async UniTask OnEnterMoveJoystickAsync()
		{
			_matchServices.PlayerInputService.OnQuantumInputSent += OnInput;
			_tutorialOverlay.Dialog.ContinueDialog(ScriptLocalization.UITTutorial.use_left_joystick, CharacterType.Female, CharacterDialogMoodType.Neutral);
			await UniTask.WaitUntil(_services.UIService.IsScreenOpen<HUDScreenPresenter>);
			_hud = _services.UIService.GetScreen<HUDScreenPresenter>();
			SetFingerPosition(_hud.MovementJoystick);
			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(PlayerUsedMovementJoystick)
			};
		}

		private void OnEnterMoveJoystick()
		{
			_ = OnEnterMoveJoystickAsync();
		}

		private void OnExitMoveJoystick()
		{
			_matchServices.PlayerInputService.OnQuantumInputSent -= OnInput;
		}

		private void OnInput(Quantum.Input input)
		{
			if (input.Direction.Magnitude > FP._0_05)
			{
				CheckGameplayProceedConditions(typeof(PlayerUsedMovementJoystick));
			}
		}

		private void OnEnterFirstMove()
		{
			_tutorialOverlay.Dialog.ContinueDialog(ScriptLocalization.UITTutorial.move_forward, CharacterType.Female, CharacterDialogMoodType.Happy);
			DespawnPointers();
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_FIRST_MOVE].transform.position, GetLocalPlayerView().transform);
			_tutorialOverlay.HideGuideHand();

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(PlayerEnteredMessageVolume),
				EventMetaId = GameConstants.Tutorial.TRIGGER_FIRST_MOVE_AREA
			};
		}

		private void OnEnterDestroyBarrier()
		{
			_tutorialOverlay.Dialog.ContinueDialog(ScriptLocalization.UITTutorial.shoot_barrier, CharacterType.Female, CharacterDialogMoodType.Neutral);
			DespawnPointers();
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_WOODEN_BARRIER].transform.position, GetLocalPlayerView().transform);
			SetFingerPosition(_hud.ShootingJoystick, 90);

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				Completed = IsBarrierDestroyed
			};
		}

		private void OnEnterPickupWeapon()
		{
			_tutorialOverlay.Dialog.ContinueDialog(ScriptLocalization.UITTutorial.pick_up_weapon, CharacterType.Female, CharacterDialogMoodType.Neutral);
			DespawnPointers();
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_FIRST_WEAPON].transform.position, GetLocalPlayerView().transform);
			_tutorialOverlay.HideGuideHand();

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				Completed = DoesLocalPlayerHaveWeapon
			};
		}

		private void OnEnterMoveToDummyArea()
		{
			_tutorialOverlay.Dialog.ContinueDialog(ScriptLocalization.UITTutorial.nice_proceed_dummy_area, CharacterType.Female, CharacterDialogMoodType.Shocked);
			DespawnPointers();
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_BOT_AREA].transform.position, GetLocalPlayerView().transform);

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(PlayerEnteredMessageVolume),
				EventMetaId = GameConstants.Tutorial.TRIGGER_DUMMY_AREA
			};
		}

		private void OnEnterKill2Bots()
		{
			_tutorialOverlay.Dialog.ContinueDialog(ScriptLocalization.UITTutorial.shoot_dummies, CharacterType.Female, CharacterDialogMoodType.Happy);
			DespawnPointers();
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_BOT1].transform.position, GetLocalPlayerView().transform);
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_BOT2].transform.position, GetLocalPlayerView().transform);

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				Completed = AreTheFirstBotsDead
			};
			_services.MessageBrokerService.Publish(new AdvancedFirstMatchMessage
			{
				State = TutorialFirstMatchStates.EnterKill2Bots
			});
		}


		private void OnEnterPickupSpecial()
		{			
			if (GetLocalPlayerView() == null) return; // reconnection edge case
			
			QuantumRunner.Default.Game.SendCommand(new TutorialSpawnSpecialCommand());
			_tutorialOverlay.Dialog.ContinueDialog(ScriptLocalization.UITTutorial.pick_up_special, CharacterType.Female, CharacterDialogMoodType.Neutral);
			DespawnPointers();
			var position = GetLocalPlayerView().transform;
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_SPECIAL_PICKUP].transform.position, position);
			_tutorialOverlay.HideGuideHand();

			_currentGameplayProceedData = new GameplayProceedEventData
			{
				Completed = HasSpecial
			};
		}

		private void OnEnterKill1BotSpecial()
		{
			if (GetLocalPlayerView() == null) return; // reconnection edge case
			
			_tutorialOverlay.Dialog.ContinueDialog(ScriptLocalization.UITTutorial.use_grenade, CharacterType.Female, CharacterDialogMoodType.Neutral);
			DespawnPointers();
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_BOT3].transform.position, GetLocalPlayerView().transform);

			SetFingerPosition(_hud.Special0, 90);

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				// Completes either when the player kills the bot or he doesn't have specials any more
				// Then the state machine will check if the bot is dead, and if it is will redirect to "Pick grenade" state
				Completed = f => IsGrenadeBotDead(f) || (!HasSpecial(f) && !IsGrenadeFlying(f))
			};
		}

		private void OnEnterMoveToGateArea()
		{
			_tutorialOverlay.Dialog.ContinueDialog(ScriptLocalization.UITTutorial.proceed_iron_gate, CharacterType.Female, CharacterDialogMoodType.Happy);
			DespawnPointers();
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_IRON_GATE].transform.position, GetLocalPlayerView().transform);
			_tutorialOverlay.HideGuideHand();

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(PlayerEnteredMessageVolume),
				EventMetaId = GameConstants.Tutorial.TRIGGER_GATE_AREA
			};
		}
		
		private void OnEnterOpenBox()
		{
			_tutorialOverlay.Dialog.ContinueDialog(ScriptLocalization.UITTutorial.open_chest, CharacterType.Female, CharacterDialogMoodType.Happy);
			DespawnPointers();
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_EQUIPMENT_CHEST].transform.position, GetLocalPlayerView().transform);

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				Completed = IsFinalChestOpened
			};
		}

		private void OnEnterKillFinalBot()
		{
			_tutorialOverlay.Dialog.ContinueDialog(ScriptLocalization.UITTutorial.drop_down_to_arena, CharacterType.Female, CharacterDialogMoodType.Neutral);
			DespawnPointers();
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_ARENA_DROPDOWN].transform.position, GetLocalPlayerView().transform);

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				Completed = IsFinalBotDead
			};
			_services.MessageBrokerService.Publish(new AdvancedFirstMatchMessage
			{
				State = TutorialFirstMatchStates.EnterKillFinalBot
			});
		}

		private void OnEnterWaitMatchFinish()
		{
			_currentGameplayProceedData = new GameplayProceedEventData();
			_tutorialOverlay.Dialog.ShowDialog(ScriptLocalization.UITTutorial.you_made_it_look_easy, CharacterType.Female, CharacterDialogMoodType.Happy,
				CharacterDialogPosition.TopLeft);
		}

		private bool IsBarrierDestroyed(Frame f)
		{
			if (!f.TryGetSingleton<TutorialRuntimeData>(out var tutorialData))
			{
				return false;
			}

			return !f.Exists(tutorialData.FirstBarrier);
		}

		private bool DoesLocalPlayerHaveWeapon(Frame f)
		{
			var playerEntity = _matchServices.SpectateService.GetSpectatedEntity();
			if (!f.Exists(playerEntity))
			{
				return false;
			}

			return f.TryGet<PlayerCharacter>(playerEntity, out var playerCharacter)
			       && playerCharacter.CurrentWeapon.IsValid()
			       && !playerCharacter.HasMeleeWeapon(f, playerEntity);
		}

		private bool AreTheFirstBotsDead(Frame f)
		{
			if (!f.TryGetSingleton<TutorialRuntimeData>(out var tutorialData))
			{
				return false;
			}

			for (int i = 0; i < tutorialData.FirstBots.Length; i++)
			{
				var entityRef = tutorialData.FirstBots[i];
				if (IsPlayerAlive(f, entityRef))
				{
					return false;
				}
			}

			return true;
		}


		private bool HasSpecial(Frame f)
		{
			var localPlayer = _matchServices.SpectateService.GetSpectatedEntity();
			return f.TryGet<PlayerInventory>(localPlayer, out var inventory) && inventory.HasAnySpecial();
		}


		private bool IsGrenadeFlying(Frame f)
		{
			return f.ComponentCount<Hazard>() > 0;
		}


		private bool IsGrenadeBotDead(Frame f)
		{
			if (!f.TryGetSingleton<TutorialRuntimeData>(out var tutorialData))
			{
				return false;
			}

			return !IsPlayerAlive(f, tutorialData.GrenadeBot);
		}

		private bool IsFinalChestOpened(Frame f)
		{
			return f.ComponentCount<Chest>() == 0;
		}


		private bool IsPlayerAlive(Frame f, EntityRef entityRef)
		{
			return f.Exists(entityRef) && f.Has<AlivePlayerCharacter>(entityRef);
		}

		private bool IsFinalBotDead(Frame f)
		{
			if (!f.TryGetSingleton<TutorialRuntimeData>(out var tutorialData))
			{
				return false;
			}

			return !IsPlayerAlive(f, tutorialData.FinalBot);
		}
	}
}
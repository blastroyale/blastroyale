using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
		}

		public static readonly IStatechartEvent ProceedTutorialEvent = new StatechartEvent("TUTORIAL - Proceed tutorial event");
		public static readonly IStatechartEvent GrenadeMissedTutorialEvent = new StatechartEvent("TUTORIAL - Grenade Missed");
		public static readonly IStatechartEvent SkipTutorialEvent = new StatechartEvent("TUTORIAL - SkipTutorialEvent");

		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly IInternalTutorialService _tutorialService;

		private IMatchServices _matchServices;
		private CharacterDialogScreenPresenter _dialogUi;
		private GuideHandPresenter _guideHandUi;
		private HUDScreenPresenter _hud;
		private Dictionary<string, GameObject> _tutorialObjectRefs = new ();
		private List<LocationPointerVfxMonoComponent> _activeLocationPointers = new ();
		private MetaTutorialSequence _sequence;
		private GameplayProceedEventData _currentGameplayProceedData;
		private short _currentKillProceedProgress;

		private bool _hasSpecial0;
		private bool _hasSpecial1;

		public FirstGameTutorialState(IGameDataProvider logic, IGameServices services,
									  IInternalTutorialService tutorialService,
									  Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_dataProvider = logic;
			_tutorialService = tutorialService;
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
			var specialDecision = stateFactory.Choice("Already Has Special?");
			var pickupSpecial = stateFactory.State("Pickup Special");
			var kill1BotSpecial = stateFactory.State("Kill 1 bot special");
			var grenadeMissed = stateFactory.Choice("Grenade Missed");
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
			kill2Bots.Event(ProceedTutorialEvent).Target(specialDecision);

			specialDecision.Transition().Condition(() => _hasSpecial0 || _hasSpecial1).Target(kill1BotSpecial);
			specialDecision.Transition().Target(pickupSpecial);

			pickupSpecial.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.PickupSpecial); });
			pickupSpecial.OnEnter(OnEnterPickupSpecial);
			pickupSpecial.Event(MatchState.MatchUnloadedEvent).Target(final);
			pickupSpecial.Event(ProceedTutorialEvent).Target(kill1BotSpecial);

			kill1BotSpecial.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.Kill1BotSpecial); });
			kill1BotSpecial.OnEnter(OnEnterKill1BotSpecial);
			kill1BotSpecial.Event(MatchState.MatchUnloadedEvent).Target(final);
			kill1BotSpecial.Event(ProceedTutorialEvent).Target(moveToGateArea);
			kill1BotSpecial.Event(GrenadeMissedTutorialEvent).Target(grenadeMissed);

			grenadeMissed.Transition().Condition(() => _hasSpecial0 || _hasSpecial1).Target(kill1BotSpecial);
			grenadeMissed.Transition().Target(pickupSpecial);

			moveToGateArea.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.MoveToGateArea); });
			moveToGateArea.OnEnter(OnEnterMoveToGateArea);
			moveToGateArea.Event(MatchState.MatchUnloadedEvent).Target(final);
			moveToGateArea.Event(ProceedTutorialEvent).Target(moveToChestArea);

			moveToChestArea.OnEnter(() => { _sequence.EnterStep(TutorialClientStep.MoveToChestArea); });
			moveToChestArea.OnEnter(OnEnterMoveToChestArea);
			moveToChestArea.Event(MatchState.MatchUnloadedEvent).Target(final);
			moveToChestArea.Event(ProceedTutorialEvent).Target(openBox);

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

		private void KillTutorialBots()
		{
			QuantumRunner.Default.Game.SendCommand(new CheatKillAllTutorialBots {BehaviourType = BotBehaviourType.Static});
			QuantumRunner.Default.Game.SendCommand(new CheatKillAllTutorialBots {BehaviourType = BotBehaviourType.WanderAndShoot});
		}

		private void GetTutorialUiRefs()
		{
			_dialogUi = _services.GameUiService.GetUi<CharacterDialogScreenPresenter>();
			_guideHandUi = _services.GameUiService.GetUi<GuideHandPresenter>();
		}

		private void GetGroundIndicatorRefs()
		{
			foreach (var indicator in _tutorialService.FindTutorialObjects(GameConstants.Tutorial.TAG_INDICATORS))
			{
				_tutorialObjectRefs.Add(indicator.name, indicator);
			}
		}

		private void CloseTutorialUi()
		{
			_dialogUi.HideDialog(CharacterType.Female);
			_guideHandUi.Hide();
		}

		private EntityView GetLocalPlayerView()
		{
			_matchServices.EntityViewUpdaterService.TryGetView(_matchServices.SpectateService.GetSpectatedEntity(), out var entityView);
			return entityView;
		}


		private void SubscribeMessages()
		{
			QuantumEvent.SubscribeManual<EventOnLocalPlayerAlive>(this, OnLocalPlayerAlive);
			QuantumEvent.SubscribeManual<EventOnEquipmentCollected>(this, OnEquipmentCollected);
			QuantumEvent.SubscribeManual<EventOnHazardLand>(this, OnHazardLand);
			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(this, OnPlayerKilledPlayer);
			QuantumEvent.SubscribeManual<EventOnChestOpened>(this, OnChestOpened);
			QuantumEvent.SubscribeManual<EventOnPlayerDead>(this, OnPlayerDead);
			QuantumEvent.SubscribeManual<EventOnPlayerSpecialUpdated>(this, OnPlayerSpecialUpdated);
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

			if (msg.VolumeId == GameConstants.Tutorial.TRIGGER_ARENA_AREA)
			{
				DespawnPointers();
				_dialogUi.HideDialog(CharacterType.Female);
			}
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

		private async void OnEquipmentCollected(EventOnEquipmentCollected callback)
		{
			await Task.Yield();

			if (callback.PlayerEntity != _matchServices.SpectateService.SpectatedPlayer.Value.Entity) return;

			CheckGameplayProceedConditions(typeof(EventOnEquipmentCollected));
		}

		private async void OnPlayerDead(EventOnPlayerDead callback)
		{
			await Task.Yield();
			CheckGameplayProceedConditions(typeof(EventOnPlayerDead));
		}

		private async void OnHazardLand(EventOnHazardLand callback)
		{
			await Task.Yield();

			if (callback.sourceId == GameId.SkipTutorial)
			{
				_statechartTrigger(SkipTutorialEvent);
				return;
			}

			CheckGameplayProceedConditions(typeof(EventOnHazardLand), callback.sourceId.ToString());

			if (callback.Hits == 0)
			{
				_statechartTrigger(GrenadeMissedTutorialEvent);
			}
		}

		private async void OnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			await Task.Yield();

			// Need to do this, instead of SpectatedPlayer, because when the last kill happens, spectated player
			// gets wiped in SpectateServices, creating a race condition.
			// Instead, just get the local player ref from the game data itself
			var localPlayer = QuantumRunner.Default.Game.GetLocalPlayerData(false, out var f);
			var localPlayerEntity = localPlayer.Entity;

			if (callback.EntityKiller != localPlayerEntity) return;

			_currentKillProceedProgress += 1;
			CheckGameplayProceedConditions(typeof(EventOnPlayerKilledPlayer), "", _currentKillProceedProgress);
		}

		private void OnChestOpened(EventOnChestOpened callback)
		{
			CheckGameplayProceedConditions(typeof(EventOnChestOpened));
		}

		private void OnPlayerSpecialUpdated(EventOnPlayerSpecialUpdated callback)
		{
			if (!_matchServices.IsSpectatingPlayer(callback.Entity)) return;

			if (callback.SpecialIndex == 0)
			{
				_hasSpecial0 = callback.Special.IsValid;
			}
			else
			{
				_hasSpecial1 = callback.Special.IsValid;
			}

			CheckGameplayProceedConditions(typeof(EventOnPlayerSpecialUpdated));
		}

		private void UnsubscribeMessages()
		{
			QuantumEvent.UnsubscribeListener(this);
			_services.MessageBrokerService.UnsubscribeAll(this);
		}

		private void StartFirstTutorialMatch()
		{
			_tutorialService.CreateJoinFirstTutorialRoom();
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
			_dialogUi.ShowDialog(ScriptLocalization.UITTutorial.welcome_to_wastelands, CharacterType.Female, CharacterDialogMoodType.Happy,
				CharacterDialogPosition.TopLeft);

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnLocalPlayerAlive)
			};
		}

		private void SetFingerPosition(VisualElement element, float angle = 45)
		{
			var root = _hud.Document.rootVisualElement;
			var elementPosition = element.GetPositionOnScreen(root);
			_guideHandUi.SetScreenPosition(elementPosition, angle);
		}

		private async Task OnEnterMoveJoystickAsync()
		{
			_matchServices.PlayerInputService.OnQuantumInputSent += OnInput;
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.use_left_joystick, CharacterType.Female, CharacterDialogMoodType.Neutral);
			_hud = _services.GameUiService.GetUi<HUDScreenPresenter>();
			await _hud.EnsureOpen();
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
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.move_forward, CharacterType.Female, CharacterDialogMoodType.Happy);
			DespawnPointers();
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_FIRST_MOVE].transform.position, GetLocalPlayerView().transform);
			_guideHandUi.Hide();

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(PlayerEnteredMessageVolume),
				EventMetaId = GameConstants.Tutorial.TRIGGER_FIRST_MOVE_AREA
			};
		}

		private void OnEnterDestroyBarrier()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.shoot_barrier, CharacterType.Female, CharacterDialogMoodType.Neutral);
			DespawnPointers();
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_WOODEN_BARRIER].transform.position, GetLocalPlayerView().transform);
			SetFingerPosition(_hud.ShootingJoystick, 90);

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnHazardLand),
				EventMetaId = GameId.Barrier.ToString()
			};
		}

		private void OnEnterPickupWeapon()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.pick_up_weapon, CharacterType.Female, CharacterDialogMoodType.Neutral);
			DespawnPointers();
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_FIRST_WEAPON].transform.position, GetLocalPlayerView().transform);
			_guideHandUi.Hide();

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnEquipmentCollected)
			};
		}

		private void OnEnterMoveToDummyArea()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.nice_proceed_dummy_area, CharacterType.Female, CharacterDialogMoodType.Shocked);
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
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.shoot_dummies, CharacterType.Female, CharacterDialogMoodType.Happy);
			DespawnPointers();
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_BOT1].transform.position, GetLocalPlayerView().transform);
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_BOT2].transform.position, GetLocalPlayerView().transform);

			_currentKillProceedProgress = 0;
			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnPlayerKilledPlayer),
				EventMetaAmount = 2
			};
			_services.MessageBrokerService.Publish(new AdvancedFirstMatchMessage
			{
				State = TutorialFirstMatchStates.EnterKill2Bots
			});
		}

		private void OnEnterPickupSpecial()
		{
			QuantumRunner.Default.Game.SendCommand(new TutorialSpawnSpecialCommand());
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.pick_up_special, CharacterType.Female, CharacterDialogMoodType.Neutral);
			DespawnPointers();
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_SPECIAL_PICKUP].transform.position, GetLocalPlayerView().transform);
			_guideHandUi.Hide();

			_currentGameplayProceedData = new GameplayProceedEventData
			{
				EventType = typeof(EventOnPlayerSpecialUpdated)
			};
		}

		private void OnEnterKill1BotSpecial()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.use_grenade, CharacterType.Female, CharacterDialogMoodType.Neutral);
			DespawnPointers();
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_BOT3].transform.position, GetLocalPlayerView().transform);

			SetFingerPosition(_hasSpecial0 ? _hud.Special0 : _hud.Special1, 90);

			_currentKillProceedProgress = 0;
			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnPlayerKilledPlayer),
				EventMetaAmount = 1
			};
		}

		private void OnEnterMoveToGateArea()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.proceed_iron_gate, CharacterType.Female, CharacterDialogMoodType.Happy);
			DespawnPointers();
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_IRON_GATE].transform.position, GetLocalPlayerView().transform);
			_guideHandUi.Hide();

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(PlayerEnteredMessageVolume),
				EventMetaId = GameConstants.Tutorial.TRIGGER_GATE_AREA
			};
		}

		private void OnEnterMoveToChestArea()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.nice_proceed_chest_area, CharacterType.Female, CharacterDialogMoodType.Neutral);
			DespawnPointers();
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_TOP_PLATFORM].transform.position, GetLocalPlayerView().transform);

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(PlayerEnteredMessageVolume),
				EventMetaId = GameConstants.Tutorial.TRIGGER_CHEST_AREA
			};
		}

		private void OnEnterOpenBox()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.open_chest, CharacterType.Female, CharacterDialogMoodType.Happy);
			DespawnPointers();
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_EQUIPMENT_CHEST].transform.position, GetLocalPlayerView().transform);

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnChestOpened)
			};
		}

		private void OnEnterKillFinalBot()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.drop_down_to_arena, CharacterType.Female, CharacterDialogMoodType.Neutral);
			DespawnPointers();
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_ARENA_DROPDOWN].transform.position, GetLocalPlayerView().transform);

			_currentKillProceedProgress = 0;
			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnPlayerDead)
			};
			_services.MessageBrokerService.Publish(new AdvancedFirstMatchMessage
			{
				State = TutorialFirstMatchStates.EnterKillFinalBot
			});
		}

		private void OnEnterWaitMatchFinish()
		{
			_dialogUi.ShowDialog(ScriptLocalization.UITTutorial.you_made_it_look_easy, CharacterType.Female, CharacterDialogMoodType.Happy,
				CharacterDialogPosition.TopLeft);
		}
	}
}
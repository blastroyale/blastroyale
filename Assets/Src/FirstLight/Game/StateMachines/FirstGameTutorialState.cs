using System;
using System.Collections;
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
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using I2.Loc;
using NUnit.Framework;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	public class FirstGameTutorialState : ITutorialSequence
	{
		struct GameplayProceedEventData
		{
			public Type EventType;
			public string EventMetaId;
			public short EventMetaAmount;
		}

		// !!! CRITICAL - UPDATE THIS WHEN STEPS ARE CHANGED !!!
		public static readonly int TOTAL_STEPS = 16;
		public static readonly IStatechartEvent ProceedGameplayTutorialEvent = new StatechartEvent("TUTORIAL - Proceed gameplay tutorial event");

		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly IInternalTutorialService _tutorialService;

		private IMatchServices _matchServices;
		private CharacterDialogScreenPresenter _dialogUi;
		private GuideHandPresenter _guideHandUi;
		private Dictionary<string, GameObject> _tutorialObjectRefs = new();
		private List<LocationPointerVfxMonoComponent> _activeLocationPointers = new();
		private EntityView _localPlayerEntityView;
		
		public string SectionName { get; set; }
		public int SectionVersion { get; set; }
		public int CurrentStep { get; set; }
		public int CurrentTotalStep => CurrentStep + TotalStepsBeforeThisSection;
		public string CurrentStepName { get; set; }
		public int TotalStepsBeforeThisSection => 0;

		private GameplayProceedEventData _currentGameplayProceedData;
		private short _currentKillProceedProgress;

		public FirstGameTutorialState(IGameDataProvider logic, IGameServices services,
									  IInternalTutorialService tutorialService,
									  Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_dataProvider = logic;
			_tutorialService = tutorialService;
			_statechartTrigger = statechartTrigger;
		}

		public void InitSequenceData()
		{
			SectionName = TutorialSection.FIRST_GUIDE_MATCH.ToString();
			SectionVersion = 1;
			CurrentStep = 1;
			CurrentStepName = "TutorialStart";
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
			var pickupWeapon = stateFactory.State("Pickup Weapon");
			var destroyBarrier = stateFactory.State("Destroy barrier");
			var moveToDummyArea = stateFactory.State("Move to dummy area");
			var kill2Bots = stateFactory.State("Kill 2 bots");
			var kill1BotSpecial = stateFactory.State("Kill 1 bot special");
			var moveToGateArea = stateFactory.State("Proceed through iron gate");
			var moveToChestArea = stateFactory.State("Move to chest area");
			var openBox = stateFactory.State("Open box");
			var killFinalBot = stateFactory.State("Kill final bot");
			var waitMatchFinish = stateFactory.State("Wait simulation finish");

			initial.Transition().Target(createTutorialRoom);
			initial.OnExit(SubscribeMessages);
			initial.OnExit(InitSequenceData);
			initial.OnExit(GetTutorialUiRefs);

			createTutorialRoom.OnEnter(() => { SendAnalyticsIncrementStep("CreateTutorialRoom"); });
			createTutorialRoom.OnEnter(StartFirstTutorialMatch);
			createTutorialRoom.Event(NetworkState.JoinedRoomEvent).Target(waitSimulationStart);

			waitSimulationStart.OnEnter(() => { SendAnalyticsIncrementStep("WaitSimulationStart"); });
			waitSimulationStart.Event(GameSimulationState.SimulationStartedEvent).Target(startedSimulation);
			waitSimulationStart.OnExit(BindMatchServices);

			startedSimulation.OnEnter(() => { SendAnalyticsIncrementStep("Spawn"); });
			startedSimulation.OnEnter(GetGroundIndicatorRefs);
			startedSimulation.OnEnter(OnEnterStartedSimulation);
			startedSimulation.Event(ProceedGameplayTutorialEvent).Target(moveJoystick);
			
			moveJoystick.OnEnter(() => { SendAnalyticsIncrementStep("MoveJoystick"); });
			moveJoystick.OnEnter(GetGuideUiRefs);
			moveJoystick.OnEnter(OnEnterMoveJoystick);
			moveJoystick.Event(ProceedGameplayTutorialEvent).Target(destroyBarrier);

			destroyBarrier.OnEnter(() => { SendAnalyticsIncrementStep("DestroyBarrier"); });
			destroyBarrier.OnEnter(OnEnterDestroyBarrier);
			destroyBarrier.Event(ProceedGameplayTutorialEvent).Target(pickupWeapon);

			pickupWeapon.OnEnter(() => { SendAnalyticsIncrementStep("PickUpWeapon"); });
			pickupWeapon.OnEnter(OnEnterPickupWeapon);
			pickupWeapon.Event(ProceedGameplayTutorialEvent).Target(moveToDummyArea);
			
			moveToDummyArea.OnEnter(() => { SendAnalyticsIncrementStep("MoveToDummyArea"); });
			moveToDummyArea.OnEnter(OnEnterMoveToDummyArea);
			moveToDummyArea.Event(ProceedGameplayTutorialEvent).Target(kill2Bots);

			kill2Bots.OnEnter(() => { SendAnalyticsIncrementStep("Kill2Bots"); });
			kill2Bots.OnEnter(OnEnterKill2Bots);
			kill2Bots.Event(ProceedGameplayTutorialEvent).Target(kill1BotSpecial);

			kill1BotSpecial.OnEnter(() => { SendAnalyticsIncrementStep("Kill1BotSpecial"); });
			kill1BotSpecial.OnEnter(OnEnterKill1BotSpecial);
			kill1BotSpecial.Event(ProceedGameplayTutorialEvent).Target(moveToGateArea);

			moveToGateArea.OnEnter(() => { SendAnalyticsIncrementStep("MoveToGateArea"); });
			moveToGateArea.OnEnter(OnEnterMoveToGateArea);
			moveToGateArea.Event(ProceedGameplayTutorialEvent).Target(moveToChestArea);
			
			moveToChestArea.OnEnter(() => { SendAnalyticsIncrementStep("MoveToChestArea"); });
			moveToChestArea.OnEnter(OnEnterMoveToChestArea);
			moveToChestArea.Event(ProceedGameplayTutorialEvent).Target(openBox);

			openBox.OnEnter(() => { SendAnalyticsIncrementStep("OpenBox"); });
			openBox.OnEnter(OnEnterOpenBox);
			openBox.Event(ProceedGameplayTutorialEvent).Target(killFinalBot);

			killFinalBot.OnEnter(() => { SendAnalyticsIncrementStep("KillFinalBot"); });
			killFinalBot.OnEnter(OnEnterKillFinalBot);
			killFinalBot.Event(ProceedGameplayTutorialEvent).Target(waitMatchFinish);

			waitMatchFinish.OnEnter(() => { SendAnalyticsIncrementStep("MatchEnded"); });
			waitMatchFinish.OnEnter(OnEnterWaitMatchFinish);
			waitMatchFinish.Event(MatchState.MatchUnloadedEvent).Target(final);
			waitMatchFinish.OnExit(() => { SendAnalyticsIncrementStep("TutorialFinish"); });

			final.OnEnter(CloseTutorialUi);
			final.OnEnter(SendStepAnalytics);
			final.OnEnter(UnsubscribeMessages);
		}

		private void GetTutorialUiRefs()
		{
			_dialogUi = _services.GameUiService.GetUi<CharacterDialogScreenPresenter>();
			_guideHandUi = _services.GameUiService.GetUi<GuideHandPresenter>();
		}

		private void GetGroundIndicatorRefs()
		{
			var indicatorObjects = GameObject.FindGameObjectsWithTag(GameConstants.Tutorial.TAG_INDICATORS);
			
			foreach (var indicator in indicatorObjects)
			{
				_tutorialObjectRefs.Add(indicator.name, indicator);
			}
		}

		private void GetGuideUiRefs()
		{
			var guideUiObjects = GameObject.FindGameObjectsWithTag(GameConstants.Tutorial.TAG_GUIDE_UI);
			
			foreach (var guideUid in guideUiObjects)
			{
				_tutorialObjectRefs.Add(guideUid.name, guideUid);
			}
		}

		private void CloseTutorialUi()
		{
			_dialogUi.HideDialog(CharacterType.Female);
			_guideHandUi.Hide();
		}

		private void SubscribeMessages()
		{
			QuantumEvent.SubscribeManual<EventOnLocalPlayerAlive>(this, OnLocalPlayerAlive);
			QuantumEvent.SubscribeManual<EventOnEquipmentCollected>(this, OnEquipmentCollected);
			QuantumEvent.SubscribeManual<EventOnHazardLand>(this, OnHazardLand);
			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(this, OnPlayerKilledPlayer);
			QuantumEvent.SubscribeManual<EventOnChestOpened>(this, OnChestOpened);
			_services.MessageBrokerService.Subscribe<PlayerUsedMovementJoystick>(OnPlayerUsedMovementJoystick);
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

		private void OnPlayerUsedMovementJoystick(PlayerUsedMovementJoystick msg)
		{
			CheckGameplayProceedConditions(typeof(PlayerUsedMovementJoystick));
		}

		private void BindMatchServices()
		{
			_matchServices = MainInstaller.Resolve<IMatchServices>();
		}

		private async void OnLocalPlayerAlive(EventOnLocalPlayerAlive callback)
		{
			if (callback.Entity == _matchServices.SpectateService.SpectatedPlayer.Value.Entity &&
			    _matchServices.EntityViewUpdaterService.TryGetView(callback.Entity, out var entityView))
			{
				_localPlayerEntityView = entityView;
			}
			
			await Task.Yield();

			CheckGameplayProceedConditions(typeof(EventOnLocalPlayerAlive));
		}

		private async void OnEquipmentCollected(EventOnEquipmentCollected callback)
		{
			await Task.Yield();

			if (callback.PlayerEntity != _matchServices.SpectateService.SpectatedPlayer.Value.Entity) return;

			CheckGameplayProceedConditions(typeof(EventOnEquipmentCollected));
		}

		private async void OnHazardLand(EventOnHazardLand callback)
		{
			await Task.Yield();

			CheckGameplayProceedConditions(typeof(EventOnHazardLand), callback.sourceId.ToString());
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

		private void UnsubscribeMessages()
		{
			QuantumEvent.UnsubscribeListener(this);
		}

		private void StartFirstTutorialMatch()
		{
			_tutorialService.CreateJoinFirstTutorialRoom();
		}

		public void SendAnalyticsIncrementStep(string newStepName)
		{
			SendStepAnalytics();

			CurrentStep += 1;
			CurrentStepName = newStepName;
		}

		public void SendStepAnalytics()
		{
			_services.AnalyticsService.TutorialCalls.CompleteTutorialStep(SectionName, SectionVersion, CurrentStep,
																		  CurrentTotalStep, CurrentStepName);
		}

		private void CheckGameplayProceedConditions(Type eventType, string metaId = "", short metaAmount = 0)
		{
			if (_currentGameplayProceedData.EventType != eventType) return;

			if (!string.IsNullOrEmpty(_currentGameplayProceedData.EventMetaId) &&
			    _currentGameplayProceedData.EventMetaId != metaId) return;

			if (_currentGameplayProceedData.EventMetaAmount != 0 &&
			    _currentGameplayProceedData.EventMetaAmount > metaAmount) return;

			_statechartTrigger(ProceedGameplayTutorialEvent);
		}

		private void OnEnterStartedSimulation()
		{
			_dialogUi.ShowDialog(ScriptLocalization.UITTutorial.welcome_to_wastelands, CharacterType.Female, CharacterDialogMoodType.Happy, CharacterDialogPosition.TopLeft);

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnLocalPlayerAlive)
			};
		}

		private void OnEnterMoveJoystick()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.use_left_joystick, CharacterType.Female, CharacterDialogMoodType.Neutral);
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_WOODEN_BARRIER].transform.position, _localPlayerEntityView.transform);
			_guideHandUi.SetPositionAndShow(_tutorialObjectRefs[GameConstants.Tutorial.GUIDE_UI_MOVEMENT_JOYSTICK].transform.position);
			
			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(PlayerUsedMovementJoystick)
			};
		}

		private void OnEnterDestroyBarrier()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.shoot_barrier, CharacterType.Female, CharacterDialogMoodType.Happy);
			_guideHandUi.Hide();
			
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
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_FIRST_WEAPON].transform.position, _localPlayerEntityView.transform);

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnEquipmentCollected)
			};
		}

		private void OnEnterMoveToDummyArea()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.nice_proceed_dummy_area, CharacterType.Female, CharacterDialogMoodType.Shocked);
			DespawnPointers();
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_BOT_AREA].transform.position, _localPlayerEntityView.transform);
			
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
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_BOT1].transform.position, _localPlayerEntityView.transform);
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_BOT2].transform.position, _localPlayerEntityView.transform);
			
			_currentKillProceedProgress = 0;
			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnPlayerKilledPlayer),
				EventMetaAmount = 2
			};
		}

		private void OnEnterKill1BotSpecial()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.use_grenade, CharacterType.Female, CharacterDialogMoodType.Neutral);
			DespawnPointers();
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_BOT3].transform.position, _localPlayerEntityView.transform);
			
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
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_IRON_GATE].transform.position, _localPlayerEntityView.transform);
			
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
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_TOP_PLATFORM].transform.position, _localPlayerEntityView.transform);

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
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_EQUIPMENT_CHEST].transform.position, _localPlayerEntityView.transform);

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnChestOpened)
			};
		}

		private void OnEnterKillFinalBot()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.drop_down_to_arena, CharacterType.Female, CharacterDialogMoodType.Neutral);
			DespawnPointers();
			SpawnNewPointer(_tutorialObjectRefs[GameConstants.Tutorial.INDICATOR_ARENA_DROPDOWN].transform.position, _localPlayerEntityView.transform);

			_currentKillProceedProgress = 0;
			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnPlayerKilledPlayer),
				EventMetaAmount = 1
			};
		}

		private void OnEnterWaitMatchFinish()
		{
			_dialogUi.ShowDialog(ScriptLocalization.UITTutorial.you_made_it_look_easy, CharacterType.Female, CharacterDialogMoodType.Happy, CharacterDialogPosition.TopLeft);
		}
	}
}
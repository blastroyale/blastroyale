using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
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

		public static readonly IStatechartEvent ProceedGameplayTutorialEvent =
			new StatechartEvent("TUTORIAL - Proceed gameplay tutorial event");

		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly IInternalTutorialService _tutorialService;

		private IMatchServices _matchServices;
		private CharacterDialogScreenPresenter _dialogUi;

		public string SectionName { get; set; }
		public int SectionVersion { get; set; }
		public int CurrentStep { get; set; }
		public int CurrentTotalStep => CurrentStep + TotalStepsBeforeThisSection;
		public string CurrentStepName { get; set; }
		public int TotalStepsBeforeThisSection { get; set; }

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
			TotalStepsBeforeThisSection = 0;
		}

		/// <summary>
		/// Setups the audio state - root state, and then per gamemode type nested states
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var loadTutorialUi = stateFactory.TaskWait("Load tutorial UI");
			var createTutorialRoom = stateFactory.State("Create tutorial room");
			var waitSimulationStart = stateFactory.State("Waiting for match start");
			var startedSimulation = stateFactory.State("Playing tutorial match");
			var moveJoystick = stateFactory.State("Move joystick");
			var pickupWeapon = stateFactory.State("Pickup Weapon");
			var destroyBarrier = stateFactory.State("Destroy barrier");
			var moveToDummyArea = stateFactory.State("Move to dummy area");
			var kill2Bots = stateFactory.State("Kill 2 bots");
			var kill1BotSpecial = stateFactory.State("Kill 1 bot special");
			var moveToChestArea = stateFactory.State("Move to chest area");
			var openBox = stateFactory.State("Open box");
			var killFinalBot = stateFactory.State("Kill final bot");
			var waitMatchFinish = stateFactory.State("Wait simulation finish");

			initial.Transition().Target(loadTutorialUi);
			initial.OnExit(SubscribeMessages);
			initial.OnExit(InitSequenceData);

			loadTutorialUi.WaitingFor(OpenTutorialScreens).Target(createTutorialRoom);

			createTutorialRoom.OnEnter(() => { SendAnalyticsIncrementStep("CreateTutorialRoom"); });
			createTutorialRoom.OnEnter(StartFirstTutorialMatch);
			createTutorialRoom.Event(NetworkState.JoinedRoomEvent).Target(waitSimulationStart);

			waitSimulationStart.OnEnter(() => { SendAnalyticsIncrementStep("StartTutorialSimulation"); });
			waitSimulationStart.Event(GameSimulationState.SimulationStartedEvent).Target(startedSimulation);
			waitSimulationStart.OnExit(BindMatchServices);

			startedSimulation.OnEnter(() => { SendAnalyticsIncrementStep("Spawn"); });
			startedSimulation.OnEnter(OnEnterStartedSimulation);
			startedSimulation.Event(ProceedGameplayTutorialEvent).Target(moveJoystick);

			moveJoystick.OnEnter(() => { SendAnalyticsIncrementStep("MoveJoystick"); });
			moveJoystick.OnEnter(OnEnterMoveJoystick);
			moveJoystick.Event(ProceedGameplayTutorialEvent).Target(pickupWeapon);

			pickupWeapon.OnEnter(() => { SendAnalyticsIncrementStep("PickUpWeapon"); });
			pickupWeapon.OnEnter(OnEnterPickupWeapon);
			pickupWeapon.Event(ProceedGameplayTutorialEvent).Target(destroyBarrier);

			destroyBarrier.OnEnter(() => { SendAnalyticsIncrementStep("DestroyBarrier"); });
			destroyBarrier.OnEnter(OnEnterDestroyBarrier);
			destroyBarrier.Event(ProceedGameplayTutorialEvent).Target(moveToDummyArea);

			moveToDummyArea.OnEnter(() => { SendAnalyticsIncrementStep("MoveToDummyArea"); });
			moveToDummyArea.OnEnter(OnEnterMoveToDummyArea);
			moveToDummyArea.Event(ProceedGameplayTutorialEvent).Target(kill2Bots);

			kill2Bots.OnEnter(() => { SendAnalyticsIncrementStep("Kill2Bots"); });
			kill2Bots.OnEnter(OnEnterKill2Bots);
			kill2Bots.Event(ProceedGameplayTutorialEvent).Target(kill1BotSpecial);

			kill1BotSpecial.OnEnter(() => { SendAnalyticsIncrementStep("Kill1BotSpecial"); });
			kill1BotSpecial.OnEnter(OnEnterKill1BotSpecial);
			kill1BotSpecial.Event(ProceedGameplayTutorialEvent).Target(moveToChestArea);

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
			waitMatchFinish.Event(MatchState.MatchEndedEvent).Target(final);
			waitMatchFinish.OnExit(() => { SendAnalyticsIncrementStep("TutorialFinish"); });

			final.OnEnter(CloseTutorialScreens);
			final.OnEnter(SendStepAnalytics);
			final.OnEnter(UnsubscribeMessages);
		}

		private async Task OpenTutorialScreens()
		{
			await _services.GameUiService.OpenUiAsync<CharacterDialogScreenPresenter>();
			_dialogUi = _services.GameUiService.GetUi<CharacterDialogScreenPresenter>();
		}
		
		private async void CloseTutorialScreens()
		{
			_dialogUi.HideDialog(CharacterType.Female);
			
			// Wait for any anims to finish from before before closing the UI
			await Task.Delay(GameConstants.Tutorial.TUTORIAL_SCREEN_OUTRO_CLOSE_TIME);
			
			_services.GameUiService.CloseUi<CharacterDialogScreenPresenter>(true);
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

		private void OnPlayerEnteredMessageVolume(PlayerEnteredMessageVolume msg)
		{
			CheckGameplayProceedConditions(typeof(PlayerEnteredMessageVolume), msg.VolumeId);

			if (msg.VolumeId == GameConstants.Tutorial.TRIGGER_ARENA_AREA)
			{
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

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(PlayerUsedMovementJoystick)
			};
		}

		private void OnEnterPickupWeapon()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.pick_up_weapon, CharacterType.Female, CharacterDialogMoodType.Neutral);

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnEquipmentCollected)
			};
		}

		private void OnEnterDestroyBarrier()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.shoot_barrier, CharacterType.Female, CharacterDialogMoodType.Happy);

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnHazardLand),
				EventMetaId = GameId.Barrier.ToString()
			};
		}

		private void OnEnterMoveToDummyArea()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.nice_proceed_dummy_area, CharacterType.Female, CharacterDialogMoodType.Shocked);

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(PlayerEnteredMessageVolume),
				EventMetaId = GameConstants.Tutorial.TRIGGER_DUMMY_AREA
			};
		}

		private void OnEnterKill2Bots()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.shoot_dummies, CharacterType.Female, CharacterDialogMoodType.Shocked);

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

			_currentKillProceedProgress = 0;
			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnPlayerKilledPlayer),
				EventMetaAmount = 1
			};
		}

		private void OnEnterMoveToChestArea()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.nice_proceed_chest_area, CharacterType.Female, CharacterDialogMoodType.Shocked);

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(PlayerEnteredMessageVolume),
				EventMetaId = GameConstants.Tutorial.TRIGGER_CHEST_AREA
			};
		}

		private void OnEnterOpenBox()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.open_chest, CharacterType.Female, CharacterDialogMoodType.Neutral);

			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnChestOpened)
			};
		}

		private void OnEnterKillFinalBot()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.drop_down_to_arena, CharacterType.Female, CharacterDialogMoodType.Neutral);

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
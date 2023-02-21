using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
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
			public GameId EventMetaId;
			public short EventMetaAmount;
		}
		
		public static readonly IStatechartEvent ProceedGameplayTutorialEvent = new StatechartEvent("TUTORIAL - Proceed gameplay tutorial event");
		
		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly IInternalTutorialService _tutorialService;
		
		private IMatchServices _matchServices;
		
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
			var createTutorialRoom = stateFactory.State("Create tutorial room");
			var waitSimulationStart = stateFactory.State("Waiting for match start");
			var startedSimulation = stateFactory.State("Playing tutorial match");
			var pickupWeapon =  stateFactory.State("Pickup Weapon");
			var destroyBarrier =  stateFactory.State("Destroy barrier");
			var kill2Bots =  stateFactory.State("Kill 2 bots");
			var kill1BotSpecial =  stateFactory.State("Kill 1 bot special");
			var openBox =  stateFactory.State("Open box");
			var killFinalBot =  stateFactory.State("Kill final bot");
			var waitMatchFinish =  stateFactory.State("Wait simulation finish");
			
			initial.Transition().Target(createTutorialRoom);
			initial.OnExit(SubscribeMessages);
			initial.OnExit(InitSequenceData);
			initial.OnExit(() => { SendAnalyticsIncrementStep("CreateTutorialRoom"); });

			createTutorialRoom.OnEnter(StartFirstTutorialMatch);
			createTutorialRoom.Event(NetworkState.JoinedRoomEvent).Target(waitSimulationStart);
			createTutorialRoom.OnExit(() => { SendAnalyticsIncrementStep("CreatedStartedTutorialMatch"); });

			waitSimulationStart.Event(GameSimulationState.SimulationStartedEvent).Target(startedSimulation);
			waitSimulationStart.OnExit(() => { SendAnalyticsIncrementStep("StartedSimulation"); });
			waitSimulationStart.OnExit(BindMatchServices);

			// Match started, player spawned and alive to proceed
			startedSimulation.OnEnter(OnStartSimulation);
			startedSimulation.Event(ProceedGameplayTutorialEvent).Target(pickupWeapon);
			startedSimulation.OnExit(() => { SendAnalyticsIncrementStep("Spawned"); });
			
			// Player spawned and alive, pickup weapon
			pickupWeapon.OnEnter(OnPlayerAlive);
			pickupWeapon.Event(ProceedGameplayTutorialEvent).Target(destroyBarrier);
			pickupWeapon.OnExit(() => { SendAnalyticsIncrementStep("PickedUpWeapon"); });
			
			// Player picked up weapon, destroy barrier to proceed
			destroyBarrier.OnEnter(OnWeaponPickedUp);
			destroyBarrier.Event(ProceedGameplayTutorialEvent).Target(kill2Bots);
			destroyBarrier.OnExit(() => { SendAnalyticsIncrementStep("DestroyedBarrier"); });
			
			// Player destroyed barrier, kill 2 bots to proceed
			kill2Bots.OnEnter(OnBarrierDestroyed);
			kill2Bots.Event(ProceedGameplayTutorialEvent).Target(kill1BotSpecial);
			kill2Bots.OnExit(() => { SendAnalyticsIncrementStep("Killed2Bots"); });
			
			// Player killed 2 bots, kill 1 more bot to proceed (explanation says special, but they can kill with anything)
			kill1BotSpecial.OnEnter(On2BotsKilled);
			kill1BotSpecial.Event(ProceedGameplayTutorialEvent).Target(openBox);
			kill1BotSpecial.OnExit(() => { SendAnalyticsIncrementStep("Killed1BotSpecial"); });
			
			// Player killed 1 more bot, open box to proceed
			openBox.OnEnter(On1BotKilledSpecial);
			openBox.Event(ProceedGameplayTutorialEvent).Target(killFinalBot);
			openBox.OnExit(() => { SendAnalyticsIncrementStep("OpenedBox"); });
			
			// Player opened box, kill final bot to proceed
			killFinalBot.OnEnter(OnChestOpened);
			killFinalBot.Event(ProceedGameplayTutorialEvent).Target(waitMatchFinish);
			killFinalBot.OnExit(() => { SendAnalyticsIncrementStep("KilledFinalBot"); });
			
			// Player killed final bot, wait for match to finish
			waitMatchFinish.OnEnter(OnFinalBotKilled);
			waitMatchFinish.Event(MatchState.MatchEndedEvent).Target(final);
			waitMatchFinish.OnExit(() => { SendAnalyticsIncrementStep("TutorialMatchFinished"); });
			
			final.OnEnter(SendStepAnalytics);
			final.OnEnter(UnsubscribeMessages);
		}
		
		private void SubscribeMessages()
		{
			QuantumEvent.SubscribeManual<EventOnLocalPlayerAlive>(this, OnLocalPlayerAlive);
			QuantumEvent.SubscribeManual<EventOnEquipmentCollected>(this, OnEquipmentCollected);
			QuantumEvent.SubscribeManual<EventOnHazardLand>(this, OnHazardLand);
			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(this, OnPlayerKilledPlayer);
			QuantumEvent.SubscribeManual<EventOnChestOpened>(this, OnChestOpened);
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
			
			CheckGameplayProceedConditions(typeof(EventOnHazardLand), callback.sourceId);
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
			CheckGameplayProceedConditions(typeof(EventOnPlayerKilledPlayer), GameId.Random, _currentKillProceedProgress);
		}
		
		private void OnChestOpened(EventOnChestOpened callback)
		{
			CheckGameplayProceedConditions(typeof(EventOnChestOpened));
		}

		private void UnsubscribeMessages()
		{
			Debug.LogError("FINITO NOW!!!!");
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

		private void CheckGameplayProceedConditions(Type eventType, GameId metaId = GameId.Random, short metaAmount = 0)
		{
			if (_currentGameplayProceedData.EventType != eventType) return;

			if (_currentGameplayProceedData.EventMetaId != GameId.Random &&
			    _currentGameplayProceedData.EventMetaId != metaId) return;
			
			if(_currentGameplayProceedData.EventMetaAmount != 0 &&
				   _currentGameplayProceedData.EventMetaAmount > metaAmount) return;

			_statechartTrigger(ProceedGameplayTutorialEvent);
		}
		
		private void OnStartSimulation()
		{
			Debug.LogError("WELCOME TO THE WASTELANDS!");
			
			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnLocalPlayerAlive)
			};
		}

		private void OnPlayerAlive()
		{
			Debug.LogError("WALK TO THE WEAPON USING LEFT JOYSTICK, AND PICK IT UP");
			
			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnEquipmentCollected)
			};
		}

		private void OnWeaponPickedUp()
		{
			Debug.LogError("SHOOT AT THE BARRIER USING THE RIGHT JOYSTICK, AND DESTROY IT");
			
			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnHazardLand),
				EventMetaId = GameId.Barrier
			};
		}

		private void OnBarrierDestroyed()
		{
			Debug.LogError("NICE JOB! PROCEED TO NEXT AREA AND SHOOT THE DUMMIES");
			
			_currentKillProceedProgress = 0;
			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnPlayerKilledPlayer),
				EventMetaAmount = 2
			};
		}
		
		private void On2BotsKilled()
		{
			Debug.LogError("USE YOUR GRENADE, AND THROW IT TO DESTROY THE LAST DUMMY");
			
			_currentKillProceedProgress = 0;
			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnPlayerKilledPlayer),
				EventMetaAmount = 1
			};
		}
		
		private void On1BotKilledSpecial()
		{
			Debug.LogError("AWESOME! GO THROUGH THE GATE, AND FIND NEW EQUIPMENT IN A BOX");
			
			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnChestOpened)
			};
		}
		
		private void OnChestOpened()
		{
			Debug.LogError("PICK UP THE EQUIPMENT, AND DROP DOWN TO THE ARENA TO FACE THE FINAL ENEMY");
			
			_currentKillProceedProgress = 0;
			_currentGameplayProceedData = new GameplayProceedEventData()
			{
				EventType = typeof(EventOnPlayerKilledPlayer),
				EventMetaAmount = 1
			};
		}
		
		private void OnFinalBotKilled()
		{
			Debug.LogError("YOU MADE IT LOOK EASY! LET'S SEE HOW YOU FARE IN THE NEXT MATCH!");
		}
	}
}
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
	public class MetaAndMatchTutorialState : ITutorialSequence
	{
		// CRITICAL - UPDATE THIS WHEN STEPS ARE CHANGED
		public static readonly int TOTAL_STEPS = 13;
		
		private static readonly IStatechartEvent _bpLevelUpEvent = new StatechartEvent("TUTORIAL - Battle pass level up event");
		private static readonly IStatechartEvent _finishedClaimingRewardsEvent = new StatechartEvent("TUTORIAL - Finished claiming event");
		private static readonly IStatechartEvent _openedEquipmentScreenEvent = new StatechartEvent("TUTORIAL - Opened equipment screen event");
		private static readonly IStatechartEvent _openedEquipmentCategoryEvent = new StatechartEvent("TUTORIAL - Opened equipment category event");
		private static readonly IStatechartEvent _selectedWeaponEvent = new StatechartEvent("TUTORIAL - Clicked weapon event");
		private static readonly IStatechartEvent _equippedWeaponEvent = new StatechartEvent("TUTORIAL - Equipped weapon event");
		
		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly IInternalTutorialService _tutorialService;
		
		private IMatchServices _matchServices;
		private TutorialUtilsScreenPresenter _tutorialUtilsUi;
		private CharacterDialogScreenPresenter _dialogUi;
		private List<string> _sentAnalyticSteps = new();
		
		public string SectionName { get; set; }
		public int SectionVersion { get; set; }
		public int CurrentStep { get; set; }
		public int CurrentTotalStep => CurrentStep + TotalStepsBeforeThisSection;
		public string CurrentStepName { get; set; }
		public int TotalStepsBeforeThisSection => FirstGameTutorialState.TOTAL_STEPS;


		public MetaAndMatchTutorialState(IGameDataProvider logic, IGameServices services,
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
			SectionName = TutorialSection.META_GUIDE_AND_MATCH.ToString();
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
			var enterName = stateFactory.State("Enter name");
			var completionCheck = stateFactory.Choice("Completion check");
			var battlePass = stateFactory.State("Battle Pass");
			var clickReward = stateFactory.State("Click rewards");
			var claimreward = stateFactory.State("Claim rewards");
			var goToEquipement = stateFactory.State("Click equipment");
			var clickWeaponCategory = stateFactory.State("Click weapon");
			var selectWeapon = stateFactory.State("Select weapon");
			var equipWeapon = stateFactory.State("Equip weapon");
			var playGame = stateFactory.State("Play game");
			var createTutorialRoom = stateFactory.State("Join Room");
			var waitSimulationStart = stateFactory.State("WaitSimulationStart");
			
			initial.Transition().Target(enterName);
			initial.OnExit(SubscribeMessages);
			initial.OnExit(InitSequenceData);
			initial.OnExit(GetTutorialScreenRefs);
			
			enterName.OnEnter(() => { SendAnalyticsIncrementStep("EnterName"); });
			enterName.OnEnter(OnEnterNameEnter);
			enterName.Event(EnterNameState.NameSetEvent).Target(completionCheck);
			enterName.OnExit(OnEnterNameExit);
			
			completionCheck.OnEnter(SendStepAnalytics);
			completionCheck.Transition().Condition(HasNotLeveledBattlePass).Target(battlePass);
			completionCheck.Transition().Condition(HasNotEquippedWeapon).OnTransition(()=>SetCurrentStep(5)).Target(goToEquipement);
			completionCheck.Transition().OnTransition(()=>SetCurrentStep(9)).Target(playGame);
			
			battlePass.OnEnter(() => { SendAnalyticsIncrementStep("BattlePassClick"); });
			battlePass.OnEnter(OnBattlePassEnter);
			battlePass.Event(MainMenuState.BattlePassClickedEvent).Target(clickReward);
			battlePass.OnExit(OnBattlePassExit);
			
			clickReward.OnEnter(() => { SendAnalyticsIncrementStep("ClickReward"); });
			clickReward.OnEnter(OnClickRewardEnter);
			clickReward.Event(_bpLevelUpEvent).Target(claimreward);
			clickReward.OnExit(OnClickRewardExit);
			
			claimreward.OnEnter(() => { SendAnalyticsIncrementStep("ClaimReward"); });
			claimreward.OnEnter(OnClaimRewardEnter);
			claimreward.Event(_finishedClaimingRewardsEvent).Target(goToEquipement);
			claimreward.OnExit(OnClaimRewardExit);

			goToEquipement.OnEnter(() => { SendAnalyticsIncrementStep("GoToEquipment"); });
			goToEquipement.OnEnter(OnGoToEquipmentEnter);
			goToEquipement.Event(_openedEquipmentScreenEvent).Target(clickWeaponCategory);
			goToEquipement.OnExit(OnGoToEquipmentExit);
			
			clickWeaponCategory.OnEnter(() => { SendAnalyticsIncrementStep("ClickWeaponCategory"); });
			clickWeaponCategory.OnEnter(OnClickWeaponCategoryEnter);
			clickWeaponCategory.Event(_openedEquipmentCategoryEvent).Target(selectWeapon);
			clickWeaponCategory.OnExit(OnClickWeaponCategoryExit);
			
			selectWeapon.OnEnter(() => { SendAnalyticsIncrementStep("SelectWeapon"); });
			selectWeapon.OnEnter(OnSelectWeaponEnter);
			selectWeapon.Event(_selectedWeaponEvent).Target(equipWeapon);
			selectWeapon.OnExit(OnSelectWeaponExit);
			
			equipWeapon.OnEnter(() => { SendAnalyticsIncrementStep("EquipWeapon"); });
			equipWeapon.OnEnter(OnEquipWeaponEnter);
			equipWeapon.Event(_equippedWeaponEvent).Target(playGame);
			equipWeapon.OnExit(OnEquipWeaponExit);
			
			playGame.OnEnter(() => { SendAnalyticsIncrementStep("PlayGameClick"); });
			playGame.OnEnter(OnPlayGameEnter);
			playGame.Event(MainMenuState.PlayClickedEvent).Target(createTutorialRoom);
			playGame.OnExit(OnPlayGameExit);
			
			createTutorialRoom.OnEnter(() => { SendAnalyticsIncrementStep("CreateTutorialRoom"); });
			createTutorialRoom.OnEnter(StartSecondTutorialMatch);
			createTutorialRoom.Event(NetworkState.JoinedRoomEvent).Target(waitSimulationStart);
			
			waitSimulationStart.OnEnter(() => { SendAnalyticsIncrementStep("WaitSimulationStart"); });
			waitSimulationStart.Event(GameSimulationState.SimulationStartedEvent).Target(final);
			waitSimulationStart.OnExit(() => { SendAnalyticsIncrementStep("TutorialFinish"); });
			
			final.OnEnter(SendStepAnalytics);
			final.OnEnter(UnsubscribeMessages);
		}

		private bool HasNotEquippedWeapon()
		{
			var slot = GameIdGroup.Weapon;
			return _dataProvider.EquipmentDataProvider.Loadout.TryGetValue(slot, out var item) == false;
		}

		private bool HasNotLeveledBattlePass()
		{
			return _dataProvider.BattlePassDataProvider.CurrentLevel.Value == 0;
		}

		private void StartSecondTutorialMatch()
		{
			_tutorialService.CreateJoinSecondTutorialRoom();
		}

		private void GetTutorialScreenRefs()
		{
			_dialogUi = _services.GameUiService.GetUi<CharacterDialogScreenPresenter>();
			_tutorialUtilsUi = _services.GameUiService.GetUi<TutorialUtilsScreenPresenter>();
		}
		
		private void CloseTutorialUi()
		{
			_tutorialUtilsUi.Unblock();
			_tutorialUtilsUi.RemoveHighlight();
		}

		private void SubscribeMessages()
		{
			_services.MessageBrokerService.Subscribe<BattlePassLevelUpMessage>(OnBattlePassLevelUpMessage);
			_services.MessageBrokerService.Subscribe<FinishedClaimingBpRewardsMessage>(OnFinishedClaimingBpRewardsMessage);
			_services.MessageBrokerService.Subscribe<EquipmentScreenOpenedMessage>(OnEquipmentScreenOpenedMessage);
			_services.MessageBrokerService.Subscribe<EquipmentSlotOpenedMessage>(OnEquipmentSlotOpenedMessage);
			_services.MessageBrokerService.Subscribe<EquippedItemMessage>(OnEquippedItemMessage);
			_services.MessageBrokerService.Subscribe<SelectedEquipmentItemMessage>(OnSelectedEquipmentItemMessage);
		}

		private void OnSelectedEquipmentItemMessage(SelectedEquipmentItemMessage msg)
		{
			_statechartTrigger(_selectedWeaponEvent);
		}

		private void OnEquippedItemMessage(EquippedItemMessage msg)
		{
			_statechartTrigger(_equippedWeaponEvent);
		}

		private void OnEquipmentSlotOpenedMessage(EquipmentSlotOpenedMessage msg)
		{
			_statechartTrigger(_openedEquipmentCategoryEvent);
		}

		private void OnEquipmentScreenOpenedMessage(EquipmentScreenOpenedMessage msg)
		{
			_statechartTrigger(_openedEquipmentScreenEvent);
		}

		private void OnFinishedClaimingBpRewardsMessage(FinishedClaimingBpRewardsMessage msg)
		{
			_statechartTrigger(_finishedClaimingRewardsEvent);
		}
		
		private void OnBattlePassLevelUpMessage(BattlePassLevelUpMessage msg)
		{
			_statechartTrigger(_bpLevelUpEvent);
		}

		private void UnsubscribeMessages()
		{
		}

		private void SetCurrentStep(int step)
		{
			CurrentStep = step;
		}

		public void SendAnalyticsIncrementStep(string newStepName)
		{
			SendStepAnalytics();

			CurrentStep += 1;
			CurrentStepName = newStepName;
		}

		public void SendStepAnalytics()
		{
			if (_sentAnalyticSteps.Contains(CurrentStepName)) return;
			
			_sentAnalyticSteps.Add(CurrentStepName);
			_services.AnalyticsService.TutorialCalls.CompleteTutorialStep(SectionName, SectionVersion, CurrentStep,
				CurrentTotalStep, CurrentStepName);
		}

		private void OnEnterNameEnter()
		{
			_dialogUi.ShowDialog(ScriptLocalization.UITTutorial.enter_your_name, CharacterType.Female, CharacterDialogMoodType.Neutral, CharacterDialogPosition.TopLeft);
		}
		
		private void OnEnterNameExit()
		{
			_tutorialUtilsUi.BlockFullScreen();
		}
		
		private async void OnBattlePassEnter()
		{
			_dialogUi.ContinueDialog("AIGHT, LET'S CLAIM SOME REWARDS", CharacterType.Female, CharacterDialogMoodType.Happy);
			
			// Wait a bit until home screen completely uncovers, and we get BP rewards
			await Task.Delay(GameConstants.Tutorial.TIME_1000MS);
			
			_tutorialUtilsUi.Unblock();
			_tutorialUtilsUi.BlockAround<HomeScreenPresenter>("battle-pass-button__holder");
			_tutorialUtilsUi.Highlight<HomeScreenPresenter>("battle-pass-button__holder");
		}
		
		private void OnBattlePassExit()
		{
			CloseTutorialUi();
			_tutorialUtilsUi.BlockFullScreen();
			_dialogUi.HideDialog(CharacterType.Female);
		}
		
		private async void OnClickRewardEnter()
		{
			// Wait a bit until home screen completely uncovers, and we get BP rewards
			await Task.Delay(GameConstants.Tutorial.TIME_1000MS);
			
			_dialogUi.ShowDialog("CLAIM THIS REWARD IDIOT!", CharacterType.Female, CharacterDialogMoodType.Happy, CharacterDialogPosition.TopLeft);
			
			_tutorialUtilsUi.Unblock();
			_services.GameUiService.GetUi<BattlePassScreenPresenter>().EnableFullScreenClaim(true);
			_tutorialUtilsUi.BlockAround<BattlePassScreenPresenter>("first-reward");
			_tutorialUtilsUi.Highlight<BattlePassScreenPresenter>("first-reward",null, 2f);
		}
		
		private void OnClickRewardExit()
		{
			_dialogUi.HideDialog(CharacterType.Female);
			_tutorialUtilsUi.RemoveHighlight();
			_tutorialUtilsUi.Unblock();
		}
		
		private void OnClaimRewardEnter()
		{
			
		}

		private async void OnClaimRewardExit()
		{
			_tutorialUtilsUi.BlockFullScreen();
			
			await Task.Delay(GameConstants.Tutorial.TIME_250MS);
			
			_services.GameUiService.GetUi<BattlePassScreenPresenter>().CloseManual();
		}
		
		private async void OnGoToEquipmentEnter()
		{
			await Task.Delay(GameConstants.Tutorial.TIME_1000MS);
			
			_dialogUi.ShowDialog("LET'S EQUIP THE NEW WEAPON", CharacterType.Female, CharacterDialogMoodType.Neutral, CharacterDialogPosition.TopRight);

			_tutorialUtilsUi.Unblock();
			_tutorialUtilsUi.BlockAround<HomeScreenPresenter>("button-with-icon--equipment");
			_tutorialUtilsUi.Highlight<HomeScreenPresenter>("button-with-icon--equipment",null, 1.5f);
		}
		
		private void OnGoToEquipmentExit()
		{
			CloseTutorialUi();
			_tutorialUtilsUi.BlockFullScreen();
		}
		
		private async void OnClickWeaponCategoryEnter()
		{
			_dialogUi.ContinueDialog("CLICK ON THE WEAPON CATEGORY", CharacterType.Female, CharacterDialogMoodType.Happy);
			
			await Task.Delay(GameConstants.Tutorial.TIME_750MS);
			
			_tutorialUtilsUi.Unblock();
			_tutorialUtilsUi.BlockAround<EquipmentPresenter>(null,"WeaponCategory");
			_tutorialUtilsUi.Highlight<EquipmentPresenter>(null,"WeaponCategory", 0.75f);
		}
		
		private void OnClickWeaponCategoryExit()
		{
			CloseTutorialUi();
			_tutorialUtilsUi.BlockFullScreen();
		}
		
		private async void OnSelectWeaponEnter()
		{
			_dialogUi.ContinueDialog("SELECT WEAPON PLEASE", CharacterType.Female, CharacterDialogMoodType.Happy);
			
			await Task.Delay(GameConstants.Tutorial.TIME_500MS);
			
			_tutorialUtilsUi.Unblock();
			_tutorialUtilsUi.BlockAround<EquipmentSelectionPresenter>("equipment-card");
			_tutorialUtilsUi.Highlight<EquipmentSelectionPresenter>("equipment-card",null, 1.5f);
		}

		private void OnSelectWeaponExit()
		{
			CloseTutorialUi();
			_tutorialUtilsUi.BlockFullScreen();
			_dialogUi.HideDialog(CharacterType.Female);
		}

		private async void OnEquipWeaponEnter()
		{
			await Task.Delay(GameConstants.Tutorial.TIME_500MS);
			
			_dialogUi.ShowDialog("EQUIP WEAPON PLEASE", CharacterType.Female, CharacterDialogMoodType.Neutral, CharacterDialogPosition.TopLeft);

			_tutorialUtilsUi.Unblock();
			_tutorialUtilsUi.BlockAround<EquipmentSelectionPresenter>(null,"EquipButton");
			_tutorialUtilsUi.Highlight<EquipmentSelectionPresenter>(null,"EquipButton");
		}

		private void OnEquipWeaponExit()
		{
			CloseTutorialUi();
			_tutorialUtilsUi.BlockFullScreen();
			_dialogUi.HideDialog(CharacterType.Female);
			
			_statechartTrigger(EquipmentMenuState.CloseButtonClickedEvent);
		}
		
		private async void OnPlayGameEnter()
		{
			await Task.Delay(GameConstants.Tutorial.TIME_1000MS);

			_dialogUi.ShowDialog(ScriptLocalization.UITTutorial.lets_play_real_match, CharacterType.Female, CharacterDialogMoodType.Happy, CharacterDialogPosition.TopLeft);

			_tutorialUtilsUi.Unblock();
			_tutorialUtilsUi.BlockAround<HomeScreenPresenter>("play-button");
			_tutorialUtilsUi.Highlight<HomeScreenPresenter>("play-button");
		}
		
		private void OnPlayGameExit()
		{
			CloseTutorialUi();
			_dialogUi.HideDialog(CharacterType.Female);
		}
	}
}
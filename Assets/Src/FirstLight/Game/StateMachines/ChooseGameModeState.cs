using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.Services;
using FirstLight.Statechart;
using I2.Loc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using PlayFab.SharedModels;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for player's authentication in the game in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class ChooseGameModeState
	{
		private readonly IStatechartEvent _gameModeSetEvent = new StatechartEvent("Game Mode Set Event");
		
		private readonly IGameServices _services;
		private readonly IGameUiService _uiService;
		private readonly IGameDataProvider _dataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		public ChooseGameModeState(IGameServices services, IGameUiService uiService, IGameDataProvider dataProvider, 
		                           Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_uiService = uiService;
			_dataProvider = dataProvider;
			_statechartTrigger = statechartTrigger;
		}

		/// <summary>
		/// Setups the Initial Loading state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var chooseGameMode = stateFactory.State("Choose Game Mode Entry");

			initial.Transition().Target(chooseGameMode);
			initial.OnExit(SubscribeEvents);

			chooseGameMode.OnEnter(OpenChooseGameModeDialog);
			chooseGameMode.Event(_gameModeSetEvent).Target(final);

			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{

		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}
		
		private void OpenChooseGameModeDialog()
		{
			var choice1Button = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.MainMenu.BattleRoyale,
				ButtonOnClick = OnBattleRoyaleSelected
			};
			
			var choice2Button = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.MainMenu.Deathmatch,
				ButtonOnClick = OnDeathmatchSelected
			};
			
			_services.GenericDialogService.OpenChoiceDialog(ScriptLocalization.MainMenu.ChooseGameMode, choice1Button, choice2Button);
		}

		private void OnBattleRoyaleSelected()
		{
			// TODO: Change Game Mode to Battle Royale.
			// GameMode = GameMode.BattleRoyale

			_statechartTrigger(_gameModeSetEvent);
		}
		
		private void OnDeathmatchSelected()
		{
			// TODO: Change Game Mode to Deathmatch.
			// GameMode = GameMode.Deathmatch
			
			_statechartTrigger(_gameModeSetEvent);
		}
	}
}
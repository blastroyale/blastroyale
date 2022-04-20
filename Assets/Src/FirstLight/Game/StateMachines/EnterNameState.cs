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
	public class EnterNameState
	{
		private readonly IStatechartEvent _nameSetEvent = new StatechartEvent("Name Set Event");
		private readonly IStatechartEvent _nameSetInvalidEvent = new StatechartEvent("Name Set Invalid Event");
		private readonly IStatechartEvent _nameInvalidAcknowledgedEvent = new StatechartEvent("Name Invalid Acknowledged Event");
		
		private readonly IGameServices _services;
		private readonly IGameUiService _uiService;
		private readonly IGameDataProvider _dataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		private string _nameInvalidStatus = "";
		
		public EnterNameState(IGameServices services, IGameUiService uiService, IGameDataProvider dataProvider, 
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
			var nameEntry = stateFactory.State("Name Entry");
			var nameInvalid = stateFactory.State("Name Invalid");
			
			initial.Transition().Target(nameEntry);
			initial.OnExit(SubscribeEvents);
			
			nameEntry.OnEnter(OpenEnterNameDialog);
			nameEntry.Event(_nameSetEvent).Target(final);
			nameEntry.Event(_nameSetInvalidEvent).Target(nameInvalid);

			nameInvalid.OnEnter(OpenNameInvalidDialog);
			nameInvalid.Event(_nameInvalidAcknowledgedEvent).Target(nameEntry);

			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{

		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}
		
		private void OpenEnterNameDialog()
		{
			var confirmButton = new GenericDialogButton<string>
			{
				ButtonText = ScriptLocalization.General.Yes,
				ButtonOnClick = OnNameSet
			};
			
			_services.GenericDialogService.OpenInputFieldDialog(ScriptLocalization.MainMenu.NameHeroTitle, 
			                                                    _dataProvider.AppDataProvider.Nickname, 
			                                                    confirmButton, false);
		}
		
		private void OpenNameInvalidDialog()
		{
			var okButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = OnNameInvalidAcknowledged
			};
			
			_services.GenericDialogService.OpenDialog(_nameInvalidStatus,false, okButton);
		}
		
		private void CloseCurrentDialog()
		{
			_services.GenericDialogService.CloseDialog();
		}

		private void OnNameSet(string newName)
		{
			if (newName.Length < GameConstants.PLAYER_NAME_MIN_LENGTH)
			{
				_nameInvalidStatus = string.Format(ScriptLocalization.MainMenu.NameTooShort, GameConstants.PLAYER_NAME_MIN_LENGTH);
				_statechartTrigger(_nameSetInvalidEvent);
				return;
			}
			if (newName.Length > GameConstants.PLAYER_NAME_MAX_LENGTH)
			{
				_nameInvalidStatus = string.Format(ScriptLocalization.MainMenu.NameTooLong, GameConstants.PLAYER_NAME_MAX_LENGTH);
				_statechartTrigger(_nameSetInvalidEvent);
				return;
			}

			if (newName != _dataProvider.AppDataProvider.Nickname)
			{
				_services.PlayfabService.UpdateNickname(newName);
			}
			
			_statechartTrigger(_nameSetEvent);
		}
		
		private void OnNameInvalidAcknowledged()
		{
			_statechartTrigger(_nameInvalidAcknowledgedEvent);
		}
	}
}
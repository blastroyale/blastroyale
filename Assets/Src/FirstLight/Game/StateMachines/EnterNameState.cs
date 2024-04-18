using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
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
		public static readonly IStatechartEvent NameSetEvent = new StatechartEvent("Name Set Event");
		private readonly IStatechartEvent _invalidNameEvent = new StatechartEvent("Name Set Invalid Event");

		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;


		public EnterNameState(IGameServices services, IGameDataProvider dataProvider,
							  Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
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
			var invalidName = stateFactory.Transition("Invalid Name");

			initial.Transition().Target(nameEntry);
			initial.OnExit(SubscribeEvents);


			nameEntry.OnEnter(OpenEnterNameDialog);
			nameEntry.Event(NameSetEvent).Target(final);
			nameEntry.Event(_invalidNameEvent).Target(invalidName);
			invalidName.Transition().Target(nameEntry);

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
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = OnNameSet
			};

			_services.GenericDialogService.OpenInputDialog(ScriptLocalization.UITHomeScreen.enter_your_name,
				ScriptLocalization.UITHomeScreen.new_name_desc,
				_dataProvider.AppDataProvider.GetDisplayName(true, false),
				confirmButton, false);
		}

		private void OnNameSet(string newName)
		{
			var newNameTrimmed = newName.Trim();

			string errorMessage = null;
			if (newNameTrimmed.Length < GameConstants.PlayerName.PLAYER_NAME_MIN_LENGTH)
			{
				errorMessage = string.Format(ScriptLocalization.UITProfileScreen.username_too_short,
					GameConstants.PlayerName.PLAYER_NAME_MIN_LENGTH);
			}
			else if (newNameTrimmed.Length > GameConstants.PlayerName.PLAYER_NAME_MAX_LENGTH)
			{
				errorMessage = string.Format(ScriptLocalization.UITProfileScreen.username_too_long,
					GameConstants.PlayerName.PLAYER_NAME_MAX_LENGTH);
			}
			else if (new Regex("[^a-zA-Z0-9 _-\uA421]+").IsMatch(newNameTrimmed))
			{
				errorMessage = ScriptLocalization.UITProfileScreen.username_invalid_characters;
			}

			if (errorMessage != null)
			{
				OnSetNameError(errorMessage).Forget();
				return;
			}

			if (newNameTrimmed == _dataProvider.AppDataProvider.DisplayNameTrimmed)
			{
				_statechartTrigger(NameSetEvent);
				return;
			}

			_services.GameBackendService.UpdateDisplayName(newNameTrimmed, (_) => _statechartTrigger(NameSetEvent), e =>
			{
				var description = GetErrorString(e);
				if (e.Error == PlayFabErrorCode.ProfaneDisplayName)
				{
					description = ScriptLocalization.UITProfileScreen.username_profanity;
				}

				OnSetNameError(description).Forget();
			});
		}

		private async UniTaskVoid OnSetNameError(string errorMessage)
		{
			// HACK: When you open a generic dialog in a close action of another generic dialog it will not work.
			// Because the ui.CloseLayer will be called after the close callback, closing it immediately 
			await UniTask.WaitUntil(() => !_services.UIService.IsScreenOpen<GenericButtonDialogPresenter>());
			await _services.GenericDialogService.OpenSimpleMessage(ScriptLocalization.UITShared.error, errorMessage, () => TriggerNameSetInvalid().Forget());
		}

		private async UniTaskVoid TriggerNameSetInvalid()
		{
			// HACK
			await UniTask.WaitUntil(() => !_services.UIService.IsScreenOpen<GenericButtonDialogPresenter>());
			_statechartTrigger(_invalidNameEvent);
		}

		private string GetErrorString(PlayFabError error)
		{
			var realError = error.ErrorDetails?.Values.FirstOrDefault()?.FirstOrDefault();
			return realError ?? error.ErrorMessage;
		}
	}
}
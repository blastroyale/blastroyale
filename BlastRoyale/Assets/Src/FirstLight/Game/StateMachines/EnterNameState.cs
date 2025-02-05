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
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Authentication;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
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
using Unity.Services.Authentication;
using Unity.Services.Core;
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

			final.OnEnter(CloseLoading);
			final.OnEnter(UnsubscribeEvents);
		}

		private void CloseLoading()
		{
			_services.UIService.CloseScreen<LoadingSpinnerScreenPresenter>(false).Forget();
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
				ButtonOnClick = newName => OnNameSet(newName).Forget()
			};

			var emptyInputText = !_services.TutorialService.HasCompletedTutorialSection(TutorialSection.ENTER_NAME_PROMPT) &&
				!_services.TutorialService.HasCompletedTutorial();

			_services.GenericDialogService.OpenInputDialog(ScriptLocalization.UITHomeScreen.enter_your_name,
				ScriptLocalization.UITHomeScreen.new_name_desc,
				emptyInputText ? "" : _services.AuthService.GetPrettyLocalPlayerName(showTags: false),
				confirmButton, false);
		}

		private async UniTaskVoid OnNameSet(string newName)
		{
			await _services.UIService.OpenScreen<LoadingSpinnerScreenPresenter>();
			var error = await _services.AuthService.SetDisplayName(newName);

			if (error == null)
			{
				_statechartTrigger(NameSetEvent);
				return;
			}

			await OnSetNameError(error);
		}

		private async UniTask OnSetNameError(string errorMessage)
		{
			await _services.UIService.CloseScreen<LoadingSpinnerScreenPresenter>(false);
			// HACK: When you open a generic dialog in a close action of another generic dialog it will not work.
			// Because the ui.CloseLayer will be called after the close callback, closing it immediately 
			await UniTask.WaitUntil(() => !_services.UIService.IsScreenOpen<GenericButtonDialogPresenter>());
			await _services.GenericDialogService.OpenSimpleMessage(ScriptLocalization.UITShared.error, errorMessage,
				() => TriggerNameSetInvalid().Forget());
		}

		private async UniTaskVoid TriggerNameSetInvalid()
		{
			// HACK
			await UniTask.WaitUntil(() => !_services.UIService.IsScreenOpen<GenericButtonDialogPresenter>());
			_statechartTrigger(_invalidNameEvent);
		}
	}
}
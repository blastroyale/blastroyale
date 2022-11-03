using System;
using FirstLight.FLogger;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.NativeUi;
using FirstLight.Statechart;
using I2.Loc;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the settings menu in the <seealso cref="MainMenuState"/>
	/// </summary>
	public class SettingsMenuState
	{
		private readonly IStatechartEvent _settingsCloseClickedEvent = new StatechartEvent("Settings Close Button Clicked Event");
		private readonly IStatechartEvent _logoutConfirmClickedEvent = new StatechartEvent("Logout Confirm Clicked Event");
		private readonly IStatechartEvent _logoutFailedEvent = new StatechartEvent("Logout Failed Event");
		private readonly IStatechartEvent _connectIdClickedEvent = new StatechartEvent("Connect ID Clicked Event");
		private readonly IStatechartEvent _connectIdBackEvent = new StatechartEvent("Connect ID Back Event");
		
		private readonly MatchState _matchState;
		private readonly MainMenuState _mainMenuState;
		private readonly IGameDataProvider _data;
		private readonly IGameServices _services;
		private readonly IGameUiService _uiService;
		private readonly IAppLogic _appLogic;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		private Coroutine _csPoolTimerCoroutine;

		public SettingsMenuState(IGameDataProvider data, IGameServices services, IGameLogic gameLogic, IGameUiService uiService, 
		                         Action<IStatechartEvent> statechartTrigger)
		{
			_data = data;
			_services = services;
			_uiService = uiService;
			_appLogic = gameLogic.AppLogic;
			_statechartTrigger = statechartTrigger;
		}

		/// <summary>
		/// Setups the Adventure gameplay state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var settingsMenu = stateFactory.State("Settings Menu");
			var connectId = stateFactory.State("Connect ID Screen");
			var serverSelect = stateFactory.State("Server Select");
			var logoutWait = stateFactory.State("Wait For Logout");

			initial.Transition().Target(settingsMenu);
			initial.OnExit(SubscribeEvents);
			
			settingsMenu.OnEnter(OpenSettingsMenuUI);
			settingsMenu.Event(_settingsCloseClickedEvent).Target(final);
			settingsMenu.Event(_logoutConfirmClickedEvent).Target(logoutWait);
			settingsMenu.Event(_connectIdClickedEvent).Target(connectId);
			settingsMenu.Event(NetworkState.OpenServerSelectScreenEvent).Target(serverSelect);

			connectId.OnEnter(OpenConnectIdScreen);
			connectId.Event(_connectIdBackEvent).Target(settingsMenu);

			serverSelect.OnEnter(OpenServerSelectScreen);
			serverSelect.Event(NetworkState.PhotonMasterConnectedEvent).Target(settingsMenu);

			logoutWait.OnEnter(TryLogOut);
			logoutWait.Event(_logoutFailedEvent).Target(final);

			final.OnEnter(UnsubscribeEvents);
		} 

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<ServerHttpErrorMessage>(OnServerHttpErrorMessage);
			_services.MessageBrokerService.Subscribe<PingedRegionsMessage>(OnPingedRegionsMessage);
			_services.MessageBrokerService.Subscribe<RegionListReceivedMessage>(OnRegionListReceivedMessage);
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
		}

		private void OpenSettingsMenuUI()
		{
			var data = new SettingsScreenPresenter.StateData
			{
				LogoutClicked = TryLogOut,
				OnClose = () => _statechartTrigger(_settingsCloseClickedEvent),
				OnServerSelectClicked = () => _statechartTrigger(NetworkState.OpenServerSelectScreenEvent),
				OnConnectIdClicked = () => _statechartTrigger(_connectIdClickedEvent),
				OnDeleteAccountClicked = () => _services.PlayfabService.CallFunction("RemovePlayerData", OnAccountDeleted)
			};

			_uiService.OpenScreen<SettingsScreenPresenter, SettingsScreenPresenter.StateData>(data);
		}

		private void CloseSettingsMenuUI()
		{
			_uiService.CloseUi<SettingsScreenPresenter>(true);
		}
		
		private void OpenServerSelectScreen()
		{
			var data = new ServerSelectScreenPresenter.StateData
			{
				BackClicked = () => _statechartTrigger(NetworkState.ConnectToRegionMasterEvent),
				RegionChosen = (region) =>
				{
					_data.AppDataProvider.ConnectionRegion.Value = region.Code;
					_statechartTrigger(NetworkState.ConnectToRegionMasterEvent);
				},
			};

			_uiService.OpenScreen<ServerSelectScreenPresenter, ServerSelectScreenPresenter.StateData>(data);
		}

		private void CloseServerSelectScreen()
		{
			_uiService.CloseUi<ServerSelectScreenPresenter>(true);
		}

		private void OnRegionListReceivedMessage(RegionListReceivedMessage msg)
		{
			if (_uiService.HasUiPresenter<ServerSelectScreenPresenter>())
			{
				_uiService.GetUi<ServerSelectScreenPresenter>().InitServerSelectionList(_services.NetworkService.QuantumClient.RegionHandler);
			}
		}

		private void OnPingedRegionsMessage(PingedRegionsMessage msg)
		{
			if (_uiService.HasUiPresenter<ServerSelectScreenPresenter>())
			{
				_uiService.GetUi<ServerSelectScreenPresenter>()
					.UpdateRegionPing(_services.NetworkService.QuantumClient.RegionHandler);
			}
		}

		private void OpenConnectIdScreen()
		{
			var data = new ConnectIdScreenPresenter.StateData
			{
				ConnectClicked = TryConnectId,
				BackClicked = () => _statechartTrigger(_connectIdBackEvent)
			};

			_uiService.OpenScreen<ConnectIdScreenPresenter, ConnectIdScreenPresenter.StateData>(data);
		}

		private void CloseConnectIdScreen()
		{
			_uiService.CloseUi<ConnectIdScreenPresenter>();
		}

		private void SetConnectIdDim(bool activateDim)
		{
			if (_uiService.HasUiPresenter<ConnectIdScreenPresenter>())
			{
				_uiService.GetUi<ConnectIdScreenPresenter>().SetFrontDimBlockerActive(activateDim);
			}
		}

		private void TryConnectId(string email, string password, string username)
		{
			SetConnectIdDim(true);
			_services.PlayfabService.AttachLoginDataToAccount(email, password, username, OnConnectIdComplete, OnConnectIdError);
		}

		private void TryLogOut()
		{
			_services.PlayfabService.UnlinkDeviceID(OnUnlinkComplete);
		}

		private void OnConnectIdComplete(AddUsernamePasswordResult result)
		{
			_services.PlayfabService.UpdateDisplayName(result.Username, OnUpdateNicknameComplete, OnUpdateNicknameError);
		}

		private void OnConnectIdError(PlayFabError error)
		{
			_services.AnalyticsService.ErrorsCalls.ReportError(AnalyticsCallsErrors.ErrorType.LinkGuestAccount, error.ErrorMessage);
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};
			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, error.ErrorMessage, false, confirmButton);
			SetConnectIdDim(false);
		}

		private void OnUpdateNicknameComplete(UpdateUserTitleDisplayNameResult result)
		{
			SetConnectIdDim(false);
			_statechartTrigger(_connectIdBackEvent);
			OpenFlgIdSuccessPopup();
		}

		private void OnUpdateNicknameError(PlayFabError error)
		{
			SetConnectIdDim(false);
			_statechartTrigger(_connectIdBackEvent);
			OpenFlgIdSuccessPopup();
		}

		private void OpenFlgIdSuccessPopup()
		{
			var title = string.Format(ScriptLocalization.MainMenu.FirstLightIdConnectionSuccess);
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.info, title, false, confirmButton);
		}
		
		private void OnUnlinkComplete()
		{
			_data.AppDataProvider.DeviceID.Value = null;
			_services.HelpdeskService.Logout();
			
#if UNITY_EDITOR
			var desc = string.Format(ScriptLocalization.MainMenu.LogoutSuccessDesc);
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.MainMenu.QuitGameButton,
				ButtonOnClick = () => { _services.QuitGame("Closing due to logout"); }
			};

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.info, desc, false, confirmButton);
#else
				var button = new FirstLight.NativeUi.AlertButton
				{
					Callback = () => {_services.QuitGame("Closing unlink complete alert"); },
					Style = FirstLight.NativeUi.AlertButtonStyle.Positive,
					Text = ScriptLocalization.MainMenu.QuitGameButton
				};

				FirstLight.NativeUi.NativeUiService.ShowAlertPopUp(false, ScriptLocalization.MainMenu.LogoutSuccessTitle,
				                               ScriptLocalization.MainMenu.LogoutSuccessDesc, button);
#endif
		}
		
		private void OnServerHttpErrorMessage(ServerHttpErrorMessage msg)
		{
			_services.AnalyticsService.CrashLog(msg.Message);
			
#if UNITY_EDITOR
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = () => { _statechartTrigger(_logoutFailedEvent); }
			};

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, msg.Message, false, confirmButton);
#else
			
			var button = new NativeUi.AlertButton
			{
				Callback = () => { _statechartTrigger(_logoutFailedEvent); },
				Style = NativeUi.AlertButtonStyle.Default,
				Text = ScriptLocalization.General.OK
			};

			NativeUi.NativeUiService.ShowAlertPopUp(false,ScriptLocalization.MainMenu.PlayfabError, msg.Message, button);
#endif
		}

		private void OnAccountDeleted(ExecuteFunctionResult res)
		{
			TryLogOut();
		}
	}
}

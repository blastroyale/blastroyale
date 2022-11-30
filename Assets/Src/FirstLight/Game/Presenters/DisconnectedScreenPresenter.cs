using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.UiService;
using I2.Loc;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Disconnected from Adventure Screen UI by:
	/// - Reconnect to the Adventure
	/// - Leave the Adventure to the Main menu
	/// </summary>
	[LoadSynchronously]
	public class DisconnectedScreenPresenter : UiToolkitPresenterData<DisconnectedScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action ReconnectClicked;
			public Action BackClicked;
		}

		private const float TIMEOUT_DIM_SECONDS = 5f;

		private VisualElement _dimElement;
		private Button _menuButton;
		private Button _reconnectButton;
		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements(VisualElement root)
		{
			_dimElement = root.Q("Dim").Required();
			_menuButton =root.Q<Button>("MenuButton").Required();
			_reconnectButton = root.Q<Button>("ReconnectButton").Required();

			_reconnectButton.clicked += OnReconnectClicked;
			_menuButton.clicked += OnLeaveClicked;
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			SetFrontDimBlockerActive(false);

			_services.AudioFxService.PlayClip2D(AudioId.DisconnectScreenAppear);

			if (NetworkUtils.IsOffline())
			{
				OpenNoInternetPopup();
			}
			
			// Disconnecting in main menu, players should only be able to reconnect
			if (_services.NetworkService.LastDisconnectLocation is LastDisconnectionLocation.Menu or LastDisconnectionLocation.Matchmaking)
			{
				_menuButton.SetDisplay(false);
				_reconnectButton.SetDisplay(true);
			}
			// Disconnecting during final preload means the game most likely started, player shouldn't be reconnecting and interfering
			if (_services.NetworkService.LastDisconnectLocation == LastDisconnectionLocation.FinalPreload)
			{
				_menuButton.SetDisplay(true);
				_reconnectButton.SetDisplay(false);
			}
			// If disconnected in simulation:
			// Solo matches - you currently cannot reconnect as quantum simulation is not running
			// Multiplayer matches - you must only reconnect
			else if (_services.NetworkService.LastDisconnectLocation == LastDisconnectionLocation.Simulation)
			{
				_menuButton.SetDisplay(_services.NetworkService.LastMatchPlayers.Count <= 1);
				_reconnectButton.SetDisplay(true);

				if (_services.NetworkService.LastMatchPlayers.Count <= 1)
				{
					var confirmButton = new GenericDialogButton
					{
						ButtonText = ScriptLocalization.General.OK,
						ButtonOnClick = () => _services.GenericDialogService.CloseDialog()
					};

					_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.info,
						ScriptLocalization.MainMenu.DisconnectedMatchEndInfo,
						false, confirmButton);
				}
			}
		}

		/// <summary>
		/// Sets the activity of the dimmed blocker image that covers the presenter
		/// </summary>
		public void SetFrontDimBlockerActive(bool active)
		{
			_dimElement.SetDisplay(active);
		}

		private void OnLeaveClicked()
		{
			Data.BackClicked();
		}

		private void OnReconnectClicked()
		{
			if (NetworkUtils.IsOffline())
			{
				OpenNoInternetPopup();
				return;
			}

			SetFrontDimBlockerActive(true);
			
			// Just in case reconnect stalls, undim the blocker after X seconds
			this.LateCoroutineCall(TIMEOUT_DIM_SECONDS, () => { SetFrontDimBlockerActive(false); });
			Data.ReconnectClicked();
		}

		private void OpenNoInternetPopup()
		{
			var button = new AlertButton
			{
				Style = AlertButtonStyle.Positive,
				Text = ScriptLocalization.General.Confirm
			};
			
			NativeUiService.ShowAlertPopUp(false, ScriptLocalization.General.NoInternet,
				ScriptLocalization.General.NoInternetDescription, button);
		}
	}
}
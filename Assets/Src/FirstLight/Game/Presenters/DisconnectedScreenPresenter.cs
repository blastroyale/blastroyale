using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.UiService;
using I2.Loc;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Disconnected from Adventure Screen UI by:
	/// - Reconnect to the Adventure
	/// - Leave the Adventure to the Main menu
	/// </summary>
	public class DisconnectedScreenPresenter : UiPresenterData<DisconnectedScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action ReconnectClicked;
			public Action BackClicked;
		}

		private const float TIMEOUT_DIM_SECONDS = 5f;

		[SerializeField, Required] private Button _reconnectButton;
		[SerializeField, Required] private Button _menuButton;
		[SerializeField, Required] private GameObject _frontDimBlocker;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			
			_reconnectButton.onClick.AddListener(OnReconnectClicked);
			_menuButton.onClick.AddListener(OnLeaveClicked);
		}

		protected override void OnOpened()
		{
			SetFrontDimBlockerActive(false);
			
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				OpenNoInternetPopup();
			}
			
			_menuButton.gameObject.SetActive(_services.NetworkService.LastDisconnectLocation != LastDisconnectionLocation.Menu);

			// Always force reconnect to ranked matches to prevent exploits
			if (_services.GameModeService.SelectedGameMode.Value.Entry.MatchType == MatchType.Ranked)
			{
				_menuButton.gameObject.SetActive(false);
			}
			
			// If disconnected in offline mode (playing solo), can't reconnect due to quantum simulation not running
			// without at least 1 player connected in match at all times
			if (_services.NetworkService.LastMatchPlayers.Count <= 1 &&
			    _services.NetworkService.LastDisconnectLocation == LastDisconnectionLocation.Simulation)
			{
				_reconnectButton.gameObject.SetActive(false);
				_menuButton.gameObject.SetActive(true);
				
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.OK,
					ButtonOnClick = () => _services.GenericDialogService.CloseDialog()
				};

				_services.GenericDialogService.OpenDialog(ScriptLocalization.MainMenu.DisconnectedMatchEndInfo, false, confirmButton);
			}
		}

		/// <summary>
		/// Sets the activity of the dimmed blocker image that covers the presenter
		/// </summary>
		public void SetFrontDimBlockerActive(bool active)
		{
			_frontDimBlocker.SetActive(active);
		}

		private void OnLeaveClicked()
		{
			Data.BackClicked.Invoke();
		}

		private void OnReconnectClicked()
		{
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				OpenNoInternetPopup();
				return;
			}

			// Just in case reconnect stalls, undim the blocker after X seconds
			this.LateCoroutineCall(TIMEOUT_DIM_SECONDS, () => { SetFrontDimBlockerActive(false); });
			Data.ReconnectClicked.Invoke();
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
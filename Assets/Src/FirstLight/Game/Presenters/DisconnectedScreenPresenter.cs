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
		
		private VisualElement _blockerElement;
		private VisualElement _menuButton;
		private VisualElement _reconnectButton;
		private IGameServices _services;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}
		
		protected override void QueryElements(VisualElement root)
		{
			_blockerElement = root.Q("Blocker").Required();
			_menuButton = root.Q("MenuButton").Required();
			_reconnectButton = root.Q("ReconnectButton").Required();
			
			root.Q<Button>("ReconnectButton").clicked += OnReconnectClicked;
			root.Q<Button>("MenuButton").clicked += OnLeaveClicked;
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			
			SetFrontDimBlockerActive(false);

			_services.AudioFxService.PlayClip2D(AudioId.DisconnectScreenAppear);
			
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				OpenNoInternetPopup();
			}
			
			_menuButton.EnableInClassList("element-hidden", _services.NetworkService.LastDisconnectLocation == LastDisconnectionLocation.Menu);
			
			// Always force reconnect to ranked matches to prevent exploits
			if (_services.GameModeService.SelectedGameMode.Value.Entry.MatchType == MatchType.Ranked)
			{
				_menuButton.EnableInClassList("element-hidden", true);
			}
			
			// Disconnecting during final preload means the game most likely started, player shouldn't be reconnecting and interfering
			if (_services.NetworkService.LastDisconnectLocation == LastDisconnectionLocation.FinalPreload)
			{
				_reconnectButton.EnableInClassList("element-hidden", true);
			}
			// If disconnected in offline mode (playing solo), can't reconnect due to quantum simulation not running
			// without at least 1 player connected in match at all times
			else if (_services.NetworkService.LastDisconnectLocation == LastDisconnectionLocation.Simulation &&
			         _services.NetworkService.LastMatchPlayers.Count <= 1)
			{
				_menuButton.EnableInClassList("element-hidden", false);
				_reconnectButton.EnableInClassList("element-hidden", true);
				
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
			_blockerElement.EnableInClassList("element-hidden", !active);
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
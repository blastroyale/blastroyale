using System;
using System.Collections.Generic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.UiService;
using I2.Loc;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
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

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();

			_reconnectButton.onClick.AddListener(OnReconnectClicked);
			_menuButton.onClick.AddListener(OnLeaveClicked);
		}

		protected override void OnOpened()
		{
			SetFrontDimBlockerActive(false);

			_menuButton.gameObject.SetActive(_services.NetworkService.LastDisconnectLocation != LastDisconnectionLocation.Menu);

			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				OpenNoInternetPopup();
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
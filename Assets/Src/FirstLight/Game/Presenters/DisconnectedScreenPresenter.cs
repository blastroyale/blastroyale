using System;
using System.Collections.Generic;
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

		private const float DIM_SCREEN_SECONDS = 5f;
		
		[SerializeField, Required] private Button _reconnectButton;
		[SerializeField, Required] private Button _leaveButton;
		[SerializeField, Required] private GameObject _frontDimBlocker;

		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			
			_reconnectButton.onClick.AddListener(OnReconnectClicked);
			_leaveButton.onClick.AddListener(OnLeaveClicked);
		}

		protected override void OnOpened()
		{
			SetFrontDimBlockerActive(false);
			
			var dictionary = new Dictionary<string, object>
			{
				{"disconnected_cause", _services.NetworkService.QuantumClient.DisconnectedCause}
			};
			
			_services.AnalyticsService.LogEvent("disconnected", dictionary);
			_services.AnalyticsService.CrashLog($"Disconnected - {_services.NetworkService.QuantumClient.DisconnectedCause}");

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
			
			Data.ReconnectClicked.Invoke();
			
			// Prevent reconect click spam for X seconds
			SetFrontDimBlockerActive(true);
			this.LateCoroutineCall(DIM_SCREEN_SECONDS, () => { SetFrontDimBlockerActive(false);});
		}

		private void OpenNoInternetPopup()
		{
			var button = new AlertButton
			{
				Callback = Data.ReconnectClicked,
				Style = AlertButtonStyle.Positive,
				Text = ScriptLocalization.General.Confirm
			};
			
			NativeUiService.ShowAlertPopUp(false, ScriptLocalization.General.NoInternet, 
			                               ScriptLocalization.General.NoInternetDescription, button);
		}
	}
}
using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Services;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace FirstLight.Game.Services
{
	public class LoginData
	{
		public bool IsGuest;
	}

	/// <summary>
	/// This services handles all authentication functionality
	/// </summary>
	public interface IAuthenticationService
	{
		/// <summary>
		/// Create a new account with a random customID (GUID), and links the current device to that account
		/// </summary>
		void SetupLoginGuestAccount(Action<LoginData> onSuccess, Action<PlayFabError> onError);
	}

	/// <inheritdoc cref="IAuthenticationService" />
	public class PlayfabAuthenticationService : IAuthenticationService
	{
		private IGameServices _services;
		private IDataSaver _dataSaver;
		private IGameDataProvider _dataProvider;

		public PlayfabAuthenticationService(IGameServices services, IDataSaver dataSaver, IGameDataProvider dataProvider)
		{
			_services = services;
			_dataSaver = dataSaver;
			_dataProvider = dataProvider;
		}

		private void LinkDevice()
		{
			_dataProvider.AppDataProvider.DeviceID.Value = PlayFabSettings.DeviceUniqueIdentifier;
			_dataSaver.SaveData<AppData>();
		}

		public void SetupLoginGuestAccount(Action<LoginData> onSuccess, Action<PlayFabError> onError)
		{
			FLog.Verbose($"Creating guest account");

			var login = new LoginWithCustomIDRequest
			{
				CreateAccount = true,
				CustomId = Guid.NewGuid().ToString(),
			};
			
			PlayFabClientAPI.LoginWithCustomID(login, res =>
			{
				FLog.Verbose($"Created guest account {res.PlayFabId} linking device");
				
				_services.GameBackendService.LinkDeviceID(() =>
				{
					FLog.Verbose("Device linked to new account");
					
					LinkDevice();
					
					onSuccess?.Invoke(new LoginData()
					{
						IsGuest = true
					});
				});
			}, error => onError?.Invoke(error));
		}
	}
}
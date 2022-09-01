using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using I2.Loc;
using Photon.Realtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles server selection in the main menu
	/// </summary>
	public class ServerSelectScreenPresenter : AnimatedUiPresenterData<ServerSelectScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action<Region> RegionChosen;
			public Action BackClicked;
		}

		[SerializeField, Required] private Button _connectButton;
		[SerializeField, Required] private Button _backButton;
		[SerializeField, Required] private GameObject _frontDimBlocker;
		[SerializeField, Required] private TMP_Dropdown _serverSelectDropdown;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private List<Region> _availableRegions;

		private Coroutine _fadeBlockerCoroutine;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();

			_connectButton.onClick.AddListener(OnConnectClicked);
			_backButton.onClick.AddListener(OnBackClicked);
		}

		protected override void OnOpened()
		{
			SetFrontDimBlockerActive(true);
		}

		protected override void OnClosed()
		{
			base.OnClosed();

			if (_fadeBlockerCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_fadeBlockerCoroutine);
			}
		}

		/// <summary>
		/// Activates and populates the server selection list
		/// </summary>
		public void InitServerSelectionList(RegionHandler regionHandler)
		{
			SetFrontDimBlockerActive(false);

			_availableRegions = regionHandler.EnabledRegions;
			_serverSelectDropdown.options.Clear();

			int currentRegion = 0;

			foreach (var region in _availableRegions)
			{
				string regionTitle = string.Format(ScriptLocalization.MainMenu.ServerSelectOption,
				                                   region.Code.GetPhotonRegionTranslation().ToUpper(), "-");

				_serverSelectDropdown.options.Add(new DropdownMenuOption(regionTitle, region));

				if (_gameDataProvider.AppDataProvider.ConnectionRegion.Value == region.Code)
				{
					currentRegion = _availableRegions.IndexOf(region);
				}
			}

			_serverSelectDropdown.SetValueWithoutNotify(currentRegion);
			_serverSelectDropdown.RefreshShownValue();
		}

		/// <summary>
		/// Updates pings of regions in the currently initialized list
		/// </summary>
		public void UpdateRegionPing(RegionHandler regionHandler)
		{
			var selectedOption = (DropdownMenuOption) _serverSelectDropdown.options[_serverSelectDropdown.value];

			_serverSelectDropdown.captionText.text = string.Format(ScriptLocalization.MainMenu.ServerSelectOption,
			                                                       selectedOption.RegionInfo.Code.GetPhotonRegionTranslation().ToUpper(),
			                                                       selectedOption.RegionInfo.Ping);

			foreach (var dropdownOption in _serverSelectDropdown.options)
			{
				var regionOption = (DropdownMenuOption) dropdownOption;

				dropdownOption.text = string.Format(ScriptLocalization.MainMenu.ServerSelectOption,
				                                    regionOption.RegionInfo.Code.GetPhotonRegionTranslation().ToUpper(),
				                                    regionOption.RegionInfo.Ping);
			}
		}

		private IEnumerator FrontDimBlockerSequence()
		{
			SetFrontDimBlockerActive(true);

			yield return new WaitForSeconds(GameConstants.Data.SERVER_SELECT_CONNECTION_TIMEOUT);
			
			SetFrontDimBlockerActive(false);
		}

		private void SetFrontDimBlockerActive(bool active)
		{
			_frontDimBlocker.SetActive(active);
		}

		private void OnBackClicked()
		{
			Data.BackClicked.Invoke();
			_fadeBlockerCoroutine = _services.CoroutineService.StartCoroutine(FrontDimBlockerSequence());
		}

		private void OnConnectClicked()
		{
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				OpenNoInternetPopup();
				return;
			}

			var selectedRegion = ((DropdownMenuOption) _serverSelectDropdown.options[_serverSelectDropdown.value]).RegionInfo;
			Data.RegionChosen.Invoke(selectedRegion);
			_fadeBlockerCoroutine = _services.CoroutineService.StartCoroutine(FrontDimBlockerSequence());
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

		private class DropdownMenuOption : TMP_Dropdown.OptionData
		{
			public Region RegionInfo { get; set; }

			public DropdownMenuOption(string text, Region regionInfo) : base(text)
			{
				RegionInfo = regionInfo;
			}
		}
	}
}
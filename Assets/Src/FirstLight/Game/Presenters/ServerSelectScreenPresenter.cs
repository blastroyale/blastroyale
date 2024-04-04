using System;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.UIService;
using I2.Loc;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles server selection in the main menu
	/// </summary>
	[UILayer(UILayer.Popup)]
	public class ServerSelectScreenPresenter : UIPresenterData<ServerSelectScreenPresenter.StateData>
	{
		public class StateData
		{
			public Action<bool> OnExit;
		}

		[SerializeField, Required] private GameObject _selectorAndButtonsContainer;
		[SerializeField, Required] private TMP_Text _statusText;
		[SerializeField, Required] private Button _connectButton;
		[SerializeField, Required] private Button _backButton;
		[SerializeField, Required] private TMP_Dropdown _serverSelectDropdown;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;


		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();

			_connectButton.onClick.AddListener(OnConnectClicked);
			_backButton.onClick.AddListener(OnBackClicked);
		}

		protected override void QueryElements()
		{
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_statusText.SetText("Pinging servers...");
			_selectorAndButtonsContainer.SetActive(false);
			WaitForRegionPing().Forget();
			return base.OnScreenOpen(reload);
		}

		private async UniTaskVoid WaitForRegionPing()
		{
			await UniTask.WaitUntil(() => _services.ServerListService.State is IServerListService.ServerListState.FetchedPings or IServerListService.ServerListState.Failed);
			if (_services.ServerListService.State == IServerListService.ServerListState.Failed)
			{
				CloseSeverSelect(false);
				return;
			}

			_selectorAndButtonsContainer.SetActive(true);
			_statusText.SetText("");
			UpdateServerList();
		}

		/// <summary>
		/// Activates and populates the server selection list
		/// </summary>
		public void UpdateServerList()
		{
			var regions = _services.ServerListService.PingsByServer;
			_serverSelectDropdown.options.Clear();

			int currentRegion = 0;
			int i = 0;
			foreach (var region in regions.Values)
			{
				string regionTitle = string.Format(ScriptLocalization.MainMenu.ServerSelectOption,
					region.ServerCode.GetPhotonRegionTranslation().ToUpper(), region.ReceivedPing ? region.Ping : "");

				_serverSelectDropdown.options.Add(new DropdownMenuOption(regionTitle, region));

				if (_gameDataProvider.AppDataProvider.ConnectionRegion.Value == region.ServerCode)
				{
					currentRegion = i;
				}

				i++;
			}

			_serverSelectDropdown.SetValueWithoutNotify(currentRegion);
			_serverSelectDropdown.RefreshShownValue();
		}
		

		private void CloseSeverSelect(bool changedServer)
		{
			Data.OnExit.Invoke(changedServer);
			_services.UIService.CloseScreen<ServerSelectScreenPresenter>().Forget();
		}

		private void OnBackClicked()
		{
			CloseSeverSelect(false);
		}

		private void OnConnectClicked()
		{
			if (!NetworkUtils.IsOnline())
			{
				OpenNoInternetPopup();
				return;
			}

			var selectedRegion = ((DropdownMenuOption) _serverSelectDropdown.options[_serverSelectDropdown.value]).RegionInfo;
			Connect(selectedRegion).Forget();
		}

		private async UniTaskVoid Connect(IServerListService.ServerPing server)
		{
			// No need to reconnect if it is the same server
			if (_gameDataProvider.AppDataProvider.ConnectionRegion.Value == server.ServerCode)
			{
				CloseSeverSelect(false);
				return;
			}
			_statusText.SetText("Connecting...");
			_selectorAndButtonsContainer.SetActive(false);
			FLog.Info("Connecting to " + server.ServerCode);
			_services.NetworkService.ChangeServerRegionAndReconnect(server.ServerCode);
			var connected = await _services.NetworkService.AwaitMasterServerConnection(10, server.ServerCode);
			if (!connected)
			{
				FLog.Error("Failed to change region!");
			}

			CloseSeverSelect(connected);
			FLog.Info("Should close this");
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
			public IServerListService.ServerPing RegionInfo { get; set; }

			public DropdownMenuOption(string text, IServerListService.ServerPing regionInfo) : base(text)
			{
				RegionInfo = regionInfo;
			}
		}
	}
}

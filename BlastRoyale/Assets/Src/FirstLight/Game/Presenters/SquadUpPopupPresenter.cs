using System;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.UIService;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Displays the squad up popup with logic for joining / creating a squad.
	/// </summary>
	[UILayer(UILayer.Popup)]
	public class SquadUpPopupPresenter : UIPresenter
	{
		private const int MAX_SQUAD_SIZE = 4;

		private GenericPopupElement _popup;
		private Button _createTeamButton;
		private Button _joinTeamButton;
		private Button _leaveTeamButton;

		private Label _teamCodeLabel;
		private VisualElement _yourTeamContainer;
		private ListView _friendsOnlineList;

		private IGameServices _services;

		protected override void QueryElements()
		{
			_services = MainInstaller.ResolveServices();

			_popup = Root.Q<GenericPopupElement>("Popup").Required();
			_createTeamButton = Root.Q<Button>("CreateTeamButton").Required();
			_joinTeamButton = Root.Q<Button>("JoinTeamButton").Required();
			_leaveTeamButton = Root.Q<Button>("LeaveTeamButton").Required();

			_teamCodeLabel = Root.Q<Label>("TeamCode").Required();
			_yourTeamContainer = Root.Q("YourTeamContainer").Required();

			_createTeamButton.clicked += () => CreateSquad().Forget();
			_joinTeamButton.clicked += OnJoinTeamButtonClicked;
			_leaveTeamButton.clicked += () => LeaveSquad().Forget();
			_popup.CloseClicked += () => _services.UIService.CloseScreen<SquadUpPopupPresenter>();
		}

		protected override async UniTask OnScreenOpen(bool reload)
		{
			await RefreshData();
		}

		private async UniTaskVoid CreateSquad()
		{
			var clo = new CreateLobbyOptions();

			clo.IsPrivate = true;
			clo.Player = new Player(AuthenticationService.Instance.PlayerId); // TODO ??

			var lobby = await LobbyService.Instance.CreateLobbyAsync($"squad_{AuthenticationService.Instance.PlayerId}", MAX_SQUAD_SIZE, clo);
			_teamCodeLabel.text = lobby.LobbyCode;

			FLog.Info($"PACO Joined lobby: code:{lobby.LobbyCode}, id:{lobby.Id}, name:{lobby.Name}, upid:{lobby.Upid}");
		}

		private void OnJoinTeamButtonClicked()
		{
		}

		private async UniTaskVoid LeaveSquad()
		{
			var squadLobby = await LobbyService.Instance.GetCurrentSquadLobby();

			if (squadLobby == null)
			{
				FLog.Error("Tried leaving squad lobby but player wasn't in one.");
				return;
			}
			
			

			try
			{
				FLog.Info($"Leaving squad: {squadLobby.Id}");
				await LobbyService.Instance.RemovePlayerAsync(squadLobby.Id, AuthenticationService.Instance.PlayerId);
				FLog.Info("Left squad successfully!");
			}
			catch (Exception e)
			{
				FLog.Error("Error leaving squad", e);
			}
		}

		private async UniTask RefreshData()
		{
			var squadLobby = await LobbyService.Instance.GetCurrentSquadLobby();

			if (squadLobby == null)
			{
				// Not in squad
				_yourTeamContainer.SetDisplay(false);
				_createTeamButton.SetDisplay(true);
				_joinTeamButton.SetDisplay(true);
				_leaveTeamButton.SetDisplay(false);
			}
			else
			{
				// In a squad
				_yourTeamContainer.SetDisplay(true);
				_createTeamButton.SetDisplay(false);
				_joinTeamButton.SetDisplay(false);
				_leaveTeamButton.SetDisplay(true);
				_teamCodeLabel.text = squadLobby.LobbyCode;
			}
		}

		private class LobbyEvents : LobbyEventCallbacks
		{
			
		}
	}
}
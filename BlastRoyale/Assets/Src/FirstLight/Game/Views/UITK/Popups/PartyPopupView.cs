using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.UIService;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Lobbies;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK.Popups
{
	public class PartyPopupView : UIView
	{
		private const string USS_PARTY_JOINED = "party-joined";

		private Button _createTeamButton;
		private Button _joinTeamButton;
		private Button _leaveTeamButton;

		private Label _teamCodeLabel;
		private Label _yourTeamHeader;
		private Label _friendsHeader;
		private VisualElement _yourTeamContainer;
		private ListView _friendsOnlineList;

		private IGameServices _services;

		private List<Relationship> _friends;

		protected override void Attached()
		{
			_services = MainInstaller.ResolveServices();

			_createTeamButton = Element.Q<Button>("CreateTeamButton").Required();
			_joinTeamButton = Element.Q<Button>("JoinTeamButton").Required();
			_leaveTeamButton = Element.Q<Button>("LeaveTeamButton").Required();
			_friendsOnlineList = Element.Q<ListView>("FriendsOnlineList").Required();
			_yourTeamHeader = Element.Q<Label>("YourTeamLabel").Required();
			_friendsHeader = Element.Q<Label>("FriendsOnline").Required();

			_teamCodeLabel = Element.Q<Label>("TeamCode").Required();
			_yourTeamContainer = Element.Q("YourTeamContainer").Required();

			_createTeamButton.clicked += () => CreateParty().Forget();
			_joinTeamButton.clicked += () =>
			{
				// TODO: Temporary
				var confirmButton = new GenericDialogButton<string>
				{
					ButtonText = "JOIN",
					ButtonOnClick = code => JoinParty(code).Forget()
				};
				_services.GenericDialogService.OpenInputDialog("JOIN ROOM",
					"CODE",
					"", confirmButton, true);
			};
			_leaveTeamButton.clicked += () => LeaveParty().Forget();

			_friendsOnlineList.makeItem = OnMakeFriendsItem;
			_friendsOnlineList.bindItem = OnBindFriendsItem;
		}

		public override void OnScreenOpen(bool reload)
		{
			RefreshData();

			_services.FLLobbyService.CurrentPartyCallbacks.LobbyChanged += OnLobbyChanged;
		}

		public override void OnScreenClose()
		{
			_services.FLLobbyService.CurrentPartyCallbacks.LobbyChanged -= OnLobbyChanged;
		}

		private async UniTaskVoid CreateParty()
		{
			await _services.FLLobbyService.CreateParty();
			RefreshData();
		}

		private async UniTaskVoid JoinParty(string partyCode)
		{
			await _services.FLLobbyService.JoinParty(partyCode);
			RefreshData();
		}

		private async UniTaskVoid LeaveParty()
		{
			await _services.FLLobbyService.LeaveParty();
			RefreshData();
		}

		private void OnLobbyChanged(ILobbyChanges lobbyChanges)
		{
			// TODO: This could be done smarter by just refreshing what changed
			RefreshData();
		}

		private VisualElement OnMakeFriendsItem()
		{
			return new FriendListElement();
		}

		private void OnBindFriendsItem(VisualElement element, int index)
		{
			var relationship = _friends[index];

			((FriendListElement) element)
				.SetPlayerName(relationship.Member.Profile.Name)
				.SetStatus(relationship.Member.Presence.GetActivity<FriendActivity>()?.Status, true)
				.SetMainAction("INVITE", _services.FLLobbyService.CurrentPartyLobby == null
					? null
					: () =>
					{
						_services.FLLobbyService.InviteToParty(relationship.Member.Id).Forget();
						RefreshData();
					}, false)
				.SetMoreActions(ve => PlayerStatisticsPopupPresenter.Open(relationship.Member.Id).Forget());
		}

		private void RefreshData()
		{
			var partyLobby = _services.FLLobbyService.CurrentPartyLobby;
			var inParty = partyLobby != null;

			Element.EnableInClassList(USS_PARTY_JOINED, inParty);

			_yourTeamHeader.text = $"YOUR PARTY ({partyLobby?.Players?.Count ?? 0}/4)";
			_yourTeamContainer.Clear();

			if (inParty)
			{
				_teamCodeLabel.text = _services.FLLobbyService.CurrentPartyLobby.LobbyCode;

				foreach (var partyMember in partyLobby.Players)
				{
					if (partyMember.Id == AuthenticationService.Instance.PlayerId) continue;
					_yourTeamContainer.Add(new FriendListElement(partyMember.GetPlayerName()));
				}
			}

			// We always show the local player
			_yourTeamContainer.Add(new FriendListElement(AuthenticationService.Instance.PlayerName));

			// TODO mihak: Add invited friends

			_friendsOnlineList.itemsSource = _friends = FriendsService.Instance.Friends.Where(r => r.IsOnline()).ToList();
			_friendsHeader.text = $"FRIENDS ONLINE ({_friends.Count})";
		}
	}
}
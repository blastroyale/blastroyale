using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.UIService;
using I2.Loc;
using QuickEye.UIToolkit;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Lobbies;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK.Popups
{
	/// <summary>
	/// Allows the user to create or join a party and invite friends.
	/// </summary>
	public class PartyPopupView : UIView
	{
		private const string USS_PARTY_JOINED = "party-joined";

		[Q("CreatePartyButton")] private Button _createTeamButton;
		[Q("JoinPartyButton")] private Button _joinTeamButton;
		[Q("LeavePartyButton")] private Button _leaveTeamButton;

		[Q("TeamCode")] private Label _teamCodeLabel;
		[Q("YourTeamLabel")] private Label _yourTeamHeader;
		[Q("FriendsOnline")] private Label _friendsHeader;
		[Q("YourTeamContainer")] private VisualElement _yourTeamContainer;
		[Q("FriendsOnlineList")] private ListView _friendsOnlineList;

		[Q("CopyCodeButton")] private LocalizedButton _copyCodeButton;

		private IGameServices _services;

		private List<Relationship> _friends;

		protected override void Attached()
		{
			_services = MainInstaller.ResolveServices();

			_createTeamButton.clicked += () => CreateParty().Forget();
			_joinTeamButton.clicked += () =>
			{
				PopupPresenter.Close()
					.ContinueWith(() => PopupPresenter.OpenJoinWithCode(code => JoinParty(code).ContinueWith(PopupPresenter.Close).ContinueWith(PopupPresenter.OpenParty).Forget()))
					.Forget();
			};
			_leaveTeamButton.clicked += () => LeaveParty().Forget();

			_copyCodeButton.clicked += OnCopyCodeClicked;

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

		private async UniTask JoinParty(string partyCode)
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
			RefreshData();
		}

		private VisualElement OnMakeFriendsItem()
		{
			return new FriendListElement();
		}

		private void OnBindFriendsItem(VisualElement element, int index)
		{
			var relationship = _friends[index];
			var canInvite = _services.FLLobbyService.CurrentPartyLobby?.IsLocalPlayerHost() ?? false;
			var e = ((FriendListElement) element);
			e	.SetFromRelationship(relationship)
				.SetMainAction(ScriptLocalization.UITFriends.invite, canInvite
					? null
					: () =>
					{
						_services.FLLobbyService.InviteToParty(relationship.Member.Id).Forget();
						RefreshData();
					}, false)
				.SetMoreActions(_ => PlayerStatisticsPopupPresenter.Open(relationship.Member.Id).Forget());
		}

		private void RefreshData()
		{
			var partyLobby = _services.FLLobbyService.CurrentPartyLobby;
			var inParty = partyLobby != null;

			Element.EnableInClassList(USS_PARTY_JOINED, inParty);

			_yourTeamHeader.text = string.Format(ScriptLocalization.UITParty.your_party, partyLobby?.Players?.Count ?? 0, 4);
			_yourTeamContainer.Clear();

			if (inParty)
			{
				_teamCodeLabel.text = _services.FLLobbyService.CurrentPartyLobby.LobbyCode;

				foreach (var partyMember in partyLobby.Players!)
				{
					if (partyMember.Id == AuthenticationService.Instance.PlayerId) continue;
					_yourTeamContainer.Add(new FriendListElement(partyMember.GetPlayerName()));
				}
			}

			// We always show the local player
			_yourTeamContainer.Add(new FriendListElement(AuthenticationService.Instance.PlayerName));

			// TODO mihak: Add invited friends
			_friendsOnlineList.itemsSource = _friends = FriendsService.Instance.Friends.Where(r => r.IsOnline()).ToList();
			_friendsHeader.text = string.Format(ScriptLocalization.UITParty.online_friends, _friends.Count);
		}

		private void OnCopyCodeClicked()
		{
			UIUtils.SaveToClipboard(_services.FLLobbyService.CurrentPartyLobby.LobbyCode);
		}
	}
}
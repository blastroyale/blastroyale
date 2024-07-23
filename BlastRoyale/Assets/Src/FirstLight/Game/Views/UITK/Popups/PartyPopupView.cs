using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Messages;
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
using Unity.Services.Friends.Notifications;
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

		[Q("CreatePartyButton")] private LocalizedButton _createTeamButton;
		[Q("JoinPartyButton")] private LocalizedButton _joinTeamButton;
		[Q("LeavePartyButton")] private LocalizedButton _leaveTeamButton;

		[Q("TeamCode")] private Label _teamCodeLabel;
		[Q("YourTeamLabel")] private Label _yourTeamHeader;
		[Q("FriendsOnline")] private Label _friendsHeader;
		[Q("YourTeamContainer")] private VisualElement _yourTeamContainer;
		[Q("FriendsOnlineList")] private ListView _friendsOnlineList;

		[Q("CopyCodeButton")] private LocalizedButton _copyCodeButton;

		private IGameServices _services;

		private List<Relationship> _friends;
		private Dictionary<string, FriendListElement> _elements = new ();

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
			_services.MessageBrokerService.Subscribe<PartyLobbyUpdatedMessage>(OnLobbyChanged);
			FriendsService.Instance.PresenceUpdated += OnPresenceUpdated;
		}

		public override void OnScreenClose()
		{
			_services.MessageBrokerService.UnsubscribeAll(this);
			FriendsService.Instance.PresenceUpdated -= OnPresenceUpdated;
		}

		private void OnPresenceUpdated(IPresenceUpdatedEvent e)
		{
			RefreshData();
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

		private void OnLobbyChanged(PartyLobbyUpdatedMessage m)
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
			var e = ((FriendListElement) element);
			e.SetFromRelationship(relationship)
				.AddOpenProfileAction(relationship)
				.TryAddInviteOption(relationship, () =>
				{
					_services.FLLobbyService.InviteToParty(relationship.Member.Id).Forget();
					RefreshData();
				});
			_elements[relationship.Member.Id] = e;
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
					var e = new FriendListElement().SetFromParty(partyMember);
					_yourTeamContainer.Add(e);
				}
			}

			// We always show the local player
			var own = new FriendListElement();
			own.SetPlayerName(AuthenticationService.Instance.PlayerName.TrimPlayerNameNumbers());
			own.SetAvatar(MainInstaller.ResolveData().AppDataProvider.AvatarUrl);
			_yourTeamContainer.Add(own);

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
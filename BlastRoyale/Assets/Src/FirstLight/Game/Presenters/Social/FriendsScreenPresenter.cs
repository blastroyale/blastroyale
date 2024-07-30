using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.Game.Views.UITK;
using FirstLight.UIService;
using I2.Loc;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Friends.Exceptions;
using Unity.Services.Friends.Models;
using Unity.Services.Friends.Notifications;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	public class FriendsScreenPresenter : UIPresenterData<FriendsScreenPresenter.StateData>
	{
		public class StateData
		{
			public Action OnBackClicked;
		}

		private IGameServices _services;

		private ListView _friendsList;
		private ListView _requestsList;
		private ListView _blockedList;

		private VisualElement _friendsEmptyContainer;
		private VisualElement _requestsEmptyContainer;
		private VisualElement _blockedEmptyContainer;

		private TextField _yourIDField;
		private TextField _addFriendIDField;
		private LocalizedButton _addFriendButton;
		private Label _requestsCount;

		private List<Relationship> _friends;
		private List<Relationship> _blocked;
		private List<Relationship> _requests;

		protected override void QueryElements()
		{
			_services = MainInstaller.ResolveServices();

			var header = Root.Q<ScreenHeaderElement>("Header").Required();
			header.SetTitle(ScriptLocalization.UITHomeScreen.friends);
			header.backClicked = Data.OnBackClicked;

			_friendsList = Root.Q<ListView>("FriendsList").Required();
			_requestsList = Root.Q<ListView>("RequestsList").Required();
			_blockedList = Root.Q<ListView>("BlockedList").Required();

			_friendsEmptyContainer = Root.Q<VisualElement>("FriendsEmptyContainer").Required();
			_requestsEmptyContainer = Root.Q<VisualElement>("RequestsEmptyContainer").Required();
			_blockedEmptyContainer = Root.Q<VisualElement>("BlockedEmptyContainer").Required();

			_yourIDField = Root.Q<TextField>("YourID").Required();
			_addFriendIDField = Root.Q<TextField>("AddFriendID").Required();
			_addFriendButton = Root.Q<LocalizedButton>("AddFriendButton").Required();
			_requestsCount = Root.Q<Label>("RequestsCount").Required();

			_addFriendButton.clicked += () => AddFriend(_addFriendIDField.value).Forget();
			Root.Q<VisualElement>("SocialsButtons").Required().AttachView(this, out SocialsView _);

			_friendsList.bindItem = OnFriendsBindItem;
			_requestsList.bindItem = OnRequestsBindItem;
			_blockedList.bindItem = OnBlockedBindItem;

			_friendsList.makeItem = OnMakeListItem;
			_requestsList.makeItem = OnMakeListItem;
			_blockedList.makeItem = OnMakeListItem;
		}

		private void OnRelationshipAdded(IRelationshipAddedEvent rde)
		{
			RefreshAll();
		}

		private void OnRelationshipDeleted(IRelationshipDeletedEvent e)
		{
			RefreshAll();
		}

		private void OnPresenceUpdate(IPresenceUpdatedEvent e)
		{
			RefreshAll();
		}

		private void OnMessageReceived(IMessageReceivedEvent e)
		{
			FLog.Info("Message from " + e.UserId);
		}

		protected override UniTask OnScreenClose()
		{
			FriendsService.Instance.RelationshipDeleted -= OnRelationshipDeleted;
			FriendsService.Instance.RelationshipAdded -= OnRelationshipAdded;
			FriendsService.Instance.PresenceUpdated -= OnPresenceUpdate;
			FriendsService.Instance.MessageReceived -= OnMessageReceived;
			return base.OnScreenClose();
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_yourIDField.value = AuthenticationService.Instance.PlayerId;
			RefreshAll();

			// TODO mihak: Temporary, we just always refresh all lists
			FriendsService.Instance.RelationshipDeleted += OnRelationshipDeleted;
			FriendsService.Instance.RelationshipAdded += OnRelationshipAdded;
			FriendsService.Instance.PresenceUpdated += OnPresenceUpdate;
			FriendsService.Instance.MessageReceived += OnMessageReceived;
			return base.OnScreenOpen(reload);
		}

		private void RefreshAll()
		{
			RefreshFriends();
			RefreshRequests();
			RefreshBlocked();
		}

		private void RefreshFriends()
		{
			_friends = FriendsService.Instance.Friends.ToList();
			// Sort by last seen so online friends are at the top
			_friends.Sort((a, b) => b.IsOnline().CompareTo(a.IsOnline()));
			_friendsList.itemsSource = _friends;
			_friendsList.RefreshItems();

			_friendsList.SetDisplay(_friends.Count > 0);
			_friendsEmptyContainer.SetDisplay(_friends.Count == 0);
		}

		private void RefreshRequests()
		{
			var incomingRequests = FriendsService.Instance.IncomingFriendRequests.ToList();
			var outgoingRequests = FriendsService.Instance.OutgoingFriendRequests.ToList();
			_requests = incomingRequests.Concat(outgoingRequests).ToList();
			_requestsList.itemsSource = _requests;
			_requestsList.RefreshItems();

			_requestsCount.SetVisibility(incomingRequests.Count > 0);
			_requestsCount.text = incomingRequests.Count.ToString();

			_requestsList.SetDisplay(_requests.Count > 0);
			_requestsEmptyContainer.SetDisplay(_requests.Count == 0);
		}

		private void RefreshBlocked()
		{
			_blocked = FriendsService.Instance.Blocks.ToList();
			_blockedList.itemsSource = _blocked;
			_blockedList.RefreshItems();

			_blockedList.SetDisplay(_blocked.Count > 0);
			_blockedEmptyContainer.SetDisplay(_blocked.Count == 0);
		}

		private VisualElement OnMakeListItem()
		{
			return new FriendListElement();
		}

		private void OnFriendsBindItem(VisualElement element, int index)
		{
			var relationship = _friends[index];
			var online = relationship.IsOnline();

			string header = null;

			// Show header if first item or if the previous item has a different status
			if (index == 0 || ((_friends[index - 1].IsOnline()) != online))
			{
				var count = online
					? _friends.Count(r => r.IsOnline())
					: _friends.Count(r => !r.IsOnline());

				header = string.Format(online ? ScriptLocalization.UITFriends.online : ScriptLocalization.UITFriends.offline, count);
			}

			var showPartyInvite = relationship.IsOnline() && _services.FLLobbyService.CurrentPartyLobby != null &&
				!_services.FLLobbyService.SentPartyInvites.Contains(relationship.Member.Id);
			var e = ((FriendListElement) element);
			e
				.SetHeader(header)
				.SetFromRelationship(relationship)
				.SetMoreActions(ve => OpenTooltip(ve, relationship))
				.TryAddInviteOption(relationship, () =>
				{
					_services.FLLobbyService.InviteToParty(relationship.Member.Id).Forget();
				});
		}

		private void OnRequestsBindItem(VisualElement element, int index)
		{
			var relationship = _requests[index];
			var sentRequest = relationship.Member.Role == MemberRole.Target; // If we sent this request or received it

			string header = null;

			if (index == 0 || (_requests[index - 1].Member.Role == MemberRole.Target) != sentRequest)
			{
				var count = sentRequest
					? _requests.Count(r => r.Member.Role == MemberRole.Target)
					: _requests.Count(r => r.Member.Role != MemberRole.Target);

				header = string.Format(
					relationship.Member.Role == MemberRole.Target ? ScriptLocalization.UITFriends.sent : ScriptLocalization.UITFriends.received,
					count);
			}

			var playerElement = ((FriendListElement) element)
				.SetFromRelationship(relationship)
				.SetStatus(string.Empty, null)
				.SetHeader(header)
				.SetMoreActions(ve => OpenTooltip(ve, relationship));

			if (!sentRequest)
			{
				playerElement.SetAcceptDecline(
					() => AcceptRequest(relationship).Forget(),
					() => DeclineRequest(relationship).Forget()
				);
			}
			else
			{
				playerElement.SetAcceptDecline(null, null);
			}
		}

		private void OnBlockedBindItem(VisualElement element, int index)
		{
			var relationship = _blocked[index];

			((FriendListElement) element)
				.SetFromRelationship(relationship)
				.SetPlayerName(relationship.Member.Profile.Name)
				.SetMoreActions(ve => OpenTooltip(ve, relationship))
				.SetMainAction(ScriptLocalization.UITFriends.unblock, () => UnblockPlayer(relationship).Forget(), true);
		}

		private void OpenTooltip(VisualElement element, Relationship relationship)
		{
			var buttons = new List<PlayerContextButton>();

			buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Normal, "Open profile",
				() => PlayerStatisticsPopupPresenter.Open(relationship.Member.Id).Forget()));

			buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Red, ScriptLocalization.UITFriends.remove_friend,
				() => RemoveRelationship(relationship).Forget()));

			if (relationship.Type != RelationshipType.Block)
			{
				buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Red, ScriptLocalization.UITFriends.block,
					() => BlockPlayer(relationship.Member.Id, false).Forget()));
			}

			TooltipUtils.OpenPlayerContextOptions(element, Root, relationship.Member.Profile.Name.TrimPlayerNameNumbers(), buttons);
		}

		private async UniTaskVoid AcceptRequest(Relationship r)
		{
			try
			{
				FLog.Info($"Accepting friend request: {r.Member.Id}");
				await FriendsService.Instance.AddFriendAsync(r.Member.Id).AsUniTask();
				FLog.Info($"Friend request accepted: {r.Member.Id}");

				RefreshRequests();
				RefreshFriends();

				_services.NotificationService.QueueNotification("#Friend request accepted#");
			}
			catch (FriendsServiceException e)
			{
				FLog.Warn("Error accepting friend request.", e);
				_services.NotificationService.QueueNotification($"#Error accepting friend request, {e.ParseError()}#");
			}
		}

		private async UniTaskVoid DeclineRequest(Relationship r)
		{
			try
			{
				FLog.Info($"Deleting friend request: {r.Member.Id}");
				await FriendsService.Instance.DeleteIncomingFriendRequestAsync(r.Member.Id).AsUniTask();
				FLog.Info($"Friend request deleted: {r.Member.Id}");
				RefreshRequests();

				_services.NotificationService.QueueNotification("#Friend request declined#");
			}
			catch (FriendsServiceException e)
			{
				FLog.Warn("Error declining friend request", e);
				_services.NotificationService.QueueNotification($"#Error declining friend request, {e.ParseError()}#");
			}
		}

		private async UniTaskVoid AddFriend(string playerID)
		{
			if (string.IsNullOrWhiteSpace(playerID)) return;

			_addFriendButton.SetEnabled(false);

			var success = await FriendsService.Instance.AddFriendHandled(playerID);

			if (success)
			{
				RefreshRequests();
				RefreshFriends(); // In case they already had a request from that friend, it accepts it
			}

			_addFriendButton.SetEnabled(true);
			_addFriendIDField.value = string.Empty;
		}

		private async UniTaskVoid RemoveRelationship(Relationship relationship)
		{
			try
			{
				await FriendsService.Instance.DeleteRelationshipAsync(relationship.Id).AsUniTask();
				if (relationship.Type == RelationshipType.Block)
				{
					try
					{
						await FriendsService.Instance.DeleteFriendAsync(relationship.Member.Id).AsUniTask();
					}
					catch (Exception e)
					{
						FLog.Verbose("Could not remove friend, likely was not a friend anymore " + e.Message);
					}
				}

				RefreshAll();
				_services.NotificationService.QueueNotification("#Player Removed#");
			}
			catch (FriendsServiceException e)
			{
				_services.NotificationService.QueueNotification($"#Error removing player, {e.ParseError()}#");
			}
		}

		private async UniTaskVoid BlockPlayer(string playerID, bool isRequest)
		{
			await FriendsService.Instance.BlockHandled(playerID, isRequest);
			RefreshAll();
		}

		private async UniTaskVoid UnblockPlayer(Relationship r)
		{
			await FriendsService.Instance.UnblockHandled(r);
			RefreshAll();
		}
	}
}
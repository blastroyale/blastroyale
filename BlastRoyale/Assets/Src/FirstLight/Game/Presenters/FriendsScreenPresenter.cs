using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	public class FriendsScreenPresenter : UIPresenterData<FriendsScreenPresenter.StateData>
	{
		public class StateData
		{
			public Action OnBackClicked;
		}

		// private ListView _squadList;
		private ListView _friendsList;
		private ListView _requestsList;
		private ListView _blockedList;

		private TextField _yourIDField;
		private TextField _addFriendIDField;
		private Button _addFriendButton;
		private Label _requestsCount;

		private List<Relationship> _friends;
		private List<Relationship> _blocked;
		private List<Relationship> _allRequests; // TODO: Only for testing

		protected override void QueryElements()
		{
			var header = Root.Q<ScreenHeaderElement>("Header").Required();
			header.SetTitle("FRIENDS");
			header.backClicked += Data.OnBackClicked;

			// _squadList = Root.Q<ListView>("SquadList").Required();
			_friendsList = Root.Q<ListView>("FriendsList").Required();
			_requestsList = Root.Q<ListView>("RequestsList").Required();
			_blockedList = Root.Q<ListView>("BlockedList").Required();

			_yourIDField = Root.Q<TextField>("YourID").Required();
			_addFriendIDField = Root.Q<TextField>("AddFriendID").Required();
			_addFriendButton = Root.Q<Button>("AddFriendButton").Required();
			_requestsCount = Root.Q<Label>("RequestsCount").Required();

			_addFriendButton.clicked += () => AddFriend(_addFriendIDField.value).Forget();
			Root.Q<ImageButton>("CopyButton").Required().clicked += CopyPlayerID;

			_friendsList.bindItem = OnFriendsBindItem;
			_requestsList.bindItem = OnRequestsBindItem;
			_blockedList.bindItem = OnBlockedBindItem;

			_friendsList.makeItem = OnMakeListItem;
			_requestsList.makeItem = OnMakeListItem;
			_blockedList.makeItem = OnMakeListItem;
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_yourIDField.value = AuthenticationService.Instance.PlayerId;
			RefreshAll();

			// TODO: Temporary, we just always refresh all lists
			FriendsService.Instance.RelationshipDeleted += rde =>
			{
				FLog.Info($"Relationship deleted: {rde.Relationship.Id} - {rde.Relationship.Member.Id}");
				RefreshAll();
			};
			FriendsService.Instance.RelationshipAdded += raa =>
			{
				FLog.Info($"Relationship added: {raa.Relationship.Id} - {raa.Relationship.Member.Id}");
				RefreshAll();
			};
			FriendsService.Instance.PresenceUpdated += pue =>
			{
				FLog.Info($"Presence updated: {pue.ID} - {pue.Presence.Availability}");
				RefreshAll();
			};
			FriendsService.Instance.MessageReceived += mre =>
			{
				FLog.Info($"Message received from: {mre.UserId}");
			};

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
			_friends.Sort((a, b) => b.Member.Presence.Availability.CompareTo(a.Member.Presence.Availability));
			_friendsList.itemsSource = _friends;
			_friendsList.RefreshItems();
		}

		private void RefreshRequests()
		{
			var incomingRequests = FriendsService.Instance.IncomingFriendRequests.ToList();
			var outgoingRequests = FriendsService.Instance.OutgoingFriendRequests.ToList();
			_allRequests = incomingRequests.Concat(outgoingRequests).ToList();
			_requestsList.itemsSource = _allRequests;
			_requestsList.RefreshItems();
			
			_requestsCount.SetVisibility(incomingRequests.Count > 0);
			_requestsCount.text = incomingRequests.Count.ToString();
		}

		private void RefreshBlocked()
		{
			_blocked = FriendsService.Instance.Blocks.ToList();
			_blockedList.itemsSource = _blocked;
			_blockedList.RefreshItems();
		}

		private VisualElement OnMakeListItem()
		{
			return new FriendListElement();
		}

		private void OnFriendsBindItem(VisualElement element, int index)
		{
			var relationship = _friends[index];
			var online = relationship.Member.Presence.Availability == Availability.Online;

			string header = null;

			// Show header if first item or if the previous item has a different status
			if (index == 0 || ((_friends[index - 1].Member.Presence.Availability == Availability.Online) != online))
			{
				var count = online ? _friends.Count(r => r.Member.Presence.Availability == Availability.Online) : 
					_friends.Count(r => r.Member.Presence.Availability != Availability.Online);
				
				header = online ? $"ONLINE({count})" : $"OFFLINE({count})";
			}

			((FriendListElement) element).SetData(relationship, header, "INVITE", !online
				? null
				: r =>
				{
					FLog.Info($"Squad invite clicked: {r.Id}");
				}, null, null, OpenFriendTooltip);
		}

		private void OnRequestsBindItem(VisualElement element, int index)
		{
			var relationship = _allRequests[index];
			FLog.Info("PACO", $"BingRequest({index}): {relationship.Member.Role}");
			var sentRequest = relationship.Member.Role == MemberRole.Target; // If we sent this request or received it

			string header = null;

			if (index == 0 || (_allRequests[index - 1].Member.Role == MemberRole.Target) != sentRequest)
			{
				header = relationship.Member.Role == MemberRole.Target ? "PENDING" : "RECEIVED";
			}

			((FriendListElement) element).SetData(relationship, header, null, null,
				sentRequest ? null : r => AcceptRequest(r).Forget(),
				sentRequest ? null : r => DeclineRequest(r).Forget(),
				OpenRequestsTooltip);
		}

		private void OnBlockedBindItem(VisualElement element, int index)
		{
			var relationship = _blocked[index];

			((FriendListElement) element).SetData(relationship, null, "UNBLOCK", r => UnblockPlayer(r).Forget(), null, null, null);
		}

		private void OpenFriendTooltip(VisualElement element, Relationship relationship)
		{
			TooltipUtils.OpenPlayerContextOptions(element, Root, relationship.Member.Profile.Name, new[]
			{
				new PlayerContextButton(PlayerButtonContextStyle.Normal, "Open profile", () => OpenProfile(relationship.Member.Id).Forget()),
				new PlayerContextButton(PlayerButtonContextStyle.Red, "Block", () => BlockPlayer(relationship.Member.Id).Forget()),
				new PlayerContextButton(PlayerButtonContextStyle.Red, "Remove friend", () => RemoveFriend(relationship.Member.Id).Forget())
			}, TipDirection.TopRight, TooltipPosition.Center);
		}

		private void OpenRequestsTooltip(VisualElement element, Relationship relationship)
		{
			TooltipUtils.OpenPlayerContextOptions(element, Root, relationship.Member.Profile.Name, new[]
			{
				new PlayerContextButton(PlayerButtonContextStyle.Normal, "Open profile", () => FLog.Info($"Open profile: {relationship.Member.Id}")),
				new PlayerContextButton(PlayerButtonContextStyle.Red, "Block", () => BlockPlayer(relationship.Member.Id).Forget()),
			}, TipDirection.TopRight, TooltipPosition.Center);
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
			}
			catch (Exception e)
			{
				FLog.Error("Error accepting friend request.", e);
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
			}
			catch (Exception e)
			{
				FLog.Error("Error declining friend request.", e);
			}
		}

		private async UniTaskVoid AddFriend(string playerId)
		{
			if (string.IsNullOrWhiteSpace(playerId)) return;

			_addFriendButton.SetEnabled(false);
			try
			{
				FLog.Info($"Sending friend request: {playerId}");
				await FriendsService.Instance.AddFriendAsync(playerId).AsUniTask();
				FLog.Info($"Friend request sent: {playerId}");
				RefreshRequests();
				RefreshFriends(); // In case they already had a request from that friend, it accepts it
			}
			catch (Exception e)
			{
				FLog.Error("Error adding friend.", e);
			}

			_addFriendButton.SetEnabled(true);
			_addFriendIDField.value = string.Empty;
		}

		private async UniTaskVoid RemoveFriend(string playerID)
		{
			try
			{
				FLog.Info($"Removing friend: {playerID}");
				await FriendsService.Instance.DeleteFriendAsync(playerID).AsUniTask();
				FLog.Info($"Friend removed: {playerID}");
				RefreshFriends();
			}
			catch (Exception e)
			{
				FLog.Error("Error removing friend.", e);
			}
		}

		private async UniTaskVoid BlockPlayer(string playerID)
		{
			try
			{
				FLog.Info($"Blocking player: {playerID}");
				await FriendsService.Instance.AddBlockAsync(playerID).AsUniTask();
				FLog.Info($"Player blocked: {playerID}");
				RefreshAll();
			}
			catch (Exception e)
			{
				FLog.Error("Error blocking", e);
			}
		}

		private async UniTaskVoid UnblockPlayer(Relationship r)
		{
			try
			{
				FLog.Info($"Unblocking player: {r.Member.Id}");
				await FriendsService.Instance.DeleteBlockAsync(r.Member.Id).AsUniTask();
				FLog.Info($"Player unblocked: {r.Member.Id}");
				RefreshAll(); // Figure out if needed
			}
			catch (Exception e)
			{
				FLog.Error("Error unblocking player.", e);
			}
		}
		
		private void CopyPlayerID()
		{
			// Copy the player ID to the clipboard
			var te = new TextEditor
			{
				text = _yourIDField.value
			};
			te.SelectAll();
			te.Copy();
		}

		private UniTask OpenProfile(string playerID)
		{
			FLog.Info($"Opening profile (not implemented yet): {playerID}");
			// var data = new PlayerStatisticsPopupPresenter.StateData
			// {
			// 	PlayerId = member.ProfileMasterId,
			// 	OnCloseClicked = () => _services.UIService.CloseScreen<PlayerStatisticsPopupPresenter>().Forget()
			// };
			// _services.UIService.OpenScreen<PlayerStatisticsPopupPresenter>(data).Forget();
			return UniTask.CompletedTask;
		}
	}
}
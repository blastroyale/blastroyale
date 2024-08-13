using System;
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
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Friends.Notifications;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK.Popups
{
	/// <summary>
	/// Shows a list of friends to invite to a match.
	/// </summary>
	public class InviteFriendsPopupView : UIView
	{
		[Q("FriendsList")] private ListView _friendsList;

		private IGameServices _services;

		private List<Relationship> _friends;

		private readonly HashSet<string> _invitedFriends = new ();

		private BufferedQueue _friendsRefresh = new (TimeSpan.FromSeconds(0.1), true);

		protected override void Attached()
		{
			_services = MainInstaller.ResolveServices();

			_friendsList.makeItem = () => new FriendListElement();
			_friendsList.bindItem = OnBindFriendsItem;

			RefreshFriends();
		}

		public override void OnScreenOpen(bool reload)
		{
			FriendsService.Instance.PresenceUpdated += OnPresenceUpdated; // not called with local changes
		}

		public override void OnScreenClose()
		{
			FriendsService.Instance.PresenceUpdated -= OnPresenceUpdated; // not called with local changes
		}

		private void OnPresenceUpdated(IPresenceUpdatedEvent obj)
		{
			RefreshFriends();
		}

		private void OnBindFriendsItem(VisualElement element, int index)
		{
			var relationship = _friends[index];
			var e = ((FriendListElement) element)
				.SetFromRelationship(relationship)
				.AddOpenProfileAction(relationship)
				.TryAddInviteOption(relationship, () =>
				{
					_invitedFriends.Add(relationship.Member.Id);
					_services.FLLobbyService.InviteToMatch(relationship.Member.Id).Forget();
					_friendsList.RefreshItem(index);
				});
		}

		private void RefreshFriends()
		{
			_friendsRefresh.Add(() =>
			{
				_friends = FriendsService.Instance.Friends
					.Where(r => r.IsOnline() && _services.FLLobbyService.CurrentMatchLobby.Players.All(p => p.Id != r.Member.Id))
					.ToList();

				_friends.Sort(FriendsServiceExtensions.FriendDefaultSorter());
				_friendsList.itemsSource = _friends;
				_friendsList.RefreshItems();
			});
		}
	}
}
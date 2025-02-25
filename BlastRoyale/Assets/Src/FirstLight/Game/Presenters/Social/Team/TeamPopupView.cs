using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Authentication;
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
using Unity.Services.Lobbies.Models;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters.Social.Team
{
	/// <summary>
	/// Allows the user to create or join a party and invite friends.
	/// </summary>
	public class TeamPopupView : UIView
	{
		private const string USS_PARTY_JOINED = "party-joined";

		[Q("CreatePartyButton")] private LocalizedButton _createTeamButton;
		[Q("JoinPartyButton")] private LocalizedButton _joinTeamButton;
		[Q("LeavePartyButton")] private LocalizedButton _leaveTeamButton;

		[Q("TeamCode")] private Label _teamCodeLabel;
		[Q("YourTeamLabel")] private Label _yourTeamHeader;
		[Q("GameModeLabel")] private Label _gamemodeHeader;
		[Q("FriendsOnlineLabel")] private Label _friendsOnlineLabel;
		[Q("YourTeamContainer")] private VisualElement _yourTeamContainer;
		[Q("FriendsOnlineList")] private ListView _friendsOnlineList;
		[Q("NoFriendsLabel")] private VisualElement _noFriendsLabel;
		[Q("CopyCodeButton")] private ImageButton _copyCodeButton;

		private FriendListElement _localPlayer;
		private VisualElement _gapHack;

		private IGameServices _services;

		private List<Relationship> _friends;
		private Dictionary<string, FriendListElement> _elements = new ();
		private BufferedQueue _updateQueue = new (TimeSpan.FromSeconds(0.02), true);

		protected override void Attached()
		{
			_services = MainInstaller.ResolveServices();

			_createTeamButton.clicked += () => CreateParty().Forget();
			_joinTeamButton.clicked += () =>
			{
				PopupPresenter.ClosePopupScreen()
					.ContinueWith(() => PopupPresenter.OpenJoinWithCode(code =>
						JoinParty(code).ContinueWith(PopupPresenter.ClosePopupScreen).ContinueWith(PopupPresenter.OpenParty).Forget()))
					.Forget();
			};
			_leaveTeamButton.clicked += UniTask.Action(LeaveParty);

			_copyCodeButton.clicked += OnCopyCodeClicked;
			_friendsOnlineList.makeItem = OnMakeFriendsItem;
			_friendsOnlineList.bindItem = OnBindFriendsItem;
		}

		public override void OnScreenOpen(bool reload)
		{
			_services.FLLobbyService.CurrentPartyCallbacks.LocalLobbyJoined += OnLocalLobbyJoined;
			_services.GameModeService.SelectedGameMode.InvokeObserve(RefreshGameMode);
			_services.FLLobbyService.CurrentPartyCallbacks.PlayerLeft += OnPlayerLeft;
			_services.FLLobbyService.CurrentPartyCallbacks.PlayerJoined += OnPlayerJoined;
			_services.FLLobbyService.CurrentPartyCallbacks.LocalLobbyUpdated += OnLobbyChanged;
			_services.FLLobbyService.CurrentPartyCallbacks.OnInvitesUpdated += OnInvitesUpdated;
			_services.FLLobbyService.CurrentPartyCallbacks.OnDeclinedInvite += OnDeclinedInvite;
			FriendsService.Instance.PresenceUpdated += OnPresenceUpdated;
			var data = MainInstaller.ResolveData();
			_yourTeamContainer.Clear();
			// We always show the local player
			_localPlayer = new FriendListElement()
				.SetLocal()
				.SetPlayerName(_services.AuthService.GetPrettyLocalPlayerName(), (int) data.PlayerDataProvider.Trophies.Value)
				.SetAvatar(data.CollectionDataProvider.GetEquippedAvatarUrl())
				.SetElementClickAction(el =>
				{
					el.OpenTooltip(Presenter.Root, ScriptLocalization.UITCustomGames.local_player_tooltip);
				});
			_gapHack = new VisualElement().AddClass("gap-hack");
			_yourTeamContainer.Add(_localPlayer);
			RefreshData();
		}

		public override void OnScreenClose()
		{
			_services.MessageBrokerService.UnsubscribeAll(this);
			FriendsService.Instance.PresenceUpdated -= OnPresenceUpdated;
			_services.FLLobbyService.CurrentPartyCallbacks.LocalLobbyUpdated -= OnLobbyChanged;
			_services.FLLobbyService.CurrentPartyCallbacks.PlayerLeft += OnPlayerLeft;
			_services.FLLobbyService.CurrentPartyCallbacks.PlayerJoined += OnPlayerJoined;
			_services.FLLobbyService.CurrentPartyCallbacks.OnInvitesUpdated -= OnInvitesUpdated;
			_services.FLLobbyService.CurrentPartyCallbacks.LocalLobbyJoined -= OnLocalLobbyJoined;
			_services.FLLobbyService.CurrentPartyCallbacks.OnDeclinedInvite -= OnDeclinedInvite;
		}

		private void OnLocalLobbyJoined(Lobby l)
		{
			RefreshData();
		}

		private void OnPlayerJoined(List<LobbyPlayerJoined> joiners)
		{
			RefreshData();
		}

		private void OnPlayerLeft(List<int> quitters)
		{
			RefreshData();
		}

		private void OnDeclinedInvite(string userId)
		{
			var elements = _yourTeamContainer.Query().OfType<PendingInviteElement>().Where(el => el.Invite.PlayerId == userId).ToList();
			foreach (var pendingInviteElement in elements)
			{
				pendingInviteElement.Decline(RefreshData);
			}
		}

		private void OnInvitesUpdated(FLLobbyEventCallbacks.InviteUpdateType type)
		{
			// If we refresh the elements with this call there is no chance of playing the declined animation
			if (type == FLLobbyEventCallbacks.InviteUpdateType.Declined) return;
			RefreshData();
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
			_services.InGameNotificationService.QueueNotification(ScriptLocalization.UITParty.notification_left_party);
		}

		private void OnLobbyChanged(ILobbyChanges m)
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
				.TryAddInviteOption(Presenter.Root, relationship, () =>
				{
					_services.FLLobbyService.InviteToParty(relationship).ContinueWith(() =>
					{
						_services.InGameNotificationService.QueueNotification(ScriptLocalization.UITParty.notification_invite_sent);
					});
				});
			_elements[relationship.Member.Id] = e;
		}

		private void RefreshGameMode(GameModeInfo _, GameModeInfo modeInfo)
		{
			_gamemodeHeader.text = modeInfo.Entry.Title.GetText();
		}

		private void RefreshData()
		{
			_updateQueue.Add(() =>
			{
				var partyLobby = _services.FLLobbyService.CurrentPartyLobby;
				var inParty = partyLobby != null;

				Element.EnableInClassList(USS_PARTY_JOINED, inParty);

				_yourTeamHeader.text = string.Format(ScriptLocalization.UITParty.your_party, partyLobby?.Players?.Count ?? 0, 4);

				var friends = FriendsService.Instance.Friends.Where(r => r.IsOnline()).ToDictionary(r => r.Member.Id, r => r);
				var friendsCount = friends.Count;
				var elements = _yourTeamContainer
					.Query()
					.OfType<FriendListElement>()
					.Where(el => el != _localPlayer)
					.ToList();
				_localPlayer.SetCrown(!inParty || partyLobby.HostId == AuthenticationService.Instance.PlayerId);
				if (inParty)
				{
					_teamCodeLabel.text = _services.FLLobbyService.CurrentPartyLobby.LobbyCode;
					foreach (var partyMember in partyLobby.Players!)
					{
						if (partyMember.Id == AuthenticationService.Instance.PlayerId) continue;
						friends.Remove(partyMember.Id);
						var currentElement = elements.FirstOrDefault(el => el.UserId == partyMember.Id);
						if (currentElement == null)
						{
							currentElement = new FriendListElement();
							_yourTeamContainer.Add(currentElement);
						}
						else
						{
							elements.Remove(currentElement);
						}

						currentElement.SetFromParty(partyMember).SetElementClickAction((el) =>
						{
							_services.GameSocialService.OpenPlayerOptions(el, Presenter.Root, partyMember.Id, partyMember.GetPlayerName(),
								new PlayerContextSettings()
								{
									ShowTeamOptions = true
								});
						});
						currentElement.SetCrown(partyLobby.HostId == partyMember.Id);
					}

					foreach (var friendListElement in elements) _yourTeamContainer.Remove(friendListElement);
					var pendingElements = _yourTeamContainer.Query().OfType<PendingInviteElement>().ToList();

					foreach (var sentPartyInvite in _services.FLLobbyService.SentPartyInvites)
					{
						var element = pendingElements.FirstOrDefault(e => e.Invite.PlayerId == sentPartyInvite.PlayerId);
						pendingElements.Remove(element);
						if (_yourTeamContainer.childCount >= 6 && element != null) continue;
						if (element == null)
						{
							_yourTeamContainer.Add(new PendingInviteElement()
								.SetPlayerInvite(sentPartyInvite)
								.OnCancel(() =>
								{
									_services.FLLobbyService.CancelPartyInvite(sentPartyInvite).Forget();
									RefreshData();
								})
							);
						}
					}

					foreach (var pendingInviteElement in pendingElements)
					{
						if (pendingInviteElement.IsDeleting)
						{
							continue;
						}

						_yourTeamContainer.Remove(pendingInviteElement);
					}
				}
				else
				{
					_yourTeamContainer.Clear();
					_yourTeamContainer.Add(_localPlayer);
				}

				if (_yourTeamContainer.childCount > 3)
				{
					if (!_gapHack.IsAttached())
					{
						_yourTeamContainer.Add(_gapHack);
					}
				}
				else
				{
					_gapHack.RemoveFromHierarchy();
				}

				_noFriendsLabel.SetDisplay(friends.Count == 0);
				_friends = friends.Values.ToList();
				_friends.Sort(FriendsServiceExtensions.FriendDefaultSorter());
				_friendsOnlineList.itemsSource = _friends;
				_friendsOnlineLabel.text = string.Format(ScriptLocalization.UITParty.online_friends, friendsCount);
			});
		}

		private void OnCopyCodeClicked()
		{
			_services.InGameNotificationService.QueueNotification(ScriptLocalization.UITShared.code_copied);
			UIUtils.SaveToClipboard(_services.FLLobbyService.CurrentPartyLobby.LobbyCode);
		}
	}
}
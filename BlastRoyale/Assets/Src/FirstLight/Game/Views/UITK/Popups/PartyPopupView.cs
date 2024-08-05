using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
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
using Unity.Services.Lobbies.Models;
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
		[Q("GameModeLabel")] private Label _gamemodeHeader;
		[Q("FriendsOnlineLabel")] private Label _friendsOnlineLabel;
		[Q("YourTeamContainer")] private VisualElement _yourTeamContainer;
		[Q("FriendsOnlineList")] private ListView _friendsOnlineList;
		[Q("NoFriendsLabel")] private VisualElement _noFriendsLabel;
		[Q("CopyCodeButton")] private ImageButton _copyCodeButton;

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
			_leaveTeamButton.clicked += UniTask.Action(LeaveParty);

			_copyCodeButton.clicked += OnCopyCodeClicked;
			_friendsOnlineList.makeItem = OnMakeFriendsItem;
			_friendsOnlineList.bindItem = OnBindFriendsItem;
		}

		public override void OnScreenOpen(bool reload)
		{
			RefreshData();
			_services.GameModeService.SelectedGameMode.InvokeObserve(RefreshGameMode);
			_services.FLLobbyService.CurrentPartyCallbacks.LocalLobbyJoined += OnLocalLobbyJoined;
			_services.FLLobbyService.CurrentPartyCallbacks.LocalLobbyUpdated += OnLobbyChanged;
			FriendsService.Instance.PresenceUpdated += OnPresenceUpdated;
		}

		private void OnLocalLobbyJoined(Lobby l)
		{
			RefreshData();
		}

		public override void OnScreenClose()
		{
			_services.MessageBrokerService.UnsubscribeAll(this);
			FriendsService.Instance.PresenceUpdated -= OnPresenceUpdated;
			_services.FLLobbyService.CurrentPartyCallbacks.LocalLobbyUpdated -= OnLobbyChanged;
			_services.FLLobbyService.CurrentPartyCallbacks.LocalLobbyJoined -= OnLocalLobbyJoined;
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
			_services.NotificationService.QueueNotification(ScriptLocalization.UITParty.notification_left_party);
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
				.AddOpenProfileAction(relationship)
				.TryAddInviteOption(relationship, UniTask.Action(async () =>
				{
					await _services.FLLobbyService.InviteToParty(relationship.Member.Id);
					_services.NotificationService.QueueNotification(ScriptLocalization.UITParty.notification_invite_sent);
				}));
			_elements[relationship.Member.Id] = e;
		}

		private void RefreshGameMode(GameModeInfo _, GameModeInfo modeInfo)
		{
			_gamemodeHeader.text = modeInfo.Entry.Visual.TitleTranslationKey.GetText();
		}

		private void RefreshData()
		{
			var partyLobby = _services.FLLobbyService.CurrentPartyLobby;
			var inParty = partyLobby != null;

			Element.EnableInClassList(USS_PARTY_JOINED, inParty);

			_yourTeamHeader.text = string.Format(ScriptLocalization.UITParty.your_party, partyLobby?.Players?.Count ?? 0, 4);
			_yourTeamContainer.Clear();
			var friends = FriendsService.Instance.Friends.Where(r => r.IsOnline()).ToDictionary(r => r.Member.Id, r => r);
			if (inParty)
			{
				_teamCodeLabel.text = _services.FLLobbyService.CurrentPartyLobby.LobbyCode;

				foreach (var partyMember in partyLobby.Players!)
				{
					if (partyMember.Id == AuthenticationService.Instance.PlayerId) continue;
					friends.Remove(partyMember.Id);
					var e = new FriendListElement().SetFromParty(partyMember).SetElementClickAction((el) =>
					{
						_services.GameSocialService.OpenPlayerOptions(el, Presenter.Root, partyMember.Id, partyMember.GetPlayerName(), new PlayerContextSettings()
						{
							ShowTeamOptions = true
						});
					});
					if (partyLobby.HostId == partyMember.Id)
					{
						e.AddCrown();
					}

					_yourTeamContainer.Add(e);
				}
			}

			// We always show the local player
			var own = new FriendListElement()
				.SetLocal()
				.SetPlayerName(AuthenticationService.Instance.PlayerName.TrimPlayerNameNumbers())
				.SetAvatar(MainInstaller.ResolveData().AppDataProvider.AvatarUrl)
				.SetElementClickAction(el =>
				{
					el.OpenTooltip(Presenter.Root, ScriptLocalization.UITCustomGames.local_player_tooltip);
				});
			if (!inParty || partyLobby.HostId == AuthenticationService.Instance.PlayerId)
			{
				own.AddCrown();
			}

			_yourTeamContainer.Add(own);
			if (inParty && partyLobby.Players.Count > 3)
			{
				_yourTeamHeader.Add(new VisualElement().AddClass("gap-hack"));
			}

			_noFriendsLabel.SetDisplay(friends.Count == 0);
			_friendsOnlineList.itemsSource = _friends = friends.Values.ToList();
			_friendsOnlineLabel.text = string.Format(ScriptLocalization.UITParty.online_friends, _friends.Count);
		}

		private void OnCopyCodeClicked()
		{
			_services.NotificationService.QueueNotification(ScriptLocalization.UITShared.code_copied);
			UIUtils.SaveToClipboard(_services.FLLobbyService.CurrentPartyLobby.LobbyCode);
		}
	}
}
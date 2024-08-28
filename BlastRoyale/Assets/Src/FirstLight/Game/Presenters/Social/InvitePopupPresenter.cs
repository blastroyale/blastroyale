using System;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.UIService;
using I2.Loc;
using Unity.Services.Friends;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Shows an invite popup for party or match invites.
	/// </summary>
	[UILayer(UILayer.Notifications, false)]
	public class InvitePopupPresenter : UIPresenterData<InvitePopupPresenter.StateData>
	{
		public class StateData
		{
			public FriendMessage.FriendInviteType Type;
			public string SenderID;
			public string LobbyCode;
		}

		private IGameServices _services;

		private GenericPopupElement _popupElement;
		private Label _contentLabel;
		private FriendListElement _sender;
		public string LobbyCode => Data.LobbyCode;

		protected override void QueryElements()
		{
			_services = MainInstaller.ResolveServices();

			_popupElement = Root.Q<GenericPopupElement>("Popup").Required();
			_sender = Root.Q<FriendListElement>("Sender").Required();
			_contentLabel = Root.Q<Label>("ContentLabel").Required();
			Root.Q<LocalizedButton>("AcceptButton").Required().clicked += () => AcceptInvite().Forget();
			Root.Q<LocalizedButton>("DeclineButton").Required().clicked += () => DeclineInvite().Forget();
			_popupElement.CloseClicked += () => DeclineInvite().Forget();
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			if (Data.SenderID == null)
			{
				// TODO: Deep link support
				return base.OnScreenOpen(reload);
			}

			var sender = FriendsService.Instance.GetFriendByID(Data.SenderID);
			var senderName = sender.Member.Profile.Name.TrimPlayerNameNumbers();
			_sender.SetFromRelationship(sender)
				.DisableActivity();
			switch (Data.Type)
			{
				case FriendMessage.FriendInviteType.Party:
					_popupElement.LocalizeTitle(ScriptTerms.UITParty.party_invite);
					_contentLabel.text = $"#{senderName} has invited you\nto join their party!";
					break;
				case FriendMessage.FriendInviteType.Match:
					_popupElement.LocalizeTitle(ScriptTerms.UITCustomGames.custom_game_invite);
					_contentLabel.text = $"#{senderName} has invited you\nto join their match!";
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return base.OnScreenOpen(reload);
		}

		private async UniTaskVoid AcceptInvite()
		{
			switch (Data.Type)
			{
				case FriendMessage.FriendInviteType.Party:
					if (_services.FLLobbyService.IsInPartyLobby())
					{
						await _services.FLLobbyService.LeaveParty();
					}

					await _services.FLLobbyService.JoinParty(Data.LobbyCode);
					break;
				case FriendMessage.FriendInviteType.Match:
					await _services.FLLobbyService.JoinMatch(Data.LobbyCode, false);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			_services.UIService.CloseLayer(UILayer.Popup).Forget();
			_services.UIService.CloseLayer(UILayer.Notifications).Forget();
		}

		private async UniTaskVoid DeclineInvite()
		{
			FriendsService.Instance.MessageAsync(Data.SenderID, FriendMessage.CreateDecline(Data.LobbyCode, Data.Type)).AsUniTask().Forget();
			await _services.UIService.CloseScreen<InvitePopupPresenter>();
		}
	}
}
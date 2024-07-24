using System;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.UIService;
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
			public InviteType Type;
			public string SenderID;
			public string LobbyCode;

			public enum InviteType
			{
				Party,
				Match
			}
		}

		private IGameServices _services;

		private Label _contentLabel;
		private FriendListElement _sender;

		protected override void QueryElements()
		{
			_services = MainInstaller.ResolveServices();

			_sender = Root.Q<FriendListElement>("Sender").Required();
			_contentLabel = Root.Q<Label>("ContentLabel").Required();
			Root.Q<Button>("AcceptButton").Required().clicked += () => AcceptInvite().Forget();
			Root.Q<Button>("DeclineButton").Required().clicked += () => DeclineInvite().Forget();
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
			_sender.SetPlayerName(senderName);
			switch (Data.Type)
			{
				case StateData.InviteType.Party:
					_contentLabel.text = $"#{senderName} has invited you\nto join their party!";
					break;
				case StateData.InviteType.Match:
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
				case StateData.InviteType.Party:
					await _services.FLLobbyService.JoinParty(Data.LobbyCode);
					break;
				case StateData.InviteType.Match:
					await _services.FLLobbyService.JoinMatch(Data.LobbyCode);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			await _services.UIService.CloseScreen<InvitePopupPresenter>();
		}

		private async UniTaskVoid DeclineInvite()
		{
			await _services.UIService.CloseScreen<InvitePopupPresenter>();
		}
	}
}
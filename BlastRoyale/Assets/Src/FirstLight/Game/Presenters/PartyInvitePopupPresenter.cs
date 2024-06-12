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
	[UILayer(UILayer.Notifications, false)]
	public class PartyInvitePopupPresenter : UIPresenterData<PartyInvitePopupPresenter.StateData>
	{
		public class StateData
		{
			public string SenderID;
			public string PartyCode;
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
			var sender = FriendsService.Instance.GetFriendByID(Data.SenderID);
			var senderName = sender.Member.Profile.Name;
			_sender.SetPlayerName(senderName);
			_contentLabel.text = $"#{senderName} has invited you\nto join their party!";
			
			return base.OnScreenOpen(reload);
		}

		private async UniTaskVoid AcceptInvite()
		{
			await _services.FLLobbyService.JoinParty(Data.PartyCode);
			await _services.UIService.CloseScreen<PartyInvitePopupPresenter>();
		}

		private async UniTaskVoid DeclineInvite()
		{
			await _services.UIService.CloseScreen<PartyInvitePopupPresenter>();
		}
	}
}
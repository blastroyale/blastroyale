using Cysharp.Threading.Tasks;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Utils;
using FirstLight.SDK.Services;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Handles deep linking into the game.
	/// </summary>
	public class DeepLinkService
	{
		private readonly UIService.UIService _uiService;

		private string _deepLink;

		public DeepLinkService(IMessageBrokerService messageBrokerService, UIService.UIService uiService)
		{
			_uiService = uiService;
			_deepLink = Application.absoluteURL;

			Application.deepLinkActivated += OnDeepLinkActivated;
			messageBrokerService.Subscribe<MainMenuLoadedMessage>(OnMainMenuOpened);
		}

		private void OnMainMenuOpened(MainMenuLoadedMessage obj)
		{
			if (string.IsNullOrEmpty(_deepLink)) return;

			ProcessDeepLink();
		}

		private void OnDeepLinkActivated(string link)
		{
			_deepLink = link;
			ProcessDeepLink();
		}

		private void ProcessDeepLink()
		{
			if (!RemoteConfigs.Instance.EnableDeepLinking) return;

			var split = _deepLink.Replace("blastroyale://", string.Empty).Split('/');
			var type = split[0];
			var id = split[1];

			switch (type)
			{
				case "match":
					_uiService.OpenScreen<InvitePopupPresenter>(new InvitePopupPresenter.StateData
					{
						Type = FriendMessage.FriendInviteType.Match,
						SenderID = null,
						LobbyCode = id
					}).Forget();
					break;
				case "party":
					_uiService.OpenScreen<InvitePopupPresenter>(new InvitePopupPresenter.StateData
					{
						Type = FriendMessage.FriendInviteType.Party,
						SenderID = null,
						LobbyCode = id
					}).Forget();
					break;
			}

			_deepLink = string.Empty;
		}
	}
}
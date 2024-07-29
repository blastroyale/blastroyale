using Cysharp.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.UIService;
using QuickEye.UIToolkit;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views
{
	public class PlayerNameView : UIView
	{
		[Q("PlayerLabel")] public Label _playerLabel;
		[Q("TrophiesLabel")] public Label _trophiesLabel;
		[Q("AddFriendButton")] public ImageButton _addFriendButton;

		private string _unityId;
		private string _playerName;
		private IGameServices _services;

		protected override void Attached()
		{
			_services = MainInstaller.ResolveServices();
		}

		public void SetData(string playerName, string unityID, int trophies, Color rankColor)
		{
			_unityId = unityID;
			_playerName = playerName;
			_playerLabel.text = playerName;
			_playerLabel.style.color = rankColor;
			_trophiesLabel.text = $"{trophies} <sprite name=\"TrophyIcon\">";
			UpdateFriendButton();
		}

		public void SetLocal(string playerName, Color color)
		{
			_addFriendButton.SetDisplay(false);
			_trophiesLabel.SetDisplay(false);
			_playerLabel.text = playerName;
			_playerLabel.style.color = color;
		}

		private void UpdateFriendButton()
		{
			if (_unityId == null) // BOT
			{
				if (_services.GameSocialService.IsBotInvited(_playerName))
				{
					_addFriendButton.SetEnabled(false);
				}
				else
				{
					_addFriendButton.clicked += FakeAddBot;
				}

				return;
			}

			if (AuthenticationService.Instance.PlayerId == _unityId)
			{
				_addFriendButton.SetDisplay(false);
				return;
			}

			var relation = FriendsService.Instance.GetRelationShipById(_unityId);
			if (relation == null)
			{
				_addFriendButton.clicked += SendInvite;
				return;
			}

			if (relation.Type == RelationshipType.FriendRequest)
			{
				_addFriendButton.SetEnabled(false);
				return;
			}

			_addFriendButton.SetDisplay(false);
		}

		private void SendInvite()
		{
			_addFriendButton.clicked -= SendInvite;
			_addFriendButton.SetEnabled(false);
			FriendsService.Instance.AddFriendHandled(_unityId).Forget();
		}

		private void FakeAddBot()
		{
			_addFriendButton.clicked -= FakeAddBot;
			_addFriendButton.SetEnabled(false);
			_services.GameSocialService.FakeInviteBot(_playerName);
		}
	}
}
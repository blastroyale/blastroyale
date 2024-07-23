using System;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using I2.Loc;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays a relationship in the friends screen (friend / request / blocked)
	/// </summary>
	public class FriendListElement : VisualElement
	{
		private const string USS_BLOCK = "friend-list-element";
		private const string USS_PLAYER_BAR_CONTAINER = USS_BLOCK + "__player-bar-container";
		private const string USS_AVATAR = USS_BLOCK + "__avatar";
		private const string USS_ONLINE_INDICATOR = USS_BLOCK + "__online-indicator";
		private const string USS_ONLINE_INDICATOR_ONLINE = USS_ONLINE_INDICATOR + "--online";
		private const string USS_HEADER = USS_BLOCK + "__header";
		private const string USS_NAME_AND_TROPHIES = USS_BLOCK + "__name-and-trophies";
		private const string USS_ACTIVITY = USS_BLOCK + "__activity";
		private const string USS_MAIN_ACTION_BUTTON = USS_BLOCK + "__main-action-button";
		private const string USS_MORE_ACTIONS_BUTTON = USS_BLOCK + "__more-actions-button";
		private const string USS_ACCEPT_DECLINE_CONTAINER = USS_BLOCK + "__accept-decline-container";
		private const string USS_BACKGROUND = USS_BLOCK + "__background";
		private const string USS_BACKGROUND_PATTERN = USS_BLOCK + "__background-pattern";

		private readonly VisualElement _avatar;
		private readonly VisualElement _onlineIndicator;
		private readonly Label _nameAndTrophiesLabel;
		private readonly Label _statusLabel;
		private readonly LocalizedButton _mainActionButton;
		private readonly ImageButton _moreActionsButton;
		private readonly Label _header;
		private readonly VisualElement _acceptDeclineContainer;
		private readonly LocalizedButton _acceptButton;
		private readonly LocalizedButton _declineButton;

		private Action _mainAction;
		private Action<VisualElement> _moreActionsAction;
		private Action _acceptAction;
		private Action _declineAction;
		
		public FriendListElement()
		{
			AddToClassList(USS_BLOCK);

			Add(_header = new Label("ONLINE (2)"));
			_header.AddToClassList(USS_HEADER);

			var background = new VisualElement {name = "background"};
			Add(background);
			background.AddToClassList(USS_BACKGROUND);
			{
				var backgroundPattern = new VisualElement {name = "background-pattern"};
				background.Add(backgroundPattern);
				backgroundPattern.AddToClassList(USS_BACKGROUND_PATTERN);

				var playerBarContainer = new VisualElement {name = "player-bar-container"};
				background.Add(playerBarContainer);
				playerBarContainer.AddToClassList(USS_PLAYER_BAR_CONTAINER);
				{
					playerBarContainer.Add(_avatar = new VisualElement {name = "avatar"});
					_avatar.AddToClassList(USS_AVATAR);
					{
						_avatar.Add(_onlineIndicator = new VisualElement {name = "online-indicator"});
						_onlineIndicator.AddToClassList(USS_ONLINE_INDICATOR);
					}

					playerBarContainer.Add(_nameAndTrophiesLabel = new Label(name) {name = "name-and-trophies"});
					_nameAndTrophiesLabel.AddToClassList(USS_NAME_AND_TROPHIES);

					playerBarContainer.Add(_statusLabel = new Label("In Main Menu") {name = "activity"});
					_statusLabel.AddToClassList(USS_ACTIVITY);

					playerBarContainer.Add(_mainActionButton = new LocalizedButton("main-action-button") {name = "UITFriends/invite"});
					_mainActionButton.AddToClassList(USS_MAIN_ACTION_BUTTON);
					_mainActionButton.AddToClassList("button-long");
					_mainActionButton.AddToClassList("button-long--yellow");

					playerBarContainer.Add(_moreActionsButton = new ImageButton {name = "more-actions-button"});
					_moreActionsButton.AddToClassList(USS_MORE_ACTIONS_BUTTON);
				}
				background.Add(_acceptDeclineContainer = new VisualElement {name = "accept-decline-container"});
				_acceptDeclineContainer.AddToClassList(USS_ACCEPT_DECLINE_CONTAINER);
				{
					_acceptDeclineContainer.Add(_acceptButton = new LocalizedButton("accept-button") {LocalizationKey = "UITFriends/accept"});
					_acceptButton.AddToClassList("button-long");
					_acceptButton.AddToClassList("button-long--green");
					_acceptDeclineContainer.Add(_declineButton = new LocalizedButton("decline-button") {LocalizationKey = "UITFriends/decline"});
					_declineButton.AddToClassList("button-long");
					_declineButton.AddToClassList("button-long--red");
				}
			}

			_mainActionButton.clicked += () => _mainAction?.Invoke();
			_moreActionsButton.clicked += () => _moreActionsAction?.Invoke(_moreActionsButton);
			_acceptButton.clicked += () => _acceptAction?.Invoke();
			_declineButton.clicked += () => _declineAction?.Invoke();
			SetStatus(null, true);
		}

		public FriendListElement SetFromParty(Player partyPlayer)
		{
			SetPlayerName(partyPlayer.GetPlayerName());
			SetAvatarHack(partyPlayer.Id).Forget();
			return this;
		}

		public FriendListElement SetFromRelationship(Relationship relationship)
		{
			var activity = relationship.Member?.Presence?.GetActivity<FriendActivity>();
			SetPlayerName(relationship.Member?.Profile.Name);
			SetStatus(activity?.Status, relationship.IsOnline());
			_avatar.SetDisplay(true);
			if (!string.IsNullOrEmpty(activity?.AvatarUrl))
			{
				MainInstaller.ResolveServices().RemoteTextureService.SetTexture(_avatar, activity.AvatarUrl);
			}
			else
			{
				SetAvatarHack(relationship.Member.Id).Forget();
			}
			return this;
		}

		private async UniTaskVoid SetAvatarHack(string unityId)
		{
			if (unityId == null)
			{
				return;
			}
			var services = MainInstaller.ResolveServices();
			FLog.Verbose("Setting avatar hack for "+unityId);
			try
			{
				var playfabId = await CloudSaveService.Instance.LoadPlayfabID(unityId);
				services.ProfileService.GetPlayerPublicProfile(playfabId, profile =>
				{
					services.RemoteTextureService.SetTexture(_avatar, profile.AvatarUrl);
				});
			}
			catch (Exception e)
			{
				FLog.Error($"Error setting friend unityid {unityId} avatar", e);
			}
		}

		public FriendListElement SetPlayerName(string playerName)
		{
			_nameAndTrophiesLabel.text = playerName?.TrimPlayerNameNumbers();
			return this;
		}

		public FriendListElement SetHeader(string header)
		{
			_header.SetDisplay(header != null);
			_header.text = header;
			return this;
		}

		public FriendListElement SetStatus(string activity, bool? online)
		{
			_statusLabel.SetVisibility(!string.IsNullOrEmpty(activity));
			_statusLabel.text = activity;

			if (online.HasValue)
			{
				_onlineIndicator.SetDisplay(true);
				_onlineIndicator.EnableInClassList(USS_ONLINE_INDICATOR_ONLINE, online.Value);
			}
			else
			{
				_onlineIndicator.SetDisplay(false);
			}

			return this;
		}

		public FriendListElement SetAcceptDecline(Action acceptAction, Action declineAction)
		{
			_acceptDeclineContainer.SetDisplay(acceptAction != null && declineAction != null);
			_acceptAction = acceptAction;
			_declineAction = declineAction;
			return this;
		}

		public FriendListElement TryAddInviteOption(Relationship friend, Action callback)
		{
			var services = MainInstaller.ResolveServices();
			var showInvite = callback != null && services.GameSocialService.CanInvite(friend);
			if(showInvite)
				return SetMainAction(ScriptLocalization.UITFriends.invite, callback, false);
			return SetMainAction("", null, false);;
		}
		
		public FriendListElement AddOpenProfileAction(Relationship friend)
		{
			return SetMoreActions(_ => PlayerStatisticsPopupPresenter.Open(friend.Member.Id).Forget());
		}

		public FriendListElement SetMainAction(string label, Action mainAction, bool negative)
		{
			_mainActionButton.SetDisplay(!string.IsNullOrEmpty(label) && mainAction != null);
			_mainActionButton.EnableInClassList("button-long--red", negative);
			_mainActionButton.EnableInClassList("button-long--yellow", !negative);

			_mainActionButton.text = label;
			_mainAction = mainAction;
			return this;
		}

		public FriendListElement SetMoreActions(Action<VisualElement> moreActionsAction)
		{
			_moreActionsButton.SetDisplay(moreActionsAction != null);
			_moreActionsAction = moreActionsAction;
			return this;
		}

		public FriendListElement SetAvatar(string avatarUrl)
		{
			MainInstaller.ResolveServices().RemoteTextureService.SetTexture(_avatar, avatarUrl);
			_avatar.SetVisibility(avatarUrl != null);
			return this;
		}

		public new class UxmlFactory : UxmlFactory<FriendListElement, UxmlTraits>
		{
		}
	}
}
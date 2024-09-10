using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Social;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using I2.Loc;
using Unity.Services.CloudSave;
using Unity.Services.Friends.Models;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays a relationship in the friends screen (friend / request / blocked)
	/// </summary>
	public class FriendListElement : ImageButton
	{
		private const string USS_BLOCK = "friend-list-element";
		private const string USS_LOCAL_MODIFIER = USS_BLOCK + "--local";
		private const string USS_OFFLINE_MODIFIER = USS_BLOCK + "--offline";
		private const string USS_CROWN = USS_BLOCK + "__crown";
		private const string USS_PLAYER_BAR_CONTAINER = USS_BLOCK + "__player-bar-container";
		private const string USS_AVATAR = USS_BLOCK + "__avatar";
		private const string USS_ONLINE_INDICATOR = USS_BLOCK + "__online-indicator";
		private const string USS_ONLINE_INDICATOR_ONLINE = USS_ONLINE_INDICATOR + "--online";
		private const string USS_HEADER = USS_BLOCK + "__header";
		private const string USS_TEXT_CONTAINER = USS_BLOCK + "__text-container";
		private const string USS_NAME_AND_TROPHIES = USS_BLOCK + "__name-and-trophies";
		private const string USS_ACTIVITY = USS_BLOCK + "__activity";
		private const string USS_ACTIVITY_IN_TEAM = USS_ACTIVITY + "--in-team";
		private const string USS_MAIN_ACTION_BUTTON = USS_BLOCK + "__main-action-button";
		private const string USS_MORE_ACTIONS_BUTTON = USS_BLOCK + "__more-actions-button";
		private const string USS_ACCEPT_DECLINE_CONTAINER = USS_BLOCK + "__accept-decline-container";
		private const string USS_BACKGROUND = USS_BLOCK + "__background";
		private const string USS_BACKGROUND_MASK = USS_BLOCK + "__background-mask";
		private const string USS_BACKGROUND_PATTERN = USS_BLOCK + "__background-pattern";
		private const string USS_BACKGROUND_PATTERN_RIGHT = USS_BLOCK + "__background-pattern--right";

		private readonly RemoteAvatarElement _avatar;
		private readonly VisualElement _crown;
		private readonly VisualElement _onlineIndicator;
		private readonly Label _nameLabel;
		private readonly Label _trophiesLabel;
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
		private string _lastAvatarUrl;
		private string _userId;

		public string UserId => _userId;

		public FriendListElement()
		{
			AddToClassList(USS_BLOCK);

			Add(_header = new LabelOutlined("ONLINE (2)") {name = "Header"});
			_header.AddToClassList(USS_HEADER);

			var background = new VisualElement {name = "background"};
			Add(background);
			background.AddToClassList(USS_BACKGROUND);
			{
				background.Add(_crown = new VisualElement() {name = "crown"}.AddClass(USS_CROWN).SetDisplay(false));
				var backgroundMask = new VisualElement() {name = "background-mask"}.AddClass(USS_BACKGROUND_MASK);
				background.Add(backgroundMask);
				{
					backgroundMask.Add(new VisualElement {name = "background-pattern-left"}.AddClass(USS_BACKGROUND_PATTERN));
					backgroundMask.Add(new VisualElement {name = "background-pattern-right"}.AddClass(USS_BACKGROUND_PATTERN).AddClass(USS_BACKGROUND_PATTERN_RIGHT));
				}
				var playerBarContainer = new VisualElement {name = "player-bar-container"};
				background.Add(playerBarContainer);

				playerBarContainer.AddToClassList(USS_PLAYER_BAR_CONTAINER);
				{
					playerBarContainer.Add(_avatar = new RemoteAvatarElement() {name = "avatar"});
					_avatar.AddToClassList(USS_AVATAR);
					{
						_avatar.Add(_onlineIndicator = new VisualElement {name = "online-indicator"});
						_onlineIndicator.AddToClassList(USS_ONLINE_INDICATOR);
					}

					var textContainer = new VisualElement() {name = "text-container"}.AddClass(USS_TEXT_CONTAINER);
					{
						textContainer.Add(_nameLabel = new LabelOutlined("Longplayername12442222") {name = "name-label"}
							.AddClass(USS_NAME_AND_TROPHIES));
						textContainer.Add(_trophiesLabel = new LabelOutlined("<color=#FFC700>123123</color>" + " <sprite name=\"Ammoicon\">") {name = "trophies-label"}
							.AddClass(USS_NAME_AND_TROPHIES));
					}
					playerBarContainer.Add(textContainer);

					playerBarContainer.Add(_statusLabel = new LabelOutlined("In Main Menu") {name = "activity"});
					_statusLabel.AddToClassList(USS_ACTIVITY);

					playerBarContainer.Add(_mainActionButton = new LocalizedButton(ScriptTerms.UITFriends.invite) {name = "main-action-button"});
					_mainActionButton.AddToClassList(USS_MAIN_ACTION_BUTTON);
					_mainActionButton.AddToClassList("button-long");
					_mainActionButton.AddToClassList("button-long--yellow");

					playerBarContainer.Add(_moreActionsButton = new ImageButton {name = "more-actions-button"});
					_moreActionsButton.AddToClassList(USS_MORE_ACTIONS_BUTTON);
				}
				background.Add(_acceptDeclineContainer = new VisualElement {name = "accept-decline-container"});
				_acceptDeclineContainer.AddToClassList(USS_ACCEPT_DECLINE_CONTAINER);
				{
					_acceptDeclineContainer.Add(_acceptButton = new LocalizedButton(ScriptTerms.UITFriends.accept) {name = "accept-button"});
					_acceptButton.AddToClassList("button-long");
					_acceptButton.AddToClassList("button-long--green");
					_acceptDeclineContainer.Add(_declineButton = new LocalizedButton(ScriptTerms.UITFriends.decline) {name = "decline-button"});
					_declineButton.AddToClassList("button-long");
					_declineButton.AddToClassList("button-long--red");
				}
			}

			_mainActionButton.clicked += () => _mainAction?.Invoke();
			_moreActionsButton.clicked += () => _moreActionsAction?.Invoke(_moreActionsButton);
			_acceptButton.clicked += () => _acceptAction?.Invoke();
			_declineButton.clicked += () => _declineAction?.Invoke();
			SetStatus((string) null, true, null);
		}

		public FriendListElement SetFromParty(Player partyPlayer)
		{
			_userId = partyPlayer.Id;

			var stringTrophies = partyPlayer.GetProperty(FLLobbyService.KEY_TROHPIES);
			int.TryParse(stringTrophies, out var trophies);

			FillElementsFromHack(new PlayfabUnityBridgeService.CacheHackData()
			{
				AvatarUrl = partyPlayer.GetProperty(FLLobbyService.KEY_AVATAR_URL),
				Trophies = trophies,
				PlayerName = partyPlayer.GetPlayerName(),
			});
			return this;
		}

		public FriendListElement SetLocal()
		{
			return this.AddClass(USS_LOCAL_MODIFIER);
		}

		public FriendListElement SetCrown(bool value)
		{
			_crown.SetDisplay(value);
			return this;
		}

		public FriendListElement SetFromRelationship(Relationship relationship)
		{
			_userId = relationship.Member.Id;
			var services = MainInstaller.ResolveServices();
			var activity = relationship.Member?.Presence?.GetActivity<FriendActivity>();
			SetPlayerName(relationship.Member?.Profile.Name);

			if (activity?.Region != null && activity?.Region != services.LocalPrefsService.ServerRegion?.Value)
			{
				SetStatus("Region " + activity.Region.GetPhotonRegionTranslation(), relationship.IsOnline(), null);
			}
			else
			{
				SetStatus(activity, relationship.IsOnline(), relationship.Member?.Presence?.LastSeen);
			}

			_avatar.SetDisplay(true);
			if (!string.IsNullOrEmpty(activity?.AvatarUrl))
			{
				FillElementsFromHack(new PlayfabUnityBridgeService.CacheHackData()
				{
					Trophies = activity.Trophies,
					AvatarUrl = activity.AvatarUrl,
					PlayerName = relationship.Member?.Profile.Name
				});
			}
			else
			{
				SetDataHack(relationship).Forget();
			}

			return this;
		}

		private void FillElementsFromHack(PlayfabUnityBridgeService.CacheHackData hack)
		{
			SetAvatar(hack.AvatarUrl);
			SetPlayerName(hack.PlayerName, hack.Trophies);
		}

		private async UniTaskVoid SetDataHack(Relationship relationship)
		{
			var services = MainInstaller.ResolveServices();
			var unityId = relationship.Member.Id;

			var data = await services.PlayfabUnityBridgeService.LoadDataForPlayer(unityId, relationship.Member.Profile?.Name?.TrimPlayerNameNumbers());
			if (panel == null || parent == null) return;
			if (data == null)
			{
				_avatar.SetFailedState();
				return;
			}

			FillElementsFromHack(data);
		}

		public FriendListElement SetPlayerName(string playerName, int trophies)
		{
			_trophiesLabel.SetDisplay(true);
			_nameLabel.text = playerName?.TrimPlayerNameNumbers();
			_trophiesLabel.text = $"<color=#FFC700>{trophies}</color> <size=+2px><sprite name=\"TrophyIcon\"></size>";
			return this;
		}

		public FriendListElement SetPlayerName(string playerName)
		{
			_trophiesLabel.SetDisplay(false);
			_nameLabel.text = playerName?.TrimPlayerNameNumbers().Replace(AuthenticationServiceExtensions.SPACE_CHAR_MATCH, ' ');
			return this;
		}

		public FriendListElement SetHeader(string header)
		{
			_header.SetDisplay(header != null);
			_header.text = header;
			return this;
		}

		public FriendListElement SetStatus(FriendActivity activity, bool? online, DateTime? presenceLastSeen)
		{
			SetStatus(activity?.Status, online, presenceLastSeen);
			if (activity?.CurrentActivityEnum == GameActivities.In_team)
			{
				_statusLabel.EnableInClassList(USS_ACTIVITY_IN_TEAM, true);
			}

			return this;
		}

		public FriendListElement SetStatus(string activity, bool? online, DateTime? presenceLastSeen)
		{
			_statusLabel.SetVisibility(!string.IsNullOrEmpty(activity));
			_statusLabel.text = activity;
			_statusLabel.RemoveFromClassList(USS_ACTIVITY_IN_TEAM);
			var isOnline = online ?? false;
			EnableInClassList(USS_OFFLINE_MODIFIER, !isOnline);
			_onlineIndicator.SetDisplay(true);
			_onlineIndicator.EnableInClassList(USS_ONLINE_INDICATOR_ONLINE, isOnline);
			if (!isOnline && presenceLastSeen.HasValue)
			{
				_statusLabel.SetVisibility(true);
				var difference = (DateTime.UtcNow - presenceLastSeen.Value);
				if (difference.TotalMilliseconds > 0)
				{
					var lastSeen = (DateTime.UtcNow - presenceLastSeen.Value)
						.Display(showSeconds: false, onlyMostRelevant: true, shortFormat: false)
						.ToLowerInvariant() + " ago";
					_statusLabel.SetDisplay(true);
					_statusLabel.text = $"Last seen\n{lastSeen}";
				}
				else
				{
					_statusLabel.SetDisplay(false);
				}
			}

			return this;
		}

		public FriendListElement DisableStatusCircle()
		{
			_onlineIndicator.SetDisplay(false);
			return this;
		}

		public FriendListElement DisableActivity()
		{
			_statusLabel.SetDisplay(false);
			return this;
		}

		public FriendListElement SetAcceptDecline(Action acceptAction, Action declineAction)
		{
			_acceptDeclineContainer.SetDisplay(acceptAction != null && declineAction != null);
			_acceptAction = acceptAction;
			_declineAction = declineAction;
			return this;
		}

		public FriendListElement TryAddInviteOption(VisualElement root, Relationship friend, Action callback)
		{
			var services = MainInstaller.ResolveServices();

			if (!friend.IsOnline())
			{
				return SetMainAction(null, null, false);
			}

			string reason = null;

			var showInvite = callback != null && services.GameSocialService.CanInvite(friend, out reason);
			if (showInvite)
				return SetMainAction(ScriptLocalization.UITFriends.invite, callback, false);

			return SetMainAction(ScriptLocalization.UITFriends.invite, null, false)
				.DisableMainActionButton(root, reason);
		}

		public FriendListElement AddOpenProfileAction(Relationship friend)
		{
			return SetMoreActions(_ => PlayerStatisticsPopupPresenter.Open(friend.Member.Id).Forget());
		}

		public FriendListElement SetMainAction(string label, Action mainAction, bool negative)
		{
			_mainActionButton.RemoveFromClassList("button-long--disabled");
			_mainActionButton.SetDisplay(!string.IsNullOrEmpty(label));
			_mainActionButton.EnableInClassList("button-long--red", negative);
			_mainActionButton.EnableInClassList("button-long--yellow", !negative);

			_mainActionButton.text = label;
			_mainAction = mainAction;
			return this;
		}

		public FriendListElement DisableMainActionButton(VisualElement root, string reason = null)
		{
			_mainAction = null;
			if (reason != null)
			{
				_mainAction = () =>
				{
					_mainActionButton.OpenLocalizedTooltip(root, "UITFriends/tooltip_disabled_" + reason);
				};
			}

			_mainActionButton.AddToClassList("button-long--disabled");
			return this;
		}

		public FriendListElement SetMoreActions(Action<VisualElement> moreActionsAction)
		{
			_moreActionsButton.SetDisplay(moreActionsAction != null);
			_moreActionsAction = moreActionsAction;
			return this;
		}

		public FriendListElement SetElementClickAction(Action<VisualElement> action)
		{
			clickable = null;
			clicked += () => action(this);
			return this;
		}

		public FriendListElement SetAvatar(string avatarUrl)
		{
			if (avatarUrl == _lastAvatarUrl)
			{
				return this;
			}

			if (avatarUrl != null)
			{
				_lastAvatarUrl = avatarUrl;
				var task = MainInstaller.ResolveServices().RemoteTextureService.RequestTexture(avatarUrl);
				_avatar.SetAvatar(task).Forget();
			}

			_avatar.SetVisibility(avatarUrl != null);
			return this;
		}

		public new class UxmlFactory : UxmlFactory<FriendListElement, UxmlTraits>
		{
		}
	}
}
using System;
using FirstLight.Game.Utils;
using Unity.Services.Friends.Models;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	public class FriendListElement : VisualElement
	{
		public const string USS_BLOCK = "friend-list-element";
		public const string USS_PLAYER_BAR_CONTAINER = USS_BLOCK + "__player-bar-container";
		public const string USS_AVATAR = USS_BLOCK + "__avatar";
		public const string USS_ONLINE_INDICATOR = USS_BLOCK + "__online-indicator";
		public const string USS_ONLINE_INDICATOR_ONLINE = USS_ONLINE_INDICATOR + "--online";
		public const string USS_HEADER = USS_BLOCK + "__header";
		public const string USS_NAME_AND_TROPHIES = USS_BLOCK + "__name-and-trophies";
		public const string USS_ACTIVITY = USS_BLOCK + "__activity";
		public const string USS_MAIN_ACTION_BUTTON = USS_BLOCK + "__main-action-button";
		public const string USS_MORE_ACTIONS_BUTTON = USS_BLOCK + "__more-actions-button";
		public const string USS_ACCEPT_BUTTON = USS_BLOCK + "__accept-button";
		public const string USS_DECLINE_BUTTON = USS_BLOCK + "__decline-button";
		public const string USS_ACCEPT_DECLINE_CONTAINER = USS_BLOCK + "__accept-decline-container";

		private readonly VisualElement _avatar;
		private readonly VisualElement _onlineIndicator;
		private readonly Label _nameAndTrophiesLabel;
		private readonly Label _activityLabel;
		private readonly Button _mainActionButton;
		private readonly ImageButton _moreActionsButton;
		private readonly Label _header;
		private readonly VisualElement _acceptDeclineContainer;
		private readonly Button _acceptButton;
		private readonly Button _declineButton;

		private Action<Relationship> _mainAction;
		private Action<VisualElement, Relationship> _moreActionsAction;
		private Action<Relationship> _acceptAction;
		private Action<Relationship> _declineAction;

		private Relationship _relationship;

		public FriendListElement()
		{
			AddToClassList(USS_BLOCK);

			Add(_header = new Label("ONLINE (2)"));
			_header.AddToClassList(USS_HEADER);

			var playerBarContainer = new VisualElement {name = "player-bar-container"};
			Add(playerBarContainer);
			playerBarContainer.AddToClassList(USS_PLAYER_BAR_CONTAINER);
			{
				playerBarContainer.Add(_avatar = new VisualElement {name = "avatar"});
				_avatar.AddToClassList(USS_AVATAR);
				{
					_avatar.Add(_onlineIndicator = new VisualElement {name = "online-indicator"});
					_onlineIndicator.AddToClassList(USS_ONLINE_INDICATOR);
				}

				playerBarContainer.Add(_nameAndTrophiesLabel = new Label("Playerwithalongname\n12345 T") {name = "name-and-trophies"});
				_nameAndTrophiesLabel.AddToClassList(USS_NAME_AND_TROPHIES);

				playerBarContainer.Add(_activityLabel = new Label("Player last seend\n30 minutes ago") {name = "activity"});
				_activityLabel.AddToClassList(USS_ACTIVITY);

				playerBarContainer.Add(_mainActionButton = new Button {name = "main-action-button"});
				_mainActionButton.text = "INVITE";
				_mainActionButton.AddToClassList(USS_MAIN_ACTION_BUTTON);

				playerBarContainer.Add(_moreActionsButton = new ImageButton {name = "more-actions-button"});
				_moreActionsButton.AddToClassList(USS_MORE_ACTIONS_BUTTON);
			}

			Add(_acceptDeclineContainer = new VisualElement {name = "accept-decline-container"});
			_acceptDeclineContainer.AddToClassList(USS_ACCEPT_DECLINE_CONTAINER);
			{
				_acceptDeclineContainer.Add(_acceptButton = new Button {name = "accept-button"});
				_acceptButton.AddToClassList(USS_ACCEPT_BUTTON);
				_acceptButton.text = "ACCEPT";
				_acceptDeclineContainer.Add(_declineButton = new Button {name = "decline-button"});
				_declineButton.AddToClassList(USS_DECLINE_BUTTON);
				_declineButton.text = "DECLINE";
			}

			_mainActionButton.clicked += () => _mainAction?.Invoke(_relationship);
			_moreActionsButton.clicked += () => _moreActionsAction?.Invoke(_moreActionsButton, _relationship);
			_acceptButton.clicked += () => _acceptAction?.Invoke(_relationship);
			_declineButton.clicked += () => _declineAction?.Invoke(_relationship);
		}

		public void SetData(Relationship relationship, string header, string mainActionLabel, Action<Relationship> mainAction,
							Action<Relationship> acceptAction, Action<Relationship> declineAction, Action<VisualElement, Relationship> moreActionsAction)
		{
			_relationship = relationship;
			_nameAndTrophiesLabel.text = $"{relationship.Member.Profile.Name}\n{222} T";

			_header.SetDisplay(header != null);
			_header.text = header;

			if (relationship.Member.Presence != null)
			{
				var presence = relationship.Member.Presence;

				_activityLabel.SetVisibility(true);
				var availability = presence.Availability;
				var lastSeenMinutes = (DateTime.UtcNow - presence.LastSeen).TotalMinutes;

				_onlineIndicator.EnableInClassList(USS_ONLINE_INDICATOR_ONLINE, availability == Availability.Online);

				_activityLabel.text = $"Last online\n{lastSeenMinutes} minutes ago\n{availability}";
			}
			else
			{
				_activityLabel.SetVisibility(false);
			}

			// Main button
			_mainActionButton.SetDisplay(mainAction != null);
			_mainActionButton.text = mainActionLabel;
			_mainAction = mainAction;

			// Accept/Decline buttons
			_acceptDeclineContainer.SetDisplay(acceptAction != null && declineAction != null);
			_acceptAction = acceptAction;
			_declineAction = declineAction;

			// Details button
			_moreActionsButton.SetDisplay(moreActionsAction != null);
			_moreActionsAction = moreActionsAction;
		}

		public new class UxmlFactory : UxmlFactory<FriendListElement, UxmlTraits>
		{
		}
	}
}
using System;
using FirstLight.Game.Data;
using FirstLight.Game.Utils;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
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

					playerBarContainer.Add(_nameAndTrophiesLabel = new Label("PlayerWithALongName\n<color=#FFC700>12345") {name = "name-and-trophies"});
					_nameAndTrophiesLabel.AddToClassList(USS_NAME_AND_TROPHIES);

					playerBarContainer.Add(_activityLabel = new Label("Player last seend\n30 minutes ago") {name = "activity"});
					_activityLabel.AddToClassList(USS_ACTIVITY);

					playerBarContainer.Add(_mainActionButton = new Button {name = "main-action-button"});
					_mainActionButton.text = "INVITE";
					_mainActionButton.AddToClassList(USS_MAIN_ACTION_BUTTON);
					_mainActionButton.AddToClassList("button-long");
					_mainActionButton.AddToClassList("button-long--yellow");

					playerBarContainer.Add(_moreActionsButton = new ImageButton {name = "more-actions-button"});
					_moreActionsButton.AddToClassList(USS_MORE_ACTIONS_BUTTON);
				}

				background.Add(_acceptDeclineContainer = new VisualElement {name = "accept-decline-container"});
				_acceptDeclineContainer.AddToClassList(USS_ACCEPT_DECLINE_CONTAINER);
				{
					_acceptDeclineContainer.Add(_acceptButton = new Button {name = "accept-button"});
					_acceptButton.AddToClassList("button-long");
					_acceptButton.AddToClassList("button-long--purple"); // TODO mihak: Change to green
					_acceptButton.text = "ACCEPT";
					_acceptDeclineContainer.Add(_declineButton = new Button {name = "decline-button"});
					_declineButton.AddToClassList("button-long");
					_declineButton.AddToClassList("button-long--red");
					_declineButton.text = "DECLINE";
				}
			}

			_mainActionButton.clicked += () => _mainAction?.Invoke(_relationship);
			_moreActionsButton.clicked += () => _moreActionsAction?.Invoke(_moreActionsButton, _relationship);
			_acceptButton.clicked += () => _acceptAction?.Invoke(_relationship);
			_declineButton.clicked += () => _declineAction?.Invoke(_relationship);
		}

		public void SetData(Relationship relationship, string header, string mainActionLabel, Action<Relationship> mainAction,
							Action<Relationship> acceptAction, Action<Relationship> declineAction,
							Action<VisualElement, Relationship> moreActionsAction)
		{
			_relationship = relationship;
			_nameAndTrophiesLabel.text = $"{relationship.Member.Profile.Name}\n{222} T";

			_header.SetDisplay(header != null);
			_header.text = header;

			if (relationship.Type == RelationshipType.FriendRequest)
			{
				_activityLabel.SetVisibility(true);
				_activityLabel.text = "Has sent you a friend request";
			}
			else if (relationship.Member.Presence != null)
			{
				var presence = relationship.Member.Presence;
				var availability = presence.Availability;

				_onlineIndicator.EnableInClassList(USS_ONLINE_INDICATOR_ONLINE, availability == Availability.Online);

				_activityLabel.SetVisibility(true);
				_activityLabel.text = relationship.Member.Presence.GetActivity<PlayerActivity>().Status;
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
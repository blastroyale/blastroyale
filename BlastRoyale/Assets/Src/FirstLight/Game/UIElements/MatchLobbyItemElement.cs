using System;
using FirstLight.Game.Utils.UCSExtensions;
using Unity.Services.Lobbies.Models;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	public class MatchLobbyItemElement : VisualElement
	{
		private const string USS_BLOCK = "match-lobby-item";
		private const string USS_ACTION_BACKGROUND = USS_BLOCK + "__action-background";

		private readonly Label _lobbyNameLabel;
		private readonly Label _lobbyModeLabel;
		private readonly Label _lobbyPlayersLabel;
		private readonly Label _lobbyStatus;
		private readonly Button _actionButton;

		private Lobby _lobby;
		private Action _onActionClicked;

		public MatchLobbyItemElement()
		{
			AddToClassList(USS_BLOCK);

			var actionBackground = new VisualElement();
			Add(actionBackground);
			actionBackground.AddToClassList(USS_ACTION_BACKGROUND);

			Add(_lobbyNameLabel = new Label("FirstLight's game"));
			Add(_lobbyModeLabel = new Label("2"));
			Add(_lobbyPlayersLabel = new Label("1/30"));
			Add(_lobbyStatus = new Label("IN LOBBY"));
			Add(_actionButton = new Button());
			_actionButton.AddToClassList("button-long");
			_actionButton.text = "JOIN";

			_actionButton.clicked += () => _onActionClicked.Invoke();
		}

		public void SetLobby(Lobby lobby, Action onActionClicked)
		{
			_lobby = lobby;
			_onActionClicked = onActionClicked;

			_lobbyNameLabel.text = lobby.Name;

			var matchSettings = lobby.GetMatchSettings();

			_lobbyModeLabel.text = matchSettings.SquadSize.ToString();
			_lobbyPlayersLabel.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
			_lobbyStatus.text = lobby.IsLocked ? "IN GAME" : "IN LOBBY"; // TODO mihak ?

			_actionButton.text = "#JOIN#";
		}

		public new class UxmlFactory : UxmlFactory<MatchLobbyItemElement>
		{
		}
	}
}
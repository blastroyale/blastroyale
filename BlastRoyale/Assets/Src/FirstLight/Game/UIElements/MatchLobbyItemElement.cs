using System;
using FirstLight.Game.Utils.UCSExtensions;
using I2.Loc;
using Unity.Services.Lobbies.Models;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	public class MatchLobbyItemElement : VisualElement
	{
		private const string USS_BLOCK = "match-lobby-item";
		private const string USS_ACTION_BACKGROUND = USS_BLOCK + "__action-background";
		private const string USS_INFO_BUTTON = USS_BLOCK + "__info-button";
		private const string USS_ACTION_BUTTON = USS_BLOCK + "__action-button";
		private const string USS_NAME = USS_BLOCK + "__name";
		private const string USS_MODE = USS_BLOCK + "__mode";
		private const string USS_PLAYERS = USS_BLOCK + "__players";
		private const string USS_STATUS = USS_BLOCK + "__status";

		private readonly Label _lobbyNameLabel;
		private readonly Label _lobbyModeLabel;
		private readonly Label _lobbyPlayersLabel;
		private readonly LocalizedLabel _lobbyStatus;
		private readonly LocalizedButton _actionButton;
		private readonly ImageButton _infoButton;

		private Lobby _lobby;
		private Action _onActionClicked;

		public MatchLobbyItemElement()
		{
			AddToClassList(USS_BLOCK);

			var actionBackground = new VisualElement();
			Add(actionBackground);
			actionBackground.AddToClassList(USS_ACTION_BACKGROUND);

			Add(_lobbyNameLabel = new Label("FirstLightReallyLongPotato's game"));
			_lobbyNameLabel.AddToClassList(USS_NAME);
			Add(_lobbyModeLabel = new Label("2"));
			_lobbyModeLabel.AddToClassList(USS_MODE);
			Add(_lobbyPlayersLabel = new Label("1/30"));
			_lobbyPlayersLabel.AddToClassList(USS_PLAYERS);
			Add(_lobbyStatus = new LocalizedLabel("IN LOBBY"));
			_lobbyStatus.AddToClassList(USS_STATUS);
			Add(_actionButton = new LocalizedButton(ScriptTerms.UITCustomGames.join));
			_actionButton.AddToClassList("button-long");
			_actionButton.AddToClassList(USS_ACTION_BUTTON);
			_actionButton.clicked += () => _onActionClicked.Invoke();
			
			Add(_infoButton = new ImageButton {name = "info-button"});
			_infoButton.AddToClassList(USS_INFO_BUTTON);
			_infoButton.AddToClassList("sprite-home__button-info");
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
		}

		public new class UxmlFactory : UxmlFactory<MatchLobbyItemElement>
		{
		}
	}
}
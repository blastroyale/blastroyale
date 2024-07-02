using System;
using System.Linq;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using I2.Loc;
using Quantum;
using Unity.Services.Lobbies.Models;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Shows a match lobby item in the match list.
	/// </summary>
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
		private const string USS_FRIEND_ICON = USS_BLOCK + "__friend-icon";
		private const string USS_MUTATORS_COUNT = USS_BLOCK + "__mutators-count";

		private readonly Label _lobbyNameLabel;
		private readonly Label _lobbyModeLabel;
		private readonly Label _lobbyPlayersLabel;
		private readonly LocalizedLabel _lobbyRegion;
		private readonly LocalizedButton _actionButton;
		private readonly ImageButton _infoButton;
		private readonly Label _mutatorsCountLabel;
		private readonly VisualElement _friendIcon;

		private Lobby _lobby;
		private Action _onActionClicked;
		private Action _onInfoClicked;

		public MatchLobbyItemElement()
		{
			AddToClassList(USS_BLOCK);

			var actionBackground = new VisualElement();
			Add(actionBackground);
			actionBackground.AddToClassList(USS_ACTION_BACKGROUND);

			Add(_lobbyNameLabel = new Label("FirstLightReallyLongPotato's game"));
			_lobbyNameLabel.AddToClassList(USS_NAME);
			Add(_lobbyModeLabel = new Label("BattleRoyale\nDUOS"));
			_lobbyModeLabel.AddToClassList(USS_MODE);
			{
				_lobbyModeLabel.Add(_mutatorsCountLabel = new Label("+3") {name = "mutators-count"});
				_mutatorsCountLabel.AddToClassList(USS_MUTATORS_COUNT);
			}
			Add(_lobbyPlayersLabel = new Label("1/30"));
			_lobbyPlayersLabel.AddToClassList(USS_PLAYERS);
			{
				_lobbyPlayersLabel.Add(_friendIcon = new VisualElement() {name = "friend-icon"});
				_friendIcon.AddToClassList(USS_FRIEND_ICON);
			}
			Add(_lobbyRegion = new LocalizedLabel("EU"));
			_lobbyRegion.AddToClassList(USS_STATUS);
			Add(_actionButton = new LocalizedButton(ScriptTerms.UITCustomGames.join));
			_actionButton.AddToClassList("button-long");
			_actionButton.AddToClassList(USS_ACTION_BUTTON);
			_actionButton.clicked += () => _onActionClicked.Invoke();

			Add(_infoButton = new ImageButton {name = "info-button"});
			_infoButton.AddToClassList(USS_INFO_BUTTON);
			_infoButton.AddToClassList("sprite-home__button-info");
			_infoButton.clicked += () => _onInfoClicked.Invoke();
		}

		public void SetLobby(Lobby lobby, Action onActionClicked, Action onInfoClicked)
		{
			_lobby = lobby;
			_onActionClicked = onActionClicked;
			_onInfoClicked = onInfoClicked;

			_lobbyNameLabel.text = lobby.Name;

			var matchSettings = lobby.GetMatchSettings();

			_lobbyModeLabel.text = $"{matchSettings.GameModeID}\n{LocalizationUtils.GetTranslationForTeamSize(matchSettings.SquadSize)}";
			_lobbyPlayersLabel.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
			_lobbyRegion.text = lobby.GetMatchRegion().GetPhotonRegionTranslation();
			_mutatorsCountLabel.SetVisibility(matchSettings.Mutators != Mutator.None);
			_mutatorsCountLabel.text = $"+{matchSettings.Mutators.CountSetFlags()}";
			_friendIcon.SetVisibility(lobby.Players.Any(p => p.IsFriend()));
		}

		public new class UxmlFactory : UxmlFactory<MatchLobbyItemElement>
		{
		}
	}
}
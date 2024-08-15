using System;
using System.Linq;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using I2.Loc;
using Quantum;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Shows a match lobby item in the match list.
	/// </summary>
	public class MatchLobbyItemElement : VisualElement
	{
		private const string USS_BLOCK = "match-lobby-item";
		private const string USS_CONTAINER = USS_BLOCK + "__container";
		private const string USS_INFO_CONTAINER = USS_BLOCK + "__info-container";
		private const string USS_ACTION_CONTAINER = USS_BLOCK + "__action-container";
		private const string USS_ACTION_BACKGROUND = USS_BLOCK + "__action-background";
		private const string USS_INFO_BUTTON = USS_BLOCK + "__info-button";
		private const string USS_ACTION_BUTTON = USS_BLOCK + "__action-button";
		private const string USS_NAME = USS_BLOCK + "__name";
		private const string USS_MODE = USS_BLOCK + "__mode";
		private const string USS_PLAYERS = USS_BLOCK + "__players";
		private const string USS_STATUS = USS_BLOCK + "__region";
		private const string USS_FRIEND_ICON = USS_BLOCK + "__friend-icon";
		private const string USS_MUTATORS_COUNT = USS_BLOCK + "__mutators-count";

		private Label _lobbyNameLabel;
		private Label _lobbyModeLabel;
		private Label _lobbyPlayersLabel;
		private LocalizedLabel _lobbyRegion;
		private LocalizedButton _actionButton;
		private ImageButton _infoButton;
		private Label _mutatorsCountLabel;
		private VisualElement _friendIcon;

		private Lobby _lobby;
		private Action _onActionClicked;
		private Action _onInfoClicked;

		public MatchLobbyItemElement()
		{
			AddToClassList(USS_BLOCK);

			var mainContainer = new VisualElement();
			Add(mainContainer);
			mainContainer.AddToClassList(USS_CONTAINER);
			{
				
				var containerBackground = new VisualElement();
				containerBackground.AddToClassList(USS_ACTION_BACKGROUND);
				mainContainer.Add(containerBackground);
				
				var infoContainer = new VisualElement();
				var actionContainer = new VisualElement();
				
				infoContainer.AddToClassList(USS_INFO_CONTAINER);
				actionContainer.AddToClassList(USS_ACTION_CONTAINER);
				
				SetupInfoContainer(infoContainer);
				SetupActionContainer(actionContainer);
				
				mainContainer.Add(infoContainer);
				mainContainer.Add(actionContainer);

				// container.Add();
				
				// container.Add(_lobbyNameLabel = new Label("FirstLightReallyLongPotato's game"));
				// _lobbyNameLabel.AddToClassList(USS_NAME);
				// container.Add(_lobbyModeLabel = new Label("BattleRoyale\nDUOS"));
				// _lobbyModeLabel.AddToClassList(USS_MODE);
				// {
				// 	_lobbyModeLabel.Add(_mutatorsCountLabel = new Label("+3") {name = "mutators-count"});
				// 	_mutatorsCountLabel.AddToClassList(USS_MUTATORS_COUNT);
				// }
				// container.Add(_lobbyPlayersLabel = new Label("1/30"));
				// _lobbyPlayersLabel.AddToClassList(USS_PLAYERS);
				// {
				// 	_lobbyPlayersLabel.Add(_friendIcon = new VisualElement() {name = "friend-icon"});
				// 	_friendIcon.AddToClassList(USS_FRIEND_ICON);
				// }
				// container.Add(_lobbyRegion = new LocalizedLabel("EU"));
				// _lobbyRegion.AddToClassList(USS_STATUS);
				// container.Add(_actionButton = new LocalizedButton(ScriptTerms.UITCustomGames.join) {name = "ActionButton"});
				// _actionButton.AddToClassList("button-long");
				// _actionButton.AddToClassList(USS_ACTION_BUTTON);
				// _actionButton.clicked += () => _onActionClicked.Invoke();
				//
				// container.Add(_infoButton = new ImageButton {name = "info-button"});
				// _infoButton.AddToClassList(USS_INFO_BUTTON);
				// _infoButton.AddToClassList("sprite-home__button-info");
				// _infoButton.clicked += () => _onInfoClicked.Invoke();
			}
		}

		private void SetupActionContainer(VisualElement actionContainer)
		{
			
			actionContainer.Add(_actionButton = new LocalizedButton(ScriptTerms.UITCustomGames.join) {name = "ActionButton"});
			_actionButton.AddToClassList("button-long");
			_actionButton.AddToClassList(USS_ACTION_BUTTON);
			_actionButton.clicked += () => _onActionClicked.Invoke();

			actionContainer.Add(_infoButton = new ImageButton {name = "info-button"});
			_infoButton.AddToClassList(USS_INFO_BUTTON);
			_infoButton.AddToClassList("sprite-home__button-info");
			_infoButton.clicked += () => _onInfoClicked.Invoke();
		}

		private void SetupInfoContainer(VisualElement infoContainer)
		{
			infoContainer.Add(_lobbyNameLabel = new Label("FirstLightReallyLongPotato's game") {name = "lobby-name"});
			_lobbyNameLabel.AddToClassList(USS_NAME);
				
			infoContainer.Add(_lobbyModeLabel = new Label("BattleRoyale\nDUOS") {name = "lobby-mode"});
			_lobbyModeLabel.AddToClassList(USS_MODE);
			{
				_lobbyModeLabel.Add(_mutatorsCountLabel = new Label("+3") {name = "mutators-count"});
				_mutatorsCountLabel.AddToClassList(USS_MUTATORS_COUNT);
			}
				
			infoContainer.Add(_lobbyPlayersLabel = new Label("1/30"));
			_lobbyPlayersLabel.AddToClassList(USS_PLAYERS);
			{
				_lobbyPlayersLabel.Add(_friendIcon = new VisualElement() {name = "friend-icon"});
				_friendIcon.AddToClassList(USS_FRIEND_ICON);
			}
				
			infoContainer.Add(_lobbyRegion = new LocalizedLabel("EU"));
			_lobbyRegion.AddToClassList(USS_STATUS);
		}

		public void SetLobby(Lobby lobby, Action onActionClicked, Action onInfoClicked)
		{
			_lobby = lobby;
			_onActionClicked = onActionClicked;
			_onInfoClicked = onInfoClicked;

			_lobbyNameLabel.text = lobby.Name;

			var matchSettings = lobby.GetMatchSettings();
			var maxPlayers = lobby.MaxPlayers - GameConstants.Data.MATCH_SPECTATOR_SPOTS;
			var totalAdjustedPlayers = Mathf.Min(lobby.PlayersInGrid().Count, maxPlayers);
			
			_lobbyModeLabel.text = $"{matchSettings.GameModeID}\n{LocalizationUtils.GetTranslationForTeamSize(matchSettings.SquadSize)}";
			_lobbyPlayersLabel.text = $"{totalAdjustedPlayers}/{maxPlayers}";
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
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Services.RoomService;
using FirstLight.Game.Services.Social;
using FirstLight.Game.UIElements;
using FirstLight.Game.UIElements.Kit;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.Game.Views.UITK;
using FirstLight.Modules.UIService.Runtime;
using FirstLight.UIService;
using I2.Loc;
using QuickEye.UIToolkit;
using Sirenix.OdinInspector;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UIElements;
using Player = Unity.Services.Lobbies.Models.Player;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Represents the presenter for the match lobby screen.
	/// </summary>
	public class MatchLobbyScreenPresenter : UIPresenterData<MatchLobbyScreenPresenter.StateData>
	{
		private const int PLAYERS_PER_ROW = 4;
		private const string USS_ROW = "players-container__row";

		private readonly AsyncBufferedQueue _updateBuffer = new (TimeSpan.FromSeconds(0.02), true);
		private LobbyGridData _lastGridSnapshot;

		public class StateData
		{
			public Action BackClicked;
		}

		[Q("Header")] private ScreenHeaderElement _header;
		[Q("SafeArea")] private SafeAreaElement _safeArea;
		[Q("PlayersScrollview")] private ScrollView _playersContainer;
		[Q("CodeLabel")] private Label _codeLabel;
		[Q("LobbyCodeContainer")] private VisualElement _lobbyCodeContainer;
		[Q("CopyCodeButton")] private KitButton _copyCodeButton;
		[Q("ShowCodeButton")] private KitButton _codeVisibilityButton;
		[Q("Tabs")] private VisualElement _tabs;

		[Q("InviteToggle")] private Toggle _inviteToggle;
		[Q("PlayersAmountLabel")] private Label _playersAmount;
		[Q("InviteFriendsButton")] private LocalizedButton _inviteFriendsButton;

		[QView("MatchSettings")] private MatchSettingsView _matchSettingsView;

		private IGameServices _services;

		private bool _joining;
		private bool _localPlayerHost;

		protected override void QueryElements()
		{
			_services = MainInstaller.ResolveServices();

			_header.backClicked = () => LeaveMatchLobby().Forget();
			_codeLabel.text = _services.FLLobbyService.CurrentMatchLobby.LobbyCode;
			_copyCodeButton.clicked += () =>
			{
				UIUtils.SaveToClipboard(_services.FLLobbyService.CurrentMatchLobby.LobbyCode);
				_services.InGameNotificationService.QueueNotification(ScriptLocalization.UITShared.code_copied);
			};
			_inviteFriendsButton.clicked += () => PopupPresenter.OpenInviteFriends().Forget();
			_codeVisibilityButton.clicked += () => SetCodeVisibility(!IsCodeVisible());
			// Adjust the width of the game title based on the width of the code container
			_lobbyCodeContainer.RegisterCallback<GeometryChangedEvent>((evt) => AdjustRemainingWidth());
			_header.Title.RegisterCallback<ClickEvent>(evt => _header.Title.OpenTooltip(Root, _services.FLLobbyService.CurrentMatchLobby.Name, new Vector2(0, 0), TooltipPosition.Bottom));
		}

		private void OnPlayerJoined(List<LobbyPlayerJoined> joiners)
		{
			RefreshData();
		}

		private void OnPlayerLeft(List<int> quitters)
		{
			RefreshData();
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_services.FLLobbyService.CurrentMatchCallbacks.PlayerLeft += OnPlayerLeft;
			_services.FLLobbyService.CurrentMatchCallbacks.PlayerJoined += OnPlayerJoined;
			_services.FLLobbyService.CurrentMatchCallbacks.LocalLobbyUpdated += OnLobbyChanged;
			_services.FLLobbyService.CurrentMatchCallbacks.KickedFromLobby += OnKickedFromLobby;
			var matchLobby = _services.FLLobbyService.CurrentMatchLobby;
			var localPlayer = matchLobby.Players.First(p => p.IsLocal());
			_localPlayerHost = matchLobby.IsLocalPlayerHost();

			_matchSettingsView.SpectatorChanged += async spectating =>
			{
				await _services.FLLobbyService.SetMatchSpectator(spectating);
				RefreshData();
			};

			_matchSettingsView.MatchSettingsChanged += settings =>
			{
				if (!_localPlayerHost) return;
				_services.FLLobbyService.UpdateMatchLobby(settings).Forget();
			};
			_inviteToggle.value = matchLobby.GetMatchSettings().AllowInvites;
			_inviteToggle.RegisterValueChangedCallback((ev) =>
			{
				_matchSettingsView.MatchSettings.AllowInvites = ev.newValue;
				_services.FLLobbyService.UpdateMatchLobby(_matchSettingsView.MatchSettings).Forget();
			});
			SetCodeVisibility(false);
			RefreshData();
			_updateBuffer.Add(CheckJoiningSpectator);
			return base.OnScreenOpen(reload);
		}

		private void AdjustRemainingWidth()
		{
			_header.AdjustLabelWidthConsidering(-100, _lobbyCodeContainer, _tabs);
		}

		protected override UniTask OnScreenClose()
		{
			_services.FLLobbyService.CurrentMatchCallbacks.PlayerLeft -= OnPlayerLeft;
			_services.FLLobbyService.CurrentMatchCallbacks.PlayerJoined -= OnPlayerJoined;
			_services.FLLobbyService.CurrentMatchCallbacks.LocalLobbyUpdated -= OnLobbyChanged;
			_services.FLLobbyService.CurrentMatchCallbacks.KickedFromLobby -= OnKickedFromLobby;
			_services.MessageBrokerService.UnsubscribeAll(this);
			return base.OnScreenClose();
		}

		private void OnLobbyChanged(ILobbyChanges changes)
		{
			if (changes == null) return;

			if (changes.LobbyDeleted)
			{
				if (!_localPlayerHost && !_services.RoomService.InRoom && !_joining)
				{
					_services.InGameNotificationService.QueueNotification("Match lobby was closed by the host.");
					Data?.BackClicked?.Invoke();
				}
			}
			else
			{
				RefreshData();

				FLog.Verbose("Received lobby changes version " + changes.Version.Value + " " + changes.Data.ChangeType);
				// TODO mihak: This should not be here, move when we refac network service
				if ((changes.Data.Changed || changes.Data.Added || changes.Data.Removed) &&
					changes.Data.Value.TryGetValue(FLLobbyService.KEY_LOBBY_MATCH_ROOM_NAME, out var value))
				{
					var joinProperties = new PlayerJoinRoomProperties();

					var squadSize = _services.FLLobbyService.CurrentMatchLobby.GetMatchSettings().SquadSize;
					var localPlayer = _services.FLLobbyService.CurrentMatchLobby.Players.First(p => p.Id == AuthenticationService.Instance.PlayerId);
					var localPlayerPosition = _services.FLLobbyService.CurrentMatchLobby.GetPlayerPosition(localPlayer);

					var isSpectator = localPlayer.IsSpectator() || localPlayerPosition == -1;
					if (!isSpectator)
					{
						joinProperties.Team = Mathf.FloorToInt((float) localPlayerPosition / squadSize).ToString();
						joinProperties.TeamColor = (byte) (localPlayerPosition % squadSize);
					}

					joinProperties.Spectator = isSpectator;
					var room = value.Value.Value;
					JoinRoom(room, joinProperties).Forget();
				}
			}
		}

		private void SetCodeVisibility(bool visible)
		{
			_codeLabel.text = visible ? _services.FLLobbyService.CurrentMatchLobby.LobbyCode : "CODE HIDDEN";
			_codeVisibilityButton.BtnIcon = visible ? Icon.HIDE : Icon.SHOW;
		}

		private bool IsCodeVisible()
		{
			return _codeVisibilityButton.BtnIcon == Icon.HIDE;
		}

		private void OnKickedFromLobby()
		{
			Data.BackClicked();
		}

		private async UniTaskVoid JoinRoom(string room, PlayerJoinRoomProperties playerJoinRoomProperties)
		{
			// Local player will create the room
			if (_services.RoomService.InRoom || _services.FLLobbyService.CurrentMatchLobby.IsLocalPlayerHost()) return;

			FLog.Verbose("Joininig room from Custom Lobby");
			_joining = true;
			_services.MessageBrokerService.Publish(new JoinedCustomMatch());
			await _services.UIService.OpenScreen<LoadingSpinnerScreenPresenter>();
			// TODO mihak: This should not be here, move when we refac network service
			await _services.RoomService.JoinRoomAsync(room, playerJoinRoomProperties);
			_services.MessageBrokerService.UnsubscribeAll(this);
		}

		public async UniTask CheckJoiningSpectator()
		{
			var lobby = _services.FLLobbyService.CurrentMatchLobby;
			var localPlayer = lobby.Players.First(p => p.IsLocal());
			var grid = lobby.GetPlayerGrid();
			if (!localPlayer.IsSpectator() && !lobby.HasRoomInGrid() && grid.GetPosition(localPlayer.Id) == -1)
			{
				FLog.Verbose("Non spectator local player that was not in grid and no room in grid, making spectator");
				await _services.FLLobbyService.SetMatchSpectator(true);
				_matchSettingsView.ToggleSpectatorTab();
			}
		}

		[Button]
		private void RefreshData()
		{
			_updateBuffer.Add(() =>
			{
				var matchLobby = _services.FLLobbyService.CurrentMatchLobby;
				var matchSettings = matchLobby.GetMatchSettings();
				var grid = matchLobby.GetPlayerGrid();

				_localPlayerHost = matchLobby.IsLocalPlayerHost();

				_playersContainer.Clear();

				_header.SetTitle(_services.FLLobbyService.CurrentMatchLobby.Name);

				var canInvite = _localPlayerHost || matchSettings.AllowInvites;
				_inviteToggle.SetDisplay(_localPlayerHost);
				_inviteFriendsButton.SetEnabled(canInvite);
				if (!canInvite)
				{
					_services.UIService.CloseScreen<PopupPresenter>(false);
				}

				if (_localPlayerHost)
				{
					_matchSettingsView.SetMainAction(ScriptTerms.UITCustomGames.start_match, () => StartMatch().Forget());
				}
				else
				{
					var localPlayer = matchLobby.GetPlayerByID(AuthenticationService.Instance.PlayerId);
					_matchSettingsView.SetMainAction(localPlayer.IsReady() ? ScriptTerms.UITHomeScreen.youre_ready : ScriptTerms.UITHomeScreen.ready,
						() => ReadyUp().Forget());
				}

				_matchSettingsView.SetMatchSettings(matchSettings, matchLobby.IsLocalPlayerHost(), true);
				_matchSettingsView.SetSpectators(_services.FLLobbyService.CurrentMatchLobby.Players.Where(p => p.IsSpectator()).ToList());
				//await UpdateSpectators();

				VisualElement row = null;

				var spots = new List<MatchLobbyPlayerElement>();

				// TODO: This only needs to be done when the max of players changes
				for (int i = 0; i < matchLobby.MaxPlayers - GameConstants.Data.MATCH_SPECTATOR_SPOTS; i++)
				{
					if (i % PLAYERS_PER_ROW == 0)
					{
						_playersContainer.Add(row = new VisualElement());
						row.AddToClassList(USS_ROW);
					}

					var link = !_matchSettingsView.MatchSettings.RandomizeTeams &&
						matchSettings.SquadSize > 1 && i % matchSettings.SquadSize < matchSettings.SquadSize - 1;
					var playerElement = new MatchLobbyPlayerElement(null, false, false, link, false);
					var i1 = i;
					playerElement.clicked += () => OnSpotClicked(playerElement, i1);
					row!.Insert(0, playerElement);
					spots.Add(playerElement);
				}

				var orderedPlayers = grid.PositionArray;

				for (var i = 0; i < orderedPlayers.Count; i++)
				{
					var id = orderedPlayers[i];
					if (id == null) continue;

					var player = matchLobby.Players.FirstOrDefault(p => p.Id == id);

					if (player == null) continue;
					if (player.IsSpectator()) continue;

					spots[i].SetData(player.GetPlayerName(),
						player.Id == matchLobby.HostId,
						player.Id == AuthenticationService.Instance.PlayerId,
						player.IsReady());
					spots[i].userData = player;

					if (_lastGridSnapshot != null)
					{
						var lastPosition = _lastGridSnapshot.GetPosition(player.Id);
						if (lastPosition != i)
						{
							spots[i].AnimatePing(1.1f);
						}
					}
				}

				_lastGridSnapshot = grid;
				_playersAmount.text = $"{matchLobby.PlayersInGrid().Count}/{matchLobby.MaxPlayers - GameConstants.Data.MATCH_SPECTATOR_SPOTS}";
				return UniTask.CompletedTask;
			});
		}

		private async UniTaskVoid StartMatch()
		{
			var noBotsOnePlayer = _matchSettingsView.MatchSettings.BotDifficulty <= 0 &&
				_services.FLLobbyService.CurrentMatchLobby.NonSpectators().Count == 1;
			var noPlayers = _services.FLLobbyService.CurrentMatchLobby.NonSpectators().Count < 1;
			if (noBotsOnePlayer || noPlayers)
			{
				PopupPresenter.OpenGenericInfo(ScriptTerms.UITCustomGames.custom_game, ScriptLocalization.UITCustomGames.no_players_bots).Forget();
				return;
			}

			var matchLobby = _services.FLLobbyService.CurrentMatchLobby;
			var matchGrid = matchLobby.GetPlayerGrid();

			foreach (var p in _services.FLLobbyService.CurrentMatchLobby.Players)
			{
				if (!p.IsLocal() && !(p.IsSpectator() || matchGrid.GetPosition(p.Id) == -1) && !p.IsReady())
				{
					_services.InGameNotificationService.QueueNotification("Not all players are ready");
					return;
				}
			}

			// TODO: user flow should not be handled here but in state machines
			await _services.UIService.OpenScreen<LoadingSpinnerScreenPresenter>();

			_services.MessageBrokerService.Publish(new StartedCustomMatch()
			{
				Settings = _matchSettingsView.MatchSettings
			});
		}

		private async UniTaskVoid ReadyUp()
		{
			await _services.FLLobbyService.ToggleMatchReady();
		}

		private async UniTaskVoid LeaveMatchLobby()
		{
			await _services.UIService.OpenScreen<LoadingSpinnerScreenPresenter>();
			_services.MessageBrokerService.UnsubscribeAll(this);
			await _services.FLLobbyService.LeaveMatch();
			FLog.Info("Left match");
		}

		private void OnSpotClicked(VisualElement source, int index)
		{
			var localPlayer = _services.FLLobbyService.CurrentMatchLobby.LocalPlayer();

			if (source.userData is not Player player)
			{
				if (!_matchSettingsView.MatchSettings.RandomizeTeams)
				{
					_services.FLLobbyService.SetMatchPositionRequest(index);
				}

				return;
			}

			if (player.IsLocal())
			{
				source.OpenTooltip(Root, ScriptLocalization.UITCustomGames.local_player_tooltip);
				return;
			}

			var buttons = new List<PlayerContextButton>();
			if (!player.IsReady() && !player.IsLocal() && !_matchSettingsView.MatchSettings.RandomizeTeams)
			{
				buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Normal, ScriptLocalization.UITCustomGames.option_swap,
					() => _services.FLLobbyService.SetMatchPositionRequest(index)));
			}

			if (_services.FLLobbyService.CurrentMatchLobby.IsLocalPlayerHost())
			{
				buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Gold, ScriptLocalization.UITCustomGames.option_promote,
					() => PromotePlayerToHost(player.Id).Forget()));
				buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Red, ScriptLocalization.UITCustomGames.option_kick,
					() => KickPlayer(player.Id).Forget()));
			}

			_services.GameSocialService.OpenPlayerOptions(source, Root, player.Id, player.GetPlayerName(), new PlayerContextSettings()
			{
				ExtraButtons = buttons,
			});
		}

		private async UniTaskVoid KickPlayer(string playerID)
		{
			if (await _services.FLLobbyService.KickPlayerFromMatch(playerID))
			{
				RefreshData();
			}
		}

		private async UniTaskVoid PromotePlayerToHost(string playerID)
		{
			if (await _services.FLLobbyService.UpdateMatchHost(playerID))
			{
				RefreshData();
			}
		}
	}
}
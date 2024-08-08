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

		private readonly BufferedQueue _updateBuffer = new (TimeSpan.FromSeconds(0.01), true);
		private LobbyGridData _lastGridSnapshot;

		public class StateData
		{
			public Action BackClicked;
		}

		[Q("Header")] private ScreenHeaderElement _header;
		[Q("PlayersScrollview")] private ScrollView _playersContainer;
		[Q("LobbyCode")] private LocalizedTextField _lobbyCode;
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

			_lobbyCode.value = _services.FLLobbyService.CurrentMatchLobby.LobbyCode;
			_inviteFriendsButton.clicked += () => PopupPresenter.OpenInviteFriends().Forget();
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_services.FLLobbyService.CurrentMatchCallbacks.LocalLobbyUpdated += OnLobbyChanged;
			_services.FLLobbyService.CurrentMatchCallbacks.KickedFromLobby += OnKickedFromLobby;
			var matchLobby = _services.FLLobbyService.CurrentMatchLobby;
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

			RefreshData();

			return base.OnScreenOpen(reload);
		}

		protected override UniTask OnScreenClose()
		{
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
					_services.NotificationService.QueueNotification("Match lobby was closed by the host.");
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

					joinProperties.Team = Mathf.FloorToInt((float) localPlayerPosition / squadSize).ToString();
					joinProperties.TeamColor = (byte) (localPlayerPosition % squadSize);
					joinProperties.Spectator = localPlayer.IsSpectator() || localPlayerPosition < 0;

					var room = value.Value.Value;
					JoinRoom(room, joinProperties).Forget();
				}
			}
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
				_matchSettingsView.SetSpectators(matchLobby.Players.Where(p => p.IsSpectator()));

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
				_playersAmount.text = $"{matchLobby.Players.Count}/{matchLobby.MaxPlayers - GameConstants.Data.MATCH_SPECTATOR_SPOTS}";
			});
		}

		private async UniTaskVoid StartMatch()
		{
			if (_matchSettingsView.MatchSettings.BotDifficulty <= 0 && _services.FLLobbyService.CurrentMatchLobby.Players.Count <= 1)
			{
				PopupPresenter.OpenGenericInfo(ScriptTerms.UITCustomGames.custom_game, ScriptLocalization.UITCustomGames.no_players_bots).Forget();
				return;
			}

			foreach (var p in _services.FLLobbyService.CurrentMatchLobby.Players)
			{
				if (!p.IsLocal() && !p.IsSpectator() && !p.IsReady())
				{
					_services.NotificationService.QueueNotification("Not all players are ready");
					return;
				}
			}

			await _services.UIService.OpenScreen<LoadingSpinnerScreenPresenter>();
			var matchSettings = _matchSettingsView.MatchSettings;
			var matchLobby = _services.FLLobbyService.CurrentMatchLobby;
			var matchGrid = matchLobby.GetPlayerGrid();
			matchGrid.ShuffleStack();

			_services.MessageBrokerService.Publish(new JoinedCustomMatch());

			await _services.FLLobbyService.UpdateMatchLobby(matchSettings, matchGrid, true);

			matchSettings = _matchSettingsView.MatchSettings;

			// TODO: remove the hack
			((IInternalGameNetworkService) _services.NetworkService).JoinSource.Value = JoinRoomSource.FirstJoin;

			var setup = new MatchRoomSetup
			{
				SimulationConfig = matchSettings.ToSimulationMatchConfig(),
				RoomIdentifier = _services.FLLobbyService.CurrentMatchLobby.Id,
			};

			var squadSize = matchSettings.SquadSize;
			var localPlayer = matchLobby.Players.First(p => p.Id == AuthenticationService.Instance.PlayerId);
			var localPlayerPosition = matchLobby.GetPlayerPosition(localPlayer);

			try
			{
				await _services.RoomService.CreateRoomAsync(setup, new PlayerJoinRoomProperties()
				{
					TeamColor = (byte) (localPlayerPosition % squadSize),
					Team = Mathf.FloorToInt((float) localPlayerPosition / squadSize).ToString(),
					Spectator = localPlayer.IsSpectator()
				});

				var started = await UniTaskUtils.WaitUntilTimeout(CanStartGame, TimeSpan.FromSeconds(5));
				if (!started)
				{
					_services.NotificationService.QueueNotification("Error starting match");
					return;
				}

				await _services.FLLobbyService.SetMatchRoom(setup.RoomIdentifier);
				await _services.FLLobbyService.LeaveMatch();
			}
			catch (Exception e)
			{
				FLog.Error("Could not create quantum room", e);
				LeaveMatchLobby().Forget();
			}
		}

		private bool CanStartGame()
		{
			return _services.RoomService.InRoom;
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
			await _services.UIService.CloseScreen<LoadingSpinnerScreenPresenter>();
		}

		private void OnSpotClicked(VisualElement source, int index)
		{
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
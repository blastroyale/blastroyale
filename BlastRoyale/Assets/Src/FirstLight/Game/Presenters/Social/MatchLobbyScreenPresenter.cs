using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Services.RoomService;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.Game.Views.UITK;
using FirstLight.Server.SDK.Modules;
using FirstLight.UIService;
using I2.Loc;
using QuickEye.UIToolkit;
using Unity.Services.Authentication;
using Unity.Services.Friends;
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

		public class StateData
		{
			public MatchListScreenPresenter.StateData MatchListStateData; // Ugly but I don't want to refac state machines
		}

		[Q("Header")] private ScreenHeaderElement _header;
		[Q("PlayersScrollview")] private ScrollView _playersContainer;
		[Q("LobbyCode")] private LocalizedTextField _lobbyCode;
		[Q("MatchSettings")] private VisualElement _matchSettings;
		[Q("PlayersAmountLabel")] private Label _playersAmount;
		[Q("InviteFriendsButton")] private LocalizedButton _inviteFriendsButton;

		private IGameServices _services;
		private MatchSettingsView _matchSettingsView;

		private bool _localPlayerHost;

		protected override void QueryElements()
		{
			_services = MainInstaller.ResolveServices();

			_header.backClicked += () => LeaveMatchLobby().Forget();

			_matchSettings.Required().AttachView(this, out _matchSettingsView);

			_lobbyCode.value = _services.FLLobbyService.CurrentMatchLobby.LobbyCode;
			_inviteFriendsButton.clicked += () => PopupPresenter.OpenInviteFriends();
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_services.MessageBrokerService.Subscribe<MatchLobbyUpdatedMessage>(OnLobbyChanged);
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
			_services.FLLobbyService.CurrentMatchCallbacks.KickedFromLobby -= OnKickedFromLobby;
			_services.MessageBrokerService.UnsubscribeAll(this);
			return base.OnScreenClose();
		}

		private void OnLobbyChanged(MatchLobbyUpdatedMessage m)
		{
			if (m.Changes.LobbyDeleted)
			{
				if (!_localPlayerHost)
				{
					_services.NotificationService.QueueNotification("Match lobby was closed by the host.");
				}

				_services.UIService.OpenScreen<MatchListScreenPresenter>(Data.MatchListStateData).Forget();
			}
			else
			{
				var changes = m.Changes;
				RefreshData();

				// TODO mihak: This should not be here, move when we refac network service
				if ((changes.Data.Changed || changes.Data.Added || changes.Data.Removed) &&
					changes.Data.Value.TryGetValue(FLLobbyService.KEY_MATCH_ROOM_NAME, out var value))
				{
					var joinProperties = new PlayerJoinRoomProperties();
					if (changes.Data.Value.TryGetValue(FLLobbyService.KEY_OVERWRITE_TEAMS, out var teamsSerialized)
						&& !string.IsNullOrEmpty(teamsSerialized.Value.Value))
					{
						var dict = ModelSerializer.Deserialize<Dictionary<string, string>>(teamsSerialized.Value.Value);
						if (dict.TryGetValue(AuthenticationService.Instance.PlayerId, out var teamValue))
						{
							joinProperties.Team = teamValue;
						}
					}

					if (changes.Data.Value.TryGetValue(FLLobbyService.KEY_OVERWRITE_COLORS, out var colorsSerialized)
						&& !string.IsNullOrEmpty(colorsSerialized.Value.Value))
					{
						var dict = ModelSerializer.Deserialize<Dictionary<string, string>>(colorsSerialized.Value.Value);
						if (dict.TryGetValue(AuthenticationService.Instance.PlayerId, out var colorValue))
						{
							joinProperties.TeamColor = byte.Parse(colorValue);
						}
					}

					var room = value.Value.Value;
					JoinRoom(room, joinProperties).Forget();
				}
			}
		}

		private void OnKickedFromLobby()
		{
			_services.UIService.OpenScreen<MatchListScreenPresenter>(Data.MatchListStateData).Forget();
		}

		private async UniTaskVoid JoinRoom(string room, PlayerJoinRoomProperties playerJoinRoomProperties)
		{
			await _services.UIService.OpenScreen<LoadingSpinnerScreenPresenter>();
			// TODO mihak: This should not be here, move when we refac network service
			await _services.RoomService.JoinRoomAsync(room, playerJoinRoomProperties);
			Data.MatchListStateData.PlayClicked();
		}

		private void RefreshData()
		{
			var matchLobby = _services.FLLobbyService.CurrentMatchLobby;
			var spectators = new List<Player>();

			_localPlayerHost = matchLobby.IsLocalPlayerHost();
			_playersContainer.Clear();

			_header.SetTitle(_services.FLLobbyService.CurrentMatchLobby.Name);

			VisualElement row = null;
			for (var i = 0; i < matchLobby.MaxPlayers; i++)
			{
				if (i % PLAYERS_PER_ROW == 0)
				{
					_playersContainer.Add(row = new VisualElement());
					row.AddToClassList(USS_ROW);
				}

				if (i < matchLobby.Players.Count)
				{
					var player = matchLobby.Players[i];

					if (player.IsSpectator())
					{
						spectators.Add(player);
						row!.Add(new MatchLobbyPlayerElement(null, false, false));
						continue;
					}

					var isHost = player.Id == _services.FLLobbyService.CurrentMatchLobby.HostId;
					var isLocal = player.Id == AuthenticationService.Instance.PlayerId;
					var playerElement = new MatchLobbyPlayerElement(player.GetPlayerName(), isHost, isLocal);

					row!.Add(playerElement);

					if (!isLocal)
					{
						playerElement.userData = player;
						playerElement.clicked += () => OnPlayerClicked(playerElement);
					}
				}
				else
				{
					row!.Add(new MatchLobbyPlayerElement(null, false, false));
				}
			}

			_matchSettingsView.SetMainAction(_localPlayerHost ? ScriptTerms.UITCustomGames.start_match : null, () => StartMatch().Forget());
			_matchSettingsView.SetMatchSettings(matchLobby.GetMatchSettings(), matchLobby.IsLocalPlayerHost(), true);
			_matchSettingsView.SetSpectators(spectators);
			_playersAmount.text = $"{matchLobby.Players.Count}/{matchLobby.MaxPlayers}";
		}

		private async UniTaskVoid StartMatch()
		{
			await _services.UIService.OpenScreen<LoadingSpinnerScreenPresenter>();
			var matchSettings = _matchSettingsView.MatchSettings;
			var matchLobby = _services.FLLobbyService.CurrentMatchLobby;

			await _services.FLLobbyService.UpdateMatchLobby(matchSettings, true);

			((IInternalGameNetworkService) _services.NetworkService).JoinSource.Value = JoinRoomSource.FirstJoin;

			var setup = new MatchRoomSetup
			{
				SimulationConfig = matchSettings.ToSimulationMatchConfig(),
				RoomIdentifier = _services.FLLobbyService.CurrentMatchLobby.Id,
			};
			// TODO: ADD AUTO BALANCE OFF
			var teams = new Dictionary<string, string>();
			teams = _services.TeamService.AutomaticDistributeTeams(matchLobby.Players.Select(p => p.Id), matchSettings.SquadSize);
			var colors = _services.TeamService.DistributeColors(teams);
			try
			{
				var localId = AuthenticationService.Instance.PlayerId;
				await _services.RoomService.CreateRoomAsync(setup, new PlayerJoinRoomProperties()
				{
					TeamColor = Byte.Parse(colors[localId]),
					Team = teams[localId]
				});
				Data.MatchListStateData.PlayClicked();
				var teamsSerialized = ModelSerializer.Serialize(teams).Value;
				var colorsSerialized = ModelSerializer.Serialize(colors).Value;
				await _services.FLLobbyService.SetMatchRoom(setup.RoomIdentifier, teamsSerialized, colorsSerialized);
				await _services.FLLobbyService.LeaveMatch();
			}
			catch (Exception e)
			{
				FLog.Error("Could not create quantum room", e);
				LeaveMatchLobby().Forget();
			}
		}

		private async UniTaskVoid LeaveMatchLobby()
		{
			await _services.UIService.OpenScreen<LoadingSpinnerScreenPresenter>();

			_services.MessageBrokerService.UnsubscribeAll(this);

			await _services.FLLobbyService.LeaveMatch();
			await _services.UIService.OpenScreen<MatchListScreenPresenter>(Data.MatchListStateData);
			await _services.UIService.CloseScreen<LoadingSpinnerScreenPresenter>();
		}

		private void OnPlayerClicked(VisualElement source)
		{
			var player = (Player) source.userData;
			var isFriend = FriendsService.Instance.GetFriendByID(player.Id) != null;

			var buttons = new List<PlayerContextButton>
			{
				new (PlayerButtonContextStyle.Normal, "Open Profile", () =>
				{
					_services.UIService.OpenScreen<PlayerStatisticsPopupPresenter>(new PlayerStatisticsPopupPresenter.StateData
					{
						UnityID = player.Id
					}).Forget();
				})
			};

			if (!isFriend) // TODO mihak: Also check if the friend request is pending
			{
				buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Normal, "Send friend request",
					() => FriendsService.Instance.AddFriendHandled(player.Id).Forget()));
			}

			if (_services.FLLobbyService.CurrentMatchLobby.IsLocalPlayerHost())
			{
				buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Normal, "Promote to room owner",
					() => PromotePlayerToHost(player.Id).Forget()));
				buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Red, "Kick",
					() => KickPlayer(player.Id).Forget()));
			}

			TooltipUtils.OpenPlayerContextOptions(source, Root, player.GetPlayerName(), buttons);
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
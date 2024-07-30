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
using Sirenix.OdinInspector;
using Unity.Services.Authentication;
using Unity.Services.Friends;
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

		public class StateData
		{
			public Action BackClicked;
		}

		[Q("Header")] private ScreenHeaderElement _header;
		[Q("PlayersScrollview")] private ScrollView _playersContainer;
		[Q("LobbyCode")] private LocalizedTextField _lobbyCode;
		[Q("MatchSettings")] private VisualElement _matchSettings;
		[Q("PlayersAmountLabel")] private Label _playersAmount;
		[Q("InviteFriendsButton")] private LocalizedButton _inviteFriendsButton;

		private IGameServices _services;
		private MatchSettingsView _matchSettingsView;

		private bool _joining;
		private bool _localPlayerHost;

		protected override void QueryElements()
		{
			_services = MainInstaller.ResolveServices();

			_header.backClicked = () => LeaveMatchLobby().Forget();

			_matchSettings.Required().AttachView(this, out _matchSettingsView);

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
			var matchLobby = _services.FLLobbyService.CurrentMatchLobby;
			var spectators = new List<Player>();

			_localPlayerHost = matchLobby.IsLocalPlayerHost();
			_playersContainer.Clear();

			_header.SetTitle(_services.FLLobbyService.CurrentMatchLobby.Name);

			var matchSettings = matchLobby.GetMatchSettings();
			_matchSettingsView.SetMainAction(_localPlayerHost ? ScriptTerms.UITCustomGames.start_match : null, () => StartMatch().Forget());
			_matchSettingsView.SetMatchSettings(matchSettings, matchLobby.IsLocalPlayerHost(), true);
			_matchSettingsView.SetSpectators(spectators);

			VisualElement row = null;

			var spots = new List<MatchLobbyPlayerElement>();

			for (int i = 0; i < matchLobby.MaxPlayers; i++)
			{
				if (i % PLAYERS_PER_ROW == 0)
				{
					_playersContainer.Add(row = new VisualElement());
					row.AddToClassList(USS_ROW);
				}

				var link = matchSettings.SquadSize > 1 && i % matchSettings.SquadSize < matchSettings.SquadSize - 1;
				var playerElement = new MatchLobbyPlayerElement(null, false, false, link);
				var i1 = i;
				playerElement.clicked += () => OnSpotClicked(playerElement, i1);
				row!.Insert(0, playerElement);
				spots.Add(playerElement);
			}

			var orderedPlayers = matchLobby.GetPlayerPositions();

			for (var i = 0; i < orderedPlayers.Length; i++)
			{
				var id = orderedPlayers[i];
				if (id == null) continue;

				var player = matchLobby.Players.FirstOrDefault(p => p.Id == id);

				if (player == null) continue;

				spots[i].SetData(player.GetPlayerName(), player.Id == matchLobby.HostId, player.Id == AuthenticationService.Instance.PlayerId);
				spots[i].userData = player;
			}

			_playersAmount.text = $"{matchLobby.Players.Count}/{matchLobby.MaxPlayers}";
		}

		private async UniTaskVoid StartMatch()
		{
			await _services.UIService.OpenScreen<LoadingSpinnerScreenPresenter>();
			var matchSettings = _matchSettingsView.MatchSettings;
			var matchLobby = _services.FLLobbyService.CurrentMatchLobby;
			_services.MessageBrokerService.Publish(new JoinedCustomMatch());

			await _services.FLLobbyService.UpdateMatchLobby(matchSettings, true);

			// TODO: remove the hack
			((IInternalGameNetworkService) _services.NetworkService).JoinSource.Value = JoinRoomSource.FirstJoin;

			var setup = new MatchRoomSetup
			{
				SimulationConfig = matchSettings.ToSimulationMatchConfig(),
				RoomIdentifier = _services.FLLobbyService.CurrentMatchLobby.Id,
			};

			var squadSize = _services.FLLobbyService.CurrentMatchLobby.GetMatchSettings().SquadSize;
			var localPlayer = _services.FLLobbyService.CurrentMatchLobby.Players.First(p => p.Id == AuthenticationService.Instance.PlayerId);
			var localPlayerPosition = _services.FLLobbyService.CurrentMatchLobby.GetPlayerPosition(localPlayer);

			try
			{
				await _services.RoomService.CreateRoomAsync(setup, new PlayerJoinRoomProperties()
				{
					TeamColor = (byte) (localPlayerPosition % squadSize),
					Team = Mathf.FloorToInt((float) localPlayerPosition / squadSize).ToString()
				});

				await _services.FLLobbyService.SetMatchRoom(setup.RoomIdentifier);
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
			await _services.UIService.CloseScreen<LoadingSpinnerScreenPresenter>();
		}

		private void OnSpotClicked(VisualElement source, int index)
		{
			if (source.userData is not Player player)
			{
				_services.FLLobbyService.SetMatchPositionRequest(index).Forget();
				return;
			}

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

			if (_services.GameSocialService.CanAddFriend(player))
			{
				buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Normal, "Send friend request",
					() => FriendsService.Instance.AddFriendHandled(player.Id).Forget()));
			}

			buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Normal, "Swap Places",
				() => _services.FLLobbyService.SetMatchPositionRequest(index).Forget()));

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
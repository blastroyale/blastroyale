using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.Game.Views.UITK;
using FirstLight.UIService;
using I2.Loc;
using QuickEye.UIToolkit;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Lobbies;
using UnityEngine.UIElements;
using Player = Unity.Services.Lobbies.Models.Player;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Represents the presenter for the match lobby screen.
	/// </summary>
	public class MatchLobbyScreenPresenter : UIPresenterData<MatchLobbyScreenPresenter.StateData>
	{
		public class StateData
		{
			public MatchListScreenPresenter.StateData MatchListStateData; // Ugly but I don't want to refac state machines
		}

		[Q("Header")] private ScreenHeaderElement _header;
		[Q("PlayersContainer")] private VisualElement _playersContainer;
		[Q("LobbyCode")] private LocalizedTextField _lobbyCode;
		[Q("MatchSettings")] private VisualElement _matchSettings;

		private IGameServices _services;
		private MatchSettingsView _matchSettingsView;

		protected override void QueryElements()
		{
			_services = MainInstaller.ResolveServices();

			var header = _header.Required();
			header.SetTitle(_services.FLLobbyService.CurrentMatchLobby.Name);
			header.backClicked += () => LeaveMatchLobby().Forget();

			_matchSettings.Required().AttachView(this, out _matchSettingsView);

			_lobbyCode.value = _services.FLLobbyService.CurrentMatchLobby.LobbyCode;
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_services.FLLobbyService.CurrentMatchCallbacks.LobbyChanged += OnLobbyChanged;
			var matchLobby = _services.FLLobbyService.CurrentMatchLobby;
			var playerIsHost = matchLobby.IsLocalPlayerHost();

			_matchSettingsView.SetMainAction(playerIsHost ? ScriptTerms.UITCustomGames.start_match : null, () => StartMatch().Forget());

			if (playerIsHost)
			{
				_matchSettingsView.MatchSettingsChanged += settings =>
				{
					_services.FLLobbyService.UpdateMatchLobby(settings).Forget();
				};
			}

			RefreshData();

			return base.OnScreenOpen(reload);
		}

		private void OnLobbyChanged(ILobbyChanges changes)
		{
			if (changes.LobbyDeleted)
			{
				_services.UIService.OpenScreen<MatchListScreenPresenter>(Data.MatchListStateData).Forget();
			}
			else
			{
				RefreshData();
			}
		}

		private void RefreshData()
		{
			// TODO: Do this more cleverly maybe
			var matchLobby = _services.FLLobbyService.CurrentMatchLobby;

			_playersContainer.Clear();

			for (var i = 0; i < matchLobby.MaxPlayers; i++)
			{
				if (i < matchLobby.Players.Count)
				{
					var player = matchLobby.Players[i];
					var isHost = player.Id == _services.FLLobbyService.CurrentMatchLobby.HostId;
					var isLocal = player.Id == AuthenticationService.Instance.PlayerId;
					var playerElement = new MatchLobbyPlayerElement(player.GetPlayerName(), isHost, isLocal);

					_playersContainer.Add(playerElement);

					if (!isLocal)
					{
						playerElement.userData = player;
						playerElement.clicked += () => OnPlayerClicked(playerElement);
					}
				}
				else
				{
					_playersContainer.Add(new MatchLobbyPlayerElement(null, false, false));
				}
			}

			_matchSettingsView.SetMatchSettings(matchLobby.GetMatchSettings(), matchLobby.IsLocalPlayerHost());
		}

		private async UniTaskVoid StartMatch()
		{
			var matchSettings = _matchSettingsView.MatchSettings;

			await _services.FLLobbyService.UpdateMatchLobby(matchSettings, true);

			// TODO start match
		}

		private async UniTaskVoid LeaveMatchLobby()
		{
			_services.FLLobbyService.CurrentMatchCallbacks.LobbyChanged -= OnLobbyChanged;

			await _services.FLLobbyService.LeaveMatch();
			await _services.UIService.OpenScreen<MatchListScreenPresenter>(Data.MatchListStateData);
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

			TooltipUtils.OpenPlayerContextOptions(source, Root, player.GetPlayerName(), buttons, TipDirection.TopLeft, TooltipPosition.BottomRight);
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
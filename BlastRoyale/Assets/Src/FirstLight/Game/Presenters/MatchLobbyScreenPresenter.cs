using Cysharp.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.Game.Views.UITK;
using FirstLight.UIService;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using UnityEngine.UIElements;
using Player = Unity.Services.Lobbies.Models.Player;

namespace FirstLight.Game.Presenters
{
	public class MatchLobbyScreenPresenter : UIPresenterData<MatchLobbyScreenPresenter.StateData>
	{
		public class StateData
		{
			public MatchListScreenPresenter.StateData MatchListStateData; // Ugly but I don't want to refac state machines
		}

		private IGameServices _services;

		private VisualElement _playersContainer;
		private VisualElement _matchSettings;

		private MatchSettingsView _matchSettingsView;

		protected override void QueryElements()
		{
			_services = MainInstaller.ResolveServices();

			var header = Root.Q<ScreenHeaderElement>("Header").Required();
			header.SetTitle(_services.FLLobbyService.CurrentMatchLobby.Name);
			header.backClicked += () => LeaveMatchLobby().Forget();

			_playersContainer = Root.Q("PlayersContainer").Required();
			_matchSettings = Root.Q("MatchSettings").Required().AttachView(this, out _matchSettingsView);
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_services.FLLobbyService.CurrentMatchCallbacks.LobbyChanged += OnLobbyChanged;
			var matchLobby = _services.FLLobbyService.CurrentMatchLobby;
			var playerIsHost = matchLobby.IsLocalPlayerHost();

			_matchSettingsView.SetMainAction(playerIsHost ? "#START MATCH#" : null, () => StartMatch().Forget());

			if (playerIsHost)
			{
				_matchSettingsView.MatchSettingsChanged += settings =>
				{
					_services.FLLobbyService.UpdateMatchSettings(settings).Forget();
				};
			}

			RefreshData();

			return base.OnScreenOpen(reload);
		}

		private void OnLobbyChanged(ILobbyChanges obj)
		{
			RefreshData();
		}

		private void RefreshData()
		{
			// TODO: Do this more cleverly
			var matchLobby = _services.FLLobbyService.CurrentMatchLobby;

			_playersContainer.Clear();
			foreach (var player in matchLobby.Players)
			{
				_playersContainer.Add(CreatePlayerElement(player));
			}

			_matchSettingsView.SetMatchSettings(matchLobby.GetMatchSettings(), matchLobby.IsLocalPlayerHost());
		}

		private async UniTaskVoid StartMatch()
		{
			var matchSettings = _matchSettingsView.MatchSettings;

			// TODO
			await UniTask.NextFrame();
		}

		private async UniTaskVoid LeaveMatchLobby()
		{
			_services.FLLobbyService.CurrentMatchCallbacks.LobbyChanged -= OnLobbyChanged;

			await _services.FLLobbyService.LeaveMatch();
			await _services.UIService.OpenScreen<MatchListScreenPresenter>(Data.MatchListStateData);
		}

		private VisualElement CreatePlayerElement(Player player)
		{
			var playerElement = new Label(player.GetPlayerName());
			playerElement.AddToClassList("player");

			// Host
			if (player.Id == _services.FLLobbyService.CurrentMatchLobby.HostId)
			{
				playerElement.AddToClassList("player--host");
				var crown = new VisualElement();
				playerElement.Add(crown);
				crown.AddToClassList("player__crown");
			}

			// Local player
			if (player.Id == AuthenticationService.Instance.PlayerId)
			{
				playerElement.AddToClassList("player--local");
			}

			return playerElement;
		}
	}
}
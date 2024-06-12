using Cysharp.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.Game.Views.UITK;
using FirstLight.UIService;
using Unity.Services.Lobbies;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	public class MatchLobbyScreenPresenter : UIPresenter
	{
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
				var playerLabel = new Label(player.GetPlayerName());
				playerLabel.AddToClassList("player");
				_playersContainer.Add(playerLabel);
			}

			_matchSettingsView.SetMatchSettings(matchLobby.GetMatchSettings(), matchLobby.IsLocalPlayerHost());
		}

		private async UniTaskVoid StartMatch()
		{
			// TODO
			await UniTask.NextFrame();
		}

		private async UniTaskVoid LeaveMatchLobby()
		{
			await _services.FLLobbyService.LeaveMatch();
			await _services.UIService.OpenScreen<MatchListScreenPresenter>();
		}
	}
}
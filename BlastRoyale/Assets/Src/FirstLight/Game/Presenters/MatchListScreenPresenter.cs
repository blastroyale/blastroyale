using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.UITK;
using FirstLight.UIService;
using Unity.Services.Lobbies.Models;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	public class MatchListScreenPresenter : UIPresenter
	{
		private ListView _gamesList;

		private MatchSettingsView _matchSettingsView;

		private IGameServices _services;

		private List<Lobby> _lobbies;

		protected override void QueryElements()
		{
			_services = MainInstaller.ResolveServices();

			var header = Root.Q<ScreenHeaderElement>("Header").Required();
			header.SetTitle("#Browse games#");

			Root.Q("MatchSettings").Required().AttachView(this, out _matchSettingsView);
			_gamesList = Root.Q<ListView>("GamesList").Required();
			_gamesList.bindItem = BindMatchLobbyItem;
			_gamesList.makeItem = MakeMatchLobbyItem;

			Root.Q<Button>("JoinWithCodeButton").clicked += OnJoinWithCodeClicked;
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			RefreshLobbies().Forget();
			
			_matchSettingsView.SetMatchSettings(_services.LocalPrefsService.LastCustomMatchSettings, true);
			_matchSettingsView.SetMainAction("#CREATE MATCH#", () => CreateMatch(_matchSettingsView.MatchSettings).Forget());

			return base.OnScreenOpen(reload);
		}

		private async UniTask RefreshLobbies()
		{
			_lobbies = await _services.FLLobbyService.GetPublicMatches();
			if (gameObject == null) return;
			_gamesList.itemsSource = _lobbies;
			_gamesList.RefreshItems();
		}

		private void OnJoinWithCodeClicked()
		{
			FLog.Info("Join with code clicked");
			// TODO mihak: Open popup
		}

		private async UniTaskVoid JoinMatch(Lobby lobby)
		{
			await _services.FLLobbyService.JoinMatch(lobby.Id);
			await _services.UIService.OpenScreen<MatchLobbyScreenPresenter>();
		}

		private async UniTaskVoid CreateMatch(CustomMatchSettings matchSettings)
		{
			await _services.FLLobbyService.CreateMatch(matchSettings);
			await _services.UIService.OpenScreen<MatchLobbyScreenPresenter>();
		}

		private void BindMatchLobbyItem(VisualElement e, int index)
		{
			var lobby = _lobbies[index];
			((MatchLobbyItemElement) e).SetLobby(lobby, () => JoinMatch(lobby).Forget());
		}

		private static VisualElement MakeMatchLobbyItem()
		{
			return new MatchLobbyItemElement();
		}
	}
}
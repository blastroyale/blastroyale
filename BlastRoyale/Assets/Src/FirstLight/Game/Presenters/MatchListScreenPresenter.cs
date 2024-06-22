using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.UITK;
using FirstLight.UIService;
using I2.Loc;
using Unity.Services.Lobbies.Models;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	public class MatchListScreenPresenter : UIPresenterData<MatchListScreenPresenter.StateData>
	{
		public class StateData
		{
			public Action BackClicked; // Fucking disgusting
		}

		private VisualElement _listHeaders;
		private LocalizedLabel _noLobbiesLabel;
		private DotsLoadingElement _loader;
		private ListView _gamesList;
		private Button _refreshButton;

		private MatchSettingsView _matchSettingsView;

		private IGameServices _services;

		private List<Lobby> _lobbies;

		protected override void QueryElements()
		{
			_services = MainInstaller.ResolveServices();

			var header = Root.Q<ScreenHeaderElement>("Header").Required();
			header.backClicked += Data.BackClicked;

			Root.Q("MatchSettings").Required().AttachView(this, out _matchSettingsView);
			_gamesList = Root.Q<ListView>("GamesList").Required();
			_gamesList.bindItem = BindMatchLobbyItem;
			_gamesList.makeItem = MakeMatchLobbyItem;

			_listHeaders = Root.Q("ListHeaders").Required();
			_loader = Root.Q<DotsLoadingElement>("Loader").Required();
			_noLobbiesLabel = Root.Q<LocalizedLabel>("NoLobbiesLabel").Required();
			_refreshButton = Root.Q<Button>("RefreshButton").Required();

			Root.Q<Button>("JoinWithCodeButton").clicked += OnJoinWithCodeClicked;
			_refreshButton.clicked += () => RefreshLobbies().Forget();
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			RefreshLobbies().Forget();

			_matchSettingsView.SetMatchSettings(_services.LocalPrefsService.LastCustomMatchSettings, true);
			_matchSettingsView.SetMainAction(ScriptTerms.UITCustomGames.create_lobby, () => CreateMatch(_matchSettingsView.MatchSettings).Forget());

			return base.OnScreenOpen(reload);
		}

		private async UniTask RefreshLobbies()
		{
			_listHeaders.SetVisibility(false);
			_loader.SetDisplay(true);
			_noLobbiesLabel.SetDisplay(false);
			_refreshButton.SetEnabled(false);
			
			_gamesList.itemsSource = null;
			_gamesList.RefreshItems();
			_lobbies = await _services.FLLobbyService.GetPublicMatches();
			if (gameObject == null) return;
			_gamesList.itemsSource = _lobbies;
			_gamesList.RefreshItems();
			
			_refreshButton.SetEnabled(true);
			_listHeaders.SetVisibility(_lobbies.Count > 0);
			_loader.SetDisplay(false);
			_noLobbiesLabel.SetDisplay(_lobbies.Count == 0);
		}

		private void OnJoinWithCodeClicked()
		{
			PopupPresenter.OpenJoinWithCode(code =>
			{
				PopupPresenter.Close().Forget();
				JoinMatch(code).Forget();
			}).Forget();
		}

		private async UniTaskVoid JoinMatch(string lobbyIDOrCode)
		{
			var success = await _services.FLLobbyService.JoinMatch(lobbyIDOrCode);
			if (success)
			{
				await OpenMatchLobby();
			}
		}

		private async UniTaskVoid CreateMatch(CustomMatchSettings matchSettings)
		{
			var success = await _services.FLLobbyService.CreateMatch(matchSettings);
			if (success)
			{
				await OpenMatchLobby();
			}
		}

		private void BindMatchLobbyItem(VisualElement e, int index)
		{
			var lobby = _lobbies[index];
			((MatchLobbyItemElement) e).SetLobby(lobby, () => JoinMatch(lobby.Id).Forget());
		}

		private static VisualElement MakeMatchLobbyItem()
		{
			return new MatchLobbyItemElement();
		}

		private async UniTask OpenMatchLobby()
		{
			await _services.UIService.OpenScreen<MatchLobbyScreenPresenter>(new MatchLobbyScreenPresenter.StateData
			{
				MatchListStateData = Data
			});
		}
	}
}
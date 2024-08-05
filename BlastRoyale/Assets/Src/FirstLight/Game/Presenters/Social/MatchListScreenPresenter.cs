using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.Game.Views.UITK;
using FirstLight.Game.Views.UITK.Popups;
using FirstLight.UIService;
using I2.Loc;
using QuickEye.UIToolkit;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Represents a presenter for the Match List screen.
	/// </summary>
	public class MatchListScreenPresenter : UIPresenterData<MatchListScreenPresenter.StateData>
	{
		public class StateData
		{
			public Action BackClicked; // Fucking disgusting, UP 23 jul 2024, 24 jul too
		}

		[Q("ListHeaders")] private VisualElement _listHeaders;
		[Q("NoLobbiesLabel")] private LocalizedLabel _noLobbiesLabel;
		[Q("Loader")] private DotsLoadingElement _loader;
		[Q("RefreshButton")] private LocalizedButton _refreshButton;
		[Q("GamesList")] private ListView _gamesList;
		[Q("MatchSettings")] private VisualElement _matchSettings;
		[Q("JoinWithCodeButton")] private LocalizedButton _joinWithCodeButton;
		[Q("ShowAllRegionsToggle")] private LocalizedToggle _allRegionsToggle;

		private MatchSettingsView _matchSettingsView;

		private IGameServices _services;

		private List<Lobby> _lobbies;
		
		private AsyncBufferedQueue _requestBuffer = new (TimeSpan.FromSeconds(1), true);

		protected override void QueryElements()
		{
			_services = MainInstaller.ResolveServices();
			
			var header = Root.Q<ScreenHeaderElement>("Header").Required();
			header.backClicked = Data.BackClicked; 

			_matchSettings.AttachView(this, out _matchSettingsView);
			_gamesList.bindItem = BindMatchLobbyItem;
			_gamesList.makeItem = MakeMatchLobbyItem;

			_joinWithCodeButton.clicked += OnJoinWithCodeClicked;
			_refreshButton.clicked += () =>
			{
				_requestBuffer.Add(RefreshLobbies);
			};

			_allRegionsToggle.RegisterValueChangedCallback(value =>
			{
				_requestBuffer.Add(RefreshLobbies);
			});
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_requestBuffer.Add(RefreshLobbies);

			_matchSettingsView.SetMatchSettings(_services.LocalPrefsService.LastCustomMatchSettings, true, false);
			_matchSettingsView.SetMainAction(ScriptTerms.UITCustomGames.create_lobby, () => CreateMatch(_matchSettingsView.MatchSettings).Forget());

			return base.OnScreenOpen(reload);
		}

		protected override UniTask OnScreenClose()
		{
			if (PopupPresenter.IsOpen<MatchInfoPopupView>())
			{
				PopupPresenter.Close();
			}
			return base.OnScreenClose();
		}

		private async UniTask RefreshLobbies()
		{
			// if in game room no need to refresh lobbies
			if (_services.RoomService.InRoom) return;
			
			_listHeaders.SetVisibility(false);
			_loader.SetDisplay(true);
			_noLobbiesLabel.SetDisplay(false);
			_refreshButton.SetEnabled(false);

			_gamesList.itemsSource = null;
			_gamesList.RefreshItems();
			_lobbies = await _services.FLLobbyService.GetPublicMatches(_allRegionsToggle.value).AttachExternalCancellation(GetCancellationTokenOnClose());
			
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
			await _services.UIService.OpenScreen<LoadingSpinnerScreenPresenter>();
			await _services.FLLobbyService.JoinMatch(lobbyIDOrCode);
			await _services.UIService.CloseScreen<LoadingSpinnerScreenPresenter>();
		}

		private async UniTaskVoid CreateMatch(CustomMatchSettings matchSettings)
		{
			await _services.UIService.OpenScreen<LoadingSpinnerScreenPresenter>();
			await _services.FLLobbyService.CreateMatch(matchSettings);
			await _services.UIService.CloseScreen<LoadingSpinnerScreenPresenter>();
		}

		private void BindMatchLobbyItem(VisualElement e, int index)
		{
			var lobby = _lobbies[index];
			((MatchLobbyItemElement) e).SetLobby(lobby, () => JoinMatch(lobby.Id).Forget(), () => OpenMatchInfo(lobby));
		}

		private static VisualElement MakeMatchLobbyItem()
		{
			return new MatchLobbyItemElement();
		}

		private void OpenMatchInfo(Lobby lobby)
		{
			var friendsPlaying = lobby.GetFriends().Select(p => p.GetPlayerName()).ToList();
			PopupPresenter.OpenMatchInfo(lobby.GetMatchSettings().ToSimulationMatchConfig(), friendsPlaying, () =>
			{
				PopupPresenter.Close();
				JoinMatch(lobby.Id).Forget();
			}).Forget();
		}
	}
}
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.UITK;
using FirstLight.UIService;
using Unity.Services.Lobbies.Models;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	public class CustomGamesScreenPresenter : UIPresenter
	{
		private ListView _gamesList;

		private MatchSettingsView _matchSettingsView;

		private IGameServices _services;

		private List<Lobby> _lobbies;

		protected override void QueryElements()
		{
			_services = MainInstaller.ResolveServices();

			Root.Q("MatchSettings").Required().AttachView(this, out _matchSettingsView);
			_gamesList = Root.Q<ListView>("GamesList").Required();
			_gamesList.bindItem = BindMatchLobbyItem;
			_gamesList.makeItem = MakeMatchLobbyItem;
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			RefreshLobbies().Forget();

			return base.OnScreenOpen(reload);
		}

		private async UniTask RefreshLobbies()
		{
			_lobbies = await _services.FLLobbyService.GetPublicGameLobbies();
			if (gameObject == null) return;
			_gamesList.itemsSource = _lobbies;
			_gamesList.RefreshItems();
		}

		private VisualElement MakeMatchLobbyItem()
		{
			return new MatchLobbyElement();
		}

		private void BindMatchLobbyItem(VisualElement e, int index)
		{
			var mle = (MatchLobbyElement) e;
			mle.SetLobby(_lobbies[index]);
		}
	}
}
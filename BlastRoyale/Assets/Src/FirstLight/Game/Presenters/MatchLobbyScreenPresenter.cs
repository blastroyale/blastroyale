using Cysharp.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.UIService;
using Newtonsoft.Json;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	public class MatchLobbyScreenPresenter : UIPresenter
	{
		private IGameServices _services;

		protected override void QueryElements()
		{
			_services = MainInstaller.ResolveServices();

			var header = Root.Q<ScreenHeaderElement>("Header").Required();
			header.SetTitle(_services.FLLobbyService.CurrentMatchLobby.Name);
			header.backClicked += () => LeaveMatchLobby().Forget();

			Root.Q<Button>("DebugBackButton").Required().clicked += () => _services.UIService.OpenScreen<CustomGamesScreenPresenter>().Forget();
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			var matchLobby = _services.FLLobbyService.CurrentMatchLobby;

			Root.Q<Label>("DebugStatus").Required().text =
				$"Name: {matchLobby.Name}\n" +
				$"Players: {matchLobby.Players.Count}/{matchLobby.MaxPlayers}\n" +
				$"ID: {matchLobby.Id}" +
				$"Host: {matchLobby.HostId}" +
				$"Data: {JsonConvert.SerializeObject(matchLobby.GetMatchSettings())}";

			return base.OnScreenOpen(reload);
		}

		private async UniTaskVoid LeaveMatchLobby()
		{
			await _services.FLLobbyService.LeaveMatch();

			_services.UIService.OpenScreen<CustomGamesScreenPresenter>().Forget();
		}
	}
}
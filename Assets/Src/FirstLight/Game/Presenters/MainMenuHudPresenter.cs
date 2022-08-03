using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Main Menu HUD UI by:
	/// - Player Currencies and animations of currency gains.
	/// </summary>
	public class MainMenuHudPresenter : UiPresenter
	{
		private IGameDataProvider _dataProvider;
		private IGameServices _services;
		private IMainMenuServices _mainMenuServices;

		private void Awake()
		{
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			_mainMenuServices = MainMenuInstaller.Resolve<IMainMenuServices>();
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}
	}
}

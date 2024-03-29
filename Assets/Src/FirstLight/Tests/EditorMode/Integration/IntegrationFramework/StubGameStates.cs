using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.StateMachines;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using FirstLight.UIService;

namespace FirstLight.Tests.EditorMode
{
	public class StubGameStates : GameStateMachine
	{
		public StubGameStates(GameLogic gameLogic, IGameServices services, IGameUiServiceInit uiService, UIService2 uiService2,
							  IInternalGameNetworkService networkService, IInternalTutorialService tutorialService, IConfigsAdder configsAdder,
							  IAssetAdderService assetAdderService, IDataService dataService,
							  IVfxInternalService<VfxId> vfxService) : base(gameLogic, services, uiService, uiService2,
			networkService, tutorialService, configsAdder, assetAdderService, dataService, vfxService)
		{
		}
	}
}
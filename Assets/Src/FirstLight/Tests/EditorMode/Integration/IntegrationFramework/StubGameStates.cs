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
		public StubGameStates(GameLogic gameLogic, IGameServices services,
							  IInternalGameNetworkService networkService, IInternalTutorialService tutorialService, IConfigsAdder configsAdder,
							  IAssetAdderService assetAdderService, IDataService dataService,
							  IVfxInternalService<VfxId> vfxService) : base(gameLogic, services,
			networkService, tutorialService, configsAdder, assetAdderService, dataService, vfxService)
		{
		}
	}
}
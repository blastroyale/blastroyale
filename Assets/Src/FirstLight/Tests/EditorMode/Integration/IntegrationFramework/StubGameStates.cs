using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.StateMachines;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;

namespace FirstLight.Tests.EditorMode
{
	public class StubGameStates : GameStateMachine
	{
		public StubGameStates(GameLogic gameLogic, IGameServices services, IGameUiServiceInit uiService, IGameBackendNetworkService networkService, IConfigsAdder configsAdder, IAssetAdderService assetAdderService, IDataService dataService, IVfxInternalService<VfxId> vfxService) : base(gameLogic, services, uiService, networkService, configsAdder, assetAdderService, dataService, vfxService)
		{
		}
	}
}
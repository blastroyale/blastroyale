using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.StateMachines;
using FirstLight.Server.SDK.Modules.GameConfiguration;

namespace FirstLight.Tests.EditorMode
{
	public class StubGameStates : GameStateMachine
	{
		public StubGameStates(GameLogic gameLogic, IGameServices services, IInternalGameNetworkService networkService, IConfigsAdder configsAdder,
							  IAssetAdderService assetAdderService) : base(gameLogic, services, networkService, assetAdderService)
		{
		}
	}
}
using System.Linq;
using FirstLight.Game.Commands.Cheats;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.SDK.Services;
using FirstLight.Services;
using I2.Loc;
using Quantum;

namespace FirstLight.Game.Services
{
	public interface ICheatsService
	{
	}

	public class CheatsService : ICheatsService
	{
		private readonly IGameCommandService _commandService;
		private readonly IGenericDialogService _genericDialogService;
		private readonly IEnvironmentService _environmentService;
		private readonly IMessageBrokerService _brokerService;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly ITutorialService _tutorialService;

		public CheatsService(IGameCommandService commandService, IGenericDialogService genericDialogService,
							 IEnvironmentService environmentService, IMessageBrokerService brokerService,
							 IGameDataProvider gameDataProvider, ITutorialService tutorialService)
		{
			_commandService = commandService;
			_genericDialogService = genericDialogService;
			_environmentService = environmentService;
			_brokerService = brokerService;
			_gameDataProvider = gameDataProvider;
			_tutorialService = tutorialService;
		}

		private bool ShouldExecute()
		{
			return _environmentService.Environment == Environment.TESTNET;
		}

	}
}

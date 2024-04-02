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
			_brokerService.Subscribe<MainMenuOpenedMessage>(OnMainMenuOpened);
		}

		private bool ShouldExecute()
		{
			return _environmentService.Environment == Environment.TESTNET;
		}

		private void Unsubscribe()
		{
			_brokerService.Unsubscribe<MainMenuOpenedMessage>(OnMainMenuOpened);

		}
		private void OnMainMenuOpened(MainMenuOpenedMessage obj)
		{
			if (!ShouldExecute()
				// Nasty workaround if player scraps all the items it will receive it again, but this only works in community build anyway
				||  _gameDataProvider.EquipmentDataProvider.Inventory.Count >= 5) 
			{
				Unsubscribe();
				return;
			}
			
			// Only give items after the tutorial is finished
			if(!_tutorialService.HasCompletedTutorialSection(TutorialSection.FIRST_GUIDE_MATCH))
			{
				return;
			}

			var confirmButton = new GenericDialogButton { ButtonText = ScriptLocalization.General.OK, ButtonOnClick = _genericDialogService.CloseDialog };

			_genericDialogService.OpenButtonDialog(
				"Welcome to Testing",
				@"You are on a test environment
<color=#c9221c>Any progress may be lost</color>

You are receiving some test equipment
<sprite name=""Crown""><color=#2bab4d> Have fun Blaster </color><sprite name=""Crown"">",
				false,
				confirmButton
			);
			GiveAllEquipments();
			Unsubscribe();
		}


		private void GiveAllEquipments()
		{
			_commandService.ExecuteCommand(new GiveAllEquipmentCommand());
		}
	}
}
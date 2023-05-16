using FirstLight.Game.Commands.Cheats;
using FirstLight.Game.Data;
using FirstLight.Game.Messages;
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


		public CheatsService(IGameCommandService gameCommandService, IMessageBrokerService brokerService, IGenericDialogService genericDialogService,
							 IEnvironmentService environmentService)
		{
			_brokerService = brokerService;
			_genericDialogService = genericDialogService;
			_commandService = gameCommandService;
			_environmentService = environmentService;

			if (ShouldExecute())
			{
				brokerService.Subscribe<CompletedTutorialSectionMessage>(OnCompletedTutorialSection);
			}
		}

		private bool ShouldExecute()
		{
			return _environmentService.Environment == Environment.TESTNET;
		}

		private void OnMainMenuOpened(MainMenuOpenedMessage obj)
		{
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
			_brokerService.Unsubscribe<MainMenuOpenedMessage>(OnMainMenuOpened);
		}


		private void OnCompletedTutorialSection(CompletedTutorialSectionMessage obj)
		{
			if (!ShouldExecute()) return;
			// Finished the tutorial
			if (obj.Section == TutorialSection.META_GUIDE_AND_MATCH)
			{
				_brokerService.Subscribe<MainMenuOpenedMessage>(OnMainMenuOpened);
				GiveAllEquipments();
			}
		}


		private void GiveAllEquipments()
		{
			_commandService.ExecuteCommand(new GiveAllEquipmentCommand());
		}
	}
}
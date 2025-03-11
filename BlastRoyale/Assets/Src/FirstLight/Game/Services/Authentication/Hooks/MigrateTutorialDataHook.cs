using Cysharp.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Services;
using PlayFab.ClientModels;

namespace FirstLight.Game.Services.Authentication.Hooks
{
	public class MigrateTutorialDataHook : IAuthenticationHook
	{
		private IDataService _dataService;
		private IGameCommandService _commandService;
		private TutorialSection _completed;

		public MigrateTutorialDataHook(IDataService dataService, IGameCommandService commandService)
		{
			_dataService = dataService;
			_commandService = commandService;
			_completed = TutorialSection.NONE;
		}

		public UniTask BeforeAuthentication(bool previouslyLoggedIn = false)
		{
			var tutorialData = _dataService.GetData<TutorialData>();
			_completed = tutorialData.TutorialSections;
			return UniTask.CompletedTask;
		}
		
		public UniTask BeforeLogout()
		{
			return UniTask.CompletedTask;
		}

		public UniTask AfterAuthentication(LoginResult result, bool previouslyLoggedIn = false)
		{
			return UniTask.CompletedTask;
		}

		public UniTask AfterFetchedState(LoginResult result)
		{
			// This should not be here, should be in logic, I could call logic directly here
			// but this is called before GameLogic.Init(), so it can cause issues in the future 
			// The old authentication called the logic and didn't care
			if (_dataService.GetData<PlayerData>().MigratedGuestData)
			{
				return UniTask.CompletedTask;
			}

			_commandService.ExecuteCommand(new MigrateGuestDataCommand
			{
				GuestMigrationData = new MigrationData()
				{
					TutorialSections = _completed
				}
			});
			return UniTask.CompletedTask;
		}
	}
}
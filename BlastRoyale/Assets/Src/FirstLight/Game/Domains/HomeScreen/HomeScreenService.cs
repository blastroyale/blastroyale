using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Domains.HomeScreen.UI;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.RoomService;
using FirstLight.Game.Utils;
using FirstLight.SDK.Services;
using I2.Loc;
using Quantum;

namespace FirstLight.Game.Domains.HomeScreen
{
	public enum HomeScreenForceBehaviourType : byte
	{
		None,
		Store,
		Matchmaking,
		PaidEvent,
	}

	/// <summary>
	/// Hack service to force behaviours when opening home screen
	/// this is due to the huge amount of complexity on the state machine, this is just a way of isolating weird code changing it here
	/// </summary>
	public interface IHomeScreenService
	{
		public event Action<List<string>> CustomPlayButtonValidations;
		public HomeScreenForceBehaviourType ForceBehaviour { get; }
		public object ForceBehaviourData { get; }
		public void SetForceBehaviour(HomeScreenForceBehaviourType type, object data = null);
		public List<string> ValidatePlayButton();

		/// <summary>
		/// Returns if interrupted the regular flow of the application
		/// </summary>
		/// <param name="openingScreen"></param>
		/// <returns></returns>
		public UniTask<ProcessorResult> ShowNotifications(Type openingScreen, Func<UniTask> openOriginalScreenTask);

		enum ProcessorResult
		{
			None, // Nothing needs to be done after finishing handling
			OpenOriginalScreen, // After finishing handling the original screen should be opened 
			CustomBehaviour // Break the current notification flow and let the handler to everything
		}

		delegate UniTask<ProcessorResult> QueueProcessorDelegate(Type screenType);
		/// <summary>
		/// Register a task that will be processed in a queue when opening the home screen, this is used for queueing notifications
		/// </summary>
		public void RegisterNotificationQueueProcessor(QueueProcessorDelegate @delegate);
	}

	public class HomeScreenService : IHomeScreenService
	{
		private readonly IGameDataProvider _gameDataProvider;
		private readonly UIService.UIService _uiService;
		private readonly IRoomService _roomService;
		private readonly IMessageBrokerService _msgBroker;
		private readonly IGameCommandService _commandService;
		private readonly IGameBackendService _gameBackendService;
		private readonly IGenericDialogService _genericDialogService;
		private readonly List<IHomeScreenService.QueueProcessorDelegate> _queuedNotificationHandlers;

		public HomeScreenService(IGameDataProvider gameDataProvider, UIService.UIService uiService, IMessageBrokerService msgBroker,
								 IRoomService roomService, IGameCommandService commandService, IGameBackendService gameBackendService,
								 IGenericDialogService genericDialogService)
		{
			_gameDataProvider = gameDataProvider;
			_uiService = uiService;
			_msgBroker = msgBroker;
			_roomService = roomService;
			_commandService = commandService;
			_gameBackendService = gameBackendService;
			_genericDialogService = genericDialogService;
			_queuedNotificationHandlers = new List<IHomeScreenService.QueueProcessorDelegate>();
			_queuedNotificationHandlers.Add(CollectAndDisplayRewards);
		}

		public event Action<List<string>> CustomPlayButtonValidations;
		public HomeScreenForceBehaviourType ForceBehaviour { get; set; }
		public object ForceBehaviourData { get; set; }

		public void SetForceBehaviour(HomeScreenForceBehaviourType type, object data = null)
		{
			ForceBehaviour = type;
			ForceBehaviourData = data;
		}

		public List<string> ValidatePlayButton()
		{
			var errors = new List<string>();
			CustomPlayButtonValidations?.Invoke(errors);
			return errors;
		}

		public async UniTask<IHomeScreenService.ProcessorResult> ShowNotifications(Type openingScreen, Func<UniTask> openOriginalScreen)
		{
			var lastResult = IHomeScreenService.ProcessorResult.None;
			foreach (var queuedNotificationHandler in _queuedNotificationHandlers)
			{
				lastResult = await queuedNotificationHandler(openingScreen);
				if (lastResult == IHomeScreenService.ProcessorResult.OpenOriginalScreen)
				{
					if (openOriginalScreen != null)
					{
						await openOriginalScreen.Invoke();
					}
				}

				if (lastResult == IHomeScreenService.ProcessorResult.CustomBehaviour)
				{
					return IHomeScreenService.ProcessorResult.CustomBehaviour;
				}
			}

			return lastResult;
		}

		public void RegisterNotificationQueueProcessor(IHomeScreenService.QueueProcessorDelegate @delegate)
		{
			_queuedNotificationHandlers.Add(@delegate);
		}

		public async UniTask<IHomeScreenService.ProcessorResult> CollectAndDisplayRewards(Type screen)
		{
			var oldFameLevel = _gameDataProvider.PlayerDataProvider.Level.Value;
			var rewards = await TryClaimUncollectedRewards();
			var leveledUp = oldFameLevel < _gameDataProvider.PlayerDataProvider.Level.Value;
			if (leveledUp)
			{
				var levelRewards = _gameDataProvider.PlayerDataProvider.GetRewardsForFameLevel(
					_gameDataProvider.PlayerDataProvider.Level.Value - 1
				);
				var levelUpWaiter = new AsyncCallbackWrapper();
				await _uiService.OpenScreen<RewardsScreenPresenter>(new RewardsScreenPresenter.StateData
				{
					FameRewards = true,
					Items = levelRewards,
					OnFinish = levelUpWaiter.Invoke
				});
				await levelUpWaiter.OnInvokeAsync();
			}

			if (rewards != null)
			{
				if (leveledUp) await _uiService.CloseScreen<RewardsScreenPresenter>();
				var rewardsCopy = rewards
					.Where(item => !item.Id.IsInGroup(GameIdGroup.Currency) && item.Id is not (GameId.XP or GameId.BPP or GameId.Trophies)).ToList();
				if (rewardsCopy.Count > 0)
				{
					var rewardsAwaiter = new AsyncCallbackWrapper();
					await _uiService.OpenScreen<RewardsScreenPresenter>(new RewardsScreenPresenter.StateData
					{
						Items = rewardsCopy,
						OnFinish = rewardsAwaiter.Invoke
					});
					await rewardsAwaiter.OnInvokeAsync();
				}
			}

			return IHomeScreenService.ProcessorResult.OpenOriginalScreen;
		}

		private async UniTask<IReadOnlyList<ItemData>> TryClaimUncollectedRewards()
		{
			if (FeatureFlags.GetLocalConfiguration().OfflineMode || !FeatureFlags.WAIT_REWARD_SYNC)
			{
				if (_gameDataProvider.RewardDataProvider.UnclaimedRewards.Count > 0)
				{
					return _commandService.ExecuteCommandWithResult(new CollectUnclaimedRewardsCommand());
				}

				return null;
			}

			var unclaimedCountCheck = 0;
			while (unclaimedCountCheck < 10)
			{
				bool matches;
				try
				{
					matches = await _gameBackendService.CheckIfRewardsMatch();
				}
				catch (Exception ex)
				{
					unclaimedCountCheck++;
					FLog.Error("Failed to check if rewards matches", ex);
					continue;
				}

				if (matches)
				{
					if (unclaimedCountCheck > 0)
					{
						_genericDialogService.CloseDialog();
					}

					if (_gameDataProvider.RewardDataProvider.UnclaimedRewards.Count > 0)
					{
						return _commandService.ExecuteCommandWithResult(new CollectUnclaimedRewardsCommand());
					}

					return null;
				}

				if (unclaimedCountCheck == 0)
				{
					_genericDialogService.OpenButtonDialog(
						ScriptLocalization.UITHomeScreen.waitforrewards_popup_title,
						ScriptLocalization.UITHomeScreen.waitforrewards_popup_description,
						false, new GenericDialogButton()).Forget();
				}

				unclaimedCountCheck++;
				await UniTask.Delay(TimeSpan.FromMilliseconds(500)); // space check calls a bit
			}

#if UNITY_EDITOR
			var confirmButton = new GenericDialogButton
			{
				ButtonText = "OK",
				ButtonOnClick = () => MainInstaller.ResolveServices().QuitGame("Desync")
			};
			_genericDialogService.OpenButtonDialog("Server Error", "Desync", false, confirmButton).Forget();
#else
				FirstLight.NativeUi.NativeUiService.ShowAlertPopUp(false, "Error", "Desync",
					new FirstLight.NativeUi.AlertButton
					{
						Callback = () => MainInstaller.ResolveServices().QuitGame("Server Desync"),
						Style = FirstLight.NativeUi.AlertButtonStyle.Negative,
						Text = "Quit Game"
					});
#endif
			return null;
		}

		private void FinishRewardSequence()
		{
		}
	}
}
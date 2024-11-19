using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Data.DataTypes;
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

		public UniTask<bool> CollectAndDisplayRewards();
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

		public async UniTask<bool> CollectAndDisplayRewards()
		{
			var oldFameLevel = _gameDataProvider.PlayerDataProvider.Level.Value;
			var rewards = await TryClaimUncollectedRewards();
			var leveledUp = oldFameLevel < _gameDataProvider.PlayerDataProvider.Level.Value;
			var displayed = false;
			if (leveledUp)
			{
				displayed = true;
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
					displayed = true;
					var rewardsAwaiter = new AsyncCallbackWrapper();
					await _uiService.OpenScreen<RewardsScreenPresenter>(new RewardsScreenPresenter.StateData
					{
						Items = rewardsCopy,
						OnFinish = rewardsAwaiter.Invoke
					});
					await rewardsAwaiter.OnInvokeAsync();
				}
			}

			if (displayed && !_roomService.InRoom)
			{
				_msgBroker.Publish(new OnViewingRewardsFinished());
			}

			return displayed;
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
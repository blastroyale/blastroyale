using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using TMPro;
using UnityEngine;
using DG.Tweening;
using I2.Loc;
using MoreMountains.NiceVibrations;
using Quantum;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Main Menu HUD UI by:
	/// - Showing the Main Menu HUD visual status.
	/// - Player Currencies and animations of currency gains.
	/// </summary>
	public class MainMenuHudPresenter : UiPresenter
	{
		[SerializeField] private Transform _scTooltipAnchor;
		[SerializeField] private Transform _hcTooltipAnchor;
		[SerializeField] private TextMeshProUGUI _softCurrencyText;
		[SerializeField] private TextMeshProUGUI _hardCurrencyText;
		[SerializeField] private Transform _scAnimationTarget;
		[SerializeField] private Transform _hcAnimationTarget;
		[SerializeField] private int _rackupTextAnimationDuration = 5;

		private IGameDataProvider _dataProvider;
		private IGameServices _services;
		private IMainMenuServices _mainMenuServices;

		private void Awake()
		{
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			_mainMenuServices = MainMenuInstaller.Resolve<IMainMenuServices>();
			_services.MessageBrokerService.Subscribe<UnclaimedRewardsCollectingStartedMessage>(OnUnclaimedRewardsCollectingStartedMessage);
			_services.MessageBrokerService.Subscribe<TrophyRoadRewardCollectingStartedMessage>(OnTrophyRoadRewardCollectingStartedMessage);
			_services.MessageBrokerService.Subscribe<UnclaimedRewardsCollectedMessage>(OnUnclaimedRewardsCollectedMessage);
			_services.MessageBrokerService.Subscribe<TrophyRoadRewardCollectedMessage>(OnTrophyRoadRewardCollectedMessage);
			_services.MessageBrokerService.Subscribe<PlayUiVfxCommandMessage>(OnPlayUiVfxCommandMessage);
			
			_dataProvider.CurrencyDataProvider.Currencies.Observe(OnCurrencyChanged);
			//Debug.LogError("AWAKE");
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_dataProvider?.CurrencyDataProvider?.Currencies?.StopObserving(OnCurrencyChanged);
		}

		protected override void OnOpened()
		{
			_softCurrencyText.text = $" {_dataProvider.CurrencyDataProvider.Currencies[GameId.SC].ToString()}";
			_hardCurrencyText.text = $" {_dataProvider.CurrencyDataProvider.Currencies[GameId.HC].ToString()}";
			
			MMVibrationManager.SetHapticsActive(_dataProvider.AppDataProvider.IsHapticOn);
		}
		
		private void OnCurrencyChanged(GameId currency, ulong previous, ulong newAmount, ObservableUpdateType updateType)
		{
			var targetValue = _dataProvider.CurrencyDataProvider.Currencies[currency];
			
			if (currency == GameId.SC)
			{
				DOVirtual.Float(previous, targetValue, _rackupTextAnimationDuration, SoftCurrencyRackupUpdate);
			}
			else if (currency == GameId.HC)
			{
				DOVirtual.Float(previous, targetValue, _rackupTextAnimationDuration, HardCurrencyRackupUpdate);
			}
		}

		private void OnUnclaimedRewardsCollectingStartedMessage(UnclaimedRewardsCollectingStartedMessage message)
		{
			_dataProvider.CurrencyDataProvider.Currencies.StopObserving(OnCurrencyChanged);
		}

		private void OnTrophyRoadRewardCollectingStartedMessage(TrophyRoadRewardCollectingStartedMessage obj)
		{
			_dataProvider.CurrencyDataProvider.Currencies.StopObserving(OnCurrencyChanged);
		}

		private void OnTrophyRoadRewardCollectedMessage(TrophyRoadRewardCollectedMessage obj)
		{
			_dataProvider.CurrencyDataProvider.Currencies.Observe(OnCurrencyChanged);
		}

		private void OnUnclaimedRewardsCollectedMessage(UnclaimedRewardsCollectedMessage obj)
		{
			_dataProvider.CurrencyDataProvider.Currencies.Observe(OnCurrencyChanged);
		}
		
		private void OnPlayUiVfxCommandMessage(PlayUiVfxCommandMessage message)
		{
			var closure = message;

			if (message.Id == GameId.SC)
			{
				_mainMenuServices.UiVfxService.PlayVfx(message.Id, message.OriginWorldPosition,
				                                       _scAnimationTarget.position, 
				                                       () => RackupTween(SoftCurrencyRackupUpdate));
			}
			else if (message.Id == GameId.HC)
			{
				_mainMenuServices.UiVfxService.PlayVfx(message.Id, message.OriginWorldPosition,
				                                       _hcAnimationTarget.position, 
				                                       () => RackupTween(HardCurrencyRackupUpdate));
			}

			void RackupTween(TweenCallback<float> textUpdated)
			{
				var targetValue = _dataProvider.CurrencyDataProvider.Currencies[closure.Id];
				var initialValue = targetValue - closure.Quantity;
				
				DOVirtual.Float(initialValue, targetValue, _rackupTextAnimationDuration, textUpdated);
			}
		}

		private void OnSCClicked()
		{
			_services.GenericDialogService.OpenTooltipDialog(ScriptLocalization.Tooltips.ToolTip_SC, _scTooltipAnchor.position, TooltipArrowPosition.Top);
		}
		
		private void OnHCClicked()
		{
			_services.GenericDialogService.OpenTooltipDialog(ScriptLocalization.Tooltips.ToolTip_HC, _hcTooltipAnchor.position, TooltipArrowPosition.Top);
		}

		private void SoftCurrencyRackupUpdate(float value)
		{
			_softCurrencyText.text = $" {value.ToString("N0")}";
		}

		private void HardCurrencyRackupUpdate(float value)
		{
			_hardCurrencyText.text = $" {value.ToString("N0")}";
		}
	}
}
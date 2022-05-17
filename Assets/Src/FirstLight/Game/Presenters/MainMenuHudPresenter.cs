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
using Sirenix.OdinInspector;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Main Menu HUD UI by:
	/// - Showing the Main Menu HUD visual status.
	/// - Player Currencies and animations of currency gains.
	/// </summary>
	public class MainMenuHudPresenter : UiPresenter
	{
		[SerializeField, Required] private Transform _scTooltipAnchor;
		[SerializeField, Required] private TextMeshProUGUI _csCurrencyText;
		[SerializeField, Required] private Transform _scAnimationTarget;
		[SerializeField] private UnityEngine.UI.Button _csButton;
		[SerializeField] private int _rackupTextAnimationDuration = 5;

		private IGameDataProvider _dataProvider;
		private IGameServices _services;
		private IMainMenuServices _mainMenuServices;

		private void Awake()
		{
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			_mainMenuServices = MainMenuInstaller.Resolve<IMainMenuServices>();
			_csButton.onClick.AddListener(OnCsClicked);
			_services.MessageBrokerService.Subscribe<UnclaimedRewardsCollectingStartedMessage>(OnUnclaimedRewardsCollectingStartedMessage);
			_services.MessageBrokerService.Subscribe<UnclaimedRewardsCollectedMessage>(OnUnclaimedRewardsCollectedMessage);
			_services.MessageBrokerService.Subscribe<PlayUiVfxCommandMessage>(OnPlayUiVfxCommandMessage);

			_dataProvider.CurrencyDataProvider.Currencies.Observe(OnCurrencyChanged);
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_dataProvider?.CurrencyDataProvider?.Currencies?.StopObserving(OnCurrencyChanged);
		}

		private void OnCurrencyChanged(GameId currency, ulong previous, ulong newAmount,
		                               ObservableUpdateType updateType)
		{
			var targetValue = _dataProvider.CurrencyDataProvider.Currencies[currency];

			if (currency == GameId.CS)
			{
				DOVirtual.Float(previous, targetValue, _rackupTextAnimationDuration, CraftSpiceRackupUpdate);
			}
		}

		private void OnUnclaimedRewardsCollectingStartedMessage(UnclaimedRewardsCollectingStartedMessage message)
		{
			_dataProvider.CurrencyDataProvider.Currencies.StopObserving(OnCurrencyChanged);
		}

		private void OnUnclaimedRewardsCollectedMessage(UnclaimedRewardsCollectedMessage obj)
		{
			_dataProvider.CurrencyDataProvider.Currencies.Observe(OnCurrencyChanged);
		}

		private void OnPlayUiVfxCommandMessage(PlayUiVfxCommandMessage message)
		{
			var closure = message;

			if (message.Id == GameId.CS)
			{
				// TODO - Re-enable when the UI is there
				/*_mainMenuServices.UiVfxService.PlayVfx(message.Id, message.OriginWorldPosition,
				                                       _scAnimationTarget.position,
				                                       () => RackupTween(CraftSpiceRackupUpdate));*/
			}

			void RackupTween(TweenCallback<float> textUpdated)
			{
				var targetValue = _dataProvider.CurrencyDataProvider.Currencies[closure.Id];
				var initialValue = targetValue - closure.Quantity;

				DOVirtual.Float(initialValue, targetValue, _rackupTextAnimationDuration, textUpdated);
			}
		}

		private void OnCsClicked()
		{
			_services.GenericDialogService.OpenTooltipDialog(ScriptLocalization.Tooltips.ToolTip_CS,
			                                                 _scTooltipAnchor.position, TooltipArrowPosition.Top);
		}

		private void CraftSpiceRackupUpdate(float value)
		{
			_csCurrencyText.text = $" {value.ToString("N0")}";
		}
	}
}
using DG.Tweening;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Handles showing an amount, tooltip, and an icon per gameID basis.
	/// Icon is not loaded in this class to allow for easier art adjustments - change icon in prefab
	/// </summary>
	public class IconAndAmountHolderView : MonoBehaviour
	{
		[SerializeField, Required] private Transform _tooltipAnchor;
		[SerializeField, Required] private TextMeshProUGUI _amountText;
		[SerializeField, Required] private Transform _animationTarget;
		[SerializeField, Required] private UnityEngine.UI.Button _button;
		[SerializeField] private float _rackupTextAnimDurationSeconds = 5f;
		[SerializeField] private GameId _targetID;


		private IGameDataProvider _dataProvider;
		private IGameServices _services;
		private IMainMenuServices _mainMenuServices;
		private int _currentAmount;

		private void Awake()
		{
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			_mainMenuServices = MainMenuInstaller.Resolve<IMainMenuServices>();
			_button.onClick.AddListener(OnClicked);
			_services.MessageBrokerService.Subscribe<PlayUiVfxMessage>(OnPlayUiVfxMessage);
			UpdateAmountText(GetAmount());
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
		}

		private void OnPlayUiVfxMessage(PlayUiVfxMessage message)
		{
			var closure = message;

			if (message.Id == GameId.CS)
			{
				_mainMenuServices.UiVfxService.PlayVfx(message.Id, message.OriginWorldPosition,
				                                       _animationTarget.position, () => RackupTween(UpdateAmountText));
			}

			void RackupTween(TweenCallback<float> textUpdated)
			{
				var targetValue = GetAmount();
				var initialValue = targetValue - (int) closure.Quantity;

				DOVirtual.Float(initialValue, targetValue, _rackupTextAnimDurationSeconds, textUpdated);
			}
		}

		private int GetAmount()
		{
			switch (_targetID)
			{
				case GameId.CS:
					return (int) _dataProvider.CurrencyDataProvider.GetCurrencyAmount(GameId.CS);
				
				case GameId.BLST:
					return (int) _dataProvider.CurrencyDataProvider.GetCurrencyAmount(GameId.BLST);
				
				case GameId.Trophies:
					return (int) _dataProvider.PlayerDataProvider.Trophies.Value;

				default:
					return 0;
			}
		}

		private void OnClicked()
		{
			string tooltip = "";

			switch (_targetID)
			{
				case GameId.CS:
					tooltip = ScriptLocalization.Tooltips.ToolTip_CS;
					break;
				
				case GameId.BLST:
					tooltip = ScriptLocalization.Tooltips.ToolTip_BLST;
					break;
				case GameId.Trophies:
					tooltip = ScriptLocalization.Tooltips.ToolTip_Trophies;
					break;
			}

			_services.GenericDialogService.OpenTooltipDialog(tooltip, _tooltipAnchor.position,
			                                                 TooltipArrowPosition.Top);
		}

		private void UpdateAmountText(float amount)
		{
			_currentAmount = (int) amount;
			_amountText.text = amount.ToString("F0");
		}
	}
}
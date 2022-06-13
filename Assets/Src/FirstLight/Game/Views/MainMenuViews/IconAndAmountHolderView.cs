using System;
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
	// TODO EVE - Create new localization terms in ToolTips for BLST and trophies
	// BLST - Blast Tokens are valuable rewards obtained in special ways in-game, or purchased. Use Blast Tokens to purchase equipment on the marketplace.
	// Trophies - More Trophies increase your Craft Spice pool. Place higher in Battle Royale matches to earn trophies.
	
	// On 'MainMenu HUD' prefab, clone the IconAndAmountHolder_CS, into 2 new ' _BLST' and '_Trophies' versions
	// This script is attached to these prefabs. Change the '_targetID' in each prefab to BLST and Trophies
	// and adjust the icon (there is BLST and Trophies icons already in project)
	
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
				
				// TODO EVE - Handle GetAmount() for BLST and Trophies here
				// BLST is in same place as CS, just different GameID
				// Trophies can be fetched from _dataProvider.PlayerDataProvider.Trophies.Value

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
				
				// TODO EVE - Handle BLST/Trophies tooltips here
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
using System;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters.Store;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using Quantum;
using QuickEye.UIToolkit;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Handles purchase confirmations
	/// </summary>
	[UILayer(UILayer.Popup)]
	public class BuyBattlepassLevelPopupPresenter : UIPresenterData<BuyBattlepassLevelPopupPresenter.StateData>
	{
		public class StateData
		{
			public ulong OwnedCurrency;
			public Action<int> OnConfirm;
			public Action OnExit;
		}

		private GameId _currency = GameId.BlastBuck;

		private GenericPopupElement _popup;
		private IGameServices _services;
		private IBattlePassDataProvider _battlePassData;

		private Label _itemPrice;
		private SliderIntWithButtons _slider;
		private ImageButton _buyButton;
		private VisualElement _costIcon;

		private void Awake()
		{
			_services = MainInstaller.ResolveServices();
			_battlePassData = MainInstaller.ResolveData().BattlePassDataProvider;
		}

		protected override UniTask OnScreenClose()
		{
			Data.OnExit?.Invoke();
			return base.OnScreenClose();
		}

		protected override void QueryElements()
		{
			_popup = Root.Q<GenericPopupElement>("Popup");
			_itemPrice = Root.Q<Label>("ItemPrice").Required();
			_costIcon = Root.Q("CostIcon").Required();
			_slider = Root.Q<SliderIntWithButtons>("Slider").Required();
			_buyButton = Root.Q<ImageButton>("BuyButton").Required();

			_buyButton.clicked += OnBuyButtonClicked;
			_slider.SetTooltipFormat("+ {0} LEVELS");
			Root.Q<ImageButton>("BlockerButton").Required().clicked += CloseRequested;
			_popup.CloseClicked += CloseRequested;
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_slider.highValue = (int) _battlePassData.GetMaxPurchasableLevels(Data.OwnedCurrency);
			_slider.lowValue = 1;
			_slider.RegisterCallback<ChangeEvent<int>>(UpdatePrice);
			var costIcon = ItemFactory.Currency(_currency, 0);
			costIcon.GetViewModel().DrawIcon(_costIcon);
			_slider.value = 1;
			_itemPrice.text = _battlePassData.GetPriceForBuying(1).ToString();
			return base.OnScreenOpen(reload);
		}

		private void UpdatePrice(ChangeEvent<int> evt)
		{
			var val = _slider.value;
			_itemPrice.text = _battlePassData.GetPriceForBuying((uint) val).ToString();
		}

		private void CloseRequested()
		{
			_services.UIService.CloseScreen<BuyBattlepassLevelPopupPresenter>();
		}

		private void OnBuyButtonClicked()
		{
			Data.OnConfirm?.Invoke(_slider.value);
			CloseRequested();
		}
	}
}
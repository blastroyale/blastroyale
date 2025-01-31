using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters.ProductsBundle;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using QuickEye.UIToolkit;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{

	[UILayer(UILayer.Popup)]
	public class ProductsBundleBannerPresenter : UIPresenterData<ProductsBundleBannerPresenter.StateData>
	{

		private IGameServices _gameServices;
		private IGameDataProvider _dataProvider;

		private Cooldown _closeCooldown;
		private ScreenResult _result = ScreenResult.Close;

		private VisualElement _rewardDisplayTemplate;
		
		[Q("CloseButton")] private Button _closeButton;
		[Q("Blocker")] private VisualElement _blocker;

		[Q("MainReward")] private VisualElement _mainReward;
		[Q("BundleTitleText")] private LabelOutlined _bundleTitleText;
		[Q("BundleDescriptionText")] private LabelOutlined _bundleDescriptionText;
		[Q("BundleRewardsContainer")] private VisualElement _bundleRewardsContainer;

		[Q("TimeLeftText")] private Label _timeLeftText;
		[Q("PurchaseBundleButton")] private LocalizedButton _buyBundleButton;

		
		private void Awake()
		{
			_gameServices = MainInstaller.ResolveServices();
			_dataProvider = MainInstaller.ResolveData();
		}


		protected override void QueryElements()
		{
			InitializeBanner();
		}



		private void InitializeBanner()
		{
			_blocker.RegisterCallback<PointerDownEvent>(ClickedOutsideBanner);
			_closeButton.clicked += () => CloseBannerPopup();
			_closeCooldown = new Cooldown(TimeSpan.FromSeconds(2));
			_gameServices.IAPService.PurchaseFinished += IAPServiceOnPurchaseFinished;

			ResolvePurchaseButtons();
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_closeCooldown.Trigger();
			
			var bundle = _gameServices.IAPService.AvailableGameProductBundles
				.FirstOrDefault(product => product.Name == Data.BundleId);

			if (bundle == null)
			{
				FLog.Error($"Couldn't find any bundle with BundleId: {Data.BundleId}}}");
			}

			LoadBundleContent(bundle);
			
			return UniTask.CompletedTask;
		}

		private void LoadBundleContent(GameProductsBundle gameProductsBundle)
		{
			_bundleRewardsContainer.Clear();

			
			var playerBundleTimeLeft = _gameServices.ProductsBundleService.GetBundlePurchaseTimeExpireAt(Data?.BundleId).Value;

			_timeLeftText.ShowCooldown(playerBundleTimeLeft, true, () => CloseBannerPopup());
			_buyBundleButton.text = gameProductsBundle.Bundle.UnityIapProduct().metadata.localizedPriceString;

			foreach (var bundleProduct in gameProductsBundle.BundleProducts)
			{
				_bundleRewardsContainer.Add(new RewardDisplayContainerElement().SetupContainer(bundleProduct));
			}
		}
		
		private void ClickedOutsideBanner(PointerDownEvent evt)
		{
			if (_closeCooldown.IsCooldown()) return;

			CloseBannerPopup();
		}

		private void CloseBannerPopup(ScreenResult result = ScreenResult.Close)
		{
			_result = result;
			_gameServices.UIService.CloseScreen<ProductsBundleBannerPresenter>(false).Forget();
		}


		protected override UniTask OnScreenClose()
		{
			_gameServices.IAPService.PurchaseFinished -= IAPServiceOnPurchaseFinished;
			Data?.OnClose?.Invoke(_result);

			return UniTask.CompletedTask;
		}

		public void ResolvePurchaseButtons()
		{

			var bundle = _gameServices.ProductsBundleService.GetGameProductBundle(Data.BundleId);
			
			if (_dataProvider.PlayerStoreDataProvider.HasPurchasedProductsBundle(bundle.Name))
			{
				_buyBundleButton.SetDisplay(false);
				return;
			}
			
			_buyBundleButton.clicked += () => CloseBannerPopup(ScreenResult.BuyBundleRealMoney);
		}

		private void IAPServiceOnPurchaseFinished(string itemId, ItemData data, bool success, IUnityStoreService.PurchaseFailureData reason)
		{
			if (!success) return;
			
			CloseBannerPopup();
		}
		
	
		//Enum-State Datas
		public enum ScreenResult
		{
			Close,
			BuyBundleRealMoney,
			BuyBundleInGame,
		}

		public class StateData
		{
			public string BundleId;
			public Action<ScreenResult> OnClose;
		}
	}
}

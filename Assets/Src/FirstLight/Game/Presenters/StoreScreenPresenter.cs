using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.UiService;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine.Purchasing;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Manages the IAP store.
	/// </summary>
	[LoadSynchronously]
	public class StoreScreenPresenter : UiToolkitPresenterData<StoreScreenPresenter.StateData>
	{
		// TODO: Read from playfab
		private const string ITEM_RARE_ID = "com.firstlight.blastroyale.core.rare";
		private const string ITEM_EPIC_ID = "com.firstlight.blastroyale.core.epic";
		private const string ITEM_LEGENDARY_ID = "com.firstlight.blastroyale.core.legendary";

		public struct StateData
		{
			public Action IapProcessingFinished;
			public Action<string> OnPurchaseItem;
			public IGameUiService UiService;
			
			public Action OnHomeClicked;
			public Action OnBackClicked;
		}

		private IGameServices _gameServices;

		private VisualElement _blocker;
		private ScreenHeaderElement _header;

		private readonly Queue<ItemData> _pendingRewards = new();

		private void Awake()
		{
			_gameServices = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements(VisualElement root)
		{
			_blocker = root.Q("Blocker").Required();

			_header = root.Q<ScreenHeaderElement>("Header").Required();
			_header.backClicked += Data.OnBackClicked;
			_header.homeClicked += Data.OnHomeClicked;

			SetupItem("ItemRare", ITEM_RARE_ID, "rare_core");
			SetupItem("ItemEpic", ITEM_EPIC_ID, "epic_core");
			SetupItem("ItemLegendary", ITEM_LEGENDARY_ID, "legendary_core");
		}

		protected override void SubscribeToEvents()
		{
			_gameServices.MessageBrokerService.Subscribe<OpenedCoreMessage>(OnCoresOpened);
			_gameServices.MessageBrokerService.Subscribe<IAPPurchaseFailedMessage>(OnPurchaseFailed);
		}

		[Button]
		private void OnPurchaseFailed(IAPPurchaseFailedMessage msg)
		{
			Data.IapProcessingFinished();

			_blocker.style.display = DisplayStyle.None;
			if (msg.Reason is PurchaseFailureReason.UserCancelled or PurchaseFailureReason.PaymentDeclined) return;

#if UNITY_EDITOR
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.UITShared.ok,
				ButtonOnClick = () => _gameServices.GenericDialogService.CloseDialog()
			};

			_gameServices.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, 
				string.Format(ScriptLocalization.UITStore.iap_error, msg.Reason.ToString()), false, confirmButton);
#else
			var button = new AlertButton
			{
				Style = AlertButtonStyle.Positive,
				Text = ScriptLocalization.UITShared.ok
			};

			NativeUiService.ShowAlertPopUp(false, ScriptLocalization.General.ErrorGeneric, msg.Reason.ToString(),
				button);
#endif
		}

		private void OnCoresOpened(OpenedCoreMessage msg)
		{
			Data.IapProcessingFinished();
			_gameServices.GameUiService.OpenScreenAsync<RewardsScreenPresenter, RewardsScreenPresenter.StateData>(new RewardsScreenPresenter.StateData()
			{
				ParentItem = msg.Core,
				Items = msg.Results,
				FameRewards = false,
				OnFinish = () =>
				{
					_gameServices.GameUiService.OpenScreenAsync<StoreScreenPresenter, StateData>(Data);
				}
			});
		}

		protected override void UnsubscribeFromEvents()
		{
			_gameServices.MessageBrokerService.UnsubscribeAll(this);
		}

		private void BuyItem(string id)
		{
			_blocker.style.display = DisplayStyle.Flex;
			Data.OnPurchaseItem(id);
		}

		private void SetupItem(string uiId, string storeId, string localizationPostfix)
		{
			var product = _gameServices.IAPService.Products.First(item => item.definition.id == storeId);

			var button = Root.Q<Button>(uiId);
			var priceLabel = button.Q<Label>("Price");
			var infoButton = button.Q<Button>("InfoButton");

			var backButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.Back,
				ButtonOnClick = _gameServices.GenericDialogService.CloseDialog
			};
			
			button.clicked += () => { BuyItem(storeId); };
			infoButton.clicked += () =>
			{
				_gameServices.GenericDialogService.OpenButtonDialog(LocalizationManager.GetTranslation ("UITStore/" + localizationPostfix),
				                                                    LocalizationManager.GetTranslation ("UITStore/description_" + localizationPostfix),
				                                                    false, backButton);
			};
			priceLabel.text = product.metadata.localizedPriceString;
		}
	}
}
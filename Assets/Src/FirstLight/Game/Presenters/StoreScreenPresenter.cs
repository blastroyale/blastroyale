using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

		private readonly Queue<KeyValuePair<UniqueId,Equipment>> _pendingRewards = new();

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

			SetupItem("ItemRare", ITEM_RARE_ID);
			SetupItem("ItemEpic", ITEM_EPIC_ID);
			SetupItem("ItemLegendary", ITEM_LEGENDARY_ID);
		}

		protected override void SubscribeToEvents()
		{
			_gameServices.MessageBrokerService.Subscribe<IAPPurchaseCompletedMessage>(OnPurchaseCompleted);
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

		private void OnPurchaseCompleted(IAPPurchaseCompletedMessage msg)
		{
			Data.IapProcessingFinished();

			_pendingRewards.Clear();

			foreach (var equipment in msg.Rewards)
			{
				_pendingRewards.Enqueue(equipment);
			}

			TryShowNextReward();
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

		private async void TryShowNextReward()
		{
			// Keep showing/dismissing reward dialogs recursively, until all have been shown
			if (Data.UiService.HasUiPresenter<EquipmentRewardDialogPresenter>())
			{
				Data.UiService.CloseUi<EquipmentRewardDialogPresenter>();

				await Task.Delay(GameConstants.Visuals.REWARD_POPUP_CLOSE_MS);
			}

			if (!_pendingRewards.TryDequeue(out var reward))
			{
				return;
			}

			var data = new EquipmentRewardDialogPresenter.StateData()
			{
				ConfirmClicked = TryShowNextReward,
				Equipment = reward.Value,
				EquipmentId = reward.Key
			};

			var popup = await Data.UiService.OpenUiAsync<EquipmentRewardDialogPresenter, EquipmentRewardDialogPresenter.StateData>(data);
			popup.InitEquipment();
		}

		private void SetupItem(string uiId, string storeId)
		{
			var product = _gameServices.IAPService.Products.First(item => item.definition.id == storeId);

			var button = Root.Q<Button>(uiId);
			var priceLabel = button.Q<Label>("Price");

			button.clicked += () => { BuyItem(storeId); };
			priceLabel.text = product.metadata.localizedPriceString;
		}
	}
}
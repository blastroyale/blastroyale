using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
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
			public Action BackClicked;
			public Action IapProcessingFinished;
			public Action<string> OnPurchaseItem;
			public IGameUiService UiService;
		}

		private IGameServices _gameServices;

		private VisualElement _blocker;

		private readonly Queue<Equipment> _pendingRewards = new();

		private void Awake()
		{
			_gameServices = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements(VisualElement root)
		{
			_blocker = root.Q("Blocker").Required();

			root.Q<Button>("BackButton").clicked += Data.BackClicked;

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
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = () => _gameServices.GenericDialogService.CloseDialog()
			};

			_gameServices.GenericDialogService.OpenDialog(
				string.Format(ScriptLocalization.General.IapError, msg.Reason.ToString()), false, confirmButton);
#else
			var button = new AlertButton
			{
				Style = AlertButtonStyle.Positive,
				Text = ScriptLocalization.General.OK
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

		private void TryShowNextReward()
		{
			// Keep showing/dismissing the battle pass generic reward dialog recursively, until all have been shown
			if (Data.UiService.HasUiPresenter<BattlepassRewardDialogPresenter>())
			{
				Data.UiService.CloseUi<BattlepassRewardDialogPresenter>();
			}

			if (!_pendingRewards.TryDequeue(out var reward))
			{
				_blocker.style.display = DisplayStyle.None;
				return;
			}

			var data = new BattlepassRewardDialogPresenter.StateData()
			{
				ConfirmClicked = TryShowNextReward,
				Reward = reward
			};

			Data.UiService
				.OpenUiAsync<BattlepassRewardDialogPresenter, BattlepassRewardDialogPresenter.StateData>(data);
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
using System.ComponentModel;
using FirstLight.FLogger;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;

public partial class SROptions
{
	[Category("IAP")]
	public void PurchaseRareCore()
	{
		PurchaseItem("com.firstlight.blastroyale.core.rare");
	}

	[Category("IAP")]
	public void PurchaseEpicCore()
	{
		PurchaseItem("com.firstlight.blastroyale.core.epic");
	}

	[Category("IAP")]
	public void PurchaseLegendaryCore()
	{
		PurchaseItem("com.firstlight.blastroyale.core.legendary");
	}

	private void PurchaseItem(string id)
	{
		var iapService = MainInstaller.Resolve<IGameServices>().IAPService;
		var messageBrokerService = MainInstaller.Resolve<IGameServices>().MessageBrokerService;

		if (!iapService.Initialized.Value)
		{
			FLog.Error("IAP Not initialized");
			return;
		}

		messageBrokerService.Unsubscribe<IAPPurchaseCompletedMessage>(OnPurchaseCompleted);
		messageBrokerService.Unsubscribe<IAPPurchaseFailedMessage>(OnPurchaseFailed);

		messageBrokerService.Subscribe<IAPPurchaseCompletedMessage>(OnPurchaseCompleted);
		messageBrokerService.Subscribe<IAPPurchaseFailedMessage>(OnPurchaseFailed);

		iapService.BuyProduct(id);
	}

	private void OnPurchaseCompleted(IAPPurchaseCompletedMessage message)
	{
		FLog.Info("DBG IAP purchase completed");

		foreach (var reward in message.Rewards)
		{
			FLog.Info($"DBG Purchased item: {reward.ToString()}");
		}
	}

	private void OnPurchaseFailed(IAPPurchaseFailedMessage message)
	{
		FLog.Error($"DBG IAP purchase failed: {message.Reason}");
	}
}
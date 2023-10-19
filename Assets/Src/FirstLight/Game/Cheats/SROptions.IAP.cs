using System;
using System.ComponentModel;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using UnityEngine;
using UnityEngine.Purchasing;

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
	
	[Category("IAP")]
	public void PurchaseBattlePass()
	{
		MainInstaller.Resolve<IGameServices>().CommandService.ExecuteCommand(new ActivateBattlepassCommand());
	}
	
	[Category("IAP")]
	public bool UseFakeStore {
		get => PlayerPrefs.GetInt("Debug.UseFakeStore", 1) == 1;
		set => PlayerPrefs.SetInt("Debug.UseFakeStore", value ? 1 : 0);
	}
	
	[Category("IAP")]
	public FakeStoreUIMode FakeStoreUI {
		get => Enum.Parse<FakeStoreUIMode>(PlayerPrefs.GetString("Debug.FakeStoreUiMode", FakeStoreUIMode.Default.ToString())) ;
		set => PlayerPrefs.SetString("Debug.FakeStoreUiMode", value.ToString());
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

		messageBrokerService.Unsubscribe<RewardClaimedMessage>(OnPurchaseCompleted);
		messageBrokerService.Unsubscribe<IAPPurchaseFailedMessage>(OnPurchaseFailed);

		messageBrokerService.Subscribe<RewardClaimedMessage>(OnPurchaseCompleted);
		messageBrokerService.Subscribe<IAPPurchaseFailedMessage>(OnPurchaseFailed);

		iapService.BuyProduct(id);
	}

	private void OnPurchaseCompleted(RewardClaimedMessage message)
	{
		FLog.Info("DBG IAP purchase completed");
		FLog.Info($"DBG Purchased item: {message.Reward}");
	}

	private void OnPurchaseFailed(IAPPurchaseFailedMessage message)
	{
		FLog.Error($"DBG IAP purchase failed: {message.Reason}");
	}
}
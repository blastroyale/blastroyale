using System;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Nethereum.Util;
using Quantum;

public partial class SROptions
{
	[Category("Web3")]
	public void ClaimNoobBank()
	{
		var services = MainInstaller.Resolve<IWeb3Service>();
		services.Withdrawal(GameId.NOOB);
	}
	
	[Category("Web3")]
	public void TransferDialog()
	{
		var web3 = MainInstaller.Resolve<IWeb3Service>();
		var services = MainInstaller.ResolveServices();
		services.GenericDialogService.OpenInputDialog("Transfer 10 noob", "Send 10 noob tokens", "", new GenericDialogButton<string>()
		{
			ButtonText = "Send 10 noob tokens",
			ButtonOnClick = s =>
			{
				if (s.IsValidEthereumAddressHexFormat())
				{
					OnSend(s).Forget();
				}
				services.GenericDialogService.CloseDialog();
			}
		}, true);
	}

	private static async UniTaskVoid OnSend(string wallet)
	{
		var web3 = MainInstaller.Resolve<IWeb3Service>();
		var services = MainInstaller.ResolveServices();
		await services.UIService.OpenScreen<LoadingSpinnerScreenPresenter>();
		try
		{
			web3.Transfer(GameId.NOOB, wallet, 10).ContinueWith(() =>
			{
				web3.GetWeb3Currencies()[GameId.NOOB].TotalPredicted.Value -= 10; 
				services.UIService.CloseScreen<LoadingSpinnerScreenPresenter>();
				services.InGameNotificationService.QueueNotification("Transfer completed");
			}).Forget();
		}
		catch (Exception e)
		{
			await services.UIService.CloseScreen<LoadingSpinnerScreenPresenter>();
			throw e;
		}
	}
}
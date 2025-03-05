using Cysharp.Threading.Tasks;
using FirstLight.Game.UIElements;
using FirstLight.Game.UIElements.Kit;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using I2.Loc;
using QuickEye.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Shows an invite popup for party or match invites.
	/// </summary>
	[UILayer(UILayer.Popup, false)]
	public class Web3PopupPresenter : UIPresenter
	{
		[Q("ConnectWalletButton")] public KitButton ConnectWallet;
		[Q("Popup")] public GenericPopupElement Popup;
		[Q("FlgWalletAddress")] public Label FlgWallet;
		[Q("CopyFlgWalletButton")] public KitButton CopyFlgWalletButton;

		protected override void QueryElements()
		{
			var web3 = MainInstaller.ResolveWeb3();
			CopyFlgWalletButton.clicked += () =>
			{
				UIUtils.SaveToClipboard(web3.CurrentWallet);
				MainInstaller.ResolveServices().InGameNotificationService.QueueNotification(ScriptLocalization.UITShared.code_copied);
			};
			FlgWallet.text = "";
			Popup.CloseClicked += () => MainInstaller.ResolveServices().UIService.CloseScreen<Web3PopupPresenter>().Forget();
			
			ConnectWallet.clicked += () =>
			{
				//Connect().Forget();
				ConnectWallet.SetEnabled(false);
			};
			UpdateLinks();
		}

		private void UpdateLinks()
		{
			var web3 = MainInstaller.ResolveWeb3();
			FlgWallet.text = web3.CurrentWallet;
		}

		private void Connect()
		{
			ConnectWallet.SetEnabled(false);
			FlgWallet.text = "Connecting...";
			Debug.Log("Trying to connect");
			
			// external wallet connection flow
			
			ConnectWallet.SetEnabled(true);
		}


	}


}
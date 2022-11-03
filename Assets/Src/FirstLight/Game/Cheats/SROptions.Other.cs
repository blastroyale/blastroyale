using System.ComponentModel;
using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;

public partial class SROptions
{
	[Category("Other")]
	public void ClearRemoteTextureCache()
	{
		MainInstaller.Resolve<IGameServices>().RemoteTextureService.ClearCache();
	}
	
	[Category("Other")]
	public void OpenButtonDialog()
	{
		var button = new GenericDialogButton
		{
			ButtonText = "Confirm",
			ButtonOnClick = CallbackConfirm
		};
		
		MainInstaller.Resolve<IGameServices>().GenericDialogService.OpenButtonDialog("THIS IS TITLE!", "THE PLAYERS WON'T READ ANY OF THIS!", 
			true, button, CallbackClose);

		void CallbackConfirm()
		{
			FLog.Warn("Confirm callback.");
		}
		
		void CallbackClose()
		{
			FLog.Warn("Close callback.");
		}
	}
}
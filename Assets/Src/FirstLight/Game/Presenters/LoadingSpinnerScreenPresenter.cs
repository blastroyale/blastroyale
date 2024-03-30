using FirstLight.Modules.UIService.Runtime;
using FirstLight.UIService;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter handles showing generic loading spinner screen
	/// </summary>	
	[UILayer(UIService2.UILayer.Foreground)]
	public class LoadingSpinnerScreenPresenter : UIPresenter2
	{
		protected override void QueryElements()
		{
		}
	}
}
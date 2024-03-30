using FirstLight.Modules.UIService.Runtime;
using FirstLight.UIService;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter handles showing generic loading spinner screen
	/// </summary>	
	[UILayer(UIService.UIService.UILayer.Foreground)]
	public class LoadingSpinnerScreenPresenter : UIPresenter
	{
		protected override void QueryElements()
		{
		}
	}
}
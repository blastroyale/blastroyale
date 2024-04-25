using FirstLight.UIService;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter handles showing generic loading spinner screen
	/// </summary>	
	[UILayer(UILayer.Loading)]
	public class LoadingSpinnerScreenPresenter : UIPresenter
	{
		protected override void QueryElements()
		{
		}
	}
}
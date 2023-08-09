using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Presenter for the swipe transition 
	/// </summary>
	public class SwipeScreenPresenter : UiToolkitPresenter
	{
		private VisualElement _swipeParent;

		protected override void QueryElements(VisualElement root)
		{
			_swipeParent = root.Q<VisualElement>("SwipeParent").Required();
		}

		protected override Task OnClosed()
		{
			_swipeParent.AddToClassList("hidden-end");
			return base.OnClosed();
		}

		protected override void OnTransitionsReady()
		{
			_swipeParent.RemoveFromClassList("hidden-start");
		}
	}
}
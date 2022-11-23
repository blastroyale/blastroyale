using System.Collections;
using System.Threading.Tasks;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	public class SwipeScreenPresenter : UiToolkitPresenter
	{
		private VisualElement _swipeParent;
		
		protected override void QueryElements(VisualElement root)
		{
			_swipeParent = root.Q<VisualElement>("SwipeParent").Required();	
		}
		
		protected override void OnOpened()
		{
			base.OnOpened();

			StartCoroutine(MakeVisible());
		}

		protected override Task OnClosed()
		{
			_swipeParent.AddToClassList("hidden-end");
			return base.OnClosed();
		}

		private IEnumerator MakeVisible()
		{
			yield return new WaitForEndOfFrame();
			
			_swipeParent.RemoveFromClassList("hidden-start");
		}
	}
}
using Cysharp.Threading.Tasks;
using FirstLight.Game.Utils;
using FirstLight.Modules.UIService.Runtime;
using FirstLight.UIService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Presenter for the swipe transition 
	/// </summary>
	[UILayer(UIService2.UILayer.Foreground)]
	public class SwipeTransitionScreenPresenter : UIPresenter2
	{
		private VisualElement _swipeParent;
		private bool _transitionFinished;

		protected override void QueryElements()
		{
			_swipeParent = Root.Q<VisualElement>("SwipeParent").Required();
			_swipeParent.RegisterCallback<TransitionEndEvent, SwipeTransitionScreenPresenter>((_, p) => p._transitionFinished = true, this);
			_swipeParent.RegisterCallback<TransitionStartEvent, SwipeTransitionScreenPresenter>((_, p) => p._transitionFinished = false, this);
		}

		protected override async UniTask OnScreenOpen(bool reload)
		{
			_swipeParent.RemoveFromClassList("hidden-start");
			await UniTask.WaitUntil(() => _transitionFinished);
		}

		protected override async UniTask OnScreenClosed()
		{
			_swipeParent.AddToClassList("hidden-end");
			await UniTask.WaitUntil(() => _transitionFinished);
		}
	}
}
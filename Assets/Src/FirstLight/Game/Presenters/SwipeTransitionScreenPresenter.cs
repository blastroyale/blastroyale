using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
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
		//private bool _transitionFinished;

		protected override void QueryElements()
		{
			_swipeParent = Root.Q<VisualElement>("SwipeParent").Required();
			// _swipeParent.RegisterCallback<TransitionEndEvent, SwipeTransitionScreenPresenter>((e, p) =>
			// {
			// 	FLog.Info("UIServiceSwipe", "Transition End");
			// 	p._transitionFinished = true;
			// }, this);
		}

		protected override async UniTask OnScreenOpen(bool reload)
		{
			FLog.Info("UIServiceSwipe", "Swipe Open Start");
			_swipeParent.RemoveFromClassList("hidden-start");
			await UniTask.WaitForSeconds(0.5f);
			// _transitionFinished = false;
			// await UniTask.WaitUntil(() => _transitionFinished);
			FLog.Info("UIServiceSwipe", "Swipe Open End");
		}

		protected override async UniTask OnScreenClose()
		{
			FLog.Info("UIServiceSwipe", "Swipe Close Start");
			_swipeParent.AddToClassList("hidden-end");
			await UniTask.WaitForSeconds(0.5f);
			// await UniTask.NextFrame();
			// _transitionFinished = false;
			// await UniTask.WaitUntil(() => _transitionFinished);
			FLog.Info("UIServiceSwipe", "Swipe Close End");
		}
	}
}
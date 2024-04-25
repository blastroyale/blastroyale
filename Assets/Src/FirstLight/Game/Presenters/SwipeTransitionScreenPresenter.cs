using Cysharp.Threading.Tasks;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Presenter for the swipe transition 
	/// </summary>
	[UILayer(UILayer.Loading)]
	public class SwipeTransitionScreenPresenter : UIPresenter
	{
		private VisualElement _swipeParent;
		private bool _transitionRunning;

		protected override void QueryElements()
		{
			_swipeParent = Root.Q<VisualElement>("SwipeParent").Required();
			
			_swipeParent.RegisterCallback<TransitionStartEvent>(e =>
			{
				_transitionRunning = true;
			});
			
			_swipeParent.RegisterCallback<TransitionEndEvent>(e =>
			{
				_transitionRunning = false;
			});
		}

		protected override async UniTask OnScreenOpen(bool reload)
		{
			_swipeParent.RemoveFromClassList("hidden-start");

			await UniTask.WaitUntil(() => !_transitionRunning);
		}

		protected override async UniTask OnScreenClose()
		{
			_swipeParent.AddToClassList("hidden-end");

			await UniTask.WaitUntil(() => !_transitionRunning);
		}
	}
}
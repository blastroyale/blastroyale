using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.UIService;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Presenter for the swipe transition 
	/// </summary>
	public class SwipeScreenPresenter : UIPresenter2
	{
		private VisualElement _swipeParent;
		public bool TransitionFinished { get; private set; }
		
		
		protected override void QueryElements()
		{
			_swipeParent = Root.Q<VisualElement>("SwipeParent").Required();
			_swipeParent.RegisterCallback<TransitionEndEvent>(_ => TransitionFinished = true);
			_swipeParent.RegisterCallback<TransitionStartEvent>(_ => TransitionFinished = false);
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_swipeParent.RemoveFromClassList("hidden-start");
			
			return base.OnScreenOpen(reload);
		}

		public async UniTask WaitTransition()
		{
			await UniTask.WaitUntil(() => TransitionFinished);
		}

		/// <summary>
		/// Starts the swipe transition.
		/// If this method is awaited will wait the transition to be where
		/// the whole screen is covered
		/// </summary>
		public static async UniTask StartSwipe()
		{
			var service = MainInstaller.ResolveServices();
			var swipe = await service.UIService.OpenScreen<SwipeScreenPresenter>();
			await swipe.WaitTransition();
		}

		/// <summary>
		/// Finishes the transition. If waited, will wait the whole transition to be finished
		/// and the whole screen to be freed.
		/// </summary>
		public static async UniTask Finish()
		{
			
			
			var service = MainInstaller.ResolveServices();
			await service.UIService.CloseScreen<SwipeScreenPresenter>();
			
			// if (!service.GameUiService.HasUiPresenter<SwipeScreenPresenter>()) return;
			// var ui = service.GameUiService.GetUi<SwipeScreenPresenter>();
			// if (ui == null) return;
			// ui._swipeParent.AddToClassList("hidden-end");
			// await ui.WaitTransition();
			// // concurrency check
			// ui = service.GameUiService.GetUi<SwipeScreenPresenter>();
			// if (ui == null || !service.GameUiService.IsOpen<SwipeScreenPresenter>()) return;
			// await service.GameUiService.CloseUi<SwipeScreenPresenter>(true);
		}
	}
}
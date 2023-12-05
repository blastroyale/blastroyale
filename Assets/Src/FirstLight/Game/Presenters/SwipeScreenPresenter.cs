using System;
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
		public bool TransitionFinished { get; private set; }
		
		
		protected override void QueryElements(VisualElement root)
		{
			_swipeParent = root.Q<VisualElement>("SwipeParent").Required();
			_swipeParent.RegisterCallback<TransitionEndEvent>(_ => TransitionFinished = true);
			_swipeParent.RegisterCallback<TransitionStartEvent>(_ => TransitionFinished = false);
		}

		public async Task WaitTransition()
		{
			while (!TransitionFinished) await Task.Delay(10);
		}

		protected override void OnTransitionsReady()
		{
			_swipeParent.RemoveFromClassList("hidden-start");
		}

		/// <summary>
		/// Starts the swipe transition.
		/// If this method is awaited will wait the transition to be where
		/// the whole screen is covered
		/// </summary>
		public static async Task StartSwipe()
		{
			var service = MainInstaller.ResolveServices();
			var swipe = await service.GameUiService.OpenUiAsync<SwipeScreenPresenter>();
			await swipe.WaitTransition();
		}

		/// <summary>
		/// Finishes the transition. If waited, will wait the whole transition to be finished
		/// and the whole screen to be freed.
		/// </summary>
		public static async Task Finish()
		{
			var service = MainInstaller.ResolveServices();
			if (!service.GameUiService.HasUiPresenter<SwipeScreenPresenter>()) return;
			var ui = service.GameUiService.GetUi<SwipeScreenPresenter>();
			if (ui == null) return;
			ui._swipeParent.AddToClassList("hidden-end");
			await ui.WaitTransition();
			// concurrency check
			ui = service.GameUiService.GetUi<SwipeScreenPresenter>();
			if (ui == null || !service.GameUiService.IsOpen<SwipeScreenPresenter>()) return;
			await service.GameUiService.CloseUi<SwipeScreenPresenter>(true);
		}
	}
}
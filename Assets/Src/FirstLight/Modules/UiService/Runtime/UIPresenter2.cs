using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace FirstLight.UIService
{
	public abstract class UIPresenter2 : MonoBehaviour
	{
		[SerializeField, Required] private UIDocument _document;

		protected VisualElement Root { private set; get; }

		internal UIService2.UILayer CurrentLayer { set; get; }
		internal object Data { set; get; }

		private readonly List<UIView2> _views = new ();
		private bool _enableTriggered;

		private void OnEnable()
		{
			// Support for live reload, we don't trigger on first enable as that is covered by the UIService
			if (!_enableTriggered)
			{
				_enableTriggered = true;
				return;
			}

			OnScreenOpenedInternal(true).Forget();
		}

		/// <summary>
		/// Adds a <see cref="UIView2"/> view to the list of views, and handles it's lifecycle events.
		/// </summary>
		public void AddView(VisualElement element, UIView2 view)
		{
			_views.Add(view);
			view.Attached(element);
		}

		internal async UniTask OnScreenOpenedInternal(bool reload = false)
		{
			// Assert.AreEqual(typeof(T), Data.GetType(), $"Screen opened with incorrect data type {Data.GetType()} instead of {typeof(T)}");

			Root = _document.rootVisualElement.Q(UIService2.ID_ROOT);
			QueryElements();

			if (reload)
			{
				// We still skip a frame there so OnScreenOpened is always called on the second frame
				await UniTask.NextFrame();
			}
			else
			{
				Root.EnableInClassList(UIService2.CLASS_HIDDEN, true);

				// We need to wait for next frame to trigger the CLASS_HIDDEN transition
				await UniTask.NextFrame();

				Root.EnableInClassList(UIService2.CLASS_HIDDEN, false);
			}

			await OnScreenOpen(reload);
		}

		internal async UniTask OnScreenClosedInternal()
		{
			Root.EnableInClassList(UIService2.CLASS_HIDDEN, true);

			await OnScreenClosed();
		}

		protected abstract void QueryElements();

		protected virtual UniTask OnScreenOpen(bool reload)
		{
			return UniTask.CompletedTask;
		}

		protected virtual UniTask OnScreenClosed()
		{
			return UniTask.CompletedTask;
		}
	}
}
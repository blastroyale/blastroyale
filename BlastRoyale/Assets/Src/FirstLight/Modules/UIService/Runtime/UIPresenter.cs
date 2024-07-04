using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.UIService
{
	[RequireComponent(typeof(UIDocument))]
	public abstract class UIPresenter : MonoBehaviour
	{
		// TODO: Add Required attribute when no more legacy screens are present
		[SerializeField] private UIDocument _document;

		public VisualElement Root { private set; get; }

		internal UILayer Layer { set; get; }
		internal object Data { set; get; }

		private readonly List<UIView> _views = new ();
		private bool _enableTriggered;
		private CancellationTokenSource _cancellationTokenSource;

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

		private void OnValidate()
		{
			if (_document != null)
			{
				// Not really necessary as we set this at runtime but just so it's clear in the editor
				_document.sortingOrder = (int) (GetType().GetAttribute<UILayerAttribute>()?.Layer ?? UILayer.Default);
			}
		}

		/// <summary>
		/// Adds a <see cref="UIView"/> view to the list of views, and handles it's lifecycle events.
		/// </summary>
		public void AddView(VisualElement element, UIView view)
		{
			_views.Add(view);
			view.AttachedInternal(element, this);
		}

		internal async UniTask OnScreenOpenedInternal(bool reload = false)
		{
			_cancellationTokenSource = new CancellationTokenSource();
			// Assert.AreEqual(typeof(T), Data.GetType(), $"Screen opened with incorrect data type {Data.GetType()} instead of {typeof(T)}");

			if (_document != null) // TODO: Only here to support legacy lobby screen, remove when it's UITK
			{
				Root = _document.rootVisualElement.Q(UIService.ID_ROOT);
				QueryElements();

				if (reload)
				{
					// We still skip a frame there so OnScreenOpened is always called on the second frame
					await UniTask.NextFrame();
				}
				else
				{
					Root.EnableInClassList(UIService.CLASS_HIDDEN, true);

					// We need to wait for next frame to trigger the CLASS_HIDDEN transition
					await UniTask.NextFrame();

					Root.EnableInClassList(UIService.CLASS_HIDDEN, false);
				}

				foreach (var view in _views)
				{
					view.OnScreenOpen(reload);
				}
			}

			await OnScreenOpen(reload);
		}

		internal async UniTask OnScreenClosedInternal()
		{
			_cancellationTokenSource.Cancel();
			if (_document != null)
			{
				Root.EnableInClassList(UIService.CLASS_HIDDEN, true);

				foreach (var view in _views)
				{
					view.OnScreenClose();
				}
			}

			await OnScreenClose();
		}

		/// <summary>
		/// A cancelation toke with the lifetime of the screen
		/// </summary>
		public CancellationToken GetCancellationTokenOnClose() => _cancellationTokenSource.Token;

		protected abstract void QueryElements();

		protected virtual UniTask OnScreenOpen(bool reload)
		{
			return UniTask.CompletedTask;
		}

		protected virtual UniTask OnScreenClose()
		{
			return UniTask.CompletedTask;
		}
	}
}
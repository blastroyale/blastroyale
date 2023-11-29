using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Utils;
using FirstLight.Game.Services;
using FirstLight.UiService;
using UnityEngine;
using UnityEngine.UIElements;
using Vector3 = UnityEngine.Vector3;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Tutorial utils
	/// </summary>
	public class TutorialUtilsScreenPresenter : UiToolkitPresenter
	{
		private const string BLOCKER_ELEMENT_STYLE = "blocker-element-blocker";
		private const string HIGHLIGHT_ELEMENT_STYLE = "highlight-element";
		private const string PARENT_ELEMENT_STYLE = "blocker-root";
		
		private const float CIRCLE_DEFAULT_SIZE = 32;
		private const float SQUARE_DEFAULT_SIZE = 512;

		private float _initialScale;
		private float _highlightedScale;

		private IGameServices _services;

		private VisualElement _blockerElementRight;
		private VisualElement _blockerElementLeft;
		private VisualElement _blockerElementBottom;
		private VisualElement _blockerElementTop;

		private VisualElement _highlighterElement;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements(VisualElement root)
		{
			Root.AddToClassList(PARENT_ELEMENT_STYLE);
			root.SetupClicks(_services);
		}

		/// <summary>
		/// Blocks full screen
		/// </summary>
		public void BlockFullScreen()
		{
			_blockerElementRight = new VisualElement();
			_blockerElementRight.AddToClassList(BLOCKER_ELEMENT_STYLE);
			Root.Add(_blockerElementRight);
			SetBlockerValues(_blockerElementRight,
				Root.resolvedStyle.height * 2,
				Root.resolvedStyle.width * 2,
				0,
				0);

			_blockerElementLeft = new VisualElement();
			Root.Add(_blockerElementLeft);

			_blockerElementBottom = new VisualElement();
			Root.Add(_blockerElementBottom);

			_blockerElementTop = new VisualElement();
			Root.Add(_blockerElementTop);
		}

		/// <summary>
		/// Creates blocker elements around ui element object on the <typeparamref name="T"/> presenter.
		/// </summary>
		public async UniTask BlockAround<T>(string className = null, string elementName = null)
			where T : UiPresenter, IUIDocumentPresenter
		{
			await UniTask.WaitUntil(() => _uiService.HasUiPresenter<T>());
			var doc = _uiService.GetUi<T>().Document;
			var element = doc.rootVisualElement.Q(elementName, className);
			
			CreateBlockers(element);
		}

		/// <summary>
		/// Removes blockers
		/// </summary>
		/// <returns></returns>
		public void Unblock()
		{
			_blockerElementRight.RemoveFromHierarchy();
			_blockerElementLeft.RemoveFromHierarchy();
			_blockerElementBottom.RemoveFromHierarchy();
			_blockerElementTop.RemoveFromHierarchy();
		}

		/// <summary>
		/// Highligts the ui element object with passed class or name from T, with an animation of a circle shringking. 
		/// </summary>
		/// <param name="className"></param>
		/// <param name="elementName"></param>
		/// <param name="sizeMultiplier"></param>
		/// <typeparam name="T"></typeparam>
		/// <exception cref="Exception"></exception>
		public void Highlight<T>(string className = null, string elementName = null, float sizeMultiplier = 1)
			where T : UiPresenter, IUIDocumentPresenter
		{
			var doc = _uiService.GetUi<T>().Document;
			var element = doc.rootVisualElement.Q(elementName, className);
			
			CreateHighlight(element, sizeMultiplier);
		}

		/// <summary>
		///  Removes the highlight circle with an increasing animation. Then destroys the highligh element
		/// </summary>
		public void RemoveHighlight()
		{
			_highlighterElement.experimental.animation.Scale(_initialScale, GameConstants.Tutorial.TIME_HIGHLIGHT_FADE)
				.OnCompleted(DeleteHighLightElement);
		}

		private void DeleteHighLightElement()
		{
			Root.Remove(_highlighterElement);
		}

		private void CreateBlockers(VisualElement objElement)
		{
			_blockerElementRight = new VisualElement();
			_blockerElementRight.AddToClassList(BLOCKER_ELEMENT_STYLE);
			Root.Add(_blockerElementRight);
			SetBlockerValues(_blockerElementRight,
				Root.resolvedStyle.height * 2,
				Root.resolvedStyle.width,
				objElement.worldBound.y - Root.resolvedStyle.height,
				objElement.worldBound.x + objElement.worldBound.width);

			_blockerElementLeft = new VisualElement();
			_blockerElementLeft.AddToClassList(BLOCKER_ELEMENT_STYLE);
			Root.Add(_blockerElementLeft);
			SetBlockerValues(_blockerElementLeft,
				Root.resolvedStyle.height * 2,
				Root.resolvedStyle.width,
				objElement.worldBound.y - Root.resolvedStyle.height,
				objElement.worldBound.x - Root.worldBound.width);

			_blockerElementBottom = new VisualElement();
			_blockerElementBottom.AddToClassList(BLOCKER_ELEMENT_STYLE);
			Root.Add(_blockerElementBottom);
			SetBlockerValues(_blockerElementBottom, Root.resolvedStyle.height,
				objElement.resolvedStyle.width,
				objElement.worldBound.y + objElement.worldBound.height,
				objElement.worldBound.x);

			_blockerElementTop = new VisualElement();
			Root.Add(_blockerElementTop);
			_blockerElementTop.AddToClassList(BLOCKER_ELEMENT_STYLE);
			SetBlockerValues(_blockerElementTop, Root.resolvedStyle.height, objElement.resolvedStyle.width,
				objElement.worldBound.y - Root.worldBound.height, objElement.worldBound.x);
		}

		private void SetBlockerValues(VisualElement blocker, float height, float width, float top, float left)
		{
			blocker.style.height = height;
			blocker.style.width = width;
			blocker.style.top = top;
			blocker.style.left = left;
		}

		private void CreateHighlight(VisualElement objElement, float sizeMultiplier)
		{
			_highlighterElement = new VisualElement();

			Root.Add(_highlighterElement);
			_highlighterElement.AddToClassList(HIGHLIGHT_ELEMENT_STYLE);
			_highlighterElement.pickingMode = PickingMode.Ignore;
			_highlighterElement.SetDisplay(false);
			float objSize = objElement.resolvedStyle.width >= objElement.resolvedStyle.height
				? objElement.resolvedStyle.width
				: objElement
					.resolvedStyle.height;

			objSize *= sizeMultiplier;

			float circleHighlightingSize = objSize;
			
			_initialScale = Root.worldBound.width * 2 / CIRCLE_DEFAULT_SIZE;
			_highlightedScale = circleHighlightingSize / CIRCLE_DEFAULT_SIZE;
			_highlighterElement.style.top = objElement.worldBound.y - SQUARE_DEFAULT_SIZE / 2 + objElement.resolvedStyle.height / 2;
			_highlighterElement.style.left = objElement.worldBound.x - SQUARE_DEFAULT_SIZE / 2 + objElement.resolvedStyle.width / 2;
			_highlighterElement.style.scale = new Scale(new Vector3(_initialScale, _initialScale, 1));

			_highlighterElement.SetDisplay(true);
			_highlighterElement.experimental.animation.Scale(_highlightedScale, GameConstants.Tutorial.TIME_HIGHLIGHT_FADE);
		}
	}
}
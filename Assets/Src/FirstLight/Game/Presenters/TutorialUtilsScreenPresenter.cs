using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using FirstLight.Game.Utils;
using FirstLight.Game.Services;
using FirstLight.UiService;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Tutorial utils
	/// </summary>
	public class TutorialUtilsScreenPresenter : UiToolkitPresenterData<TutorialUtilsScreenPresenter.StateData>
	{
		public struct StateData
		{
		}

		private const string BLOCKER_ELEMENT_STYLE = "blocker-element-blocker";
		private const string HIGHLIGHT_ELEMENT_STYLE = "highlight-element";
		private const string PARENT_ELEMENT_STYLE = "blocker-root";

		private const float circleDefaultSize = 32;
		private const float squareDefaultSize = 512;

		private const int HIGHLIGHT_ANIM_TIME = 3;
		private const int HIGHLIGHT_ANIM_SCALE_VALUE = 3;

		private IGameServices _services;
		private VisualElement _root;

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
			_root = root;
			_root.AddToClassList(PARENT_ELEMENT_STYLE);
			_root.parent.AddToClassList(PARENT_ELEMENT_STYLE);
			root.pickingMode = PickingMode.Ignore;
			root.SetupClicks(_services);
		}

		public void BlockAround(UIDocument doc, string veClass)
		{
			doc.rootVisualElement.Query(className: veClass)
				.ForEach(CreateBlockers);
		}

		public void Unblock()
		{
			_root.Remove(_blockerElementRight);
			_root.Remove(_blockerElementLeft);
			_root.Remove(_blockerElementTop);
			_root.Remove(_blockerElementBottom);
		}

		public void HighlightElement(UIDocument doc, string veClass, float sizeMultiplier)
		{
			doc.rootVisualElement.Query(className: veClass)
				.ForEach(element => CreateHighlight(element, sizeMultiplier));
		}

		public void RemoveHighlight()
		{
			StartCoroutine(RemoveHighlightIE());
		}

		IEnumerator RemoveHighlightIE()
		{
			_highlighterElement.experimental.animation.Scale(HIGHLIGHT_ANIM_SCALE_VALUE, HIGHLIGHT_ANIM_TIME);
			yield return new WaitForSeconds(HIGHLIGHT_ANIM_TIME);
			_root.Remove(_highlighterElement);
		}

		private void CreateBlockers(VisualElement objElement)
		{
			_blockerElementRight = new VisualElement();
			_blockerElementRight.AddToClassList(BLOCKER_ELEMENT_STYLE);
			_root.Add(_blockerElementRight);
			SetBlockerValues(_blockerElementRight,
				_root.resolvedStyle.height * 2,
				_root.resolvedStyle.width,
				objElement.worldBound.y - _root.resolvedStyle.height,
				objElement.worldBound.x + objElement.worldBound.width);

			_blockerElementLeft = new VisualElement();
			_blockerElementLeft.AddToClassList(BLOCKER_ELEMENT_STYLE);
			_root.Add(_blockerElementLeft);
			SetBlockerValues(_blockerElementLeft,
				_root.resolvedStyle.height * 2,
				_root.resolvedStyle.width,
				objElement.worldBound.y - _root.resolvedStyle.height,
				objElement.worldBound.x - _root.worldBound.width);

			_blockerElementBottom = new VisualElement();
			_blockerElementBottom.AddToClassList(BLOCKER_ELEMENT_STYLE);
			_root.Add(_blockerElementBottom);
			SetBlockerValues(_blockerElementBottom, _root.resolvedStyle.height,
				objElement.resolvedStyle.width,
				objElement.worldBound.y + objElement.worldBound.height,
				objElement.worldBound.x);

			_blockerElementTop = new VisualElement();
			_root.Add(_blockerElementTop);
			_blockerElementTop.AddToClassList(BLOCKER_ELEMENT_STYLE);
			SetBlockerValues(_blockerElementTop, _root.resolvedStyle.height, objElement.resolvedStyle.width,
				objElement.worldBound.y - _root.worldBound.height, objElement.worldBound.x);
		}

		void SetBlockerValues(VisualElement blocker, float height, float width, float top, float left)
		{
			blocker.style.height = height;
			blocker.style.width = width;
			blocker.style.top = top;
			blocker.style.left = left;
		}

		private void CreateHighlight(VisualElement objElement, float sizeMultiplier)
		{
			_highlighterElement = new VisualElement();

			_root.Add(_highlighterElement);
			_highlighterElement.pickingMode = PickingMode.Ignore;

			float objSize = objElement.resolvedStyle.width >= objElement.resolvedStyle.height
				? objElement.resolvedStyle.width
				: objElement
					.resolvedStyle.height;
			
			objSize *= sizeMultiplier;
			
			float squareSize = squareDefaultSize * objSize / circleDefaultSize;
			
			_highlighterElement.style.width = squareSize;
			_highlighterElement.style.height = squareSize;
			_highlighterElement.style.top =
				objElement.worldBound.y - squareSize / 2 + objElement.resolvedStyle.height / 2;
			_highlighterElement.style.left =
				objElement.worldBound.x - squareSize / 2 + objElement.resolvedStyle.width / 2;

			_highlighterElement.AddToClassList(HIGHLIGHT_ELEMENT_STYLE);

			_highlighterElement.experimental.animation.Scale(1, HIGHLIGHT_ANIM_TIME);
		}
	}
}

using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays the base popup
	/// </summary>
	public class HighlighterElement : VisualElement
	{
		private const string HighlightElement = "highlighter-element";
		private const string HighlighterElementBlocker = "highlighter-element-blocker";

		private VisualElement _rootReference;
		private VisualElement _objElement;
		private VisualElement _objElementParent;

		private VisualElement _blockerElementRight;
		private VisualElement _blockerElementLeft;
		private VisualElement _blockerElementBottom;
		private VisualElement _blockerElementTop;

		private int _elementIndex;

		private VisualElement _highlightedElement;

		private StyleEnum<Position> _prevPositionType;
		private StyleLength _prevLeft;
		private StyleLength _prevTop;

		public HighlighterElement()
		{
		}

		/// <summary>
		/// Set the Element to highlight. Blocker/Dimmers will be generated around.
		/// </summary>
		/// <param name="objElement"></param>
		public void SetHighlightedElement(VisualElement objElement)
		{
			_objElement = objElement;
			AddToClassList(HighlightElement);
			this.RegisterCallback<GeometryChangedEvent>(DoHighlight);
			//DoJob(null);
		}

		/// <summary>
		/// Calling this method will remove the blockers from the hierarchy
		/// </summary>
		public void Revert()
		{
			_rootReference.Remove(this);
		}

		private void DoHighlight(GeometryChangedEvent evt)
		{
			_objElement.UnregisterCallback<GeometryChangedEvent>(DoHighlight);
			SetRootReference();
			CreateBlockers();
		}

		private void SetRootReference()
		{
			_rootReference = _objElement;
			while (true)
			{
				if (_rootReference.hierarchy.parent == null)
					break;
				_rootReference = _rootReference.hierarchy.parent;
			}
		}

		private void CreateBlockers()
		{
			_rootReference.hierarchy.Add(this);
			pickingMode = PickingMode.Ignore;

			_blockerElementRight = new VisualElement();
			_blockerElementRight.AddToClassList(HighlighterElementBlocker);
			hierarchy.Add(_blockerElementRight);
			SetBlockerValues(_blockerElementRight,
				_rootReference.resolvedStyle.height * 2,
				_rootReference.resolvedStyle.width,
				_objElement.worldBound.y - _rootReference.resolvedStyle.height,
				_objElement.worldBound.x + _objElement.worldBound.width);

			_blockerElementLeft = new VisualElement();
			_blockerElementLeft.AddToClassList(HighlighterElementBlocker);
			hierarchy.Add(_blockerElementLeft);
			SetBlockerValues(_blockerElementLeft,
				_rootReference.resolvedStyle.height * 2,
				_rootReference.resolvedStyle.width,
				_objElement.worldBound.y - _rootReference.resolvedStyle.height,
				_objElement.worldBound.x - _rootReference.worldBound.width);

			_blockerElementBottom = new VisualElement();
			_blockerElementBottom.AddToClassList(HighlighterElementBlocker);
			hierarchy.Add(_blockerElementBottom);
			SetBlockerValues(_blockerElementBottom, _rootReference.resolvedStyle.height,
				_objElement.resolvedStyle.width,
				_objElement.worldBound.y + _objElement.worldBound.height,
				_objElement.worldBound.x);

			_blockerElementTop = new VisualElement();
			hierarchy.Add(_blockerElementTop);
			_blockerElementTop.AddToClassList(HighlighterElementBlocker);
			SetBlockerValues(_blockerElementTop, _rootReference.resolvedStyle.height, _objElement.resolvedStyle.width,
				_objElement.worldBound.y - _rootReference.worldBound.height, _objElement.worldBound.x);
		}

		void SetBlockerValues(VisualElement blocker, float height, float width, float top, float left)
		{
			blocker.style.height = height;
			blocker.style.width = width;
			blocker.style.top = top;
			blocker.style.left = left;
		}
		
		public new class UxmlFactory : UxmlFactory<HighlighterElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
		}
	}
}
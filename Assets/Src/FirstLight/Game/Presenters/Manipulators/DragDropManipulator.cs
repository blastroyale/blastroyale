using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters.Manipulators
{
	/// <summary>
	/// Makes the element be able to get dragged/dropped
	/// Can specify if want to cancel other events of the element. Default is true.
	/// </summary>
	public class DragDropManipulator : Manipulator
	{
		public event Action<VisualElement> OnStartDrag;
		public event Action<VisualElement> OnEndDrag;
		
		/// <summary>
		/// Will only allow dragging inside the parent element
		/// </summary>
		public bool OnlyInsideParent = false;
		
		/// <summary>
		/// Cancel all event propagation when element is "draggable"
		/// </summary>
		public bool CancelEvents = true;
		
		private Vector2 _lastPos;
		private PickingMode _originalPickingMode;

		protected override void RegisterCallbacksOnTarget()
		{
			_originalPickingMode = target.pickingMode;
			target.pickingMode = PickingMode.Position;
			target.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
			target.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
			target.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
		}

		protected override void UnregisterCallbacksFromTarget()
		{
			target.pickingMode = _originalPickingMode;
			target.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
			target.UnregisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
			target.UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
		}
		
		private void OnPointerDown(PointerDownEvent ev)
		{
			if(CancelEvents) CancelEvent(ev);
			OnStartDrag?.Invoke(target);
			target.CapturePointer(ev.pointerId);
			_lastPos =  target.parent.WorldToLocal(ev.position);
		}

		private void OnPointerUp(PointerUpEvent ev)
		{
			CancelEvent(ev);
			OnEndDrag?.Invoke(target);
			target.ReleasePointer(ev.pointerId);
		}

		private void OnPointerMove(PointerMoveEvent ev)
		{
			CancelEvent(ev);
			if (!target.HasPointerCapture(ev.pointerId)) return;
			var currentPos = target.parent.WorldToLocal(ev.position);
			if (OnlyInsideParent && !target.parent.worldBound.Contains(ev.position))
				return;
			target.transform.position -= (Vector3)(_lastPos - currentPos);
			_lastPos = currentPos;
		}
		
		private void CancelEvent(EventBase e)
		{
			e.PreventDefault();
			e.StopImmediatePropagation();
		}
	}
}
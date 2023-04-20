using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements.Manipulators
{
	public class DraggableInParentManipulator: PointerManipulator
	{
		protected override void RegisterCallbacksOnTarget()
		{
			// target.parent.RegisterCallback<PointerDownEvent>(OnPointerDown);
			// target.parent.RegisterCallback<PointerMoveEvent>(OnPointerMove);
			// target.parent.RegisterCallback<PointerUpEvent>(OnPointerUp);
			
		}

		protected override void UnregisterCallbacksFromTarget()
		{
			// target.parent.UnregisterCallback<PointerDownEvent>(OnPointerDown);
			// target.parent.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
			// target.parent.UnregisterCallback<PointerUpEvent>(OnPointerUp);		
		}
	}
}
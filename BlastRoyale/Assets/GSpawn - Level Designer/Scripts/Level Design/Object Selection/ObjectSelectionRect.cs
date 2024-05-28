#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class ObjectSelectionRect : ObjectSelectionShape
    {
        private enum State
        {
            Idle = 0,
            Selecting
        }

        private Rect    _rect;
        private State   _state          = State.Idle;

        public override bool selecting { get { return _state == State.Selecting; } }
        public override Type shapeType { get { return Type.Rect; } }

        public override void cancel()
        {
            switchToIdleState();
        }

        protected override void update()
        {
            Event e = Event.current;
            Vector2 mousePos = e.mousePosition;
            mousePos.y = PluginCamera.camera.pixelHeight - mousePos.y;

            if (e.type == EventType.MouseDown)
            {
                _rect.xMin = mousePos.x;
                _rect.yMin = mousePos.y;
                _rect.xMax = mousePos.x;
                _rect.yMax = mousePos.y;
                _undoConfig.groupIndex = Undo.GetCurrentGroup();
            }
            else
            if (e.type == EventType.MouseDrag && (e.button == (int)MouseButton.LeftMouse && !e.alt))
            {
                _rect.xMax = mousePos.x;
                _rect.yMax = mousePos.y;
                if (isAreaBigEnough()) _state = State.Selecting;
            }
            else
            if (e.type == EventType.MouseUp)
            {
                // Note: We will disable the event if we have been selecting the whole time.
                //       This is so that the parent module won't process this event for click
                //       select which will cancel the multi-selection.
                bool disableEvent = isAreaBigEnough();
                switchToIdleState();
                if (disableEvent) e.disable();
            }
            else
            if (e.type == EventType.MouseLeaveWindow) switchToIdleState();
        }

        protected override void detectOverlappedObjects()
        {
            var pickedObjects = HandleUtility.PickRectObjects(_rect.createInvertedYCoords(PluginCamera.camera));
            _overlappedObjects.Clear();
            _overlappedObjects.AddRange(pickedObjects);
        }

        protected override void draw()
        {
            Material material = MaterialPool.instance.simpleDiffuse;
            material.setCullModeOff();

            material.SetColor("_Color", ObjectSelectionPrefs.instance.selRectFillColor);
            material.SetPass(0);
            GLEx.drawRect2D(_rect, PluginCamera.camera);

            material.SetColor("_Color", ObjectSelectionPrefs.instance.selRectBorderColor);
            material.SetPass(0);
            GLEx.drawRectBorder2D(_rect, PluginCamera.camera);
        }

        private void switchToIdleState()
        {
            Vector2 mousePos = Event.current.mousePosition;
            _rect.xMin = mousePos.x;
            _rect.yMin = mousePos.y;
            _rect.xMax = mousePos.x;
            _rect.yMax = mousePos.y;
            _state = State.Idle;
        }

        private bool isAreaBigEnough()
        {
            return _rect.absArea() >= 5.0f;
        }
    }
}
#endif
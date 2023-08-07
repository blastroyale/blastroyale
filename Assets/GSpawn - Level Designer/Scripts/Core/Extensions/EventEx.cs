#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public static class EventEx
    {      
        public static void disable(this Event e)
        {
            GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
            e.Use();
            GUIUtility.hotControl = 0;
        }

        public static bool isLeftMouseButtonDownEvent(this Event e)
        {
            return e.type == EventType.MouseDown && e.button == 0;
        }

        public static bool isLeftMouseButtonUpEvent(this Event e)
        {
            return e.type == EventType.MouseUp && e.button == 0;
        }

        public static bool isLeftMouseButtonDragEvent(this Event e)
        {
            return e.type == EventType.MouseDrag && e.button == 0;
        }

        public static bool isRightMouseButtonDownEvent(this Event e)
        {
            return e.type == EventType.MouseDown && e.button == 1;
        }

        public static bool isMouseMoveEvent(this Event e)
        {
            return e.type == EventType.MouseMove;
        }

        public static bool shiftStrict(this Event e)
        {
            return e.shift && !e.control && !e.command && !e.alt;
        }

        public static bool controlStrict(this Event e)
        {
            return e.control && !e.shift && !e.command && !e.alt;
        }

        public static bool altStrict(this Event e)
        {
            return e.alt && !e.shift && !e.command && !e.control;
        }

        public static bool commandStrict(this Event e)
        {
            return e.command && !e.alt && !e.control && !e.shift;
        }

        public static bool noShiftCtrlCmdAlt(this Event e)
        {
            return !e.shift && !e.control && !e.alt && !e.command;
        }
    }
}
#endif
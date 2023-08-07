#if UNITY_EDITOR
using UnityEngine.UIElements;

namespace GSpawn
{
    public static class DragPerformEventEx
    {
        public static bool shiftStrict(this DragPerformEvent e)
        {
            return e.shiftKey && !e.ctrlKey && !e.commandKey && !e.altKey;
        }

        public static bool controlStrict(this DragPerformEvent e)
        {
            return e.ctrlKey && !e.shiftKey && !e.commandKey && !e.altKey;
        }

        public static bool altStrict(this DragPerformEvent e)
        {
            return e.altKey && !e.shiftKey && !e.commandKey && !e.ctrlKey;
        }

        public static bool commandStrict(this DragPerformEvent e)
        {
            return e.commandKey && !e.altKey && !e.ctrlKey && !e.shiftKey;
        }

        public static bool noShiftCtrlCmdAlt(this DragPerformEvent e)
        {
            return !e.shiftKey && !e.ctrlKey && !e.altKey && !e.commandKey;
        }

        public static bool noShiftCmdAlt(this DragPerformEvent e)
        {
            return !e.shiftKey && !e.altKey && !e.commandKey;
        }

        public static bool noCmdAlt(this DragPerformEvent e)
        {
            return !e.commandKey && !e.altKey;
        }
    }
}
#endif
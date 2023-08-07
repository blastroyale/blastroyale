#if UNITY_EDITOR
using UnityEngine.UIElements;

namespace GSpawn
{
    public static class MouseUpEventEx
    {
        public static bool shiftStrict(this MouseUpEvent e)
        {
            return e.shiftKey && !e.ctrlKey && !e.commandKey && !e.altKey;
        }

        public static bool controlStrict(this MouseUpEvent e)
        {
            return e.ctrlKey && !e.shiftKey && !e.commandKey && !e.altKey;
        }

        public static bool altStrict(this MouseUpEvent e)
        {
            return e.altKey && !e.shiftKey && !e.commandKey && !e.ctrlKey;
        }

        public static bool commandStrict(this MouseUpEvent e)
        {
            return e.commandKey && !e.altKey && !e.ctrlKey && !e.shiftKey;
        }

        public static bool noShiftCtrlCmdAlt(this MouseUpEvent e)
        {
            return !e.shiftKey && !e.ctrlKey && !e.altKey && !e.commandKey;
        }

        public static bool noShiftCtrlCmd(this MouseUpEvent e)
        {
            return !e.shiftKey && !e.ctrlKey && !e.commandKey;
        }
    }
}
#endif
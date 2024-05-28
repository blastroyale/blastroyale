#if UNITY_EDITOR
using UnityEngine.UIElements;

namespace GSpawn
{
    public static class MouseDownEventEx
    {
        public static bool shiftStrict(this MouseDownEvent e)
        {
            return e.shiftKey && !e.ctrlKey && !e.commandKey && !e.altKey;
        }

        public static bool controlStrict(this MouseDownEvent e)
        {
            return e.ctrlKey && !e.shiftKey && !e.commandKey && !e.altKey;
        }

        public static bool altStrict(this MouseDownEvent e)
        {
            return e.altKey && !e.shiftKey && !e.commandKey && !e.ctrlKey;
        }

        public static bool commandStrict(this MouseDownEvent e)
        {
            return e.commandKey && !e.altKey && !e.ctrlKey && !e.shiftKey;
        }

        public static bool noShiftCtrlCmdAlt(this MouseDownEvent e)
        {
            return !e.shiftKey && !e.ctrlKey && !e.altKey && !e.commandKey;
        }

        public static bool noShiftCtrlCmd(this MouseDownEvent e)
        {
            return !e.shiftKey && !e.ctrlKey && !e.commandKey;
        }
    }
}
#endif
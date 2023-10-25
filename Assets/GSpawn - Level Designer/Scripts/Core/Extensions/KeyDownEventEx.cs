#if UNITY_EDITOR
using UnityEngine.UIElements;

namespace GSpawn
{
    public static class KeyDownEventEx
    {
        public static bool shiftStrict(this KeyDownEvent e)
        {
            return e.shiftKey && !e.ctrlKey && !e.commandKey && !e.altKey;
        }

        public static bool controlStrict(this KeyDownEvent e)
        {
            return e.ctrlKey && !e.shiftKey && !e.commandKey && !e.altKey;
        }

        public static bool altStrict(this KeyDownEvent e)
        {
            return e.altKey && !e.shiftKey && !e.commandKey && !e.ctrlKey;
        }

        public static bool commandStrict(this KeyDownEvent e)
        {
            return e.commandKey && !e.altKey && !e.ctrlKey && !e.shiftKey;
        }

        public static bool noShiftCtrlCmdAlt(this KeyDownEvent e)
        {
            return !e.shiftKey && !e.ctrlKey && !e.altKey && !e.commandKey;
        }

        public static bool noShiftCmdAlt(this KeyDownEvent e)
        {
            return !e.shiftKey && !e.altKey && !e.commandKey;
        }

        public static bool noCmdAlt(this KeyDownEvent e)
        {
            return !e.commandKey && !e.altKey;
        }
    }
}
#endif
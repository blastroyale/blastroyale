#if UNITY_EDITOR
using System;

namespace GSpawn
{
    public static class UICopyPaste
    {
        private static bool             _isActive;
        private static Action           _onPaste;
        private static Action           _onCancel;
        private static CopyPasteMode    _copyPasteMode  = CopyPasteMode.None;
        private static int              _initiatorId;

        public static bool              isActive        { get { return _isActive; } }
        public static CopyPasteMode     copyPasteMode   { get { return _copyPasteMode; } }
        public static int               initiatorId     { get { return _initiatorId; } }

        public static void begin(CopyPasteMode copyPasteMode, int initiatorId, Action onPaste, Action onCancel)
        {
            cancel();

            _isActive       = true;
            _copyPasteMode  = copyPasteMode;
            _initiatorId    = initiatorId;
            _onPaste        = onPaste;
            _onCancel       = onCancel;
        }

        public static void paste()
        {
            if (_isActive)
            {
                if (_onPaste != null) _onPaste();
                cancel();
            }
        }

        public static void cancel()
        {
            if (_onCancel != null) _onCancel();

            _copyPasteMode  = CopyPasteMode.None;
            _initiatorId    = 0;
            _onPaste        = null;
            _onCancel       = null;
            _isActive       = false;
        }
    }
}
#endif
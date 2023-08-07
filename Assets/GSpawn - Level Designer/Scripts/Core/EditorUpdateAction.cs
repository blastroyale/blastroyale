#if UNITY_EDITOR
using UnityEditor;

namespace GSpawn
{
    public enum EditorUpdateActionDelayMode
    {
        Frame = 0,
        Seconds
    }

    public abstract class EditorUpdateAction
    {
        private EditorUpdateActionDelayMode     _delayMode          = EditorUpdateActionDelayMode.Frame;
        private int                             _numElapsedFrames   = 0;
        private int                             _numDelayFrames     = 2;
        private double                          _executionTime      = 0.0f;

        public EditorUpdateActionDelayMode      delayMode           { get { return _delayMode; } set { _delayMode = value; } }
        public double                           executionTime       { get { return _executionTime; } set { _executionTime = value; if (_executionTime < 0.0) _executionTime = 0.0f; } }

        public bool attemptExecute()
        {
            if (_delayMode == EditorUpdateActionDelayMode.Frame)
            {
                ++_numElapsedFrames;
                if (_numElapsedFrames < _numDelayFrames) return false;
            }
            else
            if (_delayMode == EditorUpdateActionDelayMode.Seconds)
            {
                if (EditorApplication.timeSinceStartup < executionTime) return false;
            }

            execute();
            return true;
        }

        protected abstract void execute();
    }
}
#endif
#if UNITY_EDITOR
namespace GSpawn
{
    public abstract class PluginCommand
    {
        private bool    _entered = false;

        public void enter()
        {
            if (_entered) return;

            _entered = true;
            onEnter();
        }

        public void exit()
        {
            if (_entered)
            {
                onExit();
                _entered = false;
            }
        }

        protected virtual void onEnter  () {}
        protected virtual void onExit   () {}
    }
}
#endif
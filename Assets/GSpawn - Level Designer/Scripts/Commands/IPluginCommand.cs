#if UNITY_EDITOR
namespace GSpawn
{
    public interface IPluginCommand
    {
        void enter  ();
        void exit   ();
    }
}
#endif
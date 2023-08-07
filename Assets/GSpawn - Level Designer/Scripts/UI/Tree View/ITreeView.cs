#if UNITY_EDITOR
namespace GSpawn
{
    public interface ITreeView
    {
        int             numSelectedItems        { get; }
        int             dragAndDropInitiatorId  { get; }
        System.Object   dragAndDropData         { get; }
    }
}
#endif
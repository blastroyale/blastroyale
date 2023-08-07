#if UNITY_EDITOR
namespace GSpawn
{
    public interface IGridView
    {
        int             dragAndDropInitiatorId  { get; }
        System.Object   dragAndDropData         { get; }
    }
}
#endif
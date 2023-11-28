#if UNITY_EDITOR
namespace GSpawn
{
    public interface IListView
    {
        int             numItems                { get; }
        int             numSelectedItems        { get; }
        int             dragAndDropInitiatorId  { get; }
        System.Object   dragAndDropData         { get; }
        bool            canDragAndDrop          { get; set; }
        bool            canRenameItems          { get; set; }
        bool            canMultiSelect          { get; set; }
        bool            canDelete               { get; set; }
        bool            canDuplicate            { get; set; }
        bool            canCopyPaste            { get; set; }
        bool            canCutPaste             { get; set; }
    }
}
#endif
#if UNITY_EDITOR
namespace GSpawn
{
    public enum CopyPasteMode
    {
        None = 0,
        Copy,
        Cut
    }

    public enum ItemDragState
    {
        AtRest = 0,
        Ready,
        Dragging
    }

    public enum VerticalStepDirection
    {
        Up,
        Down
    }

    public enum RotationAxesType
    {
        World = 0,
        Grid
    }

    public enum RotationRandomizationMode
    {
        MinMax = 0,
        Step
    }

    public enum TransformChannel
    {
        Position,
        Rotation,
        Scale
    }

    public struct UndoConfig
    {
        public bool     allowUndoRedo;
        public int      groupIndex;
        public bool     collapseToGroup;

        public static readonly UndoConfig defaultConfig = new UndoConfig()
        {
            allowUndoRedo   = true,
            groupIndex      = 0,
            collapseToGroup = false
        };
    }

    public class YesNoAnswer
    {
        private bool    _hasYes    = false;
        private bool    _hasNo     = false;

        public bool     hasYes      { get { return _hasYes; } }
        public bool     hasNo       { get { return _hasNo; } }
        public bool     hasOnlyYes  { get { return hasYes && !hasNo; } }

        public void yes()
        {
            _hasYes = true;
        }

        public void no()
        {
            _hasNo = true;
        }
    }

    public abstract class Singleton<DataType> where DataType : Singleton<DataType>, new()
    {
        private static DataType     _instance   = new DataType();
        public static DataType      instance    { get { return _instance; } }
    }
}
#endif
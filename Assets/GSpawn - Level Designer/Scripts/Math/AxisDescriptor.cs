#if UNITY_EDITOR
namespace GSpawn
{
    public enum Axis
    {
        X = 0,
        Y,
        Z
    }

    public enum AxisSign
    {
        Positive = 0,
        Negative
    }

    public class AxisDescriptor
    {
        private AxisSign    _sign;
        private int         _index;

        public AxisSign     sign        { get { return _sign; } set { _sign = value; } }
        public int          index       { get { return _index; } set { _index = value; } }
        public bool         isPositive  { get { return _sign == AxisSign.Positive; } }
        public bool         isNegative  { get { return _sign == AxisSign.Negative; } }

        public AxisDescriptor(int axisIndex, AxisSign axisSign)
        {
            _sign   = axisSign;
            _index  = axisIndex;
        }

        public AxisDescriptor(int axisIndex, bool isNegative)
        {
            _sign   = isNegative ? AxisSign.Negative : AxisSign.Positive;
            _index  = axisIndex;
        }

        public Box3DFace getBoxFace()
        {
            if(_sign == AxisSign.Negative)
            {
                if (_index == 0) return Box3DFace.Left;
                if (_index == 1) return Box3DFace.Bottom;
                return Box3DFace.Front;
            }
            else
            {
                if (_index == 0) return Box3DFace.Right;
                if (_index == 1) return Box3DFace.Top;
                return Box3DFace.Back;
            }
        }
    }
}
#endif
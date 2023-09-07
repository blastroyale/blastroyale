#if UNITY_EDITOR
namespace GSpawn
{
    /// <summary>
    /// Identifies different quadrants in which a plane can exist. The quadrants 
    /// go from 'First' to 'Fourth' in counter clockwise order when looking down
    /// the plane normal.
    /// </summary>
    public enum PlaneQuadrantId
    {
        First = 0,
        Second,
        Third,
        Fourth
    }

    public struct PlaneDescriptor
    {
        private PlaneId             _id;
        private PlaneQuadrantId     _quadrant;
        private AxisDescriptor      _firstAxisDescriptor;
        private AxisDescriptor      _secondAxisDescriptor;

        public PlaneId              id                      { get { return _id; } }
        public PlaneQuadrantId      quadrant                { get { return _quadrant; } }
        public AxisSign             firstAxisSign           { get { return _firstAxisDescriptor.sign; } }
        public AxisSign             secondAxisSign          { get { return _secondAxisDescriptor.sign; } }
        public int                  firstAxisIndex          { get { return _firstAxisDescriptor.index; } }
        public int                  secondAxisIndex         { get { return _secondAxisDescriptor.index; } }
        public AxisDescriptor       firstAxisDescriptor     { get { return _firstAxisDescriptor; } }
        public AxisDescriptor       secondAxisDescriptor    { get { return _secondAxisDescriptor; } }

        public PlaneDescriptor(PlaneId planeId, PlaneQuadrantId planeQuadrant)
        {
            _id                     = planeId;
            _quadrant               = planeQuadrant;
            _firstAxisDescriptor    = planeId.getFirstAxisDescriptor(planeQuadrant);
            _secondAxisDescriptor   = planeId.getSecondAxisDescriptor(planeQuadrant);
        }
    }
}
#endif
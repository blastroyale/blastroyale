#if UNITY_EDITOR

namespace GSpawn
{
    public class SceneRayHit
    {
        private ObjectRayHit    _objectHit;
        private GridRayHit      _gridHit;

        public bool             anyHit          { get { return _objectHit != null || _gridHit != null; } }
        public bool             wasObjectHit    { get { return _objectHit != null; } }
        public bool             wasGridHit      { get { return _gridHit != null; } }
        public ObjectRayHit     objectHit       { get { return _objectHit; } }
        public GridRayHit       gridHit         { get { return _gridHit; } }

        public SceneRayHit(ObjectRayHit objectHit, GridRayHit gridRayHit)
        {
            _objectHit  = objectHit;
            _gridHit    = gridRayHit;
        }

        public RayHit getClosestRayHit()
        {
            if (!anyHit) return null;

            if (wasObjectHit && wasGridHit)
            {
                if (_objectHit.hitEnter <= _gridHit.hitEnter) return _objectHit;
                else return _gridHit;
            }

            if (wasObjectHit) return _objectHit;
            return _gridHit;
        }
    }
}
#endif
#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public class ObjectSpawnCell
    {
        private OBB         _objectOBB          = OBB.getInvalid();
        private bool        _skipped            = false;
        private bool        _occluded           = false;
        private bool        _outOfScope         = false;

        public OBB          objectOBB           { get { return _objectOBB; } }
        public Vector3      objectOBBCenter     { get { return _objectOBB.center; } }
        public Quaternion   objectOBBRotation   { get { return _objectOBB.rotation; } }
        public Vector3      objectOBBSize       { get { return _objectOBB.size; } }
        public bool         skipped             { get { return _skipped; } set { _skipped = value; } }
        public bool         occluded            { get { return _occluded; } set { _occluded = value; } }
        public bool         outOfScope          { get { return _outOfScope; } set { _outOfScope = value; } }
        public bool         isGoodForSpawn      { get { return !_skipped && !_occluded && !_outOfScope; } }

        public ObjectSpawnCell(OBB obb)
        {
            _objectOBB = obb;
        }

        public void setObjectOBBCenter(Vector3 position)
        {
            _objectOBB.center = position;
        }       

        public void offsetObjectOBBCenter(Vector3 offset)
        {
            _objectOBB.center += offset;
        }

        public void setObjectOBBRotation(Quaternion rotation)
        {
            _objectOBB.rotation = rotation;
        }
    }
}
#endif
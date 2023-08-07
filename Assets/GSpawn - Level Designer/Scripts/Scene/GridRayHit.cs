#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public class GridRayHit : RayHit
    {
        private PluginGrid      _hitGrid;
        private GridCell        _hitCell;

        public PluginGrid       hitGrid { get { return _hitGrid; } }
        public GridCell         hitCell { get { return _hitCell; } }

        public GridRayHit(Ray hitRay, PluginGrid hitGrid, float hitEnter)
            : base(hitRay, hitRay.GetPoint(hitEnter), hitGrid.plane, hitEnter)
        {
            _hitGrid = hitGrid;
            _hitCell = _hitGrid.getCellFromPoint(hitRay.GetPoint(hitEnter));
        }
    }
}
#endif
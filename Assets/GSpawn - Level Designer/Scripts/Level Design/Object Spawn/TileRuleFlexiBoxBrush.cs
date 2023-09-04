#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GSpawn
{
    public class TileRuleFlexiBoxBrush : TileRuleBrush
    {
        private enum State
        {
            Ready = 0,
            Extending
        }

        private OBB         _obb            = new OBB(Vector3.zero, Vector3.zero, Quaternion.identity);
        private Vector3Int  _minCellCoords;
        private Vector3Int  _maxCellCoords;
        private int         _width;
        private int         _height;
        private int         _depth;

        private Vector3Int  _startCell          = new Vector3Int();
        private Vector3Int  _endCell            = new Vector3Int();
        private State       _state              = State.Ready;
        private bool        _snappedToTile      = false;
        private int         _snappedYOffset     = 0;

        private List<Vector3Int>                _mirroredCells              = new List<Vector3Int>();
        private List<Vector3>                   _shadowCasterCorners        = new List<Vector3>();
        private List<TileRuleGridCellRange>     _mirroredCellRanges         = new List<TileRuleGridCellRange>();

        public override TileRuleBrushType   brushType       { get { return TileRuleBrushType.FlexiBox; } }
        public override bool                isIdle          { get { return _state == State.Ready; } }
        public override int                 yOffset
        {
            get { return _snappedToTile ? _snappedYOffset : spawnSettings.brushYOffset; }
        }
        public OBB                          obb             { get { return _obb; } }
        public Vector3Int                   minCellCoords   { get { return _minCellCoords; } }
        public Vector3Int                   maxCellCoords   { get { return _maxCellCoords; } }
        public int                          width           { get { return _width; } set { _width = Mathf.Max(1, value); } }
        public int                          height          { get { return _height; } set { _height = Mathf.Max(1, value); } }
        public int                          depth           { get { return _depth; } set { _depth = Mathf.Max(1, value); } }

        public override void onSceneGUI()
        {
            Event e = Event.current;
            if (_state == State.Ready)
            {
                pickStartAndEndCell();
                fromStartAndEndCells();

                if (e.isLeftMouseButtonDownEvent())
                {
                    e.disable();
                    _state = State.Extending;
                }
            }
            else
            if (_state == State.Extending)
            {
                pickEndCell();
                fromStartAndEndCells();

                if (e.isLeftMouseButtonDownEvent())
                {
                    e.disable();
                    useOnGrid();
                    _state = State.Ready;
                }
                else
                if (FixedShortcuts.cancelAction(e))
                {
                    e.disable();
                    cancel();
                }
            }
        }

        public override void cancel()
        {
            _state = State.Ready;
        }

        public override void draw(Color borderColor)
        {
            Material material = MaterialPool.instance.simpleDiffuse;
            material.setZTestEnabled(true);
            material.setZWriteEnabled(true);
            material.setCullModeBack();

            int numPasses = material.passCount;
            material.SetColor("_Color", borderColor);

            Matrix4x4 brushTransformMtx = _obb.transformMatrix;
            for (int i = 0; i < numPasses; ++i)
            {
                material.SetPass(i);
                Graphics.DrawMeshNow(MeshPool.instance.unitWireBox, brushTransformMtx);
            }

            var mirrorGizmo = tileRuleGrid.mirrorGizmo;
            if (mirrorGizmo.enabled)
            {
                var cellRange = new TileRuleGridCellRange(_minCellCoords, _maxCellCoords);
                mirrorGizmo.mirrorTileRuleGridCellRange(cellRange, _mirroredCellRanges);
                foreach (var range in _mirroredCellRanges)
                {
                    OBB rangeOBB = tileRuleGrid.calcCellRangeOBB(range.min, range.max);
                    mirrorGizmo.drawMirroredOBB(rangeOBB);
                }
            }

            Handles.BeginGUI();
            Handles.Label(HandlesEx.calcLabelPositionBelowOBB(_obb),
                string.Format("Min cell:  {0}\nMax cell: {1}\nSize: {2}, {3}, {4}", _minCellCoords, _maxCellCoords, width, height, depth), GUIStyleDb.instance.sceneViewInfoLabel);
            Handles.EndGUI();
        }

        public override void drawShadow(Color shadowLineColor, Color shadowColor)
        {
            if (tileRuleGrid.calcShadowCasterOBBCorners(_obb, _minCellCoords.y, _shadowCasterCorners))
                tileRuleGrid.drawShadow(_shadowCasterCorners, shadowLineColor, shadowColor);
        }

        public override void getCellCoords(HashSet<Vector3Int> cellCoords)
        {
            cellCoords.Clear();

            var mirrorGizmo = tileRuleGrid.mirrorGizmo;
            if (spawnSettings.flexiBoxBrushFillMode == TileRuleBrushFillMode.Solid)
            {
                if (mirrorGizmo.enabled)
                {
                    for (int x = _minCellCoords.x; x <= _maxCellCoords.x; ++x)
                    {
                        for (int y = _minCellCoords.y; y <= _maxCellCoords.y; ++y)
                        {
                            for (int z = _minCellCoords.z; z <= _maxCellCoords.z; ++z)
                            {
                                var coords = new Vector3Int(x, y, z);
                                cellCoords.Add(coords);
                                mirrorGizmo.mirrorTileRuleGridCellCoords(coords, _mirroredCells);

                                foreach (var mc in _mirroredCells)
                                    cellCoords.Add(mc);
                            }
                        }
                    }
                }
                else
                {
                    for (int x = _minCellCoords.x; x <= _maxCellCoords.x; ++x)
                    {
                        for (int y = _minCellCoords.y; y <= _maxCellCoords.y; ++y)
                        {
                            for (int z = _minCellCoords.z; z <= _maxCellCoords.z; ++z)
                                cellCoords.Add(new Vector3Int(x, y, z));
                        }
                    }
                }
            }
            else
            {
                if (mirrorGizmo.enabled)
                {
                    for (int x = _minCellCoords.x; x <= _maxCellCoords.x; ++x)
                    {
                        bool acceptX = (x == minCellCoords.x || x == maxCellCoords.x);
                        for (int y = _minCellCoords.y; y <= _maxCellCoords.y; ++y)
                        {
                            bool acceptY = (y == minCellCoords.y || y == maxCellCoords.y);
                            for (int z = _minCellCoords.z; z <= _maxCellCoords.z; ++z)
                            {
                                bool acceptZ = (z == _minCellCoords.z || z == _maxCellCoords.z);
                                if (acceptX || acceptY || acceptZ)
                                {
                                    var coords = new Vector3Int(x, y, z);
                                    cellCoords.Add(coords);
                                    mirrorGizmo.mirrorTileRuleGridCellCoords(coords, _mirroredCells);

                                    foreach (var mc in _mirroredCells)
                                        cellCoords.Add(mc);
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (int x = _minCellCoords.x; x <= _maxCellCoords.x; ++x)
                    {
                        bool acceptX = (x == minCellCoords.x || x == maxCellCoords.x);
                        for (int y = _minCellCoords.y; y <= _maxCellCoords.y; ++y)
                        {
                            bool acceptY = (y == minCellCoords.y || y == maxCellCoords.y);
                            for (int z = _minCellCoords.z; z <= _maxCellCoords.z; ++z)
                            {
                                bool acceptZ = (z == _minCellCoords.z || z == _maxCellCoords.z);
                                if (acceptX || acceptY || acceptZ)
                                    cellCoords.Add(new Vector3Int(x, y, z));
                            }
                        }
                    }
                }
            }
        }

        public override void getCellsAroundVerticalBorder(int radius, List<Vector3Int> cellCoords)
        {
            cellCoords.Clear();

            int minX = _minCellCoords.x - radius;
            int maxX = _maxCellCoords.x + radius;
            int minZ = _minCellCoords.z - radius;
            int maxZ = _maxCellCoords.z + radius;

            var mirrorGizmo = tileRuleGrid.mirrorGizmo;
            if (mirrorGizmo.enabled)
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    bool acceptX = (x < _minCellCoords.x || x > _maxCellCoords.x);
                    for (int z = minZ; z <= maxZ; ++z)
                    {
                        bool acceptZ = (z < _minCellCoords.z || z > _maxCellCoords.z);
                        if (acceptX || acceptZ)
                        {
                            for (int y = _minCellCoords.y; y <= _maxCellCoords.y; ++y)
                            {
                                var coords = new Vector3Int(x, y, z);
                                cellCoords.Add(coords);
                                mirrorGizmo.mirrorTileRuleGridCellCoords(coords, _mirroredCells);

                                foreach (var mc in _mirroredCells)
                                    cellCoords.Add(mc);
                            }
                        }
                    }
                }
            }
            else
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    bool acceptX = (x < _minCellCoords.x || x > _maxCellCoords.x);
                    for (int z = minZ; z <= maxZ; ++z)
                    {
                        bool acceptZ = (z < _minCellCoords.z || z > _maxCellCoords.z);
                        if (acceptX || acceptZ)
                        {
                            for (int y = _minCellCoords.y; y <= _maxCellCoords.y; ++y)
                            {
                                cellCoords.Add(new Vector3Int(x, y, z));
                            }
                        }
                    }
                }
            }
        }

        public override void getCellCoordsBelowBrush(List<Vector3Int> cellCoords)
        {
            cellCoords.Clear();

            int minX    = _minCellCoords.x;
            int maxX    = _maxCellCoords.x;
            int minZ    = _minCellCoords.z;
            int maxZ    = _maxCellCoords.z;
            int yCoord  = yOffset - 1;

            var mirrorGizmo = tileRuleGrid.mirrorGizmo;
            if (mirrorGizmo.enabled)
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    for (int z = minZ; z <= maxZ; ++z)
                    {
                        var coords = new Vector3Int(x, yCoord, z);
                        cellCoords.Add(coords);
                        mirrorGizmo.mirrorTileRuleGridCellCoords(coords, _mirroredCells);

                        for (int i = 0; i < _mirroredCells.Count; ++i)
                        {
                            var mc = _mirroredCells[i];

                            // Note: Check the coordinate was mirrored against the XZ plane. In that
                            //       case we need to correct. Below means below and we can't let it
                            //       become above.
                            if (mc.y != yCoord) mc.y -= (_height + 1);
                            cellCoords.Add(mc);
                        }
                    }
                }
            }
            else
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    for (int z = minZ; z <= maxZ; ++z)
                    {
                        cellCoords.Add(new Vector3Int(x, yCoord, z));
                    }
                }
            }
        }

        private bool pickStartAndEndCell()
        {
            _snappedToTile = false;
            var ray = PluginCamera.camera.getCursorRay();
            if (spawnSettings.snapBrushYOffsetToTiles)
            {
                Vector3Int hitCellCoords;
                if (tileRuleGrid.pickTileCellCoords(ray, usage != TileRuleBrushUsage.Erase, out hitCellCoords))
                {
                    int haldWidth = _width / 2;
                    int halfDepth = _depth / 2;

                    _snappedYOffset     = hitCellCoords.y;

                    var bottomLeftCellCoords = new Vector3Int(hitCellCoords.x - haldWidth, _snappedYOffset, hitCellCoords.z - halfDepth);

                    _startCell  = bottomLeftCellCoords;
                    _endCell    = _startCell;

                    _snappedToTile = true;
                    return true;
                }
            }

            var hit = tileRuleGrid.raycast(ray, gridSitsBelowBrush ? yOffset : 0);
            if (hit != null)
            {
                _startCell      = hit.hitCellCoords;
                _startCell.y    += gridSitsBelowBrush ? 0 : yOffset;
                _endCell        = _startCell;
                return true;
            }

            return false;
        }

        private bool pickEndCell() 
        {
            var hit = tileRuleGrid.raycast(PluginCamera.camera.getCursorRay(), gridSitsBelowBrush || _snappedToTile ? yOffset : 0);
            if (hit != null)
            {
                _endCell        = hit.hitCellCoords;
                _endCell.y      += (gridSitsBelowBrush || _snappedToTile) ? 0 : yOffset;
                return true;
            }

            return false;
        }

        private void fromStartAndEndCells()
        {
            _minCellCoords = _startCell;
            _maxCellCoords = _endCell;
            _maxCellCoords.y += (_height - 1);
            sortMinMaxCoords();

            _width = _maxCellCoords.x - _minCellCoords.x + 1;
            _depth = _maxCellCoords.z - _minCellCoords.z + 1;

            _obb = tileRuleGrid.calcCellRangeOBB(_minCellCoords, _maxCellCoords);
        }

        private void sortMinMaxCoords()
        {
            if (_minCellCoords.x > _maxCellCoords.x) { int t = _minCellCoords.x; _minCellCoords.x = _maxCellCoords.x; _maxCellCoords.x = t; }
            if (_minCellCoords.y > _maxCellCoords.y) { int t = _minCellCoords.y; _minCellCoords.y = _maxCellCoords.y; _maxCellCoords.y = t; }
            if (_minCellCoords.z > _maxCellCoords.z) { int t = _minCellCoords.z; _minCellCoords.z = _maxCellCoords.z; _maxCellCoords.z = t; }
        }
    }
}
#endif
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GSpawn
{
    public class TileRuleBoxBrush : TileRuleBrush
    {
        private enum State
        {
            Ready = 0,
            Painting
        }

        private     OBB             _obb            = new OBB(Vector3.zero, Vector3.zero, Quaternion.identity);
        private     Vector3Int      _minCellCoords;
        private     Vector3Int      _maxCellCoords;
        private     int             _width;
        private     int             _height;
        private     int             _depth;
        private     State           _state          = State.Ready;
        private     bool            _snappedToTile  = false;
        private     int             _snappedYOffset = 0;
 
        private List<Vector3>                   _shadowCasterCorners    = new List<Vector3>();
        private List<TileRuleGridCellRange>     _mirroredCellRanges     = new List<TileRuleGridCellRange>();
        private List<Vector3Int>                _mirroredCells          = new List<Vector3Int>();

        public override TileRuleBrushType   brushType           { get { return TileRuleBrushType.Box; } }
        public override bool                isIdle              { get { return _state == State.Ready; } }
        public override int                 yOffset
        {
            get { return _snappedToTile ? _snappedYOffset : spawnSettings.brushYOffset; }
        }
        public OBB              obb                             { get { return _obb; } }
        public Vector3Int       minCellCoords                   { get { return _minCellCoords; } }
        public Vector3Int       maxCellCoords                   { get { return _maxCellCoords; } }
        public int              width                           { get { return _width; } set { _width = Mathf.Max(1, value); } }
        public int              height                          { get { return _height; } set { _height = Mathf.Max(1, value); } }
        public int              depth                           { get { return _depth; } set { _depth = Mathf.Max(1, value); } }

        public override void onSceneGUI()
        {
            fromGridRayHit();
            if (spawnSettings.snapBrushYOffsetToTiles)
                fromPickedTile();

            Event e = Event.current;
            if (e.isLeftMouseButtonDownEvent())
            {
                _state = State.Painting;
                useOnGrid();
            }
            else
            if (e.isLeftMouseButtonDragEvent())
            {
                _state = State.Painting;
                useOnGrid();
            }
            else
            if (e.isLeftMouseButtonUpEvent())
            {
                e.disable();
                cancel();
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
                string.Format("Min cell:  {0}\nMax cell: {1}", _minCellCoords, _maxCellCoords), GUIStyleDb.instance.sceneViewInfoLabel);
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

        public void fromCellCoords(Vector3Int cellCoords, TileRuleGrid grid)
        {
            _minCellCoords = cellCoords;
            _maxCellCoords = cellCoords;

            _width  = 1;
            _height = 1;
            _depth  = 1;
            spawnSettings.brushYOffset = _minCellCoords.y;

            Vector3 cellSize    = grid.settings.cellSize;
            _obb.rotation       = grid.gridRotation;
            _obb.size           = new Vector3(cellSize.x, cellSize.y, cellSize.z);
            _obb.center         = grid.cellCoordsToVisualCellPosition(cellCoords);
        }

        public void fromGridRayHit()
        {
            var rayHit = tileRuleGrid.raycast(PluginCamera.camera.getCursorRay(), (gridSitsBelowBrush || _snappedToTile) ? yOffset : 0);
            if (rayHit == null) return;

            int haldWidth       = _width / 2;
            int halfDepth       = _depth / 2;

            var hitCellCoords           = rayHit.hitCellCoords;
            var bottomLeftCellCoords    = new Vector3Int(hitCellCoords.x - haldWidth, yOffset, hitCellCoords.z - halfDepth);
            var topRightCellCoords      = new Vector3Int(bottomLeftCellCoords.x + _width - 1, yOffset, bottomLeftCellCoords.z + _depth - 1);

            if (_state == State.Ready)
            {
                _minCellCoords = bottomLeftCellCoords;
                _maxCellCoords = topRightCellCoords;
                _maxCellCoords.y += (_height - 1);
            }
            else
            {
                _minCellCoords.x = bottomLeftCellCoords.x;
                _minCellCoords.z = bottomLeftCellCoords.z;
                _maxCellCoords.x = topRightCellCoords.x;
                _maxCellCoords.z = topRightCellCoords.z;
            }

            sortMinMaxCoords();
            _obb = tileRuleGrid.calcCellRangeOBB(_minCellCoords, _maxCellCoords);
        }

        public void fromPickedTile()
        {
            if (_state != State.Ready) return;

            _snappedToTile = false;
            var ray = PluginCamera.camera.getCursorRay();
            Vector3Int hitCellCoords;
            if (tileRuleGrid.pickTileCellCoords(ray, usage != TileRuleBrushUsage.Erase, out hitCellCoords))
            {
                int haldWidth       = _width / 2;
                int halfDepth       = _depth / 2;

                _snappedYOffset             = hitCellCoords.y;
                var bottomLeftCellCoords    = new Vector3Int(hitCellCoords.x - haldWidth, _snappedYOffset, hitCellCoords.z - halfDepth);
                var topRightCellCoords      = new Vector3Int(bottomLeftCellCoords.x + _width - 1, _snappedYOffset, bottomLeftCellCoords.z + _depth - 1);

                _minCellCoords      = bottomLeftCellCoords;
                _maxCellCoords      = topRightCellCoords;
                _maxCellCoords.y    += (_height - 1);

                sortMinMaxCoords();
                _obb                = tileRuleGrid.calcCellRangeOBB(_minCellCoords, _maxCellCoords);
                _snappedToTile      = true;
            }
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
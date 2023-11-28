#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GSpawn
{
    public class TileRuleSegmentsBrush : TileRuleBrush
    {
        private enum State
        {
            Ready = 0,
            BuildingSegments
        }

        private class SegmentCellStack
        {
            public int              height          = 0;
            public Vector3Int       baseCellCoords  = new Vector3Int();
            public List<Vector3Int> coords          = new List<Vector3Int>();

            public SegmentCellStack(Vector3Int baseCoords)
            {
                baseCellCoords = baseCoords;
            }

            public TileRuleGridCellRange calcCellRange()
            {
                if (coords.Count == 0) return new TileRuleGridCellRange();

                Vector3Int min = coords[0];
                Vector3Int max = coords[0];

                for (int i = 0; i < coords.Count; ++i)
                {
                    min = Vector3Int.Min(min, coords[i]);
                    max = Vector3Int.Max(max, coords[i]);
                }

                return new TileRuleGridCellRange(min, max);
            }

            public void setHeight(int height)
            {
                coords.Clear();

                this.height     = height;
                if (height >= 0)
                {
                    for (int i = 0; i < height; ++i)
                        coords.Add(new Vector3Int(baseCellCoords.x, baseCellCoords.y + i, baseCellCoords.z));
                }
                else
                {
                    int absHeight = Mathf.Abs(height);
                    for (int i = 0; i < absHeight; ++i)
                        coords.Add(new Vector3Int(baseCellCoords.x, baseCellCoords.y - i, baseCellCoords.z));
                }
            }
        }

        private class Segment
        {
            public Vector3Int               start       = new Vector3Int();
            public Vector3Int               end         = new Vector3Int();
            public List<SegmentCellStack>   stacks      = new List<SegmentCellStack>();

            public int                      length      { get { return stacks.Count; } }
            public SegmentCellStack         firstStack  { get { return stacks[0]; } }
            public SegmentCellStack         lastStack   { get { return stacks[length - 1]; } }

            public void removeLastStack()
            {
                stacks.RemoveAt(length - 1);
            }

            public void transferLastStack(Segment destSegment)
            {
                destSegment.stacks.Add(lastStack);
                removeLastStack();
            }

            public void getStackHeights(List<int> heights)
            {
                heights.Clear();
                foreach (var stack in stacks)
                    heights.Add(stack.height);
            }
        }

        private int                             _currentHeight                  = 1;
        private bool                            _snappedToTile                  = false;
        private int                             _snappedYOffset                 = 0;

        private State                           _state                          = State.Ready;
        private Segment                         _currentSegment                 = new Segment();
        private List<Segment>                   _segments                       = new List<Segment>();
        private List<Vector3Int>                _segCoords                      = new List<Vector3Int>();

        private List<Vector3Int>                _mirroredCells                  = new List<Vector3Int>();
        private List<Vector3>                   _shadowCasterCorners            = new List<Vector3>();

        private List<Vector3Int>                _cellCoordsBuffer               = new List<Vector3Int>();
        private HashSet<Vector3Int>             _cellCoordsSet                  = new HashSet<Vector3Int>();
        private List<int>                       _heightBuffer                   = new List<int>();
        private List<TileRuleGridCellRange>     _mirroredCellRanges             = new List<TileRuleGridCellRange>();

        private ClampIntegerPatternSampler      _clampHeightPatternSampler      = new ClampIntegerPatternSampler();
        private MirrorIntegerPatternSampler     _mirrorHeightPatternSampler     = new MirrorIntegerPatternSampler();
        private RepeatIntegerPatternSampler     _repeatHeightPatternSampler     = new RepeatIntegerPatternSampler();
        private List<int>                       _heightPatternValues            = new List<int>();

        public override TileRuleBrushType           brushType               { get { return TileRuleBrushType.Segments; } }
        public override bool                        isIdle                  { get { return _state == State.Ready; } }
        public override int                         yOffset
        {
            get { return _snappedToTile ? _snappedYOffset : spawnSettings.brushYOffset; }
        }
        public int                                  currentHeight           { get { return _currentHeight; } }

        public void setCurrentHeight(int height)
        {
            if (_currentHeight == height) return;

            int oldHeight = _currentHeight;
            _currentHeight = height;
            if (spawnSettings.segBrushHeightMode == TileRuleSegmentBrushHeightMode.Constant)
            {
                foreach (var segment in _segments)
                {
                    foreach (var stack in segment.stacks)
                    {
                        stack.setHeight(_currentHeight);
                    }
                }
            }
            else if (spawnSettings.segBrushHeightMode == TileRuleSegmentBrushHeightMode.Random)
            {
                foreach (var segment in _segments)
                {
                    foreach (var stack in segment.stacks)
                    {
                        var randomHeight = stack.height - oldHeight;
                        stack.setHeight(_currentHeight + randomHeight);
                    }
                }
            }
            else
            if (spawnSettings.segBrushHeightMode == TileRuleSegmentBrushHeightMode.Pattern)
            {
                var heightSampler = getHeightPatternSampler();
                spawnSettings.segBrushHeightPattern.getValues(_heightPatternValues);

                int patternOffset = 0;
                foreach (var segment in _segments)
                {
                    int numStacks = segment.stacks.Count;
                    for (int i = 0; i < numStacks; ++i)
                    {
                        var stack = segment.stacks[i];
                        stack.setHeight(_currentHeight + heightSampler.sample(_heightPatternValues, patternOffset));

                        ++patternOffset;
                    }
                }
            }
        }

        public override void onSceneGUI()
        {
            Event e = Event.current;
            if (_state == State.Ready)
            {
                Vector3Int cellCoords;
                if (pickCell(out cellCoords))
                {
                    _currentSegment.start   = cellCoords;
                    _currentSegment.end     = cellCoords;
                    _currentSegment.stacks.Clear();

                    if (_segments.Count == 0) _segments.Add(_currentSegment);
                    
                    var cellStack           = new SegmentCellStack(cellCoords);
                    cellStack.setHeight(_currentHeight);

                    _currentSegment.stacks.Add(cellStack);
                }

                if (e.isLeftMouseButtonDownEvent())
                {
                    e.disable();
                    _state = State.BuildingSegments;

                    // Note: We always call cellStack.setHeight(_constantHeight); in the Ready state.
                    //       As soon as we switch to building state, we want to update the height accordingly
                    //       if random height mode is used.
                    if (spawnSettings.segBrushHeightMode == TileRuleSegmentBrushHeightMode.Random)
                        _segments[0].stacks[0].setHeight(UnityEngine.Random.Range(spawnSettings.segBrushMinRandomHeight, spawnSettings.segBrushMaxRandomHeight + 1));
                }
            }
            else
            if (_state == State.BuildingSegments)
            {
                Vector3Int cellCoords;
                if (pickCell(out cellCoords))
                {
                    _currentSegment.end = cellCoords;

                    calcSegmentCoords(_currentSegment.start, _currentSegment.end, _segCoords);
                    int newLength = _segCoords.Count;

                    if (spawnSettings.segBrushHeightMode == TileRuleSegmentBrushHeightMode.Constant)
                    {
                        _currentSegment.stacks.Clear();
                        foreach (var coords in _segCoords)
                        {
                            var stack = new SegmentCellStack(coords);
                            stack.setHeight(_currentHeight);
                            _currentSegment.stacks.Add(stack);
                        }
                    }
                    else
                    if (spawnSettings.segBrushHeightMode == TileRuleSegmentBrushHeightMode.Random)
                    {
                        _currentSegment.getStackHeights(_heightBuffer);
                        _currentSegment.stacks.Clear();

                        int numAvailableHeights = _heightBuffer.Count;
                        for (int i = 0; i < newLength; ++i)
                        {
                            var coords  = _segCoords[i];
                            var stack   = new SegmentCellStack(coords);
                            int height  = i < numAvailableHeights ? _heightBuffer[i] : _currentHeight + UnityEngine.Random.Range(spawnSettings.segBrushMinRandomHeight, spawnSettings.segBrushMaxRandomHeight + 1);

                            stack.setHeight(height);
                            _currentSegment.stacks.Add(stack);
                        }
                    }
                    else
                    if (spawnSettings.segBrushHeightMode == TileRuleSegmentBrushHeightMode.Pattern)
                    {
                        int patternOffset = 0;
                        int numSegments = _segments.Count;
                        for (int i = 0; i < numSegments - 1; ++i)
                            patternOffset += _segments[i].length;

                        var heightSampler = getHeightPatternSampler();
                        spawnSettings.segBrushHeightPattern.getValues(_heightPatternValues);

                        _currentSegment.stacks.Clear();
                        for (int i = 0; i < newLength; ++i)
                        {
                            var coords = _segCoords[i];
                            var stack = new SegmentCellStack(coords);

                            stack.setHeight(_currentHeight + heightSampler.sample(_heightPatternValues, i + patternOffset));
                            _currentSegment.stacks.Add(stack);
                        }
                    }
                }

                if (e.isLeftMouseButtonDownEvent())
                {
                    if (FixedShortcuts.structureBuild_EnableCommitOnLeftClick(e))
                    {
                        useOnGrid();
                        _state = State.Ready;
                        _segments.Clear();
                    }
                    else
                    {
                        var segment = new Segment();
                        _segments.Add(segment);
                        segment.start = _currentSegment.end;
                        segment.end = segment.start;
                        _currentSegment.transferLastStack(segment);

                        _currentSegment = segment;
                    }

                    e.disable();
                }
                else if (e.isRightMouseButtonDownEvent())
                {
                    if (FixedShortcuts.selectionSegments_EnableStepBack(e))
                    {
                        if (_segments.Count > 1)
                        {
                            _segments.RemoveAt(_segments.Count - 1);
                            _currentSegment = _segments[_segments.Count - 1];
                        }
                    }
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
            _segments.Clear();
        }

        public override void draw(Color borderColor)
        {
            Material material = MaterialPool.instance.simpleDiffuse;
            material.setZTestEnabled(true);
            material.setZWriteEnabled(true);
            material.setCullModeBack();

            int numPasses = material.passCount;
            material.SetColor("_Color", borderColor);

            var mirrorGizmo = tileRuleGrid.mirrorGizmo;
            foreach (var segment in _segments) 
            {
                foreach (var stack in segment.stacks)
                {
                    if (stack.coords.Count != 0)
                    {
                        OBB stackOBB = calcStackOBB(stack);
                        Matrix4x4 transformMtx = stackOBB.transformMatrix;
                        for (int i = 0; i < numPasses; ++i)
                        {
                            material.SetPass(i);
                            Graphics.DrawMeshNow(MeshPool.instance.unitWireBox, transformMtx);
                        }

                        if (mirrorGizmo.enabled)
                        {
                            var cellRange = stack.calcCellRange();
                            mirrorGizmo.mirrorTileRuleGridCellRange(cellRange, _mirroredCellRanges);
                            foreach (var range in _mirroredCellRanges)
                            {
                                OBB rangeOBB = tileRuleGrid.calcCellRangeOBB(range.min, range.max);
                                mirrorGizmo.drawMirroredOBB(rangeOBB);
                            }
                        }
                    }
                }
            }

            if (_segments.Count != 0)
            {                
                Handles.BeginGUI();
                if (_segments[0].stacks.Count != 0)
                {
                    Handles.Label(HandlesEx.calcLabelPositionBelowOBB(calcStackOBB(_segments[0].firstStack)),
                        string.Format("Cell: {0}", _segments[0].firstStack.baseCellCoords), GUIStyleDb.instance.sceneViewInfoLabel);
                }
                if (_segments[_segments.Count - 1].stacks.Count != 0)
                {
                    var lastStack = _segments[_segments.Count - 1].lastStack;
                    Handles.Label(HandlesEx.calcLabelPositionBelowOBB(calcStackOBB(lastStack)),
                        string.Format("Cell: {0}", lastStack.baseCellCoords), GUIStyleDb.instance.sceneViewInfoLabel);
                }
                Handles.EndGUI();
            }
        }

        public override void drawShadow(Color shadowLineColor, Color shadowColor)
        {
            foreach (var segment in _segments)
            {
                foreach (var stack in segment.stacks)
                {
                    if (stack.coords.Count != 0)
                    {
                        OBB stackOBB = calcStackOBB(stack);
                        if (tileRuleGrid.calcShadowCasterOBBCorners(stackOBB, stack.baseCellCoords.y, _shadowCasterCorners))
                            tileRuleGrid.drawShadow(_shadowCasterCorners, shadowLineColor, shadowColor);
                    }
                }
            }
        }

        public override void getCellCoords(HashSet<Vector3Int> cellCoords)
        {
            cellCoords.Clear();

            var mirrorGizmo = tileRuleGrid.mirrorGizmo;
            if (mirrorGizmo.enabled)
            {
                foreach (var segment in _segments)
                {
                    foreach (var stack in segment.stacks)
                    {
                        foreach (var cell in stack.coords)
                        {
                            cellCoords.Add(cell);
                            mirrorGizmo.mirrorTileRuleGridCellCoords(cell, _mirroredCells);

                            foreach (var mc in _mirroredCells)
                                cellCoords.Add(mc);
                        }
                    }
                }
            }
            else
            {
                foreach (var segment in _segments)
                {
                    foreach (var stack in segment.stacks)
                    {
                        foreach (var cell in stack.coords)
                            cellCoords.Add(cell);
                    }
                }
            }
        }

        public override void getCellsAroundVerticalBorder(int radius, List<Vector3Int> cellCoords)
        {
            cellCoords.Clear();

            _cellCoordsSet.Clear();
            foreach (var segment in _segments)
            {
                foreach (var stack in segment.stacks)
                {
                    foreach (var cell in stack.coords)
                        _cellCoordsSet.Add(cell);
                }
            }

            var mirrorGizmo = tileRuleGrid.mirrorGizmo;
            foreach (var segment in _segments)
            {
                int numStacks = segment.stacks.Count;
                for (int i = 0; i < numStacks; ++i)
                {
                    var stack = segment.stacks[i];
                    if (stack.coords.Count != 0)
                    {
                        foreach (var coords in stack.coords)
                        {
                            TileRuleGrid.getCellsAroundVerticalBorder(coords, radius, _cellCoordsBuffer);
                            _cellCoordsBuffer.RemoveAll(item => _cellCoordsSet.Contains(item) || cellCoords.Contains(item));
                            cellCoords.AddRange(_cellCoordsBuffer);

                            if (mirrorGizmo.enabled)
                            {
                                foreach (var c in _cellCoordsBuffer)
                                {
                                    mirrorGizmo.mirrorTileRuleGridCellCoords(c, _mirroredCells);
                                    foreach (var mc in _mirroredCells)
                                        cellCoords.Add(mc);
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void getCellCoordsBelowBrush(List<Vector3Int> cellCoords)
        {
            cellCoords.Clear();

            Vector3Int coordsBelow;
            var mirrorGizmo = tileRuleGrid.mirrorGizmo;

            if (mirrorGizmo.enabled)
            {
                foreach (var segment in _segments)
                {
                    foreach (var stack in segment.stacks)
                    {
                        if (stack.coords.Count != 0)
                        {
                            if (stack.height > 0) coordsBelow = stack.coords[0];
                            else coordsBelow = stack.coords[stack.coords.Count - 1];

                            --coordsBelow.y;
                            cellCoords.Add(coordsBelow);

                            mirrorGizmo.mirrorTileRuleGridCellCoords(coordsBelow, _mirroredCells);
                            for (int i = 0; i < _mirroredCells.Count; ++i)
                            {
                                var mc = _mirroredCells[i];

                                // Note: Check the coordinate was mirrored against the XZ plane. In that
                                //       case we need to correct. Below means below and we can't let it
                                //       become above.
                                if (mc.y != coordsBelow.y) mc.y -= (stack.coords.Count + 1);
                                cellCoords.Add(mc);
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var segment in _segments)
                {
                    foreach (var stack in segment.stacks)
                    {
                        if (stack.coords.Count != 0)
                        {
                            if (stack.height > 0) coordsBelow = stack.coords[0];
                            else coordsBelow = stack.coords[stack.coords.Count - 1];

                            --coordsBelow.y;
                            cellCoords.Add(coordsBelow);
                        }
                    }
                }
            }
        }

        private IntPatternSampler getHeightPatternSampler()
        {
            switch (spawnSettings.segBrushHeightPatternWrapMode)
            {
                case IntPatternWrapMode.Repeat:

                    return _repeatHeightPatternSampler;

                case IntPatternWrapMode.Mirror:

                    return _mirrorHeightPatternSampler;

                case IntPatternWrapMode.Clamp:

                    return _clampHeightPatternSampler;

                default:

                    return null;
            }
        }

        private bool pickCell(out Vector3Int cellCoords)
        {
            _snappedToTile  = false;
            cellCoords      = new Vector3Int(0, 0, 0);

            var ray = PluginCamera.camera.getCursorRay();
            if (_state == State.Ready)
            {
                if (spawnSettings.snapBrushYOffsetToTiles)
                {
                    if (tileRuleGrid.pickTileCellCoords(ray, usage != TileRuleBrushUsage.Erase, out cellCoords))
                    {
                        _snappedToTile  = true;
                        _snappedYOffset = cellCoords.y;
                        return true;
                    }
                    else return pickCellFromGridHit(ray, out cellCoords);
                }
                else return pickCellFromGridHit(ray, out cellCoords);
            }
            else
            {
                if (spawnSettings.snapBrushYOffsetToTiles)
                {
                    var hit = tileRuleGrid.raycast(ray, _snappedYOffset);
                    if (hit != null)
                    {
                        cellCoords = hit.hitCellCoords;
                        return true;
                    }
                }
                else return pickCellFromGridHit(ray, out cellCoords);
            }

            return false;
        }

        private bool pickCellFromGridHit(Ray ray, out Vector3Int cellCoords)
        {
            cellCoords  = Vector3Int.zero;
            var hit     = tileRuleGrid.raycast(ray, gridSitsBelowBrush ? yOffset : 0);
            if (hit != null)
            {
                cellCoords      = hit.hitCellCoords;
                cellCoords.y    += gridSitsBelowBrush ? 0 : yOffset;
                return true;
            }

            return false;
        }

        private OBB calcStackOBB(SegmentCellStack stack)
        {
            int numCells        = stack.coords.Count;
            if (numCells == 0) return OBB.getInvalid();

            Vector3 cellSize    = tileRuleGrid.settings.cellSize;

            OBB stackOBB        = tileRuleGrid.calcVisualCellOBB(stack.baseCellCoords);
            stackOBB.center     += (numCells - 1) * cellSize.y * 0.5f * tileRuleGrid.gridUp * Mathf.Sign(stack.height);
            stackOBB.size       = new Vector3(cellSize.x, cellSize.y * numCells, cellSize.z);
            stackOBB.rotation   = tileRuleGrid.gridRotation;

            return stackOBB;
        }

        private void calcSegmentCoords(Vector3Int start, Vector3Int end, List<Vector3Int> coords)
        {
            coords.Clear();

            // Note: All cells have the same Y coordinate.
            int yCoord = start.y;

            // Note: The algo works without handling the special case of perfectly
            //       horizontal/vertical lines, but it makes things easier to handle
            //       these cases here when filling corners.
            if (start.x == end.x)
            {
                if (start.z == end.z) coords.Add(start);
                else
                {
                    int deltaZ      = end.z - start.z;
                    int addZ        = deltaZ < 0 ? -1 : 1;
                    int overLast    = end.z + addZ;
                    for (int z = start.z; z != overLast; z += addZ)
                        coords.Add(new Vector3Int(start.x, yCoord, z));
                }

                return;
            }

            if (start.z == end.z)
            {
                if (start.x == end.x) coords.Add(start);
                else
                {
                    int deltaX = end.x - start.x;
                    int addX = deltaX < 0 ? -1 : 1;
                    int overLast = end.x + addX;
                    for (int x = start.x; x != overLast; x += addX)
                        coords.Add(new Vector3Int(x, yCoord, start.z));
                }

                return;
            }

            // Source: https://stackoverflow.com/a/11683720
            int x0 = start.x, x1 = end.x;
            int z0 = start.z, z1 = end.z;

            int dx = x1 - x0;
            int dz = z1 - z0;
            int dx0 = 0, dz0 = 0, dx1 = 0, dz1 = 0;

            if (dx < 0) dx0 = -1; else if (dx > 0) dx0 = 1;
            if (dz < 0) dz0 = -1; else if (dz > 0) dz0 = 1;
            if (dx < 0) dx1 = -1; else if (dx > 0) dx1 = 1;

            int longest = Mathf.Abs(dx);
            int shortest = Mathf.Abs(dz);
            if (longest < shortest)
            {
                longest = Mathf.Abs(dz);
                shortest = Mathf.Abs(dx);

                if (dz < 0) dz1 = -1;
                else if (dz > 0) dz1 = 1;

                dx1 = 0;
            }

            // Note: Needed to handle corner filling.
            int absDX = Mathf.Abs(dx);
            int absDZ = Mathf.Abs(dz);

            int numerator = longest >> 1;
            for (int i = 0; i <= longest; i++)
            {
                coords.Add(new Vector3Int(x0, yCoord, z0));

                numerator += shortest;
                if (!(numerator < longest))
                {
                    numerator -= longest;
                    x0 += dx0;
                    z0 += dz0;

                    // Note: Fill corners.
                    if (i < longest && spawnSettings.segBrushFillCorners)
                    {
                        if (absDX >= absDZ) coords.Add(new Vector3Int(x0, yCoord, z0 - dz0));
                        else coords.Add(new Vector3Int(x0 - dx0, yCoord, z0));
                    }
                }
                else
                {
                    x0 += dx1;
                    z0 += dz1;
                }
            }
        }
    }
}
#endif
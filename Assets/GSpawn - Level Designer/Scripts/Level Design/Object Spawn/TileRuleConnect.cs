#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GSpawn
{
    public class TileRuleConnectionPath
    {
        public List<Vector3Int> cells = new List<Vector3Int>();

        public Vector3Int firstCell { get { return cells[0]; } }
        public Vector3Int lastCell  { get { return cells[cells.Count - 1]; } }
    }

    public class TileRuleConnect
    {
        private enum State
        {
            PickStart = 0,
            PickEnd
        }

        private List<Vector3>                   _shadowCasterCorners        = new List<Vector3>();
        private List<TileRuleConnectionPath>    _connectionPaths            = new List<TileRuleConnectionPath>();
        private List<TileRuleConnectionPath>    _mirroredConnectionPaths    = new List<TileRuleConnectionPath>();
        private List<TileRuleGridCellRange>     _mirroredCellRanges         = new List<TileRuleGridCellRange>();

        private State                       _state              = State.PickStart;
        private OBB                         _startOBB           = OBB.getInvalid();
        private OBB                         _endOBB             = OBB.getInvalid();
        private Vector3Int                  _startCell          = new Vector3Int();
        private Vector3Int                  _endCell            = new Vector3Int();

       private TileRuleObjectSpawnSettings  spawnSettings       { get { return ObjectSpawn.instance.tileRuleObjectSpawn.settings; } }

        public TileRuleGrid             tileRuleGrid        { get; set; }
        public bool                     gridSitsBelow       { get; set; }
        public int                      startYOffset        { get; set; }
        public int                      endYOffset          { get; set; }
        public bool                     pickingStart        { get { return _state == State.PickStart; } }
        public bool                     pickingEnd          { get { return _state == State.PickEnd; } }
        public int                      numConnectionPaths  { get { return _connectionPaths.Count; } }

        public void cancel()
        {
            _state          = State.PickStart;
            _endOBB         = OBB.getInvalid();
            _startOBB       = OBB.getInvalid();
            _connectionPaths.Clear();
        }

        public TileRuleConnectionPath getConnectionPath(int index)
        {
            return _connectionPaths[index];
        }

        public void onSceneGUI()
        {
            Event e = Event.current;
            if (_state == State.PickStart) 
            {
                pickStart();
                if (e.isLeftMouseButtonDownEvent())
                {
                    if (_startOBB.isValid) _state = State.PickEnd;
                }
            }
            else
            if (_state == State.PickEnd) 
            {
                pickEnd();
                if (_endOBB.isValid)
                {
                    generateManhattanConnectionCells();
                }

                if (e.isLeftMouseButtonDownEvent())
                {
                    //if (FixedShortcuts.structureBuild_EnableCommitOnLeftClick(e))
                    {
                        if (_startOBB.isValid && _endOBB.isValid)
                        {
                            tileRuleGrid.connect(this);

                            _connectionPaths.Clear();
                            _startCell = _endCell;
                            _startOBB = _endOBB;

                            cancel();
                        }
                    }
/*
                    else
                    {
                        tileRuleGrid.connect(this);
                        _connectionPaths.Clear();
                        _startCell = _endCell;
                        _startOBB = _endOBB;
                    }*/
                }
                else
                if (FixedShortcuts.cancelAction(e))
                {
                    e.disable();
                    cancel();
                }
            }
        }

        public void draw()
        {
            Material material = MaterialPool.instance.simpleDiffuse;
            material.setZTestEnabled(true);
            material.setZWriteEnabled(true);
            material.setCullModeBack();

            int numPasses = material.passCount;
            if (_state == State.PickStart)
            {
                if (_startOBB.isValid)
                {
                    material.SetColor("_Color", ObjectSpawnPrefs.instance.trSpawnConnectColor);
                    Matrix4x4 transformMtx = _startOBB.transformMatrix;
                    for (int i = 0; i < numPasses; ++i)
                    {
                        material.SetPass(i);
                        Graphics.DrawMeshNow(MeshPool.instance.unitWireBox, transformMtx);
                    }

                    var mirrorGizmo = tileRuleGrid.mirrorGizmo;
                    if (mirrorGizmo.enabled)
                    {
                        var cellRange = new TileRuleGridCellRange(_startCell, _startCell);
                        mirrorGizmo.mirrorTileRuleGridCellRange(cellRange, _mirroredCellRanges);
                        foreach (var range in _mirroredCellRanges)
                        {
                            OBB rangeOBB = tileRuleGrid.calcCellRangeOBB(range.min, range.max);
                            mirrorGizmo.drawMirroredOBB(rangeOBB);
                        }
                    }
                }
            }
            else
            {
                if (_connectionPaths.Count != 0)
                {
                    material.SetColor("_Color", ObjectSpawnPrefs.instance.trSpawnConnectColor);
                    foreach (var cell in _connectionPaths[0].cells)
                    {
                        OBB cellOBB = tileRuleGrid.calcVisualCellOBB(cell);
                        Matrix4x4 transformMtx = cellOBB.transformMatrix;
                        for (int i = 0; i < numPasses; ++i)
                        {
                            material.SetPass(i);
                            Graphics.DrawMeshNow(MeshPool.instance.unitWireBox, transformMtx);
                        }
                    }

                    var mirrorGizmo = tileRuleGrid.mirrorGizmo;
                    if (mirrorGizmo.enabled)
                    {
                        for (int i = 1; i < _connectionPaths.Count; ++i)
                        {
                            var path = _connectionPaths[i];
                            foreach (var cell in path.cells)
                            {
                                OBB cellOBB = tileRuleGrid.calcVisualCellOBB(cell);
                                mirrorGizmo.drawMirroredOBB(cellOBB);
                            }
                        }
                    }
                }
            }

            if (_startOBB.isValid)
            {
                Handles.BeginGUI();
                Handles.Label(HandlesEx.calcLabelPositionBelowOBB(_startOBB),
                    string.Format("Cell: {0}", _startCell), GUIStyleDb.instance.sceneViewInfoLabel);
                Handles.EndGUI();
            }
            if (_endOBB.isValid)
            {
                Handles.BeginGUI();
                Handles.Label(HandlesEx.calcLabelPositionBelowOBB(_endOBB),
                    string.Format("Cell: {0}", _endCell), GUIStyleDb.instance.sceneViewInfoLabel);
                Handles.EndGUI();
            }
        }

        public void drawShadow(Color shadowLineColor, Color shadowColor)
        {
            if (_state == State.PickStart)
            {
                if (_startOBB.isValid)
                {
                    if (_startCell.y != 0)
                    {
                        if (tileRuleGrid.calcShadowCasterOBBCorners(_startOBB, _startCell.y, _shadowCasterCorners))
                            tileRuleGrid.drawShadow(_shadowCasterCorners, shadowLineColor, shadowColor);
                    }
                }
            }
            else
            {
                if (_connectionPaths.Count != 0)
                {
                    foreach (var cell in _connectionPaths[0].cells)
                    {
                        OBB cellOBB = tileRuleGrid.calcVisualCellOBB(cell);
                        if (tileRuleGrid.calcShadowCasterOBBCorners(cellOBB, cell.y, _shadowCasterCorners))
                            tileRuleGrid.drawShadow(_shadowCasterCorners, shadowLineColor, shadowColor);
                    }
                }
            }
        }

        private bool pickStart()
        {
            bool picked = false;
            var ray     = PluginCamera.camera.getCursorRay();

            var hit = tileRuleGrid.raycast(ray, gridSitsBelow ? startYOffset : 0);
            if (hit != null)
            {
                _startCell      = hit.hitCellCoords;
                _startCell.y    += gridSitsBelow ? 0 : startYOffset;
                _startOBB       = tileRuleGrid.calcVisualCellOBB(_startCell);
                picked          = true;
            }

            // Now try snapping to a tile
            Vector3Int cellCoords;
            if (tileRuleGrid.pickTileCellCoords(ray, out cellCoords))
            {
                _startCell      = cellCoords;
                _startOBB       = tileRuleGrid.calcVisualCellOBB(_startCell);
                return true;
            }

            if (picked) return true;

            _startOBB = OBB.getInvalid();
            return false;
        }

        private bool pickEnd()
        {
            bool picked = false;
            var ray = PluginCamera.camera.getCursorRay();

            // Pick a grid cell first
            var hit = tileRuleGrid.raycast(ray, gridSitsBelow ? endYOffset : 0);
            if (hit != null)
            {
                _endCell    = hit.hitCellCoords;
                _endCell.y  += gridSitsBelow ? 0 : endYOffset;
                _endOBB     = tileRuleGrid.calcVisualCellOBB(_endCell);
                picked      = true;
            }

            // Now try snapping to a tile
            Vector3Int cellCoords;
            if (tileRuleGrid.pickTileCellCoords(ray, out cellCoords))
            {
                _endCell = cellCoords;
                _endOBB = tileRuleGrid.calcVisualCellOBB(_endCell);
                return true;
            }

            if (picked) return true;

            _endOBB = OBB.getInvalid();
            return false;
        }

        private void generateManhattanConnectionCells()
        {
            _connectionPaths.Clear();
            var mainPath = new TileRuleConnectionPath();
            _connectionPaths.Add(mainPath);

            if (!_startOBB.isValid || !_endOBB.isValid) return;

            int addX        = _startCell.x < _endCell.x ? 1 : -1;
            int overLastX   = _endCell.x + addX;
            int absDX       = Mathf.Abs(_endCell.x - _startCell.x);
  
            int addZ        = _startCell.z < _endCell.z ? 1 : -1;
            int overLastZ   = _endCell.z + addZ;
            int absDZ       = Mathf.Abs(_endCell.z - _startCell.z);

            int addY        = _startCell.y < _endCell.y ? 1 : -1;
            int overLastY   = _endCell.y + addY;
            int absDY       = Mathf.Abs(_endCell.y - _startCell.y);

            int totalNumCells   = absDX + absDZ;
            int yIncFreq        = 0;
            if (absDY != 0)
            {
                yIncFreq = totalNumCells / absDY;
                if (totalNumCells < absDY) yIncFreq = 1;
            }
            else addY = 0;

            int yCounter    = 0;
            int yCoord      = _startCell.y;

            if (spawnSettings.connectMajorAxis == TileRuleConnectMajorAxis.X)
            {
                for (int x = _startCell.x; x != overLastX; x += addX)
                {
                    mainPath.cells.Add(new Vector3Int(x, yCoord, _startCell.z));

                    if (addY != 0 && yCoord != _endCell.y)
                    {
                        ++yCounter;
                        if (yCounter % yIncFreq == 0)
                        {
                            yCounter = 0;
                            yCoord += addY;
                        }
                    }
                }
                for (int z = _startCell.z + addZ; z != overLastZ; z += addZ)
                {
                    mainPath.cells.Add(new Vector3Int(_endCell.x, yCoord, z));

                    if (addY != 0 && yCoord != _endCell.y)
                    {
                        ++yCounter;
                        if (yCounter % yIncFreq == 0)
                        {
                            yCounter = 0;
                            yCoord += addY;
                        }
                    }
                }
            }
            else
            {
                for (int z = _startCell.z; z != overLastZ; z += addZ)
                {
                    mainPath.cells.Add(new Vector3Int(_startCell.x, yCoord, z));

                    if (addY != 0 && yCoord != _endCell.y)
                    {
                        ++yCounter;
                        if (yCounter % yIncFreq == 0)
                        {
                            yCounter = 0;
                            yCoord += addY;
                        }
                    }
                }
                for (int x = _startCell.x + addX; x != overLastX; x += addX)
                {
                    mainPath.cells.Add(new Vector3Int(x, yCoord, _endCell.z));

                    if (addY != 0 && yCoord != _endCell.y)
                    {
                        ++yCounter;
                        if (yCounter % yIncFreq == 0)
                        {
                            yCounter = 0;
                            yCoord += addY;
                        }
                    }
                }
            }

            Vector3Int lastCell = mainPath.cells[mainPath.cells.Count - 1];
            if (lastCell.y != _endCell.y)
            {
                for (int y = lastCell.y + addY; y != overLastY; y += addY)
                {
                    mainPath.cells.Add(new Vector3Int(lastCell.x, y, lastCell.z));
                }
            }

            var mirrorGizmo = tileRuleGrid.mirrorGizmo;
            if (mirrorGizmo.enabled)
            {
                mirrorGizmo.mirrorTileRuleConnectionPath(mainPath, _mirroredConnectionPaths);
                _connectionPaths.AddRange(_mirroredConnectionPaths);
            }
        }
    }
}
#endif
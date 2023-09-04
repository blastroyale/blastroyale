#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace GSpawn
{
    public class ObjectSpawnCellBox
    {
        public struct DrawConfig
        {
            public Color    xAxisColor;
            public Color    yAxisColor;
            public Color    zAxisColor;

            public float    xAxisLength;
            public float    yAxisLength;
            public float    zAxisLength;

            public Color    cellWireColor;

            public bool     hasUniformHeight;
            public int      height;
            public bool     drawInfoText;

            public bool     drawGranular;
        }

        private OBB                     _refObjectOBB;
        private Vector3                 _heightAxis;
        private Vector3                 _rightAxis;
        private Vector3                 _lookAxis;
        private float                   _horizontalPadding;
        private float                   _verticalPadding;
        private ObjectSpawnCellStack[]  _stacks;
        private int                     _numColumns;
        private int                     _numRows;

        public OBB                      refObjectOBB        { get { return _refObjectOBB; } }
        public Quaternion               objectRotation      { get { return _refObjectOBB.rotation; } }
        public Vector3                  startPosition       { get { return _refObjectOBB.center; } }
        public Vector3                  heightAxis          { get { return _heightAxis; } }
        public Vector3                  rightAxis           { get { return _rightAxis; } }
        public Vector3                  lookAxis            { get { return _lookAxis; } }
        public float                    horizontalPadding   { get { return _horizontalPadding; } }
        public float                    verticalPadding     { get { return _verticalPadding; } }
        public int                      numColumns          { get { return _numColumns; } }
        public int                      numRows             { get { return _numRows; } }
        public Vector2Int               size                { get { return new Vector2Int(_numColumns, _numRows); } }

        public ObjectSpawnCellBox(OBB refObjectOBB, Vector3 heightAxis, Vector3 rightAxis)
        {
            _refObjectOBB = refObjectOBB;
            _heightAxis = heightAxis.normalized;
            _rightAxis = rightAxis.normalized;
            updateLookAxis();
        }

        public ObjectSpawnCellStack getStack(int column, int row)
        {
            return _stacks[row * _numColumns + column];
        }

        public Vector3 calcStartCorner()
        {
            Vector3 start = startPosition;
            start -= _rightAxis * 0.5f * calcCellSizeAlongAxis(_rightAxis);
            start -= _lookAxis * 0.5f * calcCellSizeAlongAxis(_lookAxis);
            start -= _heightAxis * 0.5f * calcCellSizeAlongAxis(_heightAxis);
            return start;
        }

        public float calcCellSizeAlongAxis(Vector3 axis)
        {
            return Vector3Ex.getSizeAlongAxis(_refObjectOBB.size, _refObjectOBB.rotation, axis);
        }

        public Vector3 calcStackPosition(int column, int row, float cellSizeAlongRight, float cellSizeAlongLook)
        {
            Vector3 pos = _refObjectOBB.center + column * _rightAxis * (cellSizeAlongRight + _horizontalPadding);
            pos += row * _lookAxis * (cellSizeAlongLook + _horizontalPadding);
            return pos;
        }

        public Vector2Int snapSizeAndExtensionAxesToCursor(ObjectSpawnExtensionPlane extensionPlane, bool equalSize, Vector2Int maxSize)
        {
            Vector3 intersectPt;
            if (extensionPlane.cursorRaycast(out intersectPt))
            {
                Vector3 toIntersectPt = intersectPt - startPosition;

                Vector2Int newSize      = new Vector2Int();
                float cellSize          = calcCellSizeAlongAxis(_rightAxis) + _horizontalPadding;
                float projectedLength   = Vector3.Dot(toIntersectPt, _rightAxis.normalized);
                if (projectedLength < 0.0f) _rightAxis = -_rightAxis;
                newSize.x = 1 + (int)(Mathf.Abs(projectedLength) / cellSize);
                if (newSize.x >= 2 && newSize.x > maxSize.x) newSize.x = maxSize.x;

                cellSize                = calcCellSizeAlongAxis(_lookAxis) + _horizontalPadding;
                projectedLength         = Vector3.Dot(toIntersectPt, _lookAxis.normalized);
                if (projectedLength < 0.0f) _lookAxis = -_lookAxis;
                newSize.y = 1 + (int)(Mathf.Abs(projectedLength) / cellSize);
                if (newSize.y >= 2 && newSize.y > maxSize.y) newSize.y = maxSize.y;

                if (equalSize)
                {
                    int size = Mathf.Max(newSize.x, newSize.y);
                    newSize.x = newSize.y = size;
                }

                return setSize(newSize.x, newSize.y);
            }

            return size;
        }

        public void setVerticalPadding(float padding)
        {
            _verticalPadding = padding;
            if (_stacks == null) return;

            foreach (var stack in _stacks)
                stack.setVerticalPadding(padding);
        }

        public void setHorizontalPadding(float padding)
        {
            _horizontalPadding = padding;
            if (_stacks == null) return;

            float cellSizeAlongRightAxis    = calcCellSizeAlongAxis(_rightAxis);
            float cellSizeAlongLookAxis     = calcCellSizeAlongAxis(_lookAxis);

            for (int row = 0; row < _numRows; ++row)
            {
                for (int col = 0; col < _numColumns; ++col)
                {
                    Vector3 stackStartPosition = calcStackPosition(col, row, cellSizeAlongRightAxis, cellSizeAlongLookAxis);
                    _stacks[row * _numColumns + col].setStartPosition(stackStartPosition);
                }
            }         
        }

        public void setHeight(int height)
        {
            if (_stacks == null) return;

            foreach (var stack in _stacks)
                stack.setHeight(height);
        }

        public Vector2Int setSize(int numColumns, int numRows, int defaultStackHeight = 1)
        {
            Vector2Int oldSize = size;
            if (size.x == numColumns && size.y == numRows) return oldSize;

            if (numColumns == 0 || numRows == 0)
            {
                _numColumns = numColumns;
                _numRows    = numRows;
                _stacks     = null;
            }

            float cellSizeAlongRight    = calcCellSizeAlongAxis(_rightAxis);
            float cellSizeAlongLook     = calcCellSizeAlongAxis(_lookAxis);

            if (_stacks == null)
            {
                _stacks         = new ObjectSpawnCellStack[numColumns * numRows];
                _numColumns     = numColumns;
                _numRows        = numRows;

                for (int row = 0; row < _numRows; ++row)
                {
                    for (int col = 0; col < _numColumns; ++col)
                    {
                        Vector3 stackPos = calcStackPosition(col, row, cellSizeAlongRight, cellSizeAlongLook);
                        var stack = new ObjectSpawnCellStack(stackPos, _heightAxis, _refObjectOBB.size, _refObjectOBB.rotation);
                        stack.setVerticalPadding(_verticalPadding);
                        stack.setHeight(defaultStackHeight);
                        _stacks[row * _numColumns + col] = stack;
                    }
                }
            }
            else
            {
                var newStacks       = new ObjectSpawnCellStack[numColumns * numRows];
                var stackHeights    = new int[numColumns * numRows];
                for (int row = 0; row < numRows; ++row)
                {
                    for (int col = 0; col < numColumns; ++col)
                    {
                        if (col < _numColumns && row < _numRows) stackHeights[row * numColumns + col] = _stacks[row * _numColumns + col].height;
                        else stackHeights[row * numColumns + col] = defaultStackHeight;
                    }
                }

                for (int row = 0; row < numRows; ++row)
                {
                    for (int col = 0; col < numColumns; ++col)
                    {
                        Vector3 stackPos = calcStackPosition(col, row, cellSizeAlongRight, cellSizeAlongLook);
                        var stack = new ObjectSpawnCellStack(stackPos, _heightAxis, _refObjectOBB.size, _refObjectOBB.rotation);
                        stack.setVerticalPadding(_verticalPadding);
                        stack.setHeight(stackHeights[row * numColumns + col]);
                        newStacks[row * numColumns + col] = stack;
                    }
                }

                _stacks         = newStacks;
                _numColumns     = numColumns;
                _numRows        = numRows;
            }

            return oldSize;
        }

        public void draw(DrawConfig drawConfig)
        {
            if (_stacks == null) return;

            HandlesEx.saveColor();
            HandlesEx.saveMatrix();

            Handles.color = drawConfig.cellWireColor;
            if (drawConfig.drawGranular)
            {
                foreach (var stack in _stacks)
                {
                    int numCells = stack.numCells;
                    for (int i = 0; i < numCells; ++i)
                    {
                        var cell = stack.getCell(i);
                        if (!cell.isGoodForSpawn) continue;

                        Handles.matrix = cell.objectOBB.transformMatrix;
                        //Handles.DrawWireCube(Vector3.zero, Vector3.one);
                        HandlesEx.drawUnitWireCube();
                    }
                }
            }
            else
            {
                foreach (var stack in _stacks)
                {
                    if (!stack.anyCellsGoodForSpawn()) continue;

                    var obb         = stack.getCell(0).objectOBB;
                    obb.center      = (stack.startPosition + stack.endPosition) * 0.5f;
                    Vector3 size    = obb.size;
                    obb.size        = new Vector3(size.x, calcCellSizeAlongAxis(_heightAxis) * stack.numCells, size.z);

                    Handles.matrix = obb.transformMatrix;
                    //Handles.DrawWireCube(Vector3.zero, Vector3.one);
                    HandlesEx.drawUnitWireCube();
                }
            }

            HandlesEx.restoreMatrix();

            Vector3 startCorner = calcStartCorner();
            Handles.color = drawConfig.xAxisColor;
            Handles.DrawLine(startCorner, startCorner + rightAxis * drawConfig.xAxisLength);
            Handles.color = drawConfig.yAxisColor;
            Handles.DrawLine(startCorner, startCorner + heightAxis * drawConfig.yAxisLength);
            Handles.color = drawConfig.zAxisColor;
            Handles.DrawLine(startCorner, startCorner + lookAxis * drawConfig.zAxisLength);

            if (drawConfig.drawInfoText)
            {
                Handles.BeginGUI();
                Vector3 labelPos = startCorner;
                labelPos -= _rightAxis * calcCellSizeAlongAxis(_rightAxis) * 0.5f;
                labelPos -= _heightAxis * calcCellSizeAlongAxis(_heightAxis) * 0.5f;
                labelPos -= _lookAxis * calcCellSizeAlongAxis(_lookAxis) * 0.5f;
                if (drawConfig.hasUniformHeight) Handles.Label(labelPos, "X: " + numColumns + ", Y:" + drawConfig.height + ", Z: " + numRows, GUIStyleDb.instance.sceneViewInfoLabel);
                else Handles.Label(labelPos, "X: " + numColumns + ", Y: ?" + ", Z: " + numRows, GUIStyleDb.instance.sceneViewInfoLabel);
                Handles.EndGUI();
            }

            HandlesEx.restoreColor();
        }

        private void updateLookAxis()
        {
            _lookAxis = Vector3.Cross(_rightAxis, _heightAxis).normalized;
        }
    }
}
#endif
#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectSpawnCellStack
    {
        private int             _height;
        private Vector3         _startPosition;
        private Vector3         _heightAxis;
        private Vector3         _cellSize;
        private Quaternion      _objectRotation;
        private float           _verticalPadding;
        private OBB             _obb = OBB.getInvalid();

        private List<ObjectSpawnCell>   _cells = new List<ObjectSpawnCell>();

        public int              height          { get { return _height; } }
        public Vector3          startPosition   { get { return _startPosition; } }
        public Vector3          endPosition     { get { return numCells != 0 ? calcCellPosition(numCells - 1, calcCellSizeAlongAxis(_heightAxis)) : _startPosition; } }
        public Vector3          heightAxis      { get { return _heightAxis; } }
        public Vector3          cellSize        { get { return _cellSize; } }
        public Quaternion       objectRotation  { get { return _objectRotation; } }
        public float            verticalPadding { get { return _verticalPadding; } }
        public OBB              obb             { get { return _obb; } }
        public int              numCells        { get { return _cells.Count; } }

        public ObjectSpawnCellStack(Vector3 startPosition, Vector3 heightAxis, Vector3 cellSize, Quaternion objectRotation)
        {
            _startPosition      = startPosition;
            _heightAxis         = heightAxis.normalized;
            _cellSize           = cellSize;
            _objectRotation     = objectRotation;
        }

        public bool anyCellsGoodForSpawn()
        {
            foreach (var cell in _cells)
            {
                if (cell.isGoodForSpawn) return true;
            }

            return false;
        }

        public int cellIndexToHeight(int cellIndex)
        {
            int height = cellIndex + 1;
            if (_height < 0) height = -height;
            return height;
        }

        public float calcCellSizeAlongAxis(Vector3 axis)
        {
            return Vector3Ex.getSizeAlongAxis(_cellSize, _objectRotation, axis);
        }

        public Vector3 calcCellPosition(int cellIndex, float cellSizeAlongHeightAxis)
        {
            return _startPosition + cellIndex * _heightAxis * (cellSizeAlongHeightAxis + _verticalPadding) * Mathf.Sign(_height);
        }

        public ObjectSpawnCell getCell(int index)
        {
            return _cells[index];
        }

        public void setObjectRotation(Quaternion rotation)
        {
            foreach (var cell in _cells)
                cell.setObjectOBBRotation(rotation);

            updateOBB();
        }

        public void setAllCellsSkipped(bool skipped)
        {
            foreach (var cell in _cells)
                cell.skipped = skipped;
        }

        public void setAllCellsOccluded(bool occluded)
        {
            foreach (var cell in _cells)
                cell.occluded = occluded;
        }

        public void setAllCellsOutOfScope(bool outOfScope)
        {
            foreach (var cell in _cells)
                cell.outOfScope = outOfScope;
        }

        public void setStartPosition(Vector3 startPosition)
        {
            Vector3 offset = startPosition - _startPosition;
            _startPosition = startPosition;

            foreach (var cell in _cells)
                cell.offsetObjectOBBCenter(offset);

            updateOBB();
        }

        public void offsetStartPosition(Vector3 offset)
        {
            _startPosition += offset;
            foreach (var cell in _cells)
                cell.offsetObjectOBBCenter(offset);

            updateOBB();
        }

        public void setHeight(int height)
        {
            if (_height == height) return;

            // Note: If the cell size is 0 along the height axis, we will force the height to always be 1.
            if (calcCellSizeAlongAxis(_heightAxis) < 1e-4f) height = 1;

            int oldHeight   = _height;
            _height         = height;

            if (height == 0) _cells.Clear();
            else
            {
                float cellSizeAlongHeightAxis = calcCellSizeAlongAxis(_heightAxis);
                if (height > 0)
                {
                    if (oldHeight > 0)
                    {
                        int delta = height - oldHeight;
                        if (delta < 0) _cells.RemoveRange(height, Mathf.Abs(delta));
                        else addCells(delta, cellSizeAlongHeightAxis);
                    }
                    else
                    if (oldHeight < 0)
                    {
                        _cells.Clear();
                        addCells(height, cellSizeAlongHeightAxis);
                    }
                    else addCells(height, cellSizeAlongHeightAxis);
                }
                else
                {
                    if (oldHeight > 0)
                    {
                        _cells.Clear();
                        addCells(Mathf.Abs(height), cellSizeAlongHeightAxis);
                    }
                    else
                    if (oldHeight < 0)
                    {
                        int delta = height - oldHeight;
                        if (delta < 0) addCells(Mathf.Abs(delta), cellSizeAlongHeightAxis);
                        else if (delta > 0) _cells.RemoveRange(Mathf.Abs(height), Mathf.Abs(delta));
                    }
                    else addCells(Mathf.Abs(height), cellSizeAlongHeightAxis);
                }
            }

            updateOBB();
        }

        public void setVerticalPadding(float padding)
        {
            _verticalPadding                = padding;
            float cellSizeAlongHeightAxis   = calcCellSizeAlongAxis(_heightAxis);

            for (int i = 0; i < numCells; ++i)
                getCell(i).setObjectOBBCenter(calcCellPosition(i, cellSizeAlongHeightAxis));

            updateOBB();
        }

        private void addCells(int numCells, float cellSizeAlongHeightAxis)
        {
            int startIndex = _cells.Count;
            for (int i = 0; i < numCells; ++i)
                _cells.Add(new ObjectSpawnCell(new OBB(calcCellPosition(startIndex + i, cellSizeAlongHeightAxis), _cellSize, _objectRotation)));
        }

        private void updateOBB()
        {
            if (numCells == 0)
            {
                _obb = OBB.getInvalid();
                return;
            }

            _obb            = new OBB((startPosition + endPosition) * 0.5f);
            AABB abb        = new AABB(Vector3.zero, _cellSize);
            abb.transform(Matrix4x4.TRS(Vector3.zero, _objectRotation, Vector3.one));
            _obb.size       = new Vector3(abb.size.x, calcCellSizeAlongAxis(_heightAxis) * numCells, abb.size.z);
        }
    }
}
#endif
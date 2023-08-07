#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;

namespace GSpawn
{
    public class ObjectGrid3D
    {
        private Vector3Int                          _numCells       = Vector3Int.zero;
        private Vector3                             _cellSize       = Vector3.zero;
        private Vector3                             _padding        = Vector3.zero;

        public Vector3                              origin          { get; set; }
        public Quaternion                           cellRotation    { get; set; }
        public Vector3Int                           numCells        { get { return _numCells; } set { _numCells = value.abs(); } }
        public int                                  numCellsX       { get { return _numCells.x; } set { _numCells.x = Mathf.Abs(value); } }
        public int                                  numCellsY       { get { return _numCells.y; } set { _numCells.y = Mathf.Abs(value); } }
        public int                                  numCellsZ       { get { return _numCells.z; } set { _numCells.z = Mathf.Abs(value); } }
        public bool                                 hasCells        { get { return _numCells.x != 0 && _numCells.y != 0 && _numCells.z != 0; } }
        public Vector3                              cellSize        { get { return _cellSize; } set { _cellSize = value.abs(); } }
        public Vector3                              padding         { get { return _padding; } set { _padding = value.abs(); } }
        public Vector3                              right           { get; set; }
        public Vector3                              up              { get; set; }
        public Vector3                              look            { get; set; }
        public Color                                cellWireColor   { get; set; }
        public Color                                cellFillColor   { get; set; }
        public Func<int, int, int, Vector3, bool>   isCellMasked    { get; set; }

        public OBB calcCellOBB(int cellX, int cellY, int cellZ)
        {
            return new OBB(calcCellPosition(cellX, cellY, cellZ), cellSize, cellRotation);
        }

        public Vector3 calcCellPosition(int cellX, int cellY, int cellZ)
        {
            return origin + right * cellX * (cellSize[0] + _padding.x) +
                            up * cellY * (cellSize[1] + _padding.y) +
                            look * cellZ * (cellSize[2] + _padding.z);
        }

        public void draw()
        {
            Event e = Event.current;
            if (e.type != EventType.Repaint) return;

            HandlesEx.saveColor();
            HandlesEx.saveMatrix();
            HandlesEx.saveLit();

            Handles.lighting = false;

            // Note: Clamp cell size. Otherwise, for sprites, the cells are rendered black.
            Vector3 clampedCellSize = Vector3.Max(cellSize, Vector3Ex.create(1e-8f));
            for (int cellX = 0; cellX < numCellsX; ++cellX)
            {
                for (int cellY = 0; cellY < numCellsY; ++cellY)
                {
                    for (int cellZ = 0; cellZ < numCellsZ; ++cellZ)
                    {
                        Vector3 position = calcCellPosition(cellX, cellY, cellZ);
                        if (isCellMasked == null || !isCellMasked(cellX, cellY, cellZ, position))
                        {
                            Handles.matrix = Matrix4x4.TRS(position, cellRotation, clampedCellSize);

                            Handles.color = cellFillColor;
                            Handles.CubeHandleCap(0, Vector3.zero, Quaternion.identity, 1.0f, e.type);

                            Handles.color = cellWireColor;
                            //Handles.DrawWireCube(Vector3.zero, Vector3.one);
                            HandlesEx.drawUnitWireCube();
                        }
                    }
                }
            }

            HandlesEx.restoreColor();
            HandlesEx.restoreMatrix();
            HandlesEx.restoreLit();
        }
    }
}
#endif
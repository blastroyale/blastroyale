#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectSpawnCellSegment
    {
        public struct DrawConfig
        {
            public Color    cellWireColor;
            public bool     drawGranular;
        }

        private OBB         _refObjectOBB;
        private Vector3     _extensionAxis;
        private Vector3     _heightAxis;
        private Vector3     _rightAxis;
        private float       _horizontalPadding;
        private float       _verticalPadding;
        private OBB         _obb = OBB.getInvalid();

        private List<ObjectSpawnCellStack>      _stacks     = new List<ObjectSpawnCellStack>();

        public OBB                  refObjectOBB        { get { return _refObjectOBB; } }
        public Quaternion           objectRotation      { get { return _refObjectOBB.rotation; } }
        public Vector3              startPosition       { get { return _refObjectOBB.center; } }
        public Vector3              endPosition         { get { return startPosition + _extensionAxis * (numStacks - 1) * (calcCellSizeAlongAxis(_extensionAxis) + _horizontalPadding); } }
        public Vector3              extensionAxis       { get { return _extensionAxis; } }
        public Vector3              heightAxis          { get { return _heightAxis; } }
        public Vector3              rightAxis           { get { return _rightAxis; } }
        public float                horizontalPadding   { get { return _horizontalPadding; } }
        public float                verticalPadding     { get { return _verticalPadding; } }
        public OBB                  obb                 { get { return _obb; } }
        public int                  numStacks           { get { return _stacks.Count; } }
        public ObjectSpawnCellStack lastStack           { get { return _stacks[_stacks.Count - 1]; } }

        public static Vector3 pickInitialExtensionAxisByLongestAxis(ObjectSpawnExtensionPlane extensionPlane, OBB refOBB)
        {
            Vector3[] candidateAxes = new Vector3[]
            { extensionPlane.look, -extensionPlane.look, extensionPlane.right, -extensionPlane.right };
            float[] refOBBSizes = new float[]
            {
                Vector3Ex.getSizeAlongAxis(refOBB.size, refOBB.rotation, candidateAxes[0]),
                Vector3Ex.getSizeAlongAxis(refOBB.size, refOBB.rotation, candidateAxes[1]),
                Vector3Ex.getSizeAlongAxis(refOBB.size, refOBB.rotation, candidateAxes[2]),
                Vector3Ex.getSizeAlongAxis(refOBB.size, refOBB.rotation, candidateAxes[3]),
            };

            int bestAxis = -1;
            float maxSize = float.MinValue;

            for (int i = 0; i < candidateAxes.Length; i++)
            {
                // Note: Ignore axes which produce a size of 0.
                if (refOBBSizes[i] < 1e-5f) continue;

                if (refOBBSizes[i] > maxSize)
                {
                    maxSize = refOBBSizes[i];
                    bestAxis = i;
                }
            }

            return candidateAxes[bestAxis];
        }

        public static Vector3 pickInitialExtensionAxisByShortestAxis(ObjectSpawnExtensionPlane extensionPlane, OBB refOBB)
        {
            Vector3[] candidateAxes = new Vector3[]
            { extensionPlane.look, -extensionPlane.look, extensionPlane.right, -extensionPlane.right };
            float[] refOBBSizes = new float[]
            {
                Vector3Ex.getSizeAlongAxis(refOBB.size, refOBB.rotation, candidateAxes[0]),
                Vector3Ex.getSizeAlongAxis(refOBB.size, refOBB.rotation, candidateAxes[1]),
                Vector3Ex.getSizeAlongAxis(refOBB.size, refOBB.rotation, candidateAxes[2]),
                Vector3Ex.getSizeAlongAxis(refOBB.size, refOBB.rotation, candidateAxes[3]),
            };

            int bestAxis    = -1;
            float minSize   = float.MaxValue;

            for (int i = 0; i < candidateAxes.Length; i++)
            {
                // Note: Ignore axes which produce a size of 0.
                if (refOBBSizes[i] < 1e-5f) continue;

                if (refOBBSizes[i] < minSize)
                {
                    minSize = refOBBSizes[i];
                    bestAxis = i;
                }
            }

            return candidateAxes[bestAxis];
        }

        public static Vector3 pickInitialExtensionAxisByViewAlignment(ObjectSpawnExtensionPlane extensionPlane, OBB refOBB)
        {
            Vector3 camLook             = PluginCamera.camera.transform.forward;
            Vector3[] candidateAxes     = new Vector3[] 
            { extensionPlane.look, -extensionPlane.look, extensionPlane.right, -extensionPlane.right };
            float[] refOBBSizes = new float[]
            {
                Vector3Ex.getSizeAlongAxis(refOBB.size, refOBB.rotation, candidateAxes[0]),
                Vector3Ex.getSizeAlongAxis(refOBB.size, refOBB.rotation, candidateAxes[1]),
                Vector3Ex.getSizeAlongAxis(refOBB.size, refOBB.rotation, candidateAxes[2]),
                Vector3Ex.getSizeAlongAxis(refOBB.size, refOBB.rotation, candidateAxes[3]),
            };

            int bestAxis = -1;
            float bestScore = float.MinValue;

            for (int i = 0; i < candidateAxes.Length; i++)
            {
                // Note: Ignore axes which produce a size of 0.
                if (refOBBSizes[i] < 1e-5f) continue;

                float dot = Vector3.Dot(candidateAxes[i], camLook);
                if (dot > bestScore)
                {
                    bestScore = dot;
                    bestAxis = i;
                }
            }

            return candidateAxes[bestAxis];
        }

        public ObjectSpawnCellSegment(OBB refObjectOBB, Vector3 heightAxis, Vector3 extensionAxis)
        {
            _refObjectOBB   = refObjectOBB;
            _heightAxis     = heightAxis.normalized;
            _extensionAxis  = extensionAxis.normalized;
            updateRightAxis();
        }

        public ObjectSpawnCellStack getStack(int index)
        {
            return _stacks[index];
        }

        public int snapLengthToCursor(ObjectSpawnExtensionPlane extensionPlane, int maxLength, bool allowZeroLength)
        {
            Vector3 intersectPt;
            if (extensionPlane.cursorRaycast(out intersectPt))
            {
                float cellSize          = calcCellSizeAlongAxis(_extensionAxis) + _horizontalPadding;
                float halfCellSize      = cellSize * 0.5f;
                Vector3 start           = startPosition - _extensionAxis * halfCellSize;
                Vector3 toIntersectPt   = intersectPt - start;

                float projectedLength = Vector3Ex.absDot(toIntersectPt, _extensionAxis.normalized) - halfCellSize;
                if (allowZeroLength && (projectedLength < halfCellSize)) return setLength(0);

                int newLength = 1 + (int)(projectedLength / cellSize);

                if (newLength < 0) newLength = 0;
                if (maxLength >= 2 && newLength > maxLength) newLength = maxLength;

                return setLength(newLength);
            }

            return numStacks;
        }       

        public float calcCellSizeAlongAxis(Vector3 axis)
        {
            return Vector3Ex.getSizeAlongAxis(_refObjectOBB.size, _refObjectOBB.rotation, axis);
        }

        public Vector3 calcStackPosition(int stackIndex, float cellSizeAlongExtensionAxis)
        {
            return _refObjectOBB.center + stackIndex * _extensionAxis * (cellSizeAlongExtensionAxis + _horizontalPadding);
        }

        public void makeObjectRotation90DegreesRelativeToSegment(ObjectSpawnCellSegment segment)
        {
            float angle = Vector3.SignedAngle(segment.extensionAxis, _extensionAxis, _heightAxis);
            setObjectRotation(Quaternion.AngleAxis(angle, _heightAxis) * segment.refObjectOBB.rotation);

            // Don't remember the logic behind this. But it seems wrong.
            /*Quaternion rotation = segment.refObjectOBB.rotation;
            float dot = Vector3.Dot(segment.rightAxis, _extensionAxis);
            
            if (dot > 0.0f) rotation = Quaternion.AngleAxis(90.0f, segment.heightAxis) * rotation;
            else rotation = Quaternion.AngleAxis(-90.0f, segment.heightAxis) * rotation;
            setObjectRotation(rotation);*/
        }

        public void setObjectRotation(Quaternion rotation)
        {
            _refObjectOBB.rotation = rotation;

            int length = numStacks;
            float cellSizeAlongExtensionAxis = calcCellSizeAlongAxis(_extensionAxis);
            for (int i = 0; i < length; ++i)
            {
                Vector3 stackStartPosition = calcStackPosition(i, cellSizeAlongExtensionAxis);
                _stacks[i].setStartPosition(stackStartPosition);
                _stacks[i].setObjectRotation(rotation);
            }

            updateOBB();
        }

        public int setLength(int length, int defaultStackHeight = 1)
        {
            int oldLength = numStacks;
            if (length == numStacks) return oldLength;

            if (length <= 0)
            {
                _stacks.Clear();
                updateOBB();
                return oldLength;
            }

            if (length > numStacks)
            {
                float cellSizeAlongExtensionAxis    = calcCellSizeAlongAxis(_extensionAxis);
                int numToAdd                        = length - numStacks;

                int stackBaseIndex                  = numStacks;
                for (int i = 0; i < numToAdd; ++i)
                {
                    Vector3 stackPos = calcStackPosition(stackBaseIndex + i, cellSizeAlongExtensionAxis);
                    var stack = new ObjectSpawnCellStack(stackPos, _heightAxis, _refObjectOBB.size, _refObjectOBB.rotation);
                    stack.setVerticalPadding(_verticalPadding);
                    stack.setHeight(defaultStackHeight);
                    _stacks.Add(stack);
                }
            }
            else
            {
                int numToRemove = numStacks - length;
                _stacks.RemoveRange(length, numToRemove);
            }

            updateOBB();
            return oldLength;
        }

        public void setHeight(int height)
        {
            foreach (var stack in _stacks)
                stack.setHeight(height);

            updateOBB();
        }

        public void setHeight(int height, int firstStackIndex)
        {
            int length = numStacks;
            for (int stackIndex = firstStackIndex; stackIndex < length; ++stackIndex)
                getStack(stackIndex).setHeight(height);

            updateOBB();
        }

        public void setHeight(List<int> heightValues, int firstStackIndex)
        {
            int length = numStacks;
            for (int stackIndex = firstStackIndex; stackIndex < length; ++stackIndex)
                getStack(stackIndex).setHeight(heightValues[stackIndex - firstStackIndex]);

            updateOBB();
        }

        public void setHeight(List<int> heightValues, int heightOffset, int firstStackIndex)
        {
            int length = numStacks;
            for (int stackIndex = firstStackIndex; stackIndex < length; ++stackIndex)
                getStack(stackIndex).setHeight(heightValues[stackIndex - firstStackIndex] + heightOffset);

            updateOBB();
        }

        public void setStackHeight(int height, int stackIndex)
        {
            getStack(stackIndex).setHeight(height);
            updateOBB();
        }

        public void setExtensionAxis(Vector3 extensionAxis)
        {
            _extensionAxis                      = extensionAxis;
            updateRightAxis();

            int length                          = numStacks;
            float cellSizeAlongExtensionAxis    = calcCellSizeAlongAxis(_extensionAxis);
            for (int i = 0; i < length; ++i)
            {
                Vector3 stackStartPosition = calcStackPosition(i, cellSizeAlongExtensionAxis);
                _stacks[i].setStartPosition(stackStartPosition);
            }

            updateOBB();
        }

        public void setStartPosition(Vector3 startPosition)
        {
            Vector3 offset          = startPosition - _refObjectOBB.center;
            _refObjectOBB.center    = startPosition;

            int length = numStacks;
            for (int i = 0; i < length; ++i)
                _stacks[i].offsetStartPosition(offset);

            updateOBB();
        }

        public void setVerticalPadding(float padding)
        {
            _verticalPadding = padding;
            foreach (var stack in _stacks)
                stack.setVerticalPadding(padding);

            updateOBB();
        }

        public void setHorizontalPadding(float padding)
        {
            _horizontalPadding                  = padding;

            int length                          = numStacks;
            float cellSizeAlongExtensionAxis    = calcCellSizeAlongAxis(_extensionAxis);
            for (int i = 0; i < length; ++i)
            {
                Vector3 stackStartPosition = calcStackPosition(i, cellSizeAlongExtensionAxis);
                _stacks[i].setStartPosition(stackStartPosition);
            }

            updateOBB();
        }

        public void removeLastStack()
        {
            if (numStacks == 0) return;
            _stacks.RemoveAt(_stacks.Count - 1);

            updateOBB();
        }

        public void normalConnectAtCorner(ObjectSpawnCellSegment segment, float padding)
        {
            Vector3 newStartPos = segment.endPosition;
            float size0         = segment.calcCellSizeAlongAxis(segment._extensionAxis) * 0.5f;
            float size1         = calcCellSizeAlongAxis(segment._extensionAxis) * 0.5f;
            float delta         = size0 - size1;
            newStartPos         += segment._extensionAxis * delta;
            newStartPos         += _extensionAxis * calcCellSizeAlongAxis(_extensionAxis) * 0.5f;
            newStartPos         += _extensionAxis * segment.calcCellSizeAlongAxis(_extensionAxis) * 0.5f;
            newStartPos         += _extensionAxis * padding;

            setStartPosition(newStartPos);
        }

        public void overlapConnectAtCorner(ObjectSpawnCellSegment segment, float padding)
        {
            Vector3 newStartPos = segment.endPosition;
            float size0         = segment.calcCellSizeAlongAxis(segment._extensionAxis) * 0.5f;
            float size1         = calcCellSizeAlongAxis(segment._extensionAxis) * 0.5f;
            float delta         = size0 - size1;
            newStartPos         += segment._extensionAxis * delta;
            newStartPos         += _extensionAxis * calcCellSizeAlongAxis(_extensionAxis) * 0.5f;
            newStartPos         -= _extensionAxis * segment.calcCellSizeAlongAxis(_extensionAxis) * 0.5f;
            newStartPos         += _extensionAxis * padding;

            setStartPosition(newStartPos);
        }

        public void gapConnectAtCorner(ObjectSpawnCellSegment segment, float padding)
        {
            Vector3 newStartPos = segment.endPosition;
            float size0         = segment.calcCellSizeAlongAxis(segment._extensionAxis) * 0.5f;
            float size1         = calcCellSizeAlongAxis(segment._extensionAxis) * 0.5f;
            float delta         = size0 - size1;
            newStartPos         += segment._extensionAxis * delta;
            newStartPos         += _extensionAxis * calcCellSizeAlongAxis(_extensionAxis) * 0.5f;
            newStartPos         += _extensionAxis * segment.calcCellSizeAlongAxis(_extensionAxis) * 0.5f;
            newStartPos         += _extensionAxis * padding;
            newStartPos         += segment._extensionAxis * calcCellSizeAlongAxis(segment._extensionAxis);

            setStartPosition(newStartPos);
        }

        public void connectToParallelSegmentEnd(ObjectSpawnCellSegment segment, float padding)
        {
            Vector3 newStartPos = segment.endPosition;
            float offset0       = segment.calcCellSizeAlongAxis(segment._extensionAxis) * 0.5f;
            float offset1       = calcCellSizeAlongAxis(_extensionAxis) * 0.5f;
            newStartPos         += segment._extensionAxis * offset0 + _extensionAxis * offset1;
            newStartPos         += _extensionAxis * padding;
            setStartPosition(newStartPos);
        }

        public void setAllCellsOccluded(bool occluded)
        {
            foreach (var stack in _stacks)
                stack.setAllCellsOccluded(occluded);
        }

        public void setAllCellsOutOfScope(bool outOfScope)
        {
            foreach (var stack in _stacks)
                stack.setAllCellsOutOfScope(outOfScope);
        }

        public void draw(DrawConfig drawConfig)
        {
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
            HandlesEx.restoreColor();
        }

        private void updateRightAxis()
        {
            _rightAxis = Vector3.Cross(_heightAxis, _extensionAxis).normalized;
        }

        public void updateOBB()
        {
            int length = numStacks;
            if (length == 0)
            {
                _obb = OBB.getInvalid();
                return;
            }

            float cellSizeAlongHeightAxis   = calcCellSizeAlongAxis(_heightAxis);
            Vector3 basePosition            = startPosition - _heightAxis * cellSizeAlongHeightAxis * 0.5f;

            int numValidStacks              = 0;
            float negativeHeight            = 0.0f;
            float positiveHeight            = 0.0f;

            foreach (var stack in _stacks)
            {
                if (stack.numCells == 0) continue;

                ++numValidStacks;
                float h = Vector3.Dot(_heightAxis, stack.endPosition - basePosition);
                if (h < 0.0f)
                {
                    h -= cellSizeAlongHeightAxis * 0.5f;
                    if (h < negativeHeight) negativeHeight = h;
                }
                else
                if (h > 0.0f)
                {
                    h += cellSizeAlongHeightAxis * 0.5f;
                    if (h > positiveHeight) positiveHeight = h;
                }
            }

            if (numValidStacks == 0)
            {
                _obb = OBB.getInvalid();
                return;
            }

            float height    = positiveHeight - negativeHeight;
            _obb            = new OBB(Vector3.zero, Vector3.zero);
            _obb.center     = basePosition + _extensionAxis * (startPosition - endPosition).magnitude * 0.5f;
            _obb.center     += _heightAxis * (positiveHeight + negativeHeight) * 0.5f;

            _obb.rotation   = Quaternion.LookRotation(_extensionAxis, _heightAxis);
            _obb.size       = new Vector3(calcCellSizeAlongAxis(_rightAxis),
                                        height,
                                        calcCellSizeAlongAxis(_extensionAxis) * length + (length - 1) * _horizontalPadding);
        }
    }
}
#endif

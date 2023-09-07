#if UNITY_EDITOR
using UnityEngine;
using System;

namespace GSpawn
{
    public enum TileRuleMaskRotation
    {
        None = 0,
        R90,
        R180,
        R270
    }

    public enum TileRuleMaskMirrorAxis
    {
        None = 0,
        X,
        Z
    }

    public enum TileRuleBitMaskId
    {
        ReqOn = 0,
        ReqOff
    }

    // Note: Maps to corresponding number of neighbors.
    public enum TileRuleNeighborRadius
    {
        One     = 1,
        Two,
        [Obsolete]
        Three,
    }

    public struct TileRuleMaskMatchResult
    {
        public bool                     matched;
        public TileRuleMaskRotation     maskRotation;
        public TileRuleMaskMirrorAxis   maskMirrorAxis;

        public TileRuleMaskMatchResult(bool matched)
        {
            this.matched    = matched;
            maskRotation    = TileRuleMaskRotation.None;
            maskMirrorAxis  = TileRuleMaskMirrorAxis.None;
        }

        public TileRuleMaskMatchResult(TileRuleMaskRotation maskRotation)
        {
            matched             = true;
            this.maskRotation   = maskRotation;
            maskMirrorAxis      = TileRuleMaskMirrorAxis.None;
        }

        public TileRuleMaskMatchResult(TileRuleMaskMirrorAxis maskMirrorAxis)
        {
            matched             = true;
            maskRotation        = TileRuleMaskRotation.None;
            this.maskMirrorAxis = maskMirrorAxis;
        }

        public void reset()
        {
            matched         = false;
            maskRotation    = TileRuleMaskRotation.None;
            maskMirrorAxis  = TileRuleMaskMirrorAxis.None;
        }
    }

    [Serializable]
    public class TileRuleMask
    {        
        // Note: Maps to TileRuleMaskRotation.
        private static Vector3[] _rotationLookAxes  = new Vector3[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };

        private const int       _numAvailableBits   = 49;
        private const int       _bitRowSize         = 7;
        private const int       _numBitRows         = 7;
        private const int       _midBitIndex        = 24;
        private const ulong     _defaultReqOnMask   = (1ul << 24);
        private const ulong     _defaultReqOffMask  = 0;

        [SerializeField]
        private ulong           _reqOnMask                      = _defaultReqOnMask;
        [SerializeField]
        private ulong           _rotatedReqOnMask_90            = _defaultReqOnMask;
        [SerializeField]
        private ulong           _rotatedReqOnMask_180           = _defaultReqOnMask;
        [SerializeField]
        private ulong           _rotatedReqOnMask_270           = _defaultReqOnMask;
        [SerializeField]
        private ulong           _mirrXReqOnMask                 = _defaultReqOnMask;
        [SerializeField]
        private ulong           _mirrZReqOnMask                 = _defaultReqOnMask;
        [SerializeField]    
        private int             _numReqOnBitsSet                = 1;

        [SerializeField]
        private ulong           _reqOffMask                     = _defaultReqOffMask;
        [SerializeField]
        private ulong           _rotatedReqOffMask_90           = _defaultReqOffMask;
        [SerializeField]
        private ulong           _rotatedReqOffMask_180          = _defaultReqOffMask;
        [SerializeField]
        private ulong           _rotatedReqOffMask_270          = _defaultReqOffMask;
        [SerializeField]
        private ulong           _mirrXReqOffMask                = _defaultReqOffMask;
        [SerializeField]
        private ulong           _mirrZReqOffMask                = _defaultReqOffMask;
        [SerializeField]
        private int             _numReqOffBitsSet               = 0;

        public ulong            reqOnMaskValue
        {
            get { return _reqOnMask; }
            set
            {
                _reqOnMask                  = value;
                _rotatedReqOnMask_90        = rotateMask_90(_reqOnMask);
                _rotatedReqOnMask_180       = rotateMask_180(_reqOnMask);
                _rotatedReqOnMask_270       = rotateMask_270(_reqOnMask);
                _mirrXReqOnMask             = mirrorMaskX(_reqOnMask);
                _mirrZReqOnMask             = mirrorMaskZ(_reqOnMask);
                _numReqOnBitsSet            = countNumberOf1Bits(_reqOnMask);
            }
        }
        public int              numReqOnBitsSet         { get { return _numReqOnBitsSet; } }
        public ulong            reqOffMaskValue
        {
            get { return _reqOffMask; }
            set
            {
                _reqOffMask             = value;
                _rotatedReqOffMask_90   = rotateMask_90(_reqOffMask);
                _rotatedReqOffMask_180  = rotateMask_180(_reqOffMask);
                _rotatedReqOffMask_270  = rotateMask_270(_reqOffMask);
                _mirrXReqOffMask        = mirrorMaskX(_reqOffMask);
                _mirrZReqOffMask        = mirrorMaskZ(_reqOffMask);
                _numReqOffBitsSet       = countNumberOf1Bits(_reqOffMask);
            }
        }
        public int              numReqOffBitsSet        { get { return _numReqOffBitsSet; } }

        public static int       numAvailableBits        { get { return _numAvailableBits; } }
        public static int       numBitRows              { get { return _numBitRows; } }
        public static int       bitRowSize              { get { return _bitRowSize; } }
        public static int       middleBitRow            { get { return 3; } }
        public static int       middleBitCol            { get { return 3; } }
        public static ulong     defaultReqOnMask        { get { return _defaultReqOnMask; } }

        public static Vector3 maskRotationToLookAxis(TileRuleMaskRotation maskRotation, Quaternion frameRotation)
        {
            return frameRotation * _rotationLookAxes[(int)maskRotation];
        }

        public static void debugLog(ulong ruleMask)
        {
            string binaryString = Convert.ToString((long)ruleMask, 2).PadLeft(64, '0');
            binaryString = binaryString.Substring(64 - _numAvailableBits, _numAvailableBits);

            Debug.Log("================Rule Mask================");
            for (int i = 0; i < _numBitRows; ++i)
                Debug.Log("| " + binaryString.Substring(i * _bitRowSize, _bitRowSize) + " |");
        }

        public static ulong setBit(ulong ruleMask, int row, int col)
        {
            #pragma warning disable 0612
            int bitIndex    = rowColToBitIndex(TileRuleNeighborRadius.Three, row, col);
            ulong bitMask   = 1ul << bitIndex;
            return ruleMask | bitMask;
            #pragma warning restore 0612
        }

        public static int countNumberOf1Bits(ulong ruleMask)
        {
            int count = 0;
            for (int i = 0; i < _numAvailableBits; ++i)
            {
                count += (int)(ruleMask & 0x1);
                ruleMask >>= 1;
            }

            return count;
        }

        public static int rowColToBitIndex(int row, int col)
        {
            return 48 - (row * _bitRowSize + col);
        }

        public static void bitIndexToRowCol(int bitIndex, out int row, out int col)
        {
            row = (48 - bitIndex) / _numBitRows;
            col = (48 - bitIndex) % _bitRowSize;
        }

        public static int rowColToBitIndex(TileRuleNeighborRadius radius, int row, int col)
        {
            #pragma warning disable 0612
            switch (radius)
            {
                case TileRuleNeighborRadius.One:

                    return 32 - (row * _bitRowSize + col);

                case TileRuleNeighborRadius.Two:

                    return 40 - (row * _bitRowSize + col);

                case TileRuleNeighborRadius.Three:

                    return 48 - (row * _bitRowSize + col);
            }
            #pragma warning restore 0612
            return 0;
        }

        public static bool isMiddleBit(int row, int col)
        {
            #pragma warning disable 0612
            int bitIndex = rowColToBitIndex(TileRuleNeighborRadius.Three, row, col);
            return bitIndex == _midBitIndex;
            #pragma warning restore 0612
        }

        public static bool isBitInRadius(TileRuleNeighborRadius radius, int row, int col)
        {
            #pragma warning disable 0612
            switch (radius)
            {
                case TileRuleNeighborRadius.Three:

                    return true;

                case TileRuleNeighborRadius.Two:

                    return row >= 1 && row <= 5 && col >= 1 && col <= 5;

                case TileRuleNeighborRadius.One:

                    return row >= 2 && row <= 4 && col >= 2 && col <= 4;
            }
            #pragma warning restore 0612
            return false;
        }

        public TileRuleMaskMatchResult match(ulong ruleMask, TileRuleRotationMode rotationMode)
        {
            // Note: This is common to all rotation modes.
            if ((_reqOnMask & ruleMask) == _reqOnMask)
            {
                if ((_reqOffMask & ruleMask) != 0) return new TileRuleMaskMatchResult(false);
                return new TileRuleMaskMatchResult(TileRuleMaskRotation.None);
            }

            switch (rotationMode)
            {
                case TileRuleRotationMode.Fixed:

                    // Note: Handled above.
                    break;

                case TileRuleRotationMode.Rotated:

                    if ((_rotatedReqOnMask_90 & ruleMask) == _rotatedReqOnMask_90)
                    {
                        if ((_rotatedReqOffMask_90 & ruleMask) != 0) return new TileRuleMaskMatchResult(false);
                        return new TileRuleMaskMatchResult(TileRuleMaskRotation.R90);
                    }
                    if ((_rotatedReqOnMask_180 & ruleMask) == _rotatedReqOnMask_180)
                    {
                        if ((_rotatedReqOffMask_180 & ruleMask) != 0) return new TileRuleMaskMatchResult(false);
                        return new TileRuleMaskMatchResult(TileRuleMaskRotation.R180);
                    }
                    if ((_rotatedReqOnMask_270 & ruleMask) == _rotatedReqOnMask_270)
                    {
                        if ((_rotatedReqOffMask_270 & ruleMask) != 0) return new TileRuleMaskMatchResult(false);
                        return new TileRuleMaskMatchResult(TileRuleMaskRotation.R270);
                    }
                    break;

                    #pragma warning disable 0612
                case TileRuleRotationMode.MirrorX:

                    if ((_mirrXReqOnMask & ruleMask) == _mirrXReqOnMask)
                    {
                        if ((_mirrXReqOffMask & ruleMask) != 0) return new TileRuleMaskMatchResult(false);
                        return new TileRuleMaskMatchResult(TileRuleMaskMirrorAxis.X);
                    }
                    break;

                case TileRuleRotationMode.MirrorZ:

                    if ((_mirrZReqOnMask & ruleMask) == _mirrZReqOnMask)
                    {
                        if ((_mirrZReqOffMask & ruleMask) != 0) return new TileRuleMaskMatchResult(false);
                        return new TileRuleMaskMatchResult(TileRuleMaskMirrorAxis.Z);
                    }
                    break;
                    #pragma warning restore 0612
            }

            return new TileRuleMaskMatchResult(false);
        }

        public bool checkBit(int row, int col, TileRuleBitMaskId maskId)
        {
            #pragma warning disable 0612
            int bitIndex    = rowColToBitIndex(TileRuleNeighborRadius.Three, row, col);
            ulong bitMask   = 1ul << bitIndex;

            switch (maskId)
            {
                case TileRuleBitMaskId.ReqOn:

                    return (_reqOnMask & bitMask) != 0;

                case TileRuleBitMaskId.ReqOff:

                    return (_reqOffMask & bitMask) != 0;
            }
            #pragma warning restore 0612
            return false;
        }

        public void clearBit(int row, int col, TileRuleBitMaskId maskId)
        {
            #pragma warning disable 0612
            int bitIndex    = rowColToBitIndex(TileRuleNeighborRadius.Three, row, col);
            ulong bitMask   = ~(1ul << bitIndex);

            switch (maskId)
            {
                case TileRuleBitMaskId.ReqOn:

                    reqOnMaskValue  = reqOnMaskValue & bitMask;
                    break;

                case TileRuleBitMaskId.ReqOff:

                    reqOffMaskValue  = reqOffMaskValue & bitMask;
                    break;
            }
            #pragma warning restore 0612
        }

        public void setAllBits(TileRuleBitMaskId maskId)
        {
            switch (maskId)
            {
                case TileRuleBitMaskId.ReqOn:

                    reqOnMaskValue  = ~0ul;
                    reqOffMaskValue = _defaultReqOffMask;
                    break;

                case TileRuleBitMaskId.ReqOff:

                    reqOffMaskValue = ~0ul;
                    reqOnMaskValue  = _defaultReqOnMask;
                    break;
            }
        }

        public void setBit(int row, int col, TileRuleBitMaskId maskId)
        {
            #pragma warning disable 0612
            int bitIndex    = rowColToBitIndex(TileRuleNeighborRadius.Three, row, col);
            ulong bitMask   = 1ul << bitIndex;;

            switch (maskId)
            {
                case TileRuleBitMaskId.ReqOn:

                    reqOnMaskValue      = reqOnMaskValue | bitMask;
                    reqOffMaskValue     = reqOffMaskValue & (~bitMask);
                    break;

                case TileRuleBitMaskId.ReqOff:

                    reqOffMaskValue     = reqOffMaskValue | bitMask;
                    reqOnMaskValue      = reqOnMaskValue & (~bitMask);
                    break;
            }
            #pragma warning restore 0612
        }

        public void toggleBit(int row, int col, TileRuleBitMaskId maskId)
        {
            if (checkBit(row, col, maskId)) clearBit(row, col, maskId);
            else setBit(row, col, maskId);
        }

        public void useDefaultValue()
        {
            reqOnMaskValue  = _defaultReqOnMask;
            reqOffMaskValue = _defaultReqOffMask;
        }

        private static ulong mirrorMaskX(ulong ruleMask)
        {
            int numColumns = _bitRowSize / 2;
            for (int r = 0; r < _numBitRows; ++r)
            {
                for (int c = 0; c < numColumns; ++c) 
                {
                    int p0  = rowColToBitIndex(r, c);
                    int p1  = rowColToBitIndex(r, _bitRowSize - 1 - c);
                    ruleMask = swapBits(ruleMask, p0, p1);
                }
            }

            return ruleMask;
        }

        private static ulong mirrorMaskZ(ulong ruleMask)
        {
            int numRows = _numBitRows / 2;
            for (int c = 0; c < _bitRowSize; ++c)
            {
                for (int r = 0; r < numRows; ++r)
                {
                    int p0 = rowColToBitIndex(r, c);
                    int p1 = rowColToBitIndex(_numBitRows - 1 - r, c);
                    ruleMask = swapBits(ruleMask, p0, p1);
                }
            }

            return ruleMask;
        }

        private static ulong swapBits(ulong ruleMask, int p0, int p1)
        {
            if (p0 == p1) return ruleMask;

            // https://www.geeksforgeeks.org/how-to-swap-two-bits-in-a-given-integer/
            ulong bit0  = (ruleMask >> p0) & 1;
            ulong bit1  = (ruleMask >> p1) & 1;
            ulong xor   = (bit0 ^ bit1);
            xor         = (xor << p0) | (xor << p1);

            return ruleMask ^ xor;
        }

        private static ulong rotateMask_90(ulong ruleMask)
        {
            ulong r0 = extractMaskRow(ruleMask, 0);
            ulong r1 = extractMaskRow(ruleMask, 1);
            ulong r2 = extractMaskRow(ruleMask, 2);
            ulong r3 = extractMaskRow(ruleMask, 3);
            ulong r4 = extractMaskRow(ruleMask, 4);
            ulong r5 = extractMaskRow(ruleMask, 5);
            ulong r6 = extractMaskRow(ruleMask, 6);

            ruleMask = setMaskColumn(ruleMask, 0, r6);
            ruleMask = setMaskColumn(ruleMask, 1, r5);
            ruleMask = setMaskColumn(ruleMask, 2, r4);
            ruleMask = setMaskColumn(ruleMask, 3, r3);
            ruleMask = setMaskColumn(ruleMask, 4, r2);
            ruleMask = setMaskColumn(ruleMask, 5, r1);
            ruleMask = setMaskColumn(ruleMask, 6, r0);

            return ruleMask;
        }

        private static ulong rotateMask_180(ulong ruleMask)
        {
            ruleMask = rotateMask_90(ruleMask);
            return rotateMask_90(ruleMask);
        }

        private static ulong rotateMask_270(ulong ruleMask)
        {
            ruleMask = rotateMask_90(ruleMask);
            ruleMask = rotateMask_90(ruleMask);
            return rotateMask_90(ruleMask);
        }

        private static ulong setMaskColumn(ulong ruleMask, int col, ulong val_7bit)
        {
            // Note: High bit in val_7bit maps to top most bit in column. Low bit
            //       in val_7bit maps to bottom most bit in column.
            // Loop through each bit in the 7 bit value (0 = low bit; 7 = high bit)
            for (int bitIndex = 0; bitIndex < _bitRowSize; ++bitIndex)
            {
                // Clear bit in rule mask
                ruleMask &= ~((1ul << (_bitRowSize - 1 - col)) << (bitIndex * _bitRowSize));

                // Extract bit from 7 bit value
                ulong bitMask   = 1ul << bitIndex;
                ulong bit       = (val_7bit & bitMask) >> bitIndex;

                // Store the bit in the rule mask
                ruleMask |= ((bit << (_bitRowSize - 1 - col)) << (bitIndex * _bitRowSize));
            }

            return ruleMask;
        }

        private static ulong extractMaskRow(ulong ruleMask, int row)
        {
            int shift       = (_numBitRows - 1 - row) * _bitRowSize;
            ulong bitMask   = 0b1111111ul << shift;
            return (ruleMask & bitMask) >> shift;
        }
    }
}
#endif
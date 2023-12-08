#if UNITY_EDITOR
namespace GSpawn
{
    public enum PlaneId
    {
        XY = 0,
        YZ,
        ZX
    }

    public static class PlaneIdEx
    {
        public static AxisDescriptor getFirstAxisDescriptor(this PlaneId planeId, PlaneQuadrantId planeQuadrant)
        {
            return new AxisDescriptor(planeIdToFirstAxisIndex(planeId), getFirstAxisSign(planeId, planeQuadrant));
        }

        public static AxisDescriptor getSecondAxisDescriptor(this PlaneId planeId, PlaneQuadrantId planeQuadrant)
        {
            return new AxisDescriptor(planeIdToSecondAxisIndex(planeId), getSecondAxisSign(planeId, planeQuadrant));
        }

        public static AxisSign getFirstAxisSign(this PlaneId planeId, PlaneQuadrantId planeQuadrant)
        {
            if (planeId == PlaneId.XY)
            {
                if (planeQuadrant == PlaneQuadrantId.First) return AxisSign.Positive;
                else if (planeQuadrant == PlaneQuadrantId.Second) return AxisSign.Negative;
                else if (planeQuadrant == PlaneQuadrantId.Third) return AxisSign.Negative;
                else return AxisSign.Positive;
            }
            else
            if (planeId == PlaneId.YZ)
            {
                if (planeQuadrant == PlaneQuadrantId.First) return AxisSign.Positive;
                else if (planeQuadrant == PlaneQuadrantId.Second) return AxisSign.Positive;
                else if (planeQuadrant == PlaneQuadrantId.Third) return AxisSign.Negative;
                else return AxisSign.Negative;
            }
            else
            {
                if (planeQuadrant == PlaneQuadrantId.First) return AxisSign.Positive;
                else if (planeQuadrant == PlaneQuadrantId.Second) return AxisSign.Positive;
                else if (planeQuadrant == PlaneQuadrantId.Third) return AxisSign.Negative;
                else return AxisSign.Negative;
            }
        }

        public static AxisSign getSecondAxisSign(this PlaneId planeId, PlaneQuadrantId planeQuadrant)
        {
            if (planeId == PlaneId.XY)
            {
                if (planeQuadrant == PlaneQuadrantId.First) return AxisSign.Positive;
                else if (planeQuadrant == PlaneQuadrantId.Second) return AxisSign.Positive;
                else if (planeQuadrant == PlaneQuadrantId.Third) return AxisSign.Negative;
                else return AxisSign.Negative;
            }
            else
            if (planeId == PlaneId.YZ)
            {
                if (planeQuadrant == PlaneQuadrantId.First) return AxisSign.Positive;
                else if (planeQuadrant == PlaneQuadrantId.Second) return AxisSign.Negative;
                else if (planeQuadrant == PlaneQuadrantId.Third) return AxisSign.Negative;
                else return AxisSign.Positive;
            }
            else
            {
                if (planeQuadrant == PlaneQuadrantId.First) return AxisSign.Positive;
                else if (planeQuadrant == PlaneQuadrantId.Second) return AxisSign.Negative;
                else if (planeQuadrant == PlaneQuadrantId.Third) return AxisSign.Negative;
                else return AxisSign.Positive;
            }
        }

        public static int planeIdToFirstAxisIndex(this PlaneId planeId)
        {
            if (planeId == PlaneId.XY) return 0;
            if (planeId == PlaneId.ZX) return 2;
            return 1;
        }

        public static int planeIdToSecondAxisIndex(this PlaneId planeId)
        {
            if (planeId == PlaneId.XY) return 1;
            if (planeId == PlaneId.ZX) return 0;
            return 2;
        }

        public static PlaneId axisIndexToPlaneId(int axisIndex)
        {
            if (axisIndex == 0) return PlaneId.YZ;
            if (axisIndex == 1) return PlaneId.ZX;
            return PlaneId.XY;
        }
    }
}
#endif
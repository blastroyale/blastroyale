#if UNITY_EDITOR
using System.Collections.Generic;

namespace GSpawn
{
    public enum IntPatternWrapMode
    {
        Clamp = 0,
        Repeat,
        Mirror
    }

    public abstract class IntPatternSampler
    {
        public abstract int sample(List<int> values, int index);

        public static IntPatternSampler create(IntPatternWrapMode wrapMode)
        {
            if (wrapMode == IntPatternWrapMode.Repeat) return new RepeatIntegerPatternSampler();
            if (wrapMode == IntPatternWrapMode.Clamp) return new ClampIntegerPatternSampler();
            if (wrapMode == IntPatternWrapMode.Mirror) return new MirrorIntegerPatternSampler();

            return null;
        }
    }

    public class ClampIntegerPatternSampler : IntPatternSampler
    {
        public override int sample(List<int> values, int index)
        {
            int numValues = values.Count;
            return index < numValues ? values[index] : values[numValues - 1];
        }
    }

    public class RepeatIntegerPatternSampler : IntPatternSampler
    {
        public override int sample(List<int> values, int index)
        {
            return values[index % values.Count];
        }
    }

    public class MirrorIntegerPatternSampler : IntPatternSampler
    {
        public override int sample(List<int> values, int index)
        {
            int numValues   = values.Count;
            int groupIndex  = index / numValues;
            if (groupIndex % 2 == 0) return values[index % numValues];
            else return values[numValues - 1 - (index % numValues)];
        }
    }
}
#endif
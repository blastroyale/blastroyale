#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public struct ObjectLayerDiff
    {
        public bool layer;
    }

    public static class ObjectLayerDiffCheck
    {
        public static ObjectLayerDiff checkDiff(List<Transform> transforms)
        {
            var diff = new ObjectLayerDiff();

            int numTransforms = transforms.Count;
            for (int i = 0; i < numTransforms; ++i)
            {
                GameObject go = transforms[i].gameObject;

                for (int j = i + 1; j < numTransforms; ++j)
                {
                    GameObject otherGO = transforms[j].gameObject;

                    if (go.layer != otherGO.layer)
                    {
                        diff.layer = true;
                        return diff;
                    }
                }
            }

            return diff;
        }
    }
}
#endif
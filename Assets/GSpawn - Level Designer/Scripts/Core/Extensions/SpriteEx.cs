#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public static class SpriteEx
    {
        public static void getWorldVerts(this Sprite sprite, Transform spriteTransform, List<Vector3> verts)
        {
            sprite.getModelVerts(verts);
            spriteTransform.transformPoints(verts);
        }

        public static void getModelVerts(this Sprite sprite, List<Vector3> verts)
        {
            verts.Clear();
            var modelVerts = sprite.vertices;

            foreach (var pt in modelVerts)
                verts.Add(pt);
        }
    }
}
#endif
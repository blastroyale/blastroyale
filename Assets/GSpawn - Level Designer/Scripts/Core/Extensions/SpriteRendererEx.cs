#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public static class SpriteRendererEx
    {
        public static Vector3 calcWorldCenterPoint(this SpriteRenderer spriteRenderer)
        {
            return spriteRenderer.transform.TransformPoint(spriteRenderer.calcModelSpaceAABB().center);
        }

        public static Vector3 calcModelSpaceSize(this SpriteRenderer spriteRenderer)
        {
            return spriteRenderer.calcModelSpaceAABB().size;
        }

        public static AABB calcModelSpaceAABB(this SpriteRenderer spriteRenderer)
        {
            Sprite sprite = spriteRenderer.sprite;
            if (sprite == null) return AABB.getInvalid();

            List<Vector2> spriteVerts = new List<Vector2>(sprite.vertices);
            return new AABB(spriteVerts);
        }

        public static bool isPixelFullyTransparent(this SpriteRenderer spriteRenderer, Vector3 worldPos)
        {
            // No sprite?
            Sprite sprite               = spriteRenderer.sprite;
            if (sprite == null) return true;

            // No texture?
            Texture2D spriteTexture     = sprite.texture;
            if (spriteTexture == null) return true;

            // We need to work in the sprite's model space so the first step is to transform
            // the passed world position into a sprite local position.
            Transform spriteTransform   = spriteRenderer.transform;
            Vector3 modelSpacePos       = spriteTransform.InverseTransformPoint(worldPos);

            // Project the model space position onto the sprite's plane. The sprite's plane is
            // always the XY plane in its model space.
            Plane xyPlane               = new Plane(Vector3.forward, 0.0f);
            Vector3 projectedPos        = xyPlane.projectPoint(modelSpacePos);

            // Calculate the sprite's model space AABB and check if this AABB contains the model
            // space position. If it doesn't, it means the world position is outside the sprite's
            // bounds/area.
            AABB modelSpaceAABB         = spriteRenderer.calcModelSpaceAABB();
            modelSpaceAABB.size         = new Vector3(modelSpaceAABB.size.x, modelSpaceAABB.size.y, 1.0f);
            if (!modelSpaceAABB.containsPoint(projectedPos)) return true;

            // Build a vector which goes from the bottom left corner of the sprite to the projected world 
            // position. This will help us calculate the coordinates of the pixel where the world point resides.
            Vector3 bottomLeft          = xyPlane.projectPoint(modelSpaceAABB.min);
            Vector3 fromTopLeftToPos    = projectedPos - bottomLeft;

            // Calculate the pixel coordinates of the projected position
            Vector2 pixelCoords = new Vector2(fromTopLeftToPos.x * sprite.pixelsPerUnit, fromTopLeftToPos.y * sprite.pixelsPerUnit);
            pixelCoords += sprite.textureRectOffset;

            // Try reading the sprite pixels and check the alpha value
            try
            {
                float alpha = spriteTexture.GetPixel((int)(pixelCoords.x + 0.5f), (int)(pixelCoords.y + 0.5f)).a;
                return alpha <= 1e-3f;
            }
            catch (UnityException e)
            {
                // Ternary operator needed to avoid 'variable not used' warning
                return e != null ? false : false;
            }
        }
    }
}
#endif
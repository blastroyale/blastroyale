#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace GSpawn
{
    public enum TileRuleBrushType
    {
        Box = 0,
        FlexiBox,
        Segments
    }

    public enum TileRuleBrushUsage
    {
        Paint = 0,
        RampPaint,
        Erase
    }

    public abstract class TileRuleBrush
    {
        protected TileRuleObjectSpawnSettings   spawnSettings       { get { return ObjectSpawn.instance.tileRuleObjectSpawn.settings; } }

        public abstract TileRuleBrushType       brushType           { get; }
        public abstract int                     yOffset             { get; }         
        public TileRuleGrid                     tileRuleGrid        { get; set; }
        public bool                             gridSitsBelowBrush  { get; set; }
        public TileRuleBrushUsage               usage               { get; set; }
        public abstract bool                    isIdle              { get; }

        public abstract void onSceneGUI();
        public abstract void cancel();
        public abstract void draw(Color borderColor);
        public abstract void drawShadow(Color shadowLineColor, Color shadowColor);
        public abstract void getCellCoords(HashSet<Vector3Int> cellCoords);
        public abstract void getCellsAroundVerticalBorder(int radius, List<Vector3Int> cellCoords);
        public abstract void getCellCoordsBelowBrush(List<Vector3Int> cellCoords);

        protected void useOnGrid()
        {
            switch (usage)
            {
                case TileRuleBrushUsage.Paint:

                    tileRuleGrid.paintTiles(this);
                    break;

                case TileRuleBrushUsage.RampPaint:

                    tileRuleGrid.paintRamps(this);
                    break;

                case TileRuleBrushUsage.Erase:

                    tileRuleGrid.eraseTiles(this);
                    break;
            }
        }
    }
}
#endif
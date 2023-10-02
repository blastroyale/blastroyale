#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

namespace GSpawn
{
    public enum TileRuleSpawnToolId
    {
        Paint = 0,
        RampPaint,
        Erase,
        Connect
    }

    public enum TileRuleSpawnBrushId
    {
        Box = 0,
        FlexiBox,
        Segments
    }

    public class TileRuleObjectSpawn : ObjectSpawnTool
    {
        [SerializeField] 
        private TileRuleSpawnToolId                 _activeToolId           = TileRuleSpawnToolId.Paint;
        [SerializeField]
        private TileRuleSpawnBrushId                _activeBrushId          = TileRuleSpawnBrushId.Box;

        [NonSerialized]
        private TileRuleBoxBrush                    _boxBrush               = new TileRuleBoxBrush();
        [NonSerialized]
        private TileRuleFlexiBoxBrush               _flexiBoxBrush          = new TileRuleFlexiBoxBrush();
        [NonSerialized]
        private TileRuleSegmentsBrush               _segmentsBrush          = new TileRuleSegmentsBrush();
        [NonSerialized]
        private TileRuleBoxBrush                    _rampBoxBrush           = new TileRuleBoxBrush();
        [NonSerialized]
        private TileRuleConnect                     _connectTool            = new TileRuleConnect();

        [NonSerialized]
        private TileRuleGrid                        _currentGrid;
        [NonSerialized]
        private TileRuleObjectSpawnSettings         _settings;
        [NonSerialized]
        private TileRuleGridSettings                _gridCreationSettings;

        public TileRuleSpawnToolId                  activeToolId            
        { 
            get { return _activeToolId; }
            set 
            { 
                getCurrentBrush().cancel();
                _connectTool.cancel();

                _activeToolId = value; 
                ObjectSpawnUI.instance.onTileRuleSpawnActiveToolIdChanged(); 
                EditorUtility.SetDirty(this); 
            } 
        }  
        public TileRuleSpawnBrushId                 activeBrushId           
        { 
            get { return _activeBrushId; } 
            set 
            {
                if (_activeToolId == TileRuleSpawnToolId.Connect || 
                    _activeToolId == TileRuleSpawnToolId.RampPaint) return;

                getCurrentBrush().cancel();
                _connectTool.cancel();

                _activeBrushId = value; 
                ObjectSpawnUI.instance.onTileRuleSpawnActiveBrushIdChanged(); 
                EditorUtility.SetDirty(this); 
            } 
        }
        public override ObjectSpawnToolId           spawnToolId             { get { return ObjectSpawnToolId.TileRules; } }
        public override bool                        requiresSpawnGuide      { get { return false; } }

        public TileRuleObjectSpawnSettings          settings
        {
            get
            {
                if (_settings == null) _settings = AssetDbEx.loadScriptableObject<TileRuleObjectSpawnSettings>(PluginFolders.settings);
                return _settings;
            }
        }
        public TileRuleGridSettings                 gridCreationSettings
        {
            get
            {
                if (_gridCreationSettings == null) _gridCreationSettings = AssetDbEx.loadScriptableObject<TileRuleGridSettings>(PluginFolders.settings, typeof(TileRuleObjectSpawn).Name + "_" + typeof(TileRuleGridSettings).Name);
                return _gridCreationSettings;
            }
        }
        public Vector3Int                           rampBrushCellCoords     { get { return _rampBoxBrush.minCellCoords; } }

        public TileRuleGrid findCurrentGrid()
        {
            int numGrids = TileRuleGridDb.instance.numGrids;
            for (int i = 0; i < numGrids; ++i)
            {
                var grid = TileRuleGridDb.instance.getGrid(i);
                if (grid.uiSelected) return grid;
            }

            return null;
        }

        public override void onNoLongerActive()
        {
            getCurrentBrush().cancel();
            _connectTool.cancel();
        }

        protected override void doOnSceneGUI()
        {
            _currentGrid = findCurrentGrid();
            if (_currentGrid == null) return;

            _currentGrid.onSceneGUI(getGridYOffset());
            if (_currentGrid.mirrorGizmo.isDraggingHandles) return;
           
            if (_activeToolId == TileRuleSpawnToolId.Connect)
            {
                updateConnectTool();
                _connectTool.onSceneGUI();
                _connectTool.draw();

                if (!ObjectSpawnPrefs.instance.trSpawnDynamicGrid)
                    _connectTool.drawShadow(ObjectSpawnPrefs.instance.trSpawnShadowLineColor, ObjectSpawnPrefs.instance.trSpawnShadowColor);

                TileRuleObjectSpawnUI.instance.setEnabled(!_connectTool.pickingEnd);
                TileRuleProfileDbUI.instance.setEnabled(!_connectTool.pickingEnd);
            }
            else
            {
                updateBrushes();
                var currentBrush                = getCurrentBrush();
                currentBrush.tileRuleGrid       = _currentGrid;
                currentBrush.gridSitsBelowBrush = ObjectSpawnPrefs.instance.trSpawnDynamicGrid;
                currentBrush.usage              = getCurrentBrushUsage();

                currentBrush.onSceneGUI();
                currentBrush.draw(getCurrentBrushBorderColor());
                if (!ObjectSpawnPrefs.instance.trSpawnDynamicGrid)
                    currentBrush.drawShadow(ObjectSpawnPrefs.instance.trSpawnShadowLineColor, ObjectSpawnPrefs.instance.trSpawnShadowColor);

                TileRuleObjectSpawnUI.instance.setEnabled(getCurrentBrush().isIdle);
                TileRuleProfileDbUI.instance.setEnabled(getCurrentBrush().isIdle);
            }
        }

        private TileRuleBrushUsage getCurrentBrushUsage()
        {
            if (_activeToolId == TileRuleSpawnToolId.Paint) return TileRuleBrushUsage.Paint;
            else if (_activeToolId == TileRuleSpawnToolId.RampPaint) return TileRuleBrushUsage.RampPaint;
            else return TileRuleBrushUsage.Erase;
        }

        private Color getCurrentBrushBorderColor()
        {
            if (_activeToolId == TileRuleSpawnToolId.Erase) return ObjectSpawnPrefs.instance.trSpawnEraseBrushBorderColor;
            return ObjectSpawnPrefs.instance.trSpawnPaintBrushBorderColor;
        }

        private void updateConnectTool()
        {
            Event e = Event.current;
            if (FixedShortcuts.changeOffsetByScrollWheel(e))
            {
                if (canInputChangeConnectYOffset())
                {
                    e.disable();
                    settings.connectYOffset -= (int)(e.delta.y * 0.5f);
                    EditorUtility.SetDirty(settings);
                }
            }
            else
            if (FixedShortcuts.pickYOffsetOnClick(e))
            {
                if (canInputChangeConnectYOffset())
                {
                    e.disable();
                    Vector3Int cellCoords;
                    if (_currentGrid.pickCellCoords(PluginCamera.camera.getCursorRay(), getGridYOffset(), out cellCoords))
                    {
                        settings.connectYOffset = cellCoords.y;
                        EditorUtility.SetDirty(settings);
                    }
                }
            }

            _connectTool.tileRuleGrid       = _currentGrid;
            _connectTool.gridSitsBelow      = ObjectSpawnPrefs.instance.trSpawnDynamicGrid;

            if (_connectTool.pickingStart) _connectTool.startYOffset = settings.connectYOffset;
            else _connectTool.endYOffset = settings.connectYOffset;
        }

        private void updateBrushes()
        {
            Event e = Event.current;
            if (FixedShortcuts.changeRadiusByScrollWheel(e))
            {
                if (canInputChangeBrushSize())
                {
                    e.disable();
                    settings.brushSize -= (int)(e.delta.y * 0.5f);
                    EditorUtility.SetDirty(settings);
                }
            }
            else if (FixedShortcuts.changeHeightByScrollWheel(e))
            {
                if (canInputChangeBrushHeight())
                {
                    e.disable();
                    settings.brushHeight -= (int)(e.delta.y * 0.5f);
                    EditorUtility.SetDirty(settings);
                }
            }
            else
            if (FixedShortcuts.changeOffsetByScrollWheel(e))
            {
                if (canInputChangeBrushYOffset())
                {
                    e.disable();
                    settings.brushYOffset -= (int)(e.delta.y * 0.5f);
                    EditorUtility.SetDirty(settings);
                }
            }
            else
            if (FixedShortcuts.pickYOffsetOnClick(e))
            {
                if (canInputChangeBrushYOffset())
                {
                    e.disable();
                    Vector3Int cellCoords;
                    if (_currentGrid.pickCellCoords(PluginCamera.camera.getCursorRay(), getGridYOffset(), out cellCoords))
                    {
                        settings.brushYOffset = cellCoords.y;
                        EditorUtility.SetDirty(settings);
                    }
                }
            }

            _boxBrush.width     = settings.brushSize;
            _boxBrush.height    = settings.brushHeight;
            _boxBrush.depth     = settings.brushSize;

            _flexiBoxBrush.height   = settings.brushHeight;

            _segmentsBrush.setCurrentHeight(settings.brushHeight);
            
            _rampBoxBrush.width     = 1;
            _rampBoxBrush.height    = 1;
            _rampBoxBrush.depth     = 1;
        }

        private int getGridYOffset()
        {
            return ObjectSpawnPrefs.instance.trSpawnDynamicGrid ? settings.brushYOffset : 0;
        }

        private bool canInputChangeBrushSize()
        {
            return _activeToolId != TileRuleSpawnToolId.RampPaint && 
                _activeToolId != TileRuleSpawnToolId.Connect && 
                _activeBrushId == TileRuleSpawnBrushId.Box;
        }

        private bool canInputChangeBrushHeight()
        {
            return _activeToolId != TileRuleSpawnToolId.RampPaint && 
                _activeToolId != TileRuleSpawnToolId.Connect;
        }

        private bool canInputChangeBrushYOffset()
        {
            return _activeToolId != TileRuleSpawnToolId.Connect;
        }

        private bool canInputChangeConnectYOffset()
        {
            return _activeToolId == TileRuleSpawnToolId.Connect;
        }

        private TileRuleBrush getCurrentBrush()
        {
            if (_activeToolId == TileRuleSpawnToolId.RampPaint) return _rampBoxBrush;

            switch (_activeBrushId)
            {
                case TileRuleSpawnBrushId.Box:

                    return _boxBrush;

                case TileRuleSpawnBrushId.FlexiBox:

                    return _flexiBoxBrush;

                case TileRuleSpawnBrushId.Segments:

                    return _segmentsBrush;
            }

            return null;
        }
    }
}
#endif
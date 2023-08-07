#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class TileRuleObjectSpawnUI : PluginUI
    {
        [SerializeField]
        private ListViewState           _gridViewState;
        [NonSerialized]
        private ListView<UITileRuleGridItem, TileRuleGrid>  _gridView;
        [NonSerialized]
        private EntitySearchField       _gridSearchField;

        [SerializeField]
        private UISection               _trObjectSpawnSettingsSection;
        [SerializeField]
        private UISection               _trGridCreationSettingsSection;
        [SerializeField]
        private UISection               _tileRuleGridDbSection;
        [SerializeField]
        private string                  _newGridName;

        [NonSerialized]
        private List<TileRuleGrid>      _gridBuffer         = new List<TileRuleGrid>();
        [NonSerialized]
        private List<PluginGuid>        _gridIdBuffer       = new List<PluginGuid>();
        [NonSerialized]
        private List<GameObject>        _gridObjectBuffer   = new List<GameObject>();
        [NonSerialized]
        private List<GameObject>        _objectBuffer       = new List<GameObject>();
        [NonSerialized]
        private List<ObjectGroup>       _objectGroupBuffer  = new List<ObjectGroup>();


        public static TileRuleObjectSpawnUI instance { get { return GSpawn.active.tileRuleObjectSpawnUI; } }

        public void getVisibleSelectedGrids(List<TileRuleGrid> grids)
        {
            if (_gridView == null) return;

            grids.Clear();
            var selectedGrids = new List<TileRuleGrid>();
            _gridView.getVisibleSelectedItemData(selectedGrids);

            foreach (var itemData in selectedGrids)
                grids.Add(itemData);
        }

        public void getSelectedGrids(List<TileRuleGrid> grids)
        {
            if (_gridView == null) return;

            grids.Clear();
            var selectedGrids = new List<TileRuleGrid>();
            _gridView.getSelectedItemData(selectedGrids);

            foreach (var itemData in selectedGrids)
                grids.Add(itemData);
        }

        public void setSelectedGrid(TileRuleGrid grid)
        {
            if (_gridView == null) return;

            _gridView.setAllItemsSelected(false, false, false);
            _gridView.setItemSelected(grid.guid, true, false);
            _gridView.scheduleScrollToItem(grid.guid);
        }

        public void setSelectedGrids(List<TileRuleGrid> grids)
        {
            if (_gridView == null) return;

            _gridView.setAllItemsSelected(false, false, false);
            UITileRuleGridItem.getItemIds(grids, _gridIdBuffer);
            _gridView.setItemsSelected(_gridIdBuffer, true, false, false);
        }

        protected override void onBuild()
        {
            _trObjectSpawnSettingsSection.build("Tile Rule Spawn", TexturePool.instance.tileRuleBrushSpawn, true, contentContainer);
            ObjectSpawn.instance.tileRuleObjectSpawn.settings.buildUI(_trObjectSpawnSettingsSection.contentContainer);

            UI.createUISectionRowSeparator(contentContainer);
            _trGridCreationSettingsSection.build("Tile Rule Grid Creation", TexturePool.instance.settings, true, contentContainer);
            ObjectSpawn.instance.tileRuleObjectSpawn.gridCreationSettings.buildUI(_trGridCreationSettingsSection.contentContainer);
            createCreateNewGridControls(_trGridCreationSettingsSection.contentContainer);

            UI.createUISectionRowSeparator(contentContainer);
            _tileRuleGridDbSection.build("Tile Grids", TexturePool.instance.tileGrid, true, contentContainer);
            createGridSearchToolbar(_tileRuleGridDbSection.contentContainer);
            createTileRuleGridViewToolbar(_tileRuleGridDbSection.contentContainer);
            createGridView(_tileRuleGridDbSection.contentContainer);
            populateGridView();
        }

        private void createCreateNewGridControls(VisualElement parent)
        {
            var row                     = new VisualElement();
            row.style.flexDirection     = FlexDirection.Row;
            parent.Add(row);

            var createGridBtn           = new Button();
            row.Add(createGridBtn);

            createGridBtn.text          = "Create grid";
            createGridBtn.tooltip       = "Create a new grid with the specified settings and name. Note: If no name is specified, a default name will be used.";
            createGridBtn.style.width   = UIValues.useDefaultsButtonWidth;
            createGridBtn.clicked       += () => 
            {
                string gridName         = _newGridName;
                if (string.IsNullOrEmpty(gridName)) gridName = "TileRuleGrid";
                var tileGrid            = TileRuleGridDb.instance.createGrid(gridName, ObjectSpawn.instance.tileRuleObjectSpawn.gridCreationSettings);
                refresh();
                setSelectedGrid(tileGrid);
            };

            var gridNameField               =         UI.createTextField("_newGridName", serializedObject, "", "", row);
            gridNameField.style.flexGrow    = 1.0f;
        }

        private void createTileRuleGridViewToolbar(VisualElement parent)
        {
            var toolbar                     = UI.createStylizedToolbar(parent);

            var btn = UI.createToolbarButton(TexturePool.instance.delete, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            btn.tooltip = "Delete selected grids. Note: Pressing this button will also delete the grid objects from the scene.";
            btn.clicked += () =>
            {
                if (_gridView != null && TileRuleGridDb.instance.numGrids != 0)
                {
                    if (EditorUtility.DisplayDialog("Delete Tile Rule Grids", "This action will remove the selected grids from the scene together with all their children (including object groups). Would you like to continue? (You can Undo this operation).", "Yes", "No"))
                    {
                        getVisibleSelectedGrids(_gridBuffer);
                        UITileRuleGridItem.getItemIds(_gridBuffer, _gridIdBuffer);
                        _gridView.deleteItems(_gridIdBuffer);
                        TileRuleGridDb.getGameObjects(_gridBuffer, _gridObjectBuffer);

                        // Note: Before destroying the game objects, delete any objects groups which are associated with those objects.
                        //       This will allow object groups to be restored on Undo/Redo.
                        foreach(var gridObject in _gridObjectBuffer)
                        {
                            gridObject.getAllChildren(true, true, _objectBuffer);
                            ObjectGroupDb.instance.findObjectGroups(_objectBuffer, _objectGroupBuffer);
                            ObjectGroupDb.instance.deleteObjectGroups(_objectGroupBuffer);
                        }

                        TileRuleGridDb.instance.deleteGrids(_gridBuffer);
                        UndoEx.destroyGameObjectsImmediate(_gridObjectBuffer);
                    }
                }
            };
            UI.useDefaultMargins(btn);

            btn = UI.createToolbarButton(TexturePool.instance.fixOverlaps, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            btn.tooltip = "Fix overlaps. Traverses all selected grids and disables the renderers of objects that overlap. Useful when dealing " + 
                "with tiles that have decorations that overlap when the tiles sit next to each other.";
            btn.clicked += () =>
            {
                if (_gridView != null && TileRuleGridDb.instance.numGrids != 0)
                {
                    getVisibleSelectedGrids(_gridBuffer);
                    foreach(var grid in _gridBuffer)
                    {
                        grid.fixObjectOverlaps();
                    }
                }
            };
            UI.useDefaultMargins(btn);
        }

        private void createGridView(VisualElement parent)
        {
            _gridView                   = new ListView<UITileRuleGridItem, TileRuleGrid>(_gridViewState, parent);
            _gridView.canDelete         = true;
            _gridView.canRenameItems    = true;
            _gridView.canMultiSelect    = true;

            _gridView.canDeleteSelectedItems        += onCanDeleteSelectedGridItems;
            _gridView.selectedItemsWillBeDeleted    += onSelectedGridItemsWillBeDeleted;
            _gridView.selectionChanged              += onSelectionChanged;
            _gridView.selectionDeleted              += onSelectionDeleted;

            _gridView.style.setBorderWidth(1.0f);
            _gridView.style.setBorderColor(UIValues.listViewBorderColor);
            _gridView.style.flexGrow = 1.0f;
            _gridView.style.height = 200.0f;
        }

        private void populateGridView()
        {
            if (_gridView == null) return;
            _gridSearchField.refreshMatchNames();

            _gridView.onBeginBuild();
            TileRuleGridDb.instance.getGrids(_gridBuffer);

            foreach (var grid in _gridBuffer)
                _gridView.addItem(grid, filterGrid(grid));

            _gridView.onEndBuild();
        }

        private void createGridSearchToolbar(VisualElement parent)
        {
            var toolbar = UI.createStylizedToolbar(parent);

            _gridSearchField = new EntitySearchField(toolbar, (entityNames) =>
            { TileRuleGridDb.instance.getGridNames(entityNames, null); },
            (name) => { _gridView.filterItems(filterGrid); });
        }

        private bool filterGrid(TileRuleGrid grid)
        {
            if (!_gridSearchField.matchName(grid.gridName)) return false;
            return true;
        }

        private void onCanDeleteSelectedGridItems(ListView<UITileRuleGridItem, TileRuleGrid> listView, List<PluginGuid> itemIds, YesNoAnswer answer)
        {
            answer.yes();
        }

        private void onSelectedGridItemsWillBeDeleted(ListView<UITileRuleGridItem, TileRuleGrid> listView, List<PluginGuid> itemIds)
        {
            _gridView.getItemData(itemIds, _gridBuffer);
             TileRuleGridDb.instance.deleteGrids(_gridBuffer);
        }

        private void onSelectionChanged(ListView<UITileRuleGridItem, TileRuleGrid> listView)
        {
            listView.refreshUI();   // Note: To make sure the current grid (see TIleRuleBrushObjectSpawn) is highlighted correctly.
            SceneView.RepaintAll();
        }

        private void onSelectionDeleted(ListView<UITileRuleGridItem, TileRuleGrid> listView)
        {
            SceneView.RepaintAll();
        }

        protected override void onUndoRedo()
        {
            populateGridView();
        }

        protected override void onRefresh()
        {
            populateGridView();
        }

        protected override void onEnabled()
        {
            if (_gridViewState == null)
            {
                _gridViewState = ScriptableObject.CreateInstance<ListViewState>();
                _gridViewState.name = GetType().Name + "_GridViewState";
            }

            if (_trObjectSpawnSettingsSection == null) _trObjectSpawnSettingsSection  = ScriptableObject.CreateInstance<UISection>();
            if (_trGridCreationSettingsSection == null) _trGridCreationSettingsSection  = ScriptableObject.CreateInstance<UISection>();
            if (_tileRuleGridDbSection == null)         _tileRuleGridDbSection          = ScriptableObject.CreateInstance<UISection>();
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_trObjectSpawnSettingsSection);
            ScriptableObjectEx.destroyImmediate(_trGridCreationSettingsSection);
            ScriptableObjectEx.destroyImmediate(_tileRuleGridDbSection);
            ScriptableObjectEx.destroyImmediate(_gridViewState);
        }
    }
}
#endif
#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using System;

namespace GSpawn
{
    public class TileRuleGridDb : ScriptableObject
    {
        [SerializeField]
        private List<TileRuleGrid>      _grids          = new List<TileRuleGrid>();
        [NonSerialized]
        private List<TileRuleGrid>      _gridBuffer     = new List<TileRuleGrid>();
        [NonSerialized]
        private List<string>            _stringBuffer    = new List<string>();

        public int                      numGrids        { get { return _grids.Count; } }

        public static TileRuleGridDb    instance        { get { return GSpawn.active.tileRuleGridDb; } }

        public static void getGameObjects(List<TileRuleGrid> grids, List<GameObject> gameObjects)
        {
            gameObjects.Clear();
            foreach(var grid in grids)
                gameObjects.Add(grid.gameObject);
        }

        public void refreshTileRuleGridTiles(TileRuleProfile tileRuleProfile)
        {
            for (int i = 0; i < numGrids; ++i)
            {
                var grid = getGrid(i);
                if (grid.settings.tileRuleProfile == tileRuleProfile)
                    grid.refreshTiles();
            }
        }

        public bool isObjectChildOfTileRuleGrid(GameObject gameObject)
        {
            Transform transform = gameObject.transform;
            foreach(var grid in _grids)
            {
                if (transform.IsChildOf(grid.transform)) return true;
            }

            return false;
        }

        public TileRuleGrid createGrid(string name, TileRuleGridSettings initialGridSettings)
        {
            if (string.IsNullOrEmpty(name)) return null;

            UndoEx.saveEnabledState();
            UndoEx.enabled = false;

            getGridNames(_stringBuffer, null);
            name = UniqueNameGen.generate(name, _stringBuffer);

            UndoEx.record(this);
            var grid        = UndoEx.createScriptableObject<TileRuleGrid>();
            grid.gridName   = name;
            grid.name       = name;
            grid.initialize(initialGridSettings);

            _grids.Add(grid);

            UndoEx.restoreEnabledState();
            return grid;
        }

        public void renameGrid(TileRuleGrid grid, string newName)
        {
            if (!string.IsNullOrEmpty(newName) &&
                containsGrid(grid) && grid.gridName != newName)
            {
                getGridNames(_stringBuffer, grid.gridName);
                UndoEx.record(this);
                grid.gridName   = UniqueNameGen.generate(newName, _stringBuffer);
                grid.name       = grid.gridName;

                UndoEx.record(grid.gameObject);
                grid.gameObject.name    = newName;
            }
        }

        public void deleteGrid(TileRuleGrid grid)
        {
            //UndoEx.saveEnabledState();
            //UndoEx.enabled = false;

            if (grid != null)
            {
                if (containsGrid(grid))
                {
                    UndoEx.record(this);
                    _grids.Remove(grid);
                    UndoEx.destroyObjectImmediate(grid);
                }
            }

            //UndoEx.restoreEnabledState();
        }

        public void deleteGrids(List<TileRuleGrid> grids)
        {
            //UndoEx.saveEnabledState();
            //UndoEx.enabled = false;

            if (grids.Count != 0)
            {
                UndoEx.record(this);
                _gridBuffer.Clear();
                foreach (var grid in grids)
                {
                    if (containsGrid(grid))
                    {
                        _grids.Remove(grid);
                        _gridBuffer.Add(grid);
                    }
                }

                foreach (var grid in _gridBuffer)
                    UndoEx.destroyObjectImmediate(grid);
            }

            //UndoEx.restoreEnabledState();
        }

        public void deleteAllGrids()
        {
            //UndoEx.saveEnabledState();
            //UndoEx.enabled = false;

            if (_grids.Count != 0)
            {
                UndoEx.record(this);

                _gridBuffer.Clear();
                _gridBuffer.AddRange(_grids);
                _grids.Clear();

                foreach (var grid in _gridBuffer)
                    UndoEx.destroyObjectImmediate(grid);
            }

            //UndoEx.restoreEnabledState();
        }

        public bool containsGrid(TileRuleGrid grid)
        {
            return _grids.Contains(grid);
        }

        public TileRuleGrid getGrid(int index)
        {
            return _grids[index];
        }

        public void getGrids(List<TileRuleGrid> grids)
        {
            grids.Clear();
            grids.AddRange(_grids);
        }

        public void getGridNames(List<string> names, string ignoredName)
        {
            names.Clear();
            foreach (var grid in _grids)
            {
                if (grid.gridName != ignoredName)
                    names.Add(grid.gridName);
            }
        }

        private void OnDestroy()
        {
            deleteAllGrids();
        }
    }
}
#endif
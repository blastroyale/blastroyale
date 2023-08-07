#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System;

namespace GSpawn
{
    public class TileRuleProfile : Profile
    {
        [SerializeField]   
        private List<TileRule>          _tileRules              = new List<TileRule>();

        [NonSerialized]
        private List<TileRulePrefab>    _tileRulePrefabBuffer   = new List<TileRulePrefab>();
        [NonSerialized]
        private List<TileRule>          _tileRuleBuffer         = new List<TileRule>();

        public int                       numTileRules           { get { return _tileRules.Count;} }

        public void onPrefabAssetWillBeDeleted(GameObject prefabAsset)
        {
            foreach (var rule in _tileRules)
                rule.onPrefabAssetWillBeDeleted(prefabAsset);
        }

        public int calcTotalNumberOfPrefabs()
        {
            int numPrefabs = 0;
            foreach (var rule in _tileRules)
                numPrefabs += rule.numPrefabs;

            return numPrefabs;
        }

        public void resetPrefabPreviews()
        {
            PluginProgressDialog.begin("Refreshing Prefab Previews");
            for (int ruleIndex = 0; ruleIndex < numTileRules; ++ruleIndex)
            {
                var tileRule = _tileRules[ruleIndex];
                PluginProgressDialog.updateItemProgress("Tile rule: " + ruleIndex,
                    (ruleIndex + 1) / (float)numTileRules);

                tileRule.resetPrefabPreviews();
            }
            PluginProgressDialog.end();
        }

        public void regeneratePrefabPreviews()
        {
            PluginProgressDialog.begin("Regenerating Prefab Previews");
            for (int ruleIndex = 0; ruleIndex < numTileRules; ++ruleIndex)
            {
                var tileRule = _tileRules[ruleIndex];
                PluginProgressDialog.updateItemProgress("Tile rule: " + ruleIndex,
                    (ruleIndex + 1) / (float)numTileRules);

                tileRule.regeneratePrefabPreviews();
            }
            PluginProgressDialog.end();
        }

        public TileRule createTileRule()
        {
            UndoEx.saveEnabledState();
            UndoEx.enabled = false;

            var tileRule = UndoEx.createScriptableObject<TileRule>();
            tileRule.name = "TileRule";

            AssetDbEx.addObjectToAsset(tileRule, this);
            _tileRules.Add(tileRule);

            EditorUtility.SetDirty(this);
            UndoEx.restoreEnabledState();

            return tileRule;
        }

        public void deleteTileRule(TileRule tileRule)
        {
            if (tileRule != null)
            {
                if (containsTileRule(tileRule))
                {
                    UndoEx.record(this);

                    _tileRules.Remove(tileRule);

                    UndoEx.destroyObjectImmediate(tileRule);
                    EditorUtility.SetDirty(this);
                }
            }
        }

        public void deleteAllTileRules()
        {
            UndoEx.record(this);
            _tileRuleBuffer.Clear();

            if (_tileRules.Count != 0)
            {
                _tileRuleBuffer.AddRange(_tileRules);
                _tileRules.Clear();
            }

            foreach (var tileRule in _tileRuleBuffer)
                UndoEx.destroyObjectImmediate(tileRule);

            EditorUtility.SetDirty(this);
        }

        public bool containsTileRule(TileRule tileRule)
        {
            return _tileRules.Contains(tileRule);
        }

        public TileRule getTileRule(int index)
        {
            return _tileRules[index];
        }

        public int deleteNullPrefabs()
        {
            int numDeleted = 0;
            foreach (var rule in _tileRules)
                numDeleted += rule.deleteNullPrefabs();

            return numDeleted;
        }

        public void deletePrefabs(List<PluginPrefab> pluginPrefabs)
        {
            foreach(var rule in _tileRules)
            {
                rule.getPrefabs(pluginPrefabs, _tileRulePrefabBuffer);
                rule.deletePrefabs(_tileRulePrefabBuffer);
            }
        }

        public void deletePrefabs(List<TileRulePrefab> tileRulePrefabs)
        {
            foreach (var rule in _tileRules)
                rule.deletePrefabs(tileRulePrefabs);
        }

        public bool containsPrefab(GameObject prefabAsset)
        {
            foreach(var rule in _tileRules)
            {
                if (rule.containsPrefab(prefabAsset)) return true;
            }

            return false;
        }

        public void moveRuleUp(TileRule tileRule)
        {
            if (numTileRules <= 1) return;

            int ruleIndex = _tileRules.IndexOf(tileRule);
            if (ruleIndex <= 0) return;
       
            UndoEx.record(this);
            TileRule temp = _tileRules[ruleIndex - 1];
            _tileRules[ruleIndex - 1] = tileRule;
            _tileRules[ruleIndex] = temp;
        }

        public void moveRuleToTop(TileRule tileRule)
        {
            if (numTileRules <= 1) return;

            int ruleIndex = _tileRules.IndexOf(tileRule);
            if (ruleIndex <= 0) return;

            UndoEx.record(this);
            int above = ruleIndex - 1;
            while (above >= 0)
            {
                int below = above + 1;
                _tileRules[below] = _tileRules[above];
                --above;
            }

            _tileRules[0] = tileRule;
        }

        public void moveRuleToBottom(TileRule tileRule)
        {
            if (numTileRules <= 1) return;

            int ruleIndex = _tileRules.IndexOf(tileRule);
            if (ruleIndex < 0 || ruleIndex == (numTileRules - 1)) return;

            UndoEx.record(this);
            int below = ruleIndex + 1;
            while (below < numTileRules)
            {
                int above = below - 1;
                _tileRules[above] = _tileRules[below];
                ++below;
            }

            _tileRules[numTileRules - 1] = tileRule;
        }

        public void moveRuleDown(TileRule tileRule)
        {
            if (numTileRules <= 1) return;

            int ruleIndex = _tileRules.IndexOf(tileRule);
            if (ruleIndex < 0 || ruleIndex == (numTileRules - 1)) return;

            UndoEx.record(this);
            TileRule temp = _tileRules[ruleIndex + 1];
            _tileRules[ruleIndex + 1] = tileRule;
            _tileRules[ruleIndex] = temp;
        }

        public bool containsRule(TileRuleType ruleType)
        {
            foreach(var rule in _tileRules)
            {
                if (rule.ruleType == ruleType) return true;
            }

            return false;
        }

        public void getTileRules(List<TileRule> tileRules)
        {
            tileRules.Clear();
            tileRules.AddRange(_tileRules);
        }

        public void getTileRules(TileRuleType ruleType, List<TileRule> tileRules) 
        {
            tileRules.Clear();
            foreach(var rule in _tileRules)
            {
                if (rule.ruleType == ruleType)
                    tileRules.Add(rule);
            }
        }

        private void OnDestroy()
        {
            deleteAllTileRules();
        }
    }
}
#endif
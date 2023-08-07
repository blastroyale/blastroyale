#if UNITY_EDITOR
//#define TILE_RULE_PREFAB_PROFILE_NO_DUPLICATE_PREFABS
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using UnityEditor;

namespace GSpawn
{
    public enum TileRuleType
    {
        Standard  = 0,
        Platform,
        Ramp
    }

    public enum TileRuleRotationMode
    {
        Fixed   = 0,
        Rotated,
        [Obsolete]
        MirrorX,
        [Obsolete]
        MirrorZ
    }

    [Flags]
    public enum TileRuleFilter
    {
        None        = 0,
        Standard    = 1,
        Platform    = 2,
        Ramp        = 4,
        All         = ~0
    }

    public class TileRulePrefabPickParams
    {
        public TileRuleGrid grid;
        public Vector3Int   cellCoords;
    }

    public class TileRule : ScriptableObject
    {
        [NonSerialized]
        private SerializedObject        _serializedObject;

        [SerializeField]
        private TileRuleType            _ruleType               = TileRuleType.Standard;
        [SerializeField]
        private TileRuleRotationMode    _ruleRotationMode       = TileRuleRotationMode.Rotated;
        [SerializeField]
        private TileRuleMask            _ruleMask               = new TileRuleMask();
        [SerializeField]
        private List<TileRulePrefab>    _prefabs                = new List<TileRulePrefab>();  
        [SerializeField]
        private GridViewState           _prefabViewState        = null;

        [NonSerialized]
        private VisualElement           _ui                     = null;
        [NonSerialized]
        private VisualElement           _bitButtonGrid          = null;
        [NonSerialized]
        private GridView<UITileRulePrefabItem, UITileRulePrefabItemData>    _prefabView = null;
        [NonSerialized] 
        private List<TileRulePrefab>                        _tileRulePrefabBuffer       = new List<TileRulePrefab>();
        [NonSerialized]
        public bool                                         _probabilityTableDirty      = true;
        [NonSerialized]
        public CumulativeProbabilityTable<TileRulePrefab>   _probabilityTable           = new CumulativeProbabilityTable<TileRulePrefab>();
        [NonSerialized]
        public CumulativeProbabilityTable<TileRulePrefab>   _condSatisfyPrefabTable     = new CumulativeProbabilityTable<TileRulePrefab>();

        public int                                                      numPrefabs          { get { return _prefabs.Count; } }
        public TileRuleType                                             ruleType            { get { return _ruleType; } set { UndoEx.record(this); _ruleType = value; EditorUtility.SetDirty(this); } }
        public TileRuleRotationMode                                     ruleRotationMode    { get { return _ruleRotationMode; } set { UndoEx.record(this); _ruleRotationMode = value; EditorUtility.SetDirty(this); } }
        public VisualElement                                            ui                  { get { return _ui; } set { _ui = value; _prefabView = null; _bitButtonGrid = null; } }
        public VisualElement                                            bitButtonGrid       { get { return _bitButtonGrid; } set { _bitButtonGrid = value; } }
        public int                                                      numReqOnBitsSet     { get { return _ruleMask.numReqOnBitsSet; } }
        public GridView<UITileRulePrefabItem, UITileRulePrefabItemData> prefabView          { get { return _prefabView; } set { _prefabView = value; } }
        public GridViewState                                            prefabViewState     { get { return _prefabViewState; } }
        public SerializedObject                                         serializedObject    { get { if (_serializedObject == null) _serializedObject = new SerializedObject(this); return _serializedObject; } }

        public bool checkMaskBit(int row, int col, TileRuleBitMaskId maskId)
        {
            UndoEx.record(this);
            return _ruleMask.checkBit(row, col, maskId);
        }

        public void clearMaskBit(int row, int col, TileRuleBitMaskId maskId)
        {
            UndoEx.record(this);
            _ruleMask.clearBit(row, col, maskId);
        }

        public void setMaskBit(int row, int col, TileRuleBitMaskId maskId)
        {
            UndoEx.record(this);
            _ruleMask.setBit(row, col, maskId);
        }

        public void setAllMaskBits(TileRuleBitMaskId maskId)
        {
            UndoEx.record(this);
            _ruleMask.setAllBits(maskId);
        }

        public void toggleMaskBit(int row, int col, TileRuleBitMaskId maskId)
        {
            UndoEx.record(this);
            _ruleMask.toggleBit(row, col, maskId);
        }

        public TileRuleMaskMatchResult match(ulong ruleMask)
        {
            return _ruleMask.match(ruleMask, ruleRotationMode);
        }

        public void useDefaultMask()
        {
            UndoEx.record(this);
            _ruleMask.useDefaultValue();
        }

        public void resetPrefabPreviews()
        {
            int numPrefabs = _prefabs.Count;
            for (int prefabIndex = 0; prefabIndex < numPrefabs; ++prefabIndex)
                _prefabs[prefabIndex].resetPreview();
        }

        public void regeneratePrefabPreviews()
        {
            int numPrefabs = _prefabs.Count;
            for (int prefabIndex = 0; prefabIndex < numPrefabs; ++prefabIndex)
                _prefabs[prefabIndex].regeneratePreview();
        }

        public void onPrefabsSettingsChanged()
        {
            _probabilityTableDirty = true;
        }

        public void onPrefabsUsedStateChanged()
        {
            _probabilityTableDirty = true;
        }

        public void onPrefabsSpawnChanceChanged()
        {
            _probabilityTableDirty = true;
        }

        public void onPrefabsConditionsChanged()
        {
            _probabilityTableDirty = true;
        }

        public void onPrefabAssetWillBeDeleted(GameObject prefabAsset)
        {
            if (_prefabView != null) _prefabView.deleteItems(itemData => itemData.tileRulePrefab.prefabAsset == prefabAsset);

            var prefabsToRemove =   _prefabs.FindAll(item => item.prefabAsset == prefabAsset);

            foreach (var rulePrefab in prefabsToRemove)
            {
                _prefabs.Remove(rulePrefab);
                AssetDbEx.removeObjectFromAsset(rulePrefab, this);
                DestroyImmediate(rulePrefab);
            }

            if (prefabsToRemove.Count != 0)
                _probabilityTableDirty = true;
        }

        public int deleteNullPrefabs()
        {
            var prefabsToRemove = _prefabs.FindAll(item => item.pluginPrefab == null || item.prefabAsset == null);
            if (prefabsToRemove.Count != 0)
            {
                foreach (var rulePrefab in prefabsToRemove)
                {
                    _prefabs.Remove(rulePrefab);
                    AssetDbEx.removeObjectFromAsset(rulePrefab, this);
                    DestroyImmediate(rulePrefab);
                }

                _probabilityTableDirty = true;
            }

            return prefabsToRemove.Count;
        }

        public TileRulePrefab pickPrefab(TileRulePrefabPickParams pickParams)
        {
            // First, traverse the prefabs and identify the ones that satisfy all conditions
            _condSatisfyPrefabTable.clear();
            Vector3 cellCoords = pickParams.cellCoords;
            foreach (var prefab in _prefabs)
            {
                // Note: Take only used prefabs into account and the ones that have at least one condition active.
                if (prefab.used && prefab.isAnyConditionActive())
                {
                    // All conditions must be met
                    if (prefab.cellXCondition)
                    {
                        if (cellCoords.x < prefab.minCellX || cellCoords.x > prefab.maxCellX) continue;
                    }
                    if (prefab.cellYCondition)
                    {
                        if (cellCoords.y < prefab.minCellY || cellCoords.y > prefab.maxCellY) continue;
                    }
                    if (prefab.cellZCondition)
                    {
                        if (cellCoords.z < prefab.minCellZ || cellCoords.z > prefab.maxCellZ) continue;
                    }

                    // All conditions were met. Store the prefab in the table.
                    _condSatisfyPrefabTable.addEntity(prefab, prefab.spawnChance);
                }
            }

            // If there were any prefabs that satisfy the conditions, pick one of them
            // based on their spawn chance.
            if (_condSatisfyPrefabTable.numEntities != 0)
            {
                _condSatisfyPrefabTable.refresh();
                return _condSatisfyPrefabTable.pickEntity();
            }

            // We must pick based on prefab spawn chance. Make sure the probability table is up to date.
            if (_probabilityTableDirty)
            {
                _probabilityTable.clear();
                foreach (var prefab in _prefabs)
                {
                    // Note: We use only prefab marked as 'used'. We also allow conditioned prefabs
                    //       because we don't want tiles to disappear when making changes to the prefab conditions.
                    //       This can happen when all prefabs are conditioned but none satisfies all conditions.
                    if (prefab.used /*&& !prefab.isAnyConditionActive()*/)
                        _probabilityTable.addEntityAndRefresh(prefab, prefab.spawnChance);
                }

                _probabilityTableDirty = false;
            }

            return _probabilityTable.pickEntity();
        }

        public void createPrefabs(List<PluginPrefab> pluginPrefabs, List<TileRulePrefab> createdPrefabs, bool appendCreated, string progressTitle)
        {
            _tileRulePrefabBuffer.Clear();
            PluginProgressDialog.begin(progressTitle);
            if (!appendCreated) createdPrefabs.Clear();

            UndoEx.saveEnabledState();
            UndoEx.enabled = false;

            for (int prefabIndex = 0; prefabIndex < pluginPrefabs.Count; ++prefabIndex)
            {
                var pluginPrefab = pluginPrefabs[prefabIndex];
                PluginProgressDialog.updateItemProgress(pluginPrefab.prefabAsset.name, (prefabIndex + 1) / (float)pluginPrefabs.Count);

                #if TILE_RULE_PREFAB_PROFILE_NO_DUPLICATE_PREFABS
                if (!containsPrefab(pluginPrefab))
                #endif
                {
                    var rulePrefab          = UndoEx.createScriptableObject<TileRulePrefab>();
                    rulePrefab.pluginPrefab = pluginPrefab;
                    rulePrefab.name         = rulePrefab.pluginPrefab.prefabAsset.name;

                    AssetDbEx.addObjectToAsset(rulePrefab, this);
                    createdPrefabs.Add(rulePrefab);
                    _tileRulePrefabBuffer.Add(rulePrefab);
                }
            }

            UndoEx.record(this);
            foreach (var rulePrefab in _tileRulePrefabBuffer)
                _prefabs.Add(rulePrefab);

            EditorUtility.SetDirty(this);
            _probabilityTableDirty = true;

            PluginProgressDialog.end();
            UndoEx.restoreEnabledState();
        }

        public void deletePrefab(TileRulePrefab rulePrefab)
        {
            if (rulePrefab != null)
            {
                if (containsPrefab(rulePrefab))
                {
                    UndoEx.record(this);

                    _prefabs.Remove(rulePrefab);
                    _probabilityTableDirty = true;

                    UndoEx.destroyObjectImmediate(rulePrefab);
                    EditorUtility.SetDirty(this);
                }
            }
        }

        public void deletePrefabs(List<TileRulePrefab> rulePrefabs)
        {
            if (rulePrefabs.Count != 0)
            {
                UndoEx.record(this);
                _tileRulePrefabBuffer.Clear();

                foreach (var rulePrefab in rulePrefabs)
                {
                    if (containsPrefab(rulePrefab))
                    {
                        _prefabs.Remove(rulePrefab);
                        _tileRulePrefabBuffer.Add(rulePrefab);
                        _probabilityTableDirty = true;
                    }
                }

                foreach (var prefab in _tileRulePrefabBuffer)
                    UndoEx.destroyObjectImmediate(prefab);

                EditorUtility.SetDirty(this);
            }
        }

        public void deleteAllPrefabs()
        {
            UndoEx.record(this);
            _tileRulePrefabBuffer.Clear();

            if (_prefabs.Count != 0)
            {
                _tileRulePrefabBuffer.AddRange(_prefabs);
                _prefabs.Clear();
                _probabilityTableDirty = true;
            }

            foreach (var prefab in _tileRulePrefabBuffer)
                UndoEx.destroyObjectImmediate(prefab);

            EditorUtility.SetDirty(this);
        }

        public bool containsPrefab(GameObject prefabAsset)
        {
            foreach (var rulePrefab in _prefabs)
            {
                if (rulePrefab.prefabAsset == prefabAsset) return true;
            }

            return false;
        }

        public bool containsPrefab(PluginPrefab pluginPrefab)
        {
            foreach (var rulePrefab in _prefabs)
            {
                if (rulePrefab.pluginPrefab == pluginPrefab) return true;
            }

            return false;
        }

        public bool containsPrefab(TileRulePrefab rulePrefab)
        {
            return _prefabs.Contains(rulePrefab);
        }

        public TileRulePrefab getPrefab(int index)
        {
            return _prefabs[index];
        }

        public TileRulePrefab getPrefab(PluginPrefab pluginPrefab)
        {
            foreach (var rulePrefab in _prefabs)
            {
                if (rulePrefab.pluginPrefab == pluginPrefab) return rulePrefab;
            }

            return null;
        }

        public void getPrefabs(List<TileRulePrefab> tileRulePrefabs)
        {
            tileRulePrefabs.Clear();
            tileRulePrefabs.AddRange(_prefabs);
        }

        public void getPrefabs(List<PluginPrefab> pluginPrefabs, List<TileRulePrefab> tileRulePrefabs)
        {
            tileRulePrefabs.Clear();

            foreach(var pluginPrefab in pluginPrefabs)
                tileRulePrefabs.Add(getPrefab(pluginPrefab));
        }

        public void getPluginPrefabs(List<PluginPrefab> pluginPrefabs, bool append)
        {
            if (!append) pluginPrefabs.Clear();
            foreach (var prefab in _prefabs)
                pluginPrefabs.Add(prefab.pluginPrefab);
        }

        private void onUndoRedo()
        {
            _probabilityTableDirty = true;
        }

        private void OnEnable()
        {
            if (_prefabViewState == null)
            {
                _prefabViewState        = ScriptableObject.CreateInstance<GridViewState>();
                _prefabViewState.name   = GetType().Name + "_PrefabViewState";
                AssetDbEx.addObjectToAsset(_prefabViewState, TileRuleProfileDb.instance);
            }

            Undo.undoRedoPerformed += onUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= onUndoRedo;
        }

        private void OnDestroy()
        {
            if (_ui != null)
            {
                if (_ui.parent != null) _ui.parent.Remove(_ui);
                _ui             = null;
            }
            _prefabView         = null;
            _bitButtonGrid      = null;

            if (_prefabViewState != null)
            {
                UndoEx.record(this);
                UndoEx.destroyObjectImmediate(_prefabViewState);
            }

            deleteAllPrefabs();
        }
    }
}
#endif
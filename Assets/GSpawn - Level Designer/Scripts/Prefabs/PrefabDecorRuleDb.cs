#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor;

namespace GSpawn
{
    [Serializable]
    public class DecorRule
    {
        public GameObject   decoratedPrefab         = null;
        Vector3             _relativePosition       = Vector3.zero;
        float               _relativeDistance       = 0.0f;
        public Quaternion   relativeRotation        = Quaternion.identity;

        public Vector3 relativePosition
        {
            get { return _relativePosition; }
            set { _relativePosition = value; _relativeDistance = _relativePosition.magnitude; }
        }
        public float relativeDistance { get { return _relativeDistance; } }

        public bool isSameRule(DecorRule other)
        {
            if (this == other) return true;

            if (decoratedPrefab != other.decoratedPrefab) return false;
            if (Vector3.Magnitude(_relativePosition - other._relativePosition) > 1e-4f) return false;
            if (Mathf.Abs(Quaternion.Dot(relativeRotation, other.relativeRotation) - 1.0f) > 1e-4f) return false;

            return true;
        }

        public DecorRule clone()
        {
            var clone                       = new DecorRule();
            clone.decoratedPrefab           = decoratedPrefab;
            clone._relativePosition         = _relativePosition;
            clone._relativeDistance         = _relativeDistance;
            clone.relativeRotation          = relativeRotation;

            return clone;
        }
    }

    [Serializable]
    public class DecorRuleList
    {
        public int favPropsSpawnRule = 0;
        public List<DecorRule> rules = new List<DecorRule>();

        public void sortRulesByRelativeDistance()
        {
            rules.Sort((DecorRule r0, DecorRule r1) => 
            { return r0.relativeDistance.CompareTo(r1.relativeDistance); });
        }
    }

    [Serializable]
    public class DecoratedMap : SerializableDictionary<GameObject, DecorRuleList> { }

    [Serializable]
    public class DecorPrefab
    {
        public GameObject       decorPrefab;
        public DecoratedMap     decoratedMap    = new DecoratedMap();

        public DecorPrefab(GameObject decorPrefabAsset)
        {
            decorPrefab = decorPrefabAsset;
        }

        public void setFavPropsSpawnRule(GameObject decoratedObject, int favRule)
        {
            if (decoratedObject == null) return;
            GameObject decoratedPrefab = decoratedObject.getOutermostPrefabAsset();
            if (decoratedPrefab == null) return;

            DecorRuleList ruleList = null;
            if (decoratedMap.TryGetValue(decoratedPrefab, out ruleList))
            {
                ruleList.favPropsSpawnRule = favRule;
            }
        }

        public int getFavPropsSpawnRule(GameObject decoratedObject)
        {
            if (decoratedObject == null) return 0;
            GameObject decoratedPrefab = decoratedObject.getOutermostPrefabAsset();
            if (decoratedPrefab == null) return 0;

            DecorRuleList ruleList = null;
            if (decoratedMap.TryGetValue(decoratedPrefab, out ruleList))
            {
                return ruleList.favPropsSpawnRule;
            }
            else return 0;
        }
    }

    [Serializable]
    public class DecorMap : SerializableDictionary<GameObject, DecorPrefab> { }

    public class DecorRuleApplyResult
    {
        public GameObject       decoratedPrefab;
        public List<DecorRule>  rules = new List<DecorRule>();
        public DecorRule        rule;
        public int              ruleIndex;
        public bool             applied;

        public void clear()
        {
            decoratedPrefab         = null;
            rules.Clear();
            rule                    = null;
            ruleIndex               = -1;
            applied                 = false;
        }

        public void copy(DecorRuleApplyResult src)
        {
            decoratedPrefab         = src.decoratedPrefab;
            rule                    = src.rule;
            ruleIndex               = src.ruleIndex;
            applied                 = src.applied;

            rules.Clear();
            rules.AddRange(src.rules);
        }
    }

    [Serializable]
    public class PrefabAssetSet : SerializableHashSet<GameObject> { };
    [Serializable]
    public class DecorPrefabSet
    {
        public PrefabAssetSet decorPrefabs = new PrefabAssetSet();
    }
    [Serializable]
    public class DecoratedTrackerMap : SerializableDictionary<GameObject, DecorPrefabSet> { }

    public class PrefabDecorRuleDb : ScriptableObject
    {
        const int       _decorRuleCap           = 20;
        private static  PrefabDecorRuleDb        _instance;

        [NonSerialized]
        private ObjectOverlapFilter             _gatherDecoratedFilter      = new ObjectOverlapFilter();
        [NonSerialized]
        private List<GameObject>                _decorPrefabInstances       = new List<GameObject>();
        [NonSerialized]
        private List<GameObject>                _decoratedBuffer            = new List<GameObject>();
        [NonSerialized]
        private List<GameObject>                _childrenAndSelfBuffer      = new List<GameObject>();

        [SerializeField]
        private DecorMap                        _decorMap                   = new DecorMap();
        //[SerializeField]
        //private DecoratedTrackerMap             _decoratedTracker           = new DecoratedTrackerMap();

        public static PrefabDecorRuleDb         instance
        {
            get
            {
                if (_instance == null) _instance = AssetDbEx.loadScriptableObject<PrefabDecorRuleDb>(PluginFolders.pluginInternal);
                return _instance;
            }
        }
        public static bool exists { get { return _instance != null; } }

/*
        public void getDecorPrefabs(GameObject decoratedPrefab, List<GameObject> decorPrefabs)
        {
            decorPrefabs.Clear();
            DecorPrefabSet decorPrefabSet = null;
            if (_decoratedTracker.TryGetValue(decoratedPrefab, out decorPrefabSet))
            {
                foreach (var p in decorPrefabSet.decorPrefabs)
                    decorPrefabs.Add(p);
            }
        }*/

        public DecorPrefab getDecor(GameObject decorPrefab)
        {
            DecorPrefab decor = null;
            if (_decorMap.TryGetValue(decorPrefab, out decor)) return decor;

            return null;
        }

        public void onPrefabAssetWillBeDeleted(GameObject prefabAsset)
        {
            if (_decorMap.ContainsKey(prefabAsset))
                _decorMap.Remove(prefabAsset);

            EditorUtility.SetDirty(this);
        }

        public void getDecoratedPrefabs(GameObject decorPrefab, List<GameObject> decoratedPrefabs)
        {
            decoratedPrefabs.Clear();

            DecorPrefab decor = null;
            if (_decorMap.TryGetValue(decorPrefab, out decor))
            {
                foreach (var pair in decor.decoratedMap)
                    decoratedPrefabs.Add(pair.Key);
            }
        }

        public void applyDecorRule(GameObject decorObject, GameObject decorPrefab, GameObject decoratedObject,
            int ruleIndex, bool applyRotation, DecorRuleApplyResult result)
        {
            result.clear();
            if (decorPrefab == null || decoratedObject == null) return;

            GameObjectType decoratedObjectType = GameObjectDataDb.instance.getGameObjectType(decoratedObject);
            if (decoratedObjectType == GameObjectType.Mesh && !decoratedObject.isTerrainMesh() && !decoratedObject.isSphericalMesh())
            {
                GameObject decoratedPrefabAsset = decoratedObject.getOutermostPrefabAsset();
                if (decoratedPrefabAsset != null)
                {
                    getDecorRules(decorPrefab, decoratedPrefabAsset, result.rules);
                    if (result.rules.Count != 0)
                    {
                        if (ruleIndex >= result.rules.Count) ruleIndex = 0;
                        else if (ruleIndex < 0) ruleIndex = result.rules.Count - 1;

                        var decorRule = result.rules[ruleIndex];
                        decorObject.transform.position = decoratedObject.transform.TransformPoint(decorRule.relativePosition);
                        if (applyRotation) decorObject.transform.rotation = decoratedObject.transform.rotation * decorRule.relativeRotation;

                        result.applied              = true;
                        result.rule                 = decorRule;
                        result.ruleIndex            = ruleIndex;
                        result.decoratedPrefab      = decoratedPrefabAsset;
                    }
                }
            }
        }

        public void getDecorRules(GameObject decorPrefab, GameObject decoratedPrefab, List<DecorRule> decorRules)
        {
            decorRules.Clear();

            DecorPrefab decor = null;
            if (_decorMap.TryGetValue(decorPrefab, out decor))
            {
                DecorRuleList ruleList = null;
                if (decor.decoratedMap.TryGetValue(decoratedPrefab, out ruleList))
                    decorRules.AddRange(ruleList.rules);
            }
        }

        public void getDecorRules(GameObject decorPrefab, List<DecorRule> decorRules)
        {
            decorRules.Clear();

            DecorPrefab decor = null;
            if (_decorMap.TryGetValue(decorPrefab, out decor))
            {
                foreach (var pair in decor.decoratedMap)
                    decorRules.AddRange(pair.Value.rules);
            }
        }

        [NonSerialized]
        private List<GameObject>        _profilePrefabAssets        = new List<GameObject>();
        [NonSerialized]
        private List<DecorRule>         _decorRuleBuffer            = new List<DecorRule>();
        [NonSerialized]
        private List<DecorRule>         _otherDecorRuleBuffer       = new List<DecorRule>();
        public void generateDecorRules(PrefabLibProfile libProfile)
        {           
            UndoEx.saveEnabledState();
            UndoEx.enabled = false;

            _decorMap.Clear();
            //_decoratedTracker.Clear();

            libProfile.getAllPrefabAssets(_profilePrefabAssets);
            int numProfilePrefabAssets = _profilePrefabAssets.Count;
            PluginProgressDialog.begin("Generating Decor Rules (" + numProfilePrefabAssets + " prefabs)");
            for (int profilePrefabIndex = 0; profilePrefabIndex < numProfilePrefabAssets; ++profilePrefabIndex)
            {                
                var decorPrefabAsset = _profilePrefabAssets[profilePrefabIndex];
                PluginProgressDialog.updateProgress(decorPrefabAsset.name, (profilePrefabIndex + 1) / (float)numProfilePrefabAssets);
                generateDecorRules(decorPrefabAsset, _profilePrefabAssets);
            }
            PluginProgressDialog.end();
           
            // Copy decor rules between prefabs with similar names
            PrefabCategoryName prefabCategoryName = new PrefabCategoryName();
            PluginProgressDialog.begin("Syncing Similar Prefabs");
            for (int profilePrefabIndex = 0; profilePrefabIndex < numProfilePrefabAssets; ++profilePrefabIndex)
            {
                var profilePrefab   = _profilePrefabAssets[profilePrefabIndex];
                PluginProgressDialog.updateProgress(profilePrefab.name, (profilePrefabIndex + 1) / (float)numProfilePrefabAssets);

                DecorPrefab decor = null;
                if (!_decorMap.TryGetValue(profilePrefab, out decor))
                {
                    decor = new DecorPrefab(profilePrefab);
                    _decorMap.Add(profilePrefab, decor);
                }

                int numDecorated = decor.decoratedMap.Count;

                prefabCategoryName.extract(profilePrefab.name);
                for (int otherIndex = 0; otherIndex < numProfilePrefabAssets; ++otherIndex)
                {
                    if (otherIndex == profilePrefabIndex) continue;

                    var otherProfilePrefab  = _profilePrefabAssets[otherIndex];
                    if (prefabCategoryName.matchPrefabName(otherProfilePrefab.name))
                    { 
                        DecorPrefab otherDecor = null;
                        if (!_decorMap.TryGetValue(otherProfilePrefab, out otherDecor))
                        {
                            otherDecor = new DecorPrefab(otherProfilePrefab);
                            _decorMap.Add(otherProfilePrefab, otherDecor);
                        }

                        int otherNumDecorated = otherDecor.decoratedMap.Count;
                        if (numDecorated != 0 && otherNumDecorated == 0) otherDecor.decoratedMap = decor.decoratedMap;
                        else if (numDecorated == 0 && otherNumDecorated != 0) decor.decoratedMap = otherDecor.decoratedMap;
                        else if (numDecorated > otherNumDecorated)
                        {
                            getDecorRules(profilePrefab, _decorRuleBuffer);

                            int numRules = _decorRuleBuffer.Count;
                            for (int ruleIndex = 0; ruleIndex < numRules; ++ruleIndex)
                                addDecorRule(otherProfilePrefab, _decorRuleBuffer[ruleIndex]);
                        }
                        else
                        if (numDecorated < otherNumDecorated)
                        {
                            getDecorRules(otherProfilePrefab, _decorRuleBuffer);

                            int numRules = _decorRuleBuffer.Count;
                            for (int ruleIndex = 0; ruleIndex < numRules; ++ruleIndex)
                                addDecorRule(profilePrefab, _decorRuleBuffer[ruleIndex]);
                        }
                    }
                }
            }
            PluginProgressDialog.end();
            UndoEx.restoreEnabledState();
            EditorUtility.SetDirty(this);
        }

        public void generateDecorRules(List<GameObject> decorPrefabAssets, PrefabLibProfile libProfile)
        {
            UndoEx.saveEnabledState();
            UndoEx.enabled = false;

            /*foreach (var pair in _decoratedTracker)
            {
                pair.Value.decorPrefabs.RemoveWhere(item => decorPrefabAssets.Contains(item));
            }*/

            libProfile.getAllPrefabAssets(_profilePrefabAssets);
            int numDecorPrefabs = decorPrefabAssets.Count;
            PluginProgressDialog.begin("Generating Decor Rules (" + numDecorPrefabs + " prefabs)");
            for (int decorPrefabIndex = 0; decorPrefabIndex < numDecorPrefabs; ++decorPrefabIndex)
            {
                var decorPrefabAsset = decorPrefabAssets[decorPrefabIndex];
                if (_decorMap.ContainsKey(decorPrefabAsset)) _decorMap.Remove(decorPrefabAsset);
                PluginProgressDialog.updateProgress(decorPrefabAsset.name, (decorPrefabIndex + 1) / (float)numDecorPrefabs);
                generateDecorRules(decorPrefabAsset, _profilePrefabAssets);
            }
            PluginProgressDialog.end();

            // Copy decor rules between prefabs with similar names
            PrefabCategoryName prefabCategoryName = new PrefabCategoryName();
            PluginProgressDialog.begin("Syncing Similar Prefabs");
            for (int decorPrefabIndex = 0; decorPrefabIndex < numDecorPrefabs; ++decorPrefabIndex)
            {
                var decorPrefabAsset = decorPrefabAssets[decorPrefabIndex];
                PluginProgressDialog.updateProgress(decorPrefabAsset.name, (decorPrefabIndex + 1) / (float)numDecorPrefabs);

                DecorPrefab decor = null;
                if (!_decorMap.TryGetValue(decorPrefabAsset, out decor))
                {
                    decor = new DecorPrefab(decorPrefabAsset);
                    _decorMap.Add(decorPrefabAsset, decor);
                }

                int numDecorated = decor.decoratedMap.Count;

                prefabCategoryName.extract(decor.decorPrefab.name);
                foreach (var pair in _decorMap)
                {
                    var otherDecor = pair.Value;
                    if (otherDecor == decor) continue;
                
                    if (prefabCategoryName.matchPrefabName(otherDecor.decorPrefab.name))
                    {
                        int otherNumDecorated = otherDecor.decoratedMap.Count;
                        if (otherNumDecorated > numDecorated)
                        {
                            getDecorRules(otherDecor.decorPrefab, _decorRuleBuffer);

                            int numRules = _decorRuleBuffer.Count;
                            for (int ruleIndex = 0; ruleIndex < numRules; ++ruleIndex)
                                addDecorRule(decor.decorPrefab, _decorRuleBuffer[ruleIndex]);
                        }
                        else
                        if (numDecorated > otherNumDecorated)
                        {
                            getDecorRules(decor.decorPrefab, _decorRuleBuffer);

                            int numRules = _decorRuleBuffer.Count;
                            for (int ruleIndex = 0; ruleIndex < numRules; ++ruleIndex)
                                addDecorRule(otherDecor.decorPrefab, _decorRuleBuffer[ruleIndex]);
                        }
                    }
                }
            }
            PluginProgressDialog.end();
            UndoEx.restoreEnabledState();
            EditorUtility.SetDirty(this);
        }

        [NonSerialized]
        private HashSet<GameObject> _decoratedPrefabAssetSet = new HashSet<GameObject>();
        private void generateDecorRules(GameObject decorPrefabAsset, List<GameObject> profilePrefabAssets)
        {
            _decoratedPrefabAssetSet.Clear();
            ObjectBounds.QueryConfig decorBoundsQConfig     = getDecorBoundsQConfig();
            ObjectOverlapConfig decoratedOverlapConfig      = ObjectOverlapConfig.defaultConfig;
            decoratedOverlapConfig.prefabMode               = ObjectOverlapPrefabMode.None;
            _gatherDecoratedFilter.objectTypes              = GameObjectType.Mesh;

            PluginScene.instance.findPrefabInstances(decorPrefabAsset, _decorPrefabInstances);
            int numInstances = _decorPrefabInstances.Count;
            for (int instIndex = 0; instIndex < numInstances; ++instIndex)
            {
                var decorPrefabInstance = _decorPrefabInstances[instIndex];
                if (!decorPrefabInstance.activeSelf) continue;

                OBB decorOBB = ObjectBounds.calcHierarchyWorldOBB(decorPrefabInstance, decorBoundsQConfig);
                if (!decorOBB.isValid) continue;

                decorPrefabInstance.getAllChildrenAndSelf(true, true, _childrenAndSelfBuffer);
                _gatherDecoratedFilter.setIgnoredObjects(_childrenAndSelfBuffer);
                PluginScene.instance.overlapBox(decorOBB, _gatherDecoratedFilter, decoratedOverlapConfig, _decoratedBuffer);

                int numDecorated = _decoratedBuffer.Count;
                int decoratedIndex = 0;
                while (decoratedIndex < numDecorated)
                {
                    var decorated = _decoratedBuffer[decoratedIndex];
                    if (decorated.isTerrainMesh() || decorated.isSphericalMesh())
                    {
                        ++decoratedIndex;
                        continue;
                    }

                    GameObject decoratedPrefabAsset = decorated.getOutermostPrefabAsset();
                    if (decoratedPrefabAsset == null)
                    {
                        ++decoratedIndex;
                        continue;
                    }
                  
                    // Note: The decor prefab OBB must intersect the decorated geometry.
                    if (!decorated.obbIntersectsMeshTriangles(decorOBB))
                    {
                        ++decoratedIndex;
                        continue;
                    }
 
                    var decorRule                   = new DecorRule();
                    decorRule.relativePosition      = decorated.transform.InverseTransformPoint(decorPrefabInstance.transform.position);
                    decorRule.relativeRotation      = Quaternion.Inverse(decorated.transform.rotation) * decorPrefabInstance.transform.rotation;
                    decorRule.decoratedPrefab       = decoratedPrefabAsset;
                    addDecorRule(decorPrefabAsset, decorRule);
                    _decoratedPrefabAssetSet.Add(decoratedPrefabAsset);

                    ++decoratedIndex;                
                }
            }

            PrefabCategoryName prefabCategoryName = new PrefabCategoryName();
            int numProfilePrefabAssets = profilePrefabAssets.Count;
            foreach (var decoratedPrefabAsset in _decoratedPrefabAssetSet)
            {
                prefabCategoryName.extract(decoratedPrefabAsset.name);

                for (int prefabIndex = 0; prefabIndex < numProfilePrefabAssets; ++prefabIndex)
                {
                    var maybeDecoratedPrefab = profilePrefabAssets[prefabIndex];

                    // Note: Don't check '_decoratedPrefabAssetSet.Contains(maybeDecoratedPrefab)'. Even if the prefab has already been
                    //       decorated, we are interested in borrowing decorations from prefabs of the same category.
                    if (maybeDecoratedPrefab == decoratedPrefabAsset) continue;

                    // If this prefab's name matches the category name of a prefab that has been decorated,
                    // copy the decor rules.
                    if (prefabCategoryName.matchPrefabName(maybeDecoratedPrefab.name))
                    {
                        getDecorRules(decorPrefabAsset, decoratedPrefabAsset, _decorRuleBuffer);
                        int numRules = _decorRuleBuffer.Count;
                        for (int ruleIndex = 0; ruleIndex < numRules; ++ruleIndex)
                        {
                            var decorRule               = _decorRuleBuffer[ruleIndex].clone();
                            decorRule.decoratedPrefab   = maybeDecoratedPrefab;
                            addDecorRule(decorPrefabAsset, decorRule);
                        }
                    }
                }
            }
        }

        private void addDecorRule(GameObject decorPrefab, DecorRule decorRule)
        {
            DecorPrefab     decor       = null;
            DecorRuleList   ruleList    = null;
            if (_decorMap.TryGetValue(decorPrefab, out decor))
            {               
                if (decor.decoratedMap.TryGetValue(decorRule.decoratedPrefab, out ruleList))
                {
                    if (ruleList.rules.Count < _decorRuleCap)
                    {
                        if (!checkForSameRules(decorRule, ruleList.rules))
                        {
                            ruleList.rules.Add(decorRule);

                            // Note: Keep array sorted.
                            ruleList.sortRulesByRelativeDistance();
                        }                            
                    }
                    else
                    {
                        if (!checkForSameRules(decorRule, ruleList.rules))
                        {
                            var rules = ruleList.rules;
                            for (int i = 0; i < _decorRuleCap; ++i)
                            {
                                if (decorRule.relativeDistance < rules[i].relativeDistance)
                                {
                                    rules[i] = decorRule;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    ruleList = new DecorRuleList();
                    ruleList.rules.Add(decorRule);
                    decor.decoratedMap.Add(decorRule.decoratedPrefab, ruleList);
                }
            }
            else
            {
                decor       = new DecorPrefab(decorPrefab);
                _decorMap.Add(decorPrefab, decor);
                ruleList    = new DecorRuleList();
                ruleList.rules.Add(decorRule);
                decor.decoratedMap.Add(decorRule.decoratedPrefab, ruleList);
            }

            /*DecorPrefabSet decorPrefabSet = null;
            if (_decoratedTracker.TryGetValue(decorRule.decoratedPrefab, out decorPrefabSet))
            {
                decorPrefabSet.decorPrefabs.Add(decorPrefab);
            }
            else
            {
                decorPrefabSet = new DecorPrefabSet();
                decorPrefabSet.decorPrefabs.Add(decorPrefab);
                _decoratedTracker.Add(decorRule.decoratedPrefab, decorPrefabSet);
            }*/

            EditorUtility.SetDirty(this);
        }

        private bool checkForSameRules(DecorRule rule, List<DecorRule> rules)
        {
            int numRules = rules.Count;
            for (int i = 0; i < numRules; ++i) 
            {
                if (rule.isSameRule(rules[i])) return true;
            }

            return false;
        }

        private ObjectBounds.QueryConfig getDecorBoundsQConfig()
        {
            ObjectBounds.QueryConfig decorBoundsQConfig     = ObjectBounds.QueryConfig.defaultConfig;
            decorBoundsQConfig.objectTypes                  = GameObjectType.Mesh | GameObjectType.Light | GameObjectType.ParticleSystem;
            decorBoundsQConfig.volumelessSize               = Vector3Ex.create(0.1f);
            decorBoundsQConfig.includeAddedObjectOverrides  = false;
            return decorBoundsQConfig;
        }
    }
}
#endif
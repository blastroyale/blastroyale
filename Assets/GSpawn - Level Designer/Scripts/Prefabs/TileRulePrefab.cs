#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public struct TileRulePrefabDiff
    {
        public bool used;
        public bool spawnChance;
        public bool cellXCondition;
        public bool minCellX;
        public bool maxCellX;
        public bool cellYCondition;
        public bool minCellY;
        public bool maxCellY;
        public bool cellZCondition;
        public bool minCellZ;
        public bool maxCellZ;
    }

    public class TileRulePrefab : ScriptableObject, IUIItemStateProvider
    {
        private SerializedObject    _serializedObject;

        [SerializeField]
        private PluginGuid          _guid                   = new PluginGuid(Guid.NewGuid());
        [SerializeField]
        private PluginPrefab        _pluginPrefab;
        [SerializeField]
        private PrefabPreview       _preview                = new PrefabPreview();
        [SerializeField]
        private bool                _uiSelected             = false;
        [NonSerialized]
        private CopyPasteMode       _uiCopyPasteMode        = CopyPasteMode.None;

        [SerializeField]
        private bool                _used                   = defaultUsed;
        [SerializeField]
        private float               _spawnChance            = defaultSpawnChance;
        [SerializeField]
        private bool                _cellXCondition         = defaultCellXCondition;
        [SerializeField]
        private int                 _minCellX               = defaultMinCellX;
        [SerializeField]
        private int                 _maxCellX               = defaultMaxCellX;
        [SerializeField]
        private bool                _cellYCondition         = defaultCellYCondition;
        [SerializeField]
        private int                 _minCellY               = defaultMinCellY;
        [SerializeField]
        private int                 _maxCellY               = defaultMaxCellY;
        [SerializeField]
        private bool                _cellZCondition         = defaultCellZCondition;
        [SerializeField]
        private int                 _minCellZ               = defaultMinCellZ;
        [SerializeField]
        private int                 _maxCellZ               = defaultMaxCellZ;

        public Texture2D            previewTexture          { get { return _preview.texture; } }
        public PluginPrefab         pluginPrefab
        {
            get { return _pluginPrefab; }
            set
            {
                _pluginPrefab = value;
                _preview.setPrefab(_pluginPrefab.prefabAsset);
                EditorUtility.SetDirty(this);
            }
        }
        public GameObject           prefabAsset             { get { return pluginPrefab.prefabAsset; } }
        public PluginGuid           guid                    { get { return _guid; } }
        public bool                 uiSelected              { get { return _uiSelected; } set { UndoEx.record(this); _uiSelected = value; } }
        public CopyPasteMode        uiCopyPasteMode         { get { return _uiCopyPasteMode; } set { _uiCopyPasteMode = value; } }
        public SerializedObject     serializedObject
        {
            get
            {
                if (_serializedObject == null) _serializedObject = new SerializedObject(this);
                return _serializedObject;
            }
        }
        public bool                 used                    { get { return _used; } set { UndoEx.record(this); _used = value; EditorUtility.SetDirty(this); } }
        public float                spawnChance             { get { return _spawnChance; } set { UndoEx.record(this); _spawnChance = Mathf.Clamp01(value); EditorUtility.SetDirty(this); } }
        public bool                 cellXCondition          { get { return _cellXCondition; } set { UndoEx.record(this); _cellXCondition = value; EditorUtility.SetDirty(this); } }
        public int                  minCellX 
        { 
            get { return _minCellX; } 
            set 
            { 
                UndoEx.record(this);
                _minCellX = value;
                if (_maxCellX < _minCellX) _maxCellX = _minCellX;
                EditorUtility.SetDirty(this);
            } 
        }
        public int                  maxCellX
        {
            get { return _maxCellX; }
            set
            {
                UndoEx.record(this);
                _maxCellX = value;
                if (_minCellX > _maxCellX) _minCellX = _maxCellX;
                EditorUtility.SetDirty(this);
            }
        }
        public bool                 cellYCondition { get { return _cellYCondition; } set { UndoEx.record(this); _cellYCondition = value; EditorUtility.SetDirty(this); } }
        public int                  minCellY
        {
            get { return _minCellY; }
            set
            {
                UndoEx.record(this);
                _minCellY = value;
                if (_maxCellY < _minCellY) _maxCellY = _minCellY;
                EditorUtility.SetDirty(this);
            }
        }
        public int                  maxCellY
        {
            get { return _maxCellY; }
            set
            {
                UndoEx.record(this);
                _maxCellY = value;
                if (_minCellY > _maxCellY) _minCellY = _maxCellY;
                EditorUtility.SetDirty(this);
            }
        }
        public bool                 cellZCondition { get { return _cellZCondition; } set { UndoEx.record(this); _cellZCondition = value; EditorUtility.SetDirty(this); } }
        public int                  minCellZ
        {
            get { return _minCellZ; }
            set
            {
                UndoEx.record(this);
                _minCellZ = value;
                if (_maxCellZ < _minCellZ) _maxCellZ = _minCellZ;
                EditorUtility.SetDirty(this);
            }
        }
        public int                  maxCellZ
        {
            get { return _maxCellY; }
            set
            {
                UndoEx.record(this);
                _maxCellZ = value;
                if (_minCellZ > _maxCellZ) _minCellZ = _maxCellZ;
                EditorUtility.SetDirty(this);
            }
        }

        public static bool          defaultUsed             { get { return true; } }
        public static float         defaultSpawnChance      { get { return 1.0f; } }
        public static bool          defaultCellXCondition   { get { return false; } }
        public static int           defaultMinCellX         { get { return -10; } }
        public static int           defaultMaxCellX         { get { return 10; } }
        public static bool          defaultCellYCondition   { get { return false; } }
        public static int           defaultMinCellY         { get { return -10; } }
        public static int           defaultMaxCellY         { get { return 10; } }
        public static bool          defaultCellZCondition   { get { return false; } }
        public static int           defaultMinCellZ         { get { return -10; } }
        public static int           defaultMaxCellZ         { get { return 10; } }

        public static TileRulePrefabDiff checkDiff(List<TileRulePrefab> tileRulePrefabs)
        {
            int maxNumDiffs         = typeof(TileRulePrefabDiff).GetFields().Length;
            TileRulePrefabDiff diff = new TileRulePrefabDiff();
            int numPrefabs          = tileRulePrefabs.Count;

            for (int i = 0; i < numPrefabs; ++i)
            {
                var prefab = tileRulePrefabs[i];
                for (int j = i + 1; j < numPrefabs; ++j)
                {
                    var otherPrefab = tileRulePrefabs[j];

                    int diffCount = 0;
                    if (prefab.used != otherPrefab.used)
                    {
                        ++diffCount;
                        diff.used = true;
                    }
                    if (prefab.spawnChance != otherPrefab.spawnChance)
                    {
                        ++diffCount;
                        diff.spawnChance = true;
                    }
                    if (prefab.cellXCondition != otherPrefab.cellXCondition)
                    {
                        ++diffCount;
                        diff.cellXCondition = true;
                    }
                    if (prefab.minCellX != otherPrefab.minCellX)
                    {
                        ++diffCount;
                        diff.minCellX = true;
                    }
                    if (prefab.maxCellX != otherPrefab.maxCellX)
                    {
                        ++diffCount;
                        diff.maxCellX = true;
                    }
                    if (prefab.cellYCondition != otherPrefab.cellYCondition)
                    {
                        ++diffCount;
                        diff.cellYCondition = true;
                    }
                    if (prefab.minCellY != otherPrefab.minCellY)
                    {
                        ++diffCount;
                        diff.minCellY = true;
                    }
                    if (prefab.maxCellY != otherPrefab.maxCellY)
                    {
                        ++diffCount;
                        diff.maxCellY = true;
                    }
                    if (prefab.cellZCondition != otherPrefab.cellZCondition)
                    {
                        ++diffCount;
                        diff.cellZCondition = true;
                    }
                    if (prefab.minCellZ != otherPrefab.minCellZ)
                    {
                        ++diffCount;
                        diff.minCellZ = true;
                    }
                    if (prefab.maxCellZ != otherPrefab.maxCellZ)
                    {
                        ++diffCount;
                        diff.maxCellZ = true;
                    }

                    if (diffCount == maxNumDiffs) return diff;
                }
            }

            return diff;
        }

        public static void getPluginPrefabs(List<TileRulePrefab> tileRulePrefabs, List<PluginPrefab> pluginPrefabs)
        {
            pluginPrefabs.Clear();
            foreach (var rulePrefab in tileRulePrefabs)
                pluginPrefabs.Add(rulePrefab.pluginPrefab);
        }

        public bool isAnyConditionActive()
        {
            return _cellXCondition || _cellYCondition || _cellZCondition;
        }

        public void resetPreview()
        {
            _preview.reset();
        }

        public void regeneratePreview()
        {
            _preview.regenerate();
        }

        public void rotatePreview(Vector2 yawPitch)
        {
            _preview.rotate(yawPitch);
        }

        public void useDefaults()
        {
            used                = defaultUsed;
            spawnChance         = defaultSpawnChance;

            cellXCondition      = defaultCellXCondition;
            minCellX            = defaultMinCellX;
            maxCellX            = defaultMaxCellX;
            cellYCondition      = defaultCellYCondition;
            minCellY            = defaultMinCellY;
            maxCellY            = defaultMaxCellY;
            cellZCondition      = defaultCellZCondition;
            minCellZ            = defaultMinCellZ;
            maxCellZ            = defaultMaxCellZ;

            EditorUtility.SetDirty(this);
        }
    }
}
#endif
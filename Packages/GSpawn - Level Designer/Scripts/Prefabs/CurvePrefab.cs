#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GSpawn
{
    public struct CurvePrefabDiff
    {
        public bool used;
        public bool spawnChance;
        public bool alignAxes;
        public bool upAxis;
        public bool invertUpAxis;
        public bool alignUpAxisWhenProjected;
        public bool forwardAxis;
        public bool invertForwardAxis;
        public bool jitterMode;
        public bool jitter;
        public bool minRandomJitter;
        public bool maxRandomJitter;
        public bool randomizeForwardAxisRotation;
        public bool minRandomForwardAxisRotation;
        public bool maxRandomForwardAxisRotation;
        public bool randomizeUpAxisRotation;
        public bool minRandomUpAxisRotation;
        public bool maxRandomUpAxisRotation;
        public bool upAxisOffsetMode;
        public bool upAxisOffset;
        public bool minRandomUpAxisOffset;
        public bool maxRandomUpAxisOffset;
        public bool randomizeScale;
        public bool minRandomScale;
        public bool maxRandomScale;
    }

    public enum CurvePrefabJitterMode
    {
        None = 0,
        Constant,
        Random,

        [Obsolete]
        UIMixed
    }

    public enum CurvePrefabUpAxisOffsetMode
    {
        Constant = 0,
        Random,

        [Obsolete]
        UIMixed
    }

    public class CurvePrefab : ScriptableObject, IUIItemStateProvider
    {
        private SerializedObject            _serializedObject;

        [SerializeField]
        private PluginGuid                  _guid                           = new PluginGuid(Guid.NewGuid());
        [SerializeField]
        private PluginPrefab                _pluginPrefab;
        [SerializeField]
        private PrefabPreview               _preview                        = new PrefabPreview();
        [SerializeField]
        private bool                        _uiSelected                     = false;
        [NonSerialized]
        private CopyPasteMode               _uiCopyPasteMode                = CopyPasteMode.None;

        [SerializeField]
        private bool                        _used                           = defaultUsed;
        [SerializeField]    
        private float                       _spawnChance                    = defaultSpawnChance;
        [SerializeField]
        private bool                        _alignAxes                      = defaultAlignAxes;
        [SerializeField]
        private FlexiAxis                   _upAxis                         = defaultUpAxis;
        [SerializeField]
        private bool                        _invertUpAxis                   = defaultInvertUpAxis;
        [SerializeField]
        private bool                        _alignUpAxisWhenProjected       = defaultAlignUpAxisWhenProjected;
        [SerializeField]
        private FlexiAxis                   _forwardAxis                    = defaultForwardAxis;
        [SerializeField]
        private bool                        _invertForwardAxis              = defaultInvertForwardAxis;
        [SerializeField]
        private CurvePrefabJitterMode       _jitterMode                     = defaultJitterMode;
        [SerializeField]
        private float                       _jitter                         = defaultJitter;
        [SerializeField]
        private float                       _minRandomJitter                = defaultMinRandomJitter;
        [SerializeField]
        private float                       _maxRandomJitter                = defaultMaxRandomJitter;
        [SerializeField]
        private bool                        _randomizeForwardAxisRotation   = defaultRandomizeForwardAxisRotation;
        [SerializeField]
        private float                       _minRandomForwardAxisRotation   = defaultMinRandomForwardAxisRotation;
        [SerializeField]
        private float                       _maxRandomForwardAxisRotation   = defaultMaxRandomForwardAxisRotation;
        [SerializeField]
        private bool                        _randomizeUpAxisRotation        = defaultRandomizeUpAxisRotation;
        [SerializeField]
        private float                       _minRandomUpAxisRotation        = defaultMinRandomUpAxisRotation;
        [SerializeField]
        private float                       _maxRandomUpAxisRotation        = defaultMaxRandomUpAxisRotation;
        [SerializeField]
        private CurvePrefabUpAxisOffsetMode _upAxisOffsetMode               = defaultUpAxisOffsetMode;
        [SerializeField]
        private float                       _upAxisOffset                   = defaultUpAxisOffset;
        [SerializeField]
        private float                       _minRandomUpAxisOffset          = defaultMinRandomUpAxisOffset;
        [SerializeField]
        private float                       _maxRandomUpAxisOffset          = defaultMaxRandomUpAxisOffset;
        [SerializeField]
        private bool                        _randomizeScale                 = defaultRandomizeScale;
        [SerializeField]
        private float                       _minRandomScale                 = defaultMinRandomScale;
        [SerializeField]
        private float                       _maxRandomScale                 = defaultMaxRandomScale;

        public Texture2D                    previewTexture                  { get { return _preview.texture; } }
        public PluginPrefab                 pluginPrefab
        {
            get { return _pluginPrefab; }
            set
            {
                _pluginPrefab = value;
                _preview.setPrefab(_pluginPrefab.prefabAsset);
                EditorUtility.SetDirty(this);
            }
        }
        public GameObject                   prefabAsset                     { get { return pluginPrefab.prefabAsset; } }
        public PluginGuid                   guid                            { get { return _guid; } }
        public bool                         uiSelected                      { get { return _uiSelected; } set { UndoEx.record(this); _uiSelected = value; } }
        public CopyPasteMode                uiCopyPasteMode                 { get { return _uiCopyPasteMode; } set { _uiCopyPasteMode = value; } }
        public SerializedObject             serializedObject
        {
            get
            {
                if (_serializedObject == null) _serializedObject = new SerializedObject(this);
                return _serializedObject;
            }
        }

        public bool                         used                            { get { return _used; } set { UndoEx.record(this); _used = value; EditorUtility.SetDirty(this); } }
        public float                        spawnChance                     { get { return _spawnChance; } set { UndoEx.record(this); _spawnChance = Mathf.Clamp01(value); EditorUtility.SetDirty(this); } }
        public bool                         alignAxes                       { get { return _alignAxes; } set { UndoEx.record(this); _alignAxes = value; EditorUtility.SetDirty(this); } }
        public FlexiAxis                    upAxis
        {
            get { return _upAxis; }
            set
            {
                UndoEx.record(this);
                _upAxis = value;
                EditorUtility.SetDirty(this);
            }
        }
        public bool                         invertUpAxis                    { get { return _invertUpAxis; } set { UndoEx.record(this); _invertUpAxis = value; EditorUtility.SetDirty(this); } }
        public bool                         alignUpAxisWhenProjected        { get { return _alignUpAxisWhenProjected; } set { UndoEx.record(this); _alignUpAxisWhenProjected = value; EditorUtility.SetDirty(this); } }
        public FlexiAxis                    forwardAxis
        {
            get { return _forwardAxis; }
            set
            {
                UndoEx.record(this);
                _forwardAxis = value;
                EditorUtility.SetDirty(this);
            }
        }
        public bool                         invertForwardAxis               { get { return _invertForwardAxis; } set { UndoEx.record(this); _invertForwardAxis = value; EditorUtility.SetDirty(this); } }
        public CurvePrefabJitterMode        jitterMode                      { get { return _jitterMode; } set { UndoEx.record(this); _jitterMode = value; EditorUtility.SetDirty(this); } }
        public float                        jitter                          { get { return _jitter; } set { UndoEx.record(this); _jitter = Mathf.Max(0.0f, value); EditorUtility.SetDirty(this); } }
        public float                        minRandomJitter
        {
            get { return _minRandomJitter; }
            set
            {
                _minRandomJitter = Mathf.Max(0.0f, value);
                if (_maxRandomJitter < _minRandomJitter)
                    _maxRandomJitter = _minRandomJitter;
                EditorUtility.SetDirty(this);
            }
        }
        public float                        maxRandomJitter
        {
            get { return _maxRandomJitter; }
            set
            {
                _maxRandomJitter = Mathf.Max(0.0f, value);
                if (_minRandomJitter > _maxRandomJitter)
                    _minRandomJitter = _maxRandomJitter;
                EditorUtility.SetDirty(this);
            }
        }
        public bool                         randomizeForwardAxisRotation    { get { return _randomizeForwardAxisRotation; } set { UndoEx.record(this); _randomizeForwardAxisRotation = value; EditorUtility.SetDirty(this); } }
        public float                        minRandomForwardAxisRotation
        {
            get { return _minRandomForwardAxisRotation; }
            set
            {
                UndoEx.record(this);
                _minRandomForwardAxisRotation = Math.Clamp(value, -90.0f, 90.0f);
                if (_maxRandomForwardAxisRotation < _minRandomForwardAxisRotation)
                    _maxRandomForwardAxisRotation = _minRandomForwardAxisRotation;
                EditorUtility.SetDirty(this);
            }
        }
        public float                        maxRandomForwardAxisRotation
        {
            get { return _maxRandomForwardAxisRotation; }
            set
            {
                UndoEx.record(this);
                _maxRandomForwardAxisRotation = Math.Clamp(value, -90.0f, 90.0f);
                if (_minRandomForwardAxisRotation > _maxRandomForwardAxisRotation)
                    _minRandomForwardAxisRotation = _maxRandomForwardAxisRotation;
                EditorUtility.SetDirty(this);
            }
        }
        public bool                         randomizeUpAxisRotation         { get { return _randomizeUpAxisRotation; } set { UndoEx.record(this); _randomizeUpAxisRotation = value; EditorUtility.SetDirty(this); } }
        public float                        minRandomUpAxisRotation
        {   
            get { return _minRandomUpAxisRotation; }
            set
            {
                UndoEx.record(this);
                _minRandomUpAxisRotation = Math.Clamp(value, -360.0f, 360.0f);
                if (_maxRandomUpAxisRotation < _minRandomUpAxisRotation)
                    _maxRandomUpAxisRotation = _minRandomUpAxisRotation;
                EditorUtility.SetDirty(this);
            }
        }
        public float                        maxRandomUpAxisRotation
        {
            get { return _maxRandomUpAxisRotation; }
            set
            {
                UndoEx.record(this);
                _maxRandomUpAxisRotation = Math.Clamp(value, -360.0f, 360.0f);
                if (_minRandomUpAxisRotation > _maxRandomUpAxisRotation)
                    _minRandomUpAxisRotation = _maxRandomUpAxisRotation;
                EditorUtility.SetDirty(this);
            }
        }
        public CurvePrefabUpAxisOffsetMode  upAxisOffsetMode                { get { return _upAxisOffsetMode; } set { UndoEx.record(this); _upAxisOffsetMode = value; EditorUtility.SetDirty(this); } }
        public float                        upAxisOffset                    { get { return _upAxisOffset; } set { UndoEx.record(this); _upAxisOffset = value; EditorUtility.SetDirty(this); } }
        public float                        minRandomUpAxisOffset
        {
            get { return _minRandomUpAxisOffset; }
            set
            {
                UndoEx.record(this);
                _minRandomUpAxisOffset = value;
                if (_maxRandomUpAxisOffset < _minRandomUpAxisOffset)
                    _maxRandomUpAxisOffset = _minRandomUpAxisOffset;
                EditorUtility.SetDirty(this);
            }
        }
        public float                        maxRandomUpAxisOffset
        {
            get { return _maxRandomUpAxisOffset; }
            set
            {
                UndoEx.record(this);
                _maxRandomUpAxisOffset = value;
                if (_minRandomUpAxisOffset > _maxRandomUpAxisOffset)
                    _minRandomUpAxisOffset = _maxRandomUpAxisOffset;
                EditorUtility.SetDirty(this);
            }
        }
        public bool                         randomizeScale                  { get { return _randomizeScale; } set { UndoEx.record(this); _randomizeScale = value; EditorUtility.SetDirty(this); } }
        public float                        minRandomScale
        {
            get { return _minRandomScale; }
            set
            {
                UndoEx.record(this);
                _minRandomScale = Math.Max(0.1f, value);
                if (_maxRandomScale < _minRandomScale)
                    _maxRandomScale = _minRandomScale;
                EditorUtility.SetDirty(this);
            }
        }
        public float                        maxRandomScale
        {
            get { return _maxRandomScale; }
            set
            {
                UndoEx.record(this);
                _maxRandomScale = Math.Max(0.1f, value);
                if (_minRandomScale > _maxRandomScale)
                    _minRandomScale = _maxRandomScale;
                EditorUtility.SetDirty(this);
            }
        }

        public static bool                  defaultUsed                         { get { return true; } }
        public static float                 defaultSpawnChance                  { get { return 1.0f; } }
        public static bool                  defaultAlignAxes                    { get { return true; } }
        public static FlexiAxis             defaultUpAxis                       { get { return FlexiAxis.Y; } }
        public static bool                  defaultInvertUpAxis                 { get { return false; } }
        public static bool                  defaultAlignUpAxisWhenProjected     { get { return true; } }
        public static FlexiAxis             defaultForwardAxis                  { get { return FlexiAxis.Longest; } }
        public static bool                  defaultInvertForwardAxis            { get { return false; } }
        public static CurvePrefabJitterMode defaultJitterMode                   { get { return CurvePrefabJitterMode.None; } }
        public static float                 defaultJitter                       { get { return 0.5f; } }
        public static float                 defaultMinRandomJitter              { get { return 0.0f; } }
        public static float                 defaultMaxRandomJitter              { get { return 0.5f; } }
        public static bool                  defaultRandomizeForwardAxisRotation { get { return false; } }
        public static float                 defaultMinRandomForwardAxisRotation { get { return -15.0f; } }
        public static float                 defaultMaxRandomForwardAxisRotation { get { return 15.0f; } }
        public static bool                  defaultRandomizeUpAxisRotation      { get { return false; } }
        public static float                 defaultMinRandomUpAxisRotation      { get { return -15.0f; } }
        public static float                 defaultMaxRandomUpAxisRotation      { get { return 15.0f; } }
        public static CurvePrefabUpAxisOffsetMode   defaultUpAxisOffsetMode     { get { return CurvePrefabUpAxisOffsetMode.Constant; } }
        public static float                 defaultUpAxisOffset                 { get { return 0.0f; } }
        public static float                 defaultMinRandomUpAxisOffset        { get { return -0.2f; } }
        public static float                 defaultMaxRandomUpAxisOffset        { get { return 0.0f; } }
        public static bool                  defaultRandomizeScale               { get { return false; } }
        public static float                 defaultMinRandomScale               { get { return 0.5f; } }
        public static float                 defaultMaxRandomScale               { get { return 1.0f; } }

        public static CurvePrefabDiff checkDiff(List<CurvePrefab> curvePrefabs)
        {
            int maxNumDiffs         = typeof(CurvePrefabDiff).GetFields().Length;
            CurvePrefabDiff diff    = new CurvePrefabDiff();
            int numPrefabs          = curvePrefabs.Count;

            for (int i = 0; i < numPrefabs; ++i)
            {
                var prefab = curvePrefabs[i];
                for (int j = i + 1; j < numPrefabs; ++j)
                {
                    var otherPrefab = curvePrefabs[j];

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
                    if (prefab.alignAxes != otherPrefab.alignAxes)
                    {
                        ++diffCount;
                        diff.alignAxes = true;
                    }
                    if (prefab.upAxis != otherPrefab.upAxis)
                    {
                        ++diffCount;
                        diff.upAxis = true;
                    }
                    if (prefab.invertUpAxis != otherPrefab.invertUpAxis)
                    {
                        ++diffCount;
                        diff.invertUpAxis = true;
                    }
                    if (prefab.alignUpAxisWhenProjected != otherPrefab.alignUpAxisWhenProjected)
                    {
                        ++diffCount;
                        diff.alignUpAxisWhenProjected = true;
                    }
                    if (prefab.forwardAxis != otherPrefab.forwardAxis)
                    {
                        ++diffCount;
                        diff.forwardAxis = true;
                    }
                    if (prefab.invertForwardAxis != otherPrefab.invertForwardAxis)
                    {
                        ++diffCount;
                        diff.invertForwardAxis = true;
                    }
                    if (prefab.jitterMode != otherPrefab.jitterMode)
                    {
                        ++diffCount;
                        diff.jitterMode = true;
                    }
                    if (prefab.jitter != otherPrefab.jitter)
                    {
                        ++diffCount;
                        diff.jitter = true;
                    }
                    if (prefab.minRandomJitter != otherPrefab.minRandomJitter)
                    {
                        ++diffCount;
                        diff.minRandomJitter = true;
                    }
                    if (prefab.maxRandomJitter != otherPrefab.maxRandomJitter)
                    {
                        ++diffCount;
                        diff.maxRandomJitter = true;
                    }
                    if (prefab.randomizeForwardAxisRotation != otherPrefab.randomizeForwardAxisRotation)
                    {
                        ++diffCount;
                        diff.randomizeForwardAxisRotation = true;
                    }
                    if (prefab.minRandomForwardAxisRotation != otherPrefab.minRandomForwardAxisRotation)
                    {
                        ++diffCount;
                        diff.minRandomForwardAxisRotation = true;
                    }
                    if (prefab.maxRandomForwardAxisRotation != otherPrefab.maxRandomForwardAxisRotation)
                    {
                        ++diffCount;
                        diff.maxRandomForwardAxisRotation = true;
                    }
                    if (prefab.randomizeUpAxisRotation != otherPrefab.randomizeUpAxisRotation)
                    {
                        ++diffCount;
                        diff.randomizeUpAxisRotation = true;
                    }
                    if (prefab.minRandomUpAxisRotation != otherPrefab.minRandomUpAxisRotation)
                    {
                        ++diffCount;
                        diff.minRandomUpAxisRotation = true;
                    }
                    if (prefab.maxRandomUpAxisRotation != otherPrefab.maxRandomUpAxisRotation)
                    {
                        ++diffCount;
                        diff.maxRandomUpAxisRotation = true;
                    }
                    if (prefab.upAxisOffsetMode != otherPrefab.upAxisOffsetMode)
                    {
                        ++diffCount;
                        diff.upAxisOffsetMode = true;
                    }
                    if (prefab.upAxisOffset != otherPrefab.upAxisOffset)
                    {
                        ++diffCount;
                        diff.upAxisOffset = true;
                    }
                    if (prefab.minRandomUpAxisOffset != otherPrefab.minRandomUpAxisOffset)
                    {
                        ++diffCount;
                        diff.minRandomUpAxisOffset = true;
                    }
                    if (prefab.maxRandomUpAxisOffset != otherPrefab.maxRandomUpAxisOffset)
                    {
                        ++diffCount;
                        diff.maxRandomUpAxisOffset = true;
                    }
                    if (prefab.randomizeScale != otherPrefab.randomizeScale)
                    {
                        ++diffCount;
                        diff.randomizeScale = true;
                    }
                    if (prefab.minRandomScale != otherPrefab.minRandomScale)
                    {
                        ++diffCount;
                        diff.minRandomScale = true;
                    }
                    if (prefab.maxRandomScale != otherPrefab.maxRandomScale)
                    {
                        ++diffCount;
                        diff.maxRandomScale = true;
                    }

                    if (diffCount == maxNumDiffs) return diff;
                }
            }

            return diff;
        }

        public static void getPluginPrefabs(List<CurvePrefab> curvePrefabs, List<PluginPrefab> pluginPrefabs)
        {
            pluginPrefabs.Clear();
            foreach (var curvePrefab in curvePrefabs)
                pluginPrefabs.Add(curvePrefab.pluginPrefab);
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
            used                            = defaultUsed;
            spawnChance                     = defaultSpawnChance;
            alignAxes                       = defaultAlignAxes;
            upAxis                          = defaultUpAxis;
            invertUpAxis                    = defaultInvertUpAxis;
            alignUpAxisWhenProjected        = defaultAlignUpAxisWhenProjected;
            forwardAxis                     = defaultForwardAxis;
            invertForwardAxis               = defaultInvertForwardAxis;
            jitterMode                      = defaultJitterMode;
            jitter                          = defaultJitter;
            minRandomJitter                 = defaultMinRandomJitter;
            maxRandomJitter                 = defaultMaxRandomJitter;
            randomizeForwardAxisRotation    = defaultRandomizeForwardAxisRotation;
            minRandomForwardAxisRotation    = defaultMinRandomForwardAxisRotation;
            maxRandomForwardAxisRotation    = defaultMaxRandomForwardAxisRotation;
            randomizeUpAxisRotation         = defaultRandomizeUpAxisRotation;
            minRandomUpAxisRotation         = defaultMinRandomUpAxisRotation;
            maxRandomUpAxisRotation         = defaultMaxRandomUpAxisRotation;
            upAxisOffsetMode                = defaultUpAxisOffsetMode;
            upAxisOffset                    = defaultUpAxisOffset;
            minRandomUpAxisOffset           = defaultMinRandomUpAxisOffset;
            maxRandomUpAxisOffset           = defaultMaxRandomUpAxisOffset;
            randomizeScale                  = defaultRandomizeScale;
            minRandomScale                  = defaultMinRandomScale;
            maxRandomScale                  = defaultMaxRandomScale;

            EditorUtility.SetDirty(this);
        }
    }
}
#endif
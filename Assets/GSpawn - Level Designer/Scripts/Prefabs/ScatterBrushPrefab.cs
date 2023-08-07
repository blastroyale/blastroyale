#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public struct ScatterBrushPrefabDiff
    {
        public bool used;
        public bool spawnChance;
        public bool volumeRadius;
        public bool alignAxis;
        public bool alignmentAxis;
        public bool invertAlignmentAxis;
        public bool offsetFromSurface;
        public bool embedInSurface;
        public bool alignToStroke;
        public bool strokeAlignmentAxis;
        public bool invertStrokeAlignmentAxis;
        public bool randomizeRotation;
        public bool rotationRandomizationAxis;
        public bool minRandomRotation;
        public bool maxRandomRotation;
        public bool randomizeScale;
        public bool minRandomScale;
        public bool maxRandomScale;
        public bool enableSlopeCheck;
        public bool minSlope;
        public bool maxSlope;
    }

    public enum ScatterBrushPrefabRotationRandomizationAxis
    {
        X = 0,
        Y,
        Z,

        SurfaceNormal,

        [Obsolete]
        UIMixed
    }

    public class ScatterBrushPrefab : ScriptableObject, IUIItemStateProvider
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
        private float               _volumeRadius           = defaultVolumeRadius;
        [SerializeField]
        private bool                _alignAxis              = defaultAlignAxis;
        [SerializeField]
        private FlexiAxis           _alignmentAxis          = defaultAlignmentAxis;
        [SerializeField]
        private bool                _invertAlignmentAxis    = defaultInvertAlignmentAxis;
        [SerializeField]
        private float               _offsetFromSurface      = defaultOffsetFromSurface;
        [SerializeField]
        private bool                _embedInSurface         = defaultEmbedInSurface;
        [SerializeField]
        private bool                _alignToStroke          = defaultAlignToStroke;
        [SerializeField]
        private FlexiAxis           _strokeAlignmentAxis    = defaultStrokeAlignmentAxis;
        [SerializeField]
        private bool                _invertStrokeAlignmentAxis  = defaultInvertStrokeAlignmentAxis;
        [SerializeField]
        private bool                _randomizeRotation          = defaultRandomizeRotation;
        [SerializeField]
        private ScatterBrushPrefabRotationRandomizationAxis   _rotationRandomizationAxis  = defaultRotationRandomizationAxis;
        [SerializeField]
        private float               _minRandomRotation          = defaultMinRandomRotation;
        [SerializeField]
        private float               _maxRandomRotation          = defaultMaxRandomRotation;
        [SerializeField]
        private bool                _randomizeScale             = defaultRandomizeScale;
        [SerializeField]
        private float               _minRandomScale             = defaultMinRandomScale;
        [SerializeField]
        private float               _maxRandomScale             = defaultMaxRandomScale;
        [SerializeField]
        private bool                _enableSlopeCheck           = defaultEnableSlopeCheck;
        [SerializeField]
        private float               _minSlope                   = defaultMinSlope;
        [SerializeField]
        private float               _maxSlope                   = defaultMaxSlope;

        public Texture2D            previewTexture              { get { return _preview.texture; } }
        public PluginPrefab         pluginPrefab
        {
            get { return _pluginPrefab; }
            set
            {
                _pluginPrefab = value;
                _preview.setPrefab(_pluginPrefab.prefabAsset);
                volumeRadius = calcFlatPrefabVolumeRadius();
                EditorUtility.SetDirty(this);
            }
        }
        public GameObject           prefabAsset                 { get { return pluginPrefab.prefabAsset; } }
        public PluginGuid           guid                        { get { return _guid; } }
        public bool                 uiSelected                  { get { return _uiSelected; } set { UndoEx.record(this); _uiSelected = value; } }
        public CopyPasteMode        uiCopyPasteMode             { get { return _uiCopyPasteMode; } set { _uiCopyPasteMode = value; } }
        public SerializedObject     serializedObject
        {
            get
            {
                if (_serializedObject == null) _serializedObject = new SerializedObject(this);
                return _serializedObject;
            }
        }

        public bool                 used                        { get { return _used; } set { UndoEx.record(this); _used = value; EditorUtility.SetDirty(this); } }
        public float                spawnChance                 { get { return _spawnChance; } set { UndoEx.record(this); _spawnChance = Mathf.Clamp01(value); EditorUtility.SetDirty(this); } }
        public float                volumeRadius                { get { return _volumeRadius; } set { UndoEx.record(this); _volumeRadius = Mathf.Max(value, 1e-2f); EditorUtility.SetDirty(this); } }
        public bool                 alignAxis                   { get { return _alignAxis; } set { UndoEx.record(this); _alignAxis = value; EditorUtility.SetDirty(this); } }
        public FlexiAxis            alignmentAxis               { get { return _alignmentAxis; } set { UndoEx.record(this); _alignmentAxis = value; EditorUtility.SetDirty(this); } }
        public bool                 invertAlignmentAxis         { get { return _invertAlignmentAxis; } set { UndoEx.record(this); _invertAlignmentAxis = value; EditorUtility.SetDirty(this); } }
        public float                offsetFromSurface           { get { return _offsetFromSurface; } set { UndoEx.record(this); _offsetFromSurface = value; EditorUtility.SetDirty(this); } }
        public bool                 embedInSurface              { get { return _embedInSurface; } set { UndoEx.record(this); _embedInSurface = value; EditorUtility.SetDirty(this); } }
        public bool                 alignToStroke               { get { return _alignToStroke; } set { UndoEx.record(this); _alignToStroke = value; EditorUtility.SetDirty(this); } }
        public FlexiAxis            strokeAlignmentAxis         { get { return _strokeAlignmentAxis; } set { UndoEx.record(this); _strokeAlignmentAxis = value; EditorUtility.SetDirty(this); } }
        public bool                 invertStrokeAlignmentAxis   { get { return _invertStrokeAlignmentAxis; } set { UndoEx.record(this); _invertStrokeAlignmentAxis = value; EditorUtility.SetDirty(this); } }
        public bool                 randomizeRotation           { get { return _randomizeRotation; } set { UndoEx.record(this); _randomizeRotation = value; EditorUtility.SetDirty(this); } }
        public ScatterBrushPrefabRotationRandomizationAxis rotationRandomizationAxis { get { return _rotationRandomizationAxis; } set { UndoEx.record(this); _rotationRandomizationAxis = value; EditorUtility.SetDirty(this); } }
        public float                minRandomRotation 
        { 
            get { return _minRandomRotation; } 
            set 
            { 
                UndoEx.record(this); 
                _minRandomRotation = Mathf.Clamp(value, 0.0f, 360.0f);
                if (_maxRandomRotation < _minRandomRotation) _maxRandomRotation = _minRandomRotation;
                EditorUtility.SetDirty(this); 
            } 
        }
        public float                maxRandomRotation 
        { 
            get { return _maxRandomRotation; } 
            set 
            { 
                UndoEx.record(this); 
                _maxRandomRotation = Mathf.Clamp(value, 0.0f, 360.0f);
                if (_minRandomRotation > _maxRandomRotation) _minRandomRotation = _maxRandomRotation;
                EditorUtility.SetDirty(this); 
            } 
        }
        public bool                 randomizeScale              { get { return _randomizeScale; } set { UndoEx.record(this); _randomizeScale = value; EditorUtility.SetDirty(this); } }
        public float                minRandomScale 
        { 
            get { return _minRandomScale; } 
            set 
            { 
                UndoEx.record(this); 
                _minRandomScale = Mathf.Max(0.1f, value);
                if (_maxRandomScale < _minRandomScale) _maxRandomScale = _minRandomScale;
                EditorUtility.SetDirty(this);
            } 
        }
        public float                maxRandomScale 
        { 
            get { return _maxRandomScale; } 
            set 
            { 
                UndoEx.record(this); 
                _maxRandomScale = Mathf.Max(0.1f, value);
                if (_minRandomScale > _maxRandomScale) _minRandomScale = _maxRandomScale;
                EditorUtility.SetDirty(this); 
            } 
        }
        public bool                 enableSlopeCheck            { get { return _enableSlopeCheck; } set { UndoEx.record(this); _enableSlopeCheck = value; EditorUtility.SetDirty(this); } }
        public float                minSlope 
        { 
            get { return _minSlope; } 
            set 
            { 
                UndoEx.record(this); 
                _minSlope = Mathf.Clamp(value, 0.0f, 90.0f);
                if (_maxSlope < _minSlope) _maxSlope = _minSlope;
                EditorUtility.SetDirty(this); 
            } 
        }
        public float                maxSlope 
        {
            get { return _maxSlope; } 
            set
            { 
                UndoEx.record(this); 
                _maxSlope = Mathf.Clamp(value, 0.0f, 90.0f);
                if (_minSlope > _maxSlope) _minSlope = _maxSlope;
                EditorUtility.SetDirty(this); 
            } 
        }

        public static bool          defaultUsed                         { get { return true; } }
        public static float         defaultSpawnChance                  { get { return 1.0f; } }
        public static float         defaultVolumeRadius                 { get { return 1.0f; } }
        public static bool          defaultAlignAxis                    { get { return false; } }
        public static FlexiAxis     defaultAlignmentAxis                { get { return FlexiAxis.Y; } }
        public static bool          defaultInvertAlignmentAxis          { get { return false; } }
        public static float         defaultOffsetFromSurface            { get { return 0.0f; } }
        public static bool          defaultEmbedInSurface               { get { return true; } }
        public static bool          defaultAlignToStroke                { get { return false; } }
        public static FlexiAxis     defaultStrokeAlignmentAxis          { get { return FlexiAxis.Longest; } }
        public static bool          defaultInvertStrokeAlignmentAxis    { get { return false; } }
        public static bool          defaultRandomizeRotation            { get { return true; } }
        public static ScatterBrushPrefabRotationRandomizationAxis defaultRotationRandomizationAxis { get { return ScatterBrushPrefabRotationRandomizationAxis.SurfaceNormal; } }
        public static float         defaultMinRandomRotation            { get { return 0.0f; } }
        public static float         defaultMaxRandomRotation            { get { return 360.0f; } }
        public static bool          defaultRandomizeScale               { get { return true; } }
        public static float         defaultMinRandomScale               { get { return 0.8f; } }
        public static float         defaultMaxRandomScale               { get { return 1.0f; } }
        public static bool          defaultEnableSlopeCheck             { get { return false; } }
        public static float         defaultMinSlope                     { get { return 0.0f; } }
        public static float         defaultMaxSlope                     { get { return 45.0f; } }

        public static ScatterBrushPrefabDiff checkDiff(List<ScatterBrushPrefab> brushPrefabs)
        {
            int maxNumDiffs             = typeof(ScatterBrushPrefabDiff).GetFields().Length;
            ScatterBrushPrefabDiff diff   = new ScatterBrushPrefabDiff();
            int numPrefabs              = brushPrefabs.Count;

            for (int i = 0; i < numPrefabs; ++i)
            {
                var prefab = brushPrefabs[i];
                for (int j = i + 1; j < numPrefabs; ++j)
                {
                    var otherPrefab = brushPrefabs[j];

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
                    if (prefab.volumeRadius != otherPrefab.volumeRadius)
                    {
                        ++diffCount;
                        diff.volumeRadius = true;
                    }
                    if (prefab.alignAxis != otherPrefab.alignAxis)
                    {
                        ++diffCount;
                        diff.alignAxis = true;
                    }
                    if (prefab.alignmentAxis != otherPrefab.alignmentAxis)
                    {
                        ++diffCount;
                        diff.alignmentAxis = true;
                    }
                    if (prefab.invertAlignmentAxis != otherPrefab.invertAlignmentAxis)
                    {
                        ++diffCount;
                        diff.invertAlignmentAxis = true;
                    }
                    if (prefab.offsetFromSurface != otherPrefab.offsetFromSurface)
                    {
                        ++diffCount;
                        diff.offsetFromSurface = true;
                    }
                    if (prefab.embedInSurface != otherPrefab.embedInSurface)
                    {
                        ++diffCount;
                        diff.embedInSurface = true;
                    }
                    if (prefab.alignToStroke != otherPrefab.alignToStroke)
                    {
                        ++diffCount;
                        diff.alignToStroke = true;
                    }
                    if (prefab.strokeAlignmentAxis != otherPrefab.strokeAlignmentAxis)
                    {
                        ++diffCount;
                        diff.strokeAlignmentAxis = true;
                    }
                    if (prefab.invertStrokeAlignmentAxis != otherPrefab.invertStrokeAlignmentAxis)
                    {
                        ++diffCount;
                        diff.invertStrokeAlignmentAxis = true;
                    }
                    if (prefab.randomizeRotation != otherPrefab.randomizeRotation)
                    {
                        ++diffCount;
                        diff.randomizeRotation = true;
                    }
                    if (prefab.rotationRandomizationAxis != otherPrefab.rotationRandomizationAxis)
                    {
                        ++diffCount;
                        diff.rotationRandomizationAxis = true;
                    }
                    if (prefab.minRandomRotation != otherPrefab.minRandomRotation)
                    {
                        ++diffCount;
                        diff.minRandomRotation = true;
                    }
                    if (prefab.maxRandomRotation != otherPrefab.maxRandomRotation)
                    {
                        ++diffCount;
                        diff.maxRandomRotation = true;
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
                    if (prefab.enableSlopeCheck != otherPrefab.enableSlopeCheck)
                    {
                        ++diffCount;
                        diff.enableSlopeCheck = true;
                    }
                    if (prefab.minSlope != otherPrefab.minSlope)
                    {
                        ++diffCount;
                        diff.minSlope = true;
                    }
                    if (prefab.maxSlope != otherPrefab.maxSlope)
                    {
                        ++diffCount;
                        diff.maxSlope = true;
                    }

                    if (diffCount == maxNumDiffs) return diff;
                }
            }

            return diff;
        }

        public static void getPluginPrefabs(List<ScatterBrushPrefab> scatterBrushPrefabs, List<PluginPrefab> pluginPrefabs)
        {
            pluginPrefabs.Clear();
            foreach (var brushPrefab in scatterBrushPrefabs)
                pluginPrefabs.Add(brushPrefab.pluginPrefab);
        }

        public void usePrefabVolumeRadius()
        {
            volumeRadius = calcPrefabVolumeRadius();
        }

        public void useFlatPrefabVolumeRadius()
        {
            volumeRadius = calcFlatPrefabVolumeRadius();
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
            used                        = defaultUsed;
            spawnChance                 = defaultSpawnChance;
            volumeRadius                = calcPrefabVolumeRadius();
            alignAxis                   = defaultAlignAxis;
            alignmentAxis               = defaultAlignmentAxis;
            invertAlignmentAxis         = defaultInvertAlignmentAxis;
            offsetFromSurface           = defaultOffsetFromSurface;
            embedInSurface              = defaultEmbedInSurface;
            alignToStroke               = defaultAlignToStroke;
            strokeAlignmentAxis         = defaultStrokeAlignmentAxis;
            randomizeRotation           = defaultRandomizeRotation;
            rotationRandomizationAxis   = defaultRotationRandomizationAxis;
            minRandomRotation           = defaultMinRandomRotation;
            maxRandomRotation           = defaultMaxRandomRotation;
            randomizeScale              = defaultRandomizeScale;
            minRandomScale              = defaultMinRandomScale;
            maxRandomScale              = defaultMaxRandomScale;
            enableSlopeCheck            = defaultEnableSlopeCheck;
            minSlope                    = defaultMinSlope;
            maxSlope                    = defaultMaxSlope;

            EditorUtility.SetDirty(this);
        }

        private float calcPrefabVolumeRadius()
        {
            if (pluginPrefab != null) return pluginPrefab.modelSize.magnitude * 0.5f;
            return 1.0f;
        }

        private float calcFlatPrefabVolumeRadius()
        {
            if (pluginPrefab != null) return pluginPrefab.modelSize.replace(1, 0.0f).magnitude * 0.5f;
            return 1.0f;
        }
    }
}
#endif
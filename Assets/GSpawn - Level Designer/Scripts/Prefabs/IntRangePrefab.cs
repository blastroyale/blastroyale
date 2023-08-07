#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public struct IntRangePrefabDiff
    {
        public bool used;
        public bool min;
        public bool max;
    }

    public class IntRangePrefab : ScriptableObject, IUIItemStateProvider
    {
        private SerializedObject    _serializedObject;

        [SerializeField]
        private PluginGuid          _guid               = new PluginGuid(Guid.NewGuid());
        [SerializeField]
        private PluginPrefab        _pluginPrefab;
        [SerializeField]
        private PrefabPreview       _preview            = new PrefabPreview();
        [SerializeField]
        private bool                _uiSelected         = false;
        [NonSerialized]
        private CopyPasteMode       _uiCopyPasteMode    = CopyPasteMode.None;

        [SerializeField]
        private bool                _used               = defaultUsed;
        [SerializeField]
        private int                 _min                = defaultMin;
        [SerializeField]
        private int                 _max                = defaultMax;

        public Texture2D            previewTexture      { get { return _preview.texture; } }
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
        public GameObject           prefabAsset         { get { return pluginPrefab != null ? pluginPrefab.prefabAsset : null; } }
        public PluginGuid           guid                { get { return _guid; } }
        public bool                 uiSelected          { get { return _uiSelected; } set { UndoEx.record(this); _uiSelected = value; } }
        public bool                 used                { get { return _used; } set { UndoEx.record(this); _used = value; EditorUtility.SetDirty(this); } }
        public int                  min 
        { 
            get { return _min; } 
            set 
            { 
                UndoEx.record(this); 
                _min = value;
                if (_max < _min) _max = _min;
                EditorUtility.SetDirty(this);
            } 
        }
        public int                  max
        { 
            get { return _max; }
            set
            { 
                UndoEx.record(this); 
                _max = value;
                if (_min > _max) _min = _max;
                EditorUtility.SetDirty(this);
            } 
        }
        public CopyPasteMode        uiCopyPasteMode     { get { return _uiCopyPasteMode; } set { _uiCopyPasteMode = value; } }
        public SerializedObject     serializedObject
        {
            get
            {
                if (_serializedObject == null) _serializedObject = new SerializedObject(this);
                return _serializedObject;
            }
        }

        public static bool          defaultUsed         { get { return true; } }
        public static int           defaultMin          { get { return 0; } }
        public static int           defaultMax          { get { return 1; } }

        public static IntRangePrefabDiff checkDiff(List<IntRangePrefab> intRangePrefabs)
        {
            int maxNumDiffs         = typeof(IntRangePrefabDiff).GetFields().Length;
            IntRangePrefabDiff diff = new IntRangePrefabDiff();
            int numPrefabs          = intRangePrefabs.Count;

            for (int i = 0; i < numPrefabs; ++i)
            {
                var prefab = intRangePrefabs[i];
                for (int j = i + 1; j < numPrefabs; ++j)
                {
                    var otherPrefab = intRangePrefabs[j];

                    int diffCount = 0;

                    if (prefab.used != otherPrefab.used)
                    {
                        ++diffCount;
                        diff.used = true;
                    }
                    if (prefab.min != otherPrefab.min)
                    {
                        ++diffCount;
                        diff.min = true;
                    }
                    if (prefab.max != otherPrefab.max)
                    {
                        ++diffCount;
                        diff.max = true;
                    }

                    if (diffCount == maxNumDiffs) return diff;
                }
            }

            return diff;
        }

        public static void getPluginPrefabs(List<IntRangePrefab> irPrefabs, List<PluginPrefab> pluginPrefabs)
        {
            pluginPrefabs.Clear();
            foreach (var irPrefab in irPrefabs)
                pluginPrefabs.Add(irPrefab.pluginPrefab);
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

        public bool isValueInRange(int val)
        {
            return val >= _min && val <= _max;
        }

        public void useDefaults()
        {
            used    = defaultUsed;
            min     = defaultMin;
            max     = defaultMax;
            EditorUtility.SetDirty(this);
        }
    }
}
#endif
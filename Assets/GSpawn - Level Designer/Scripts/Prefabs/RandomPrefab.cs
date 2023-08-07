#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public struct RandomPrefabDiff
    {
        public bool used;
        public bool probability;
    }

    public class RandomPrefab : ScriptableObject, IUIItemStateProvider
    {
        private SerializedObject    _serializedObject;

        [SerializeField]
        private PluginGuid          _guid               = new PluginGuid(Guid.NewGuid());
        [SerializeField]
        private PluginPrefab        _pluginPrefab;
        [SerializeField]
        private bool                _used               = defaultUsed;
        [SerializeField]
        private float               _probability        = defaultProbability;
        [SerializeField]
        private PrefabPreview       _preview            = new PrefabPreview();
        [SerializeField]
        private bool                _uiSelected         = false;
        [NonSerialized]
        private CopyPasteMode       _uiCopyPasteMode    = CopyPasteMode.None;

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
        public bool                 used                { get { return _used; } set { UndoEx.record(this); _used = value; EditorUtility.SetDirty(this); } }
        public float                probability         { get { return _probability; } set { UndoEx.record(this); _probability = Mathf.Clamp01(value); EditorUtility.SetDirty(this); } }
        public PluginGuid           guid                { get { return _guid; } }
        public bool                 uiSelected          { get { return _uiSelected; } set { UndoEx.record(this); _uiSelected = value; EditorUtility.SetDirty(this); } }
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
        public static float         defaultProbability  { get { return 1.0f; } }

        public static RandomPrefabDiff checkDiff(List<RandomPrefab> randomPrefabs)
        {
            int maxNumDiffs         = typeof(RandomPrefabDiff).GetFields().Length;
            RandomPrefabDiff diff   = new RandomPrefabDiff();
            int numPrefabs          = randomPrefabs.Count;

            for (int i = 0; i < numPrefabs; ++i)
            {
                var prefab = randomPrefabs[i];
                for (int j = i + 1; j < numPrefabs; ++j)
                {
                    var otherPrefab = randomPrefabs[j];

                    int diffCount = 0;

                    if (prefab.used != otherPrefab.used)
                    {
                        ++diffCount;
                        diff.used = true;
                    }
                    if (prefab.probability != otherPrefab.probability)
                    {
                        ++diffCount;
                        diff.probability = true;
                    }

                    if (diffCount == maxNumDiffs) return diff;
                }
            }

            return diff;
        }

        public static void getPluginPrefabs(List<RandomPrefab> randomPrefabs, List<PluginPrefab> pluginPrefabs)
        {
            pluginPrefabs.Clear();
            foreach (var randomPrefab in randomPrefabs)
                pluginPrefabs.Add(randomPrefab.pluginPrefab);
        }

        public void useDefaults()
        {
            used        = defaultUsed;
            probability = defaultProbability;

            EditorUtility.SetDirty(this);
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
    }
}
#endif
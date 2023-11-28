#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public struct ModularWallPrefabDiff
    {
        public bool used;
        public bool spawnChance;
    }

    public class ModularWallPrefab : ScriptableObject, IUIItemStateProvider
    {
        private SerializedObject _serializedObject;

        [SerializeField]
        private PluginGuid      _guid               = new PluginGuid(Guid.NewGuid());
        [SerializeField]
        private PluginPrefab    _pluginPrefab;

        [SerializeField]
        private PrefabPreview   _preview            = new PrefabPreview();
        [SerializeField]
        private bool            _uiSelected         = false;
        [NonSerialized]
        private CopyPasteMode   _uiCopyPasteMode    = CopyPasteMode.None;

        [SerializeField]
        private bool            _used               = defaultUsed;
        [SerializeField]
        private float           _spawnChance        = defaultSpawnChance;

        public Texture2D        previewTexture      { get { return _preview.texture; } }
        public PluginPrefab     pluginPrefab
        {
            get { return _pluginPrefab; }
            set
            {
                _pluginPrefab = value;
                _preview.setPrefab(_pluginPrefab.prefabAsset);
                EditorUtility.SetDirty(this);
            }
        }
        public GameObject       prefabAsset         { get { return pluginPrefab.prefabAsset; } }
        public PluginGuid       guid                { get { return _guid; } }
        public bool             uiSelected          { get { return _uiSelected; } set { UndoEx.record(this); _uiSelected = value; } }
        public CopyPasteMode    uiCopyPasteMode     { get { return _uiCopyPasteMode; } set { _uiCopyPasteMode = value; } }
        public SerializedObject serializedObject
        {
            get
            {
                if (_serializedObject == null) _serializedObject = new SerializedObject(this);
                return _serializedObject;
            }
        }
        public bool             used                { get { return _used; } set { UndoEx.record(this); _used = value; EditorUtility.SetDirty(this); } }
        public float            spawnChance         { get { return _spawnChance; } set { UndoEx.record(this); _spawnChance = Mathf.Clamp(value, 0.0f, 1.0f); EditorUtility.SetDirty(this); } }

        public static bool      defaultUsed         { get { return true; } }
        public static float     defaultSpawnChance  { get { return 1.0f; } }

        public static ModularWallPrefabDiff checkDiff(List<ModularWallPrefab> modularWallPrefabs)
        {
            int maxNumDiffs = typeof(ModularWallPrefabDiff).GetFields().Length;
            ModularWallPrefabDiff diff = new ModularWallPrefabDiff();
            int numPrefabs = modularWallPrefabs.Count;

            for (int i = 0; i < numPrefabs; ++i)
            {
                var prefab = modularWallPrefabs[i];
                for (int j = i + 1; j < numPrefabs; ++j)
                {
                    var otherPrefab = modularWallPrefabs[j];

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

                    if (diffCount == maxNumDiffs) return diff;
                }
            }

            return diff;
        }

        public static void getPluginPrefabs(List<ModularWallPrefab> modularWallPrefabs, List<PluginPrefab> pluginPrefabs)
        {
            pluginPrefabs.Clear();
            foreach (var mdWallPrefab in modularWallPrefabs)
                pluginPrefabs.Add(mdWallPrefab.pluginPrefab);
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
            used        = defaultUsed;
            spawnChance = defaultSpawnChance;

            EditorUtility.SetDirty(this);
        }
    }
}
#endif
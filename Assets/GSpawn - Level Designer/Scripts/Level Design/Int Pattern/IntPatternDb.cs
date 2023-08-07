#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class IntPatternDb : ScriptableObject
    {
        private static IntPatternDb     _instance;

        [SerializeField]
        private List<IntPattern>        _patterns           = new List<IntPattern>();
        [SerializeField]
        private IntPattern              _defaultPattern     = null;
        [NonSerialized]
        private IntPatternDbUI          _ui;

        [NonSerialized]
        private List<IntPattern>        _patternBuffer      = new List<IntPattern>();
        [NonSerialized]
        private List<string>            _stringBuffer       = new List<string>();

        public int                      numPatterns         { get { return _patterns.Count; } }
        public IntPattern               defaultPattern      { get { return _defaultPattern; } }
        public IntPatternDbUI           ui
        {
            get
            {
                if (_ui == null)
                    _ui = AssetDbEx.loadScriptableObject<IntPatternDbUI>(PluginFolders.intPatternProfiles);

                return _ui;
            }
        }

        public static IntPatternDb      instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = AssetDbEx.loadScriptableObject<IntPatternDb>(PluginFolders.intPatternProfiles);
                    UndoEx.saveEnabledState();
                    UndoEx.enabled = false;
                    _instance.createDefaultPattern();
                    UndoEx.restoreEnabledState();
                } 
                return _instance;
            }
        }
        public static bool              exists              { get { return _instance != null; } }
        public static string            defaultPatternName  { get { return "Default"; } }

        public void onPostProcessAllAssets()
        {
            _patterns.RemoveAll(item => item == null);
            createDefaultPattern();
        }

        public IntPattern createPattern(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            getPatternNames(_stringBuffer, null);
            name = UniqueNameGen.generate(name, _stringBuffer);

            UndoEx.saveEnabledState();
            UndoEx.enabled      = false;

            IntPattern pattern  = UndoEx.createScriptableObject<IntPattern>();
            pattern.patternName = name;
            pattern.name        = name;

            UndoEx.record(this);
            _patterns.Add(pattern);

            AssetDbEx.addObjectToAsset(pattern, this);
            EditorUtility.SetDirty(this);

            UndoEx.restoreEnabledState();

            return pattern;
        }

        public void renamePattern(IntPattern pattern, string newName)
        {
            if (pattern != defaultPattern && 
                !string.IsNullOrEmpty(newName) && 
                containsPattern(pattern) && pattern.patternName != newName)
            {
                getPatternNames(_stringBuffer, pattern.patternName);
                UndoEx.record(this);
                pattern.patternName = UniqueNameGen.generate(newName, _stringBuffer);
                pattern.name        = pattern.patternName;
            }
        }

        public void deletePattern(IntPattern pattern)
        {
            if (pattern != null && pattern != defaultPattern)
            {
                UndoEx.record(this);
                if (containsPattern(pattern))
                {
                    _patterns.Remove(pattern);
                    UndoEx.destroyObjectImmediate(pattern);
                }
            }
        }

        public void deletePatterns(List<IntPattern> patterns)
        {
            if (patterns.Count != 0)
            {
                UndoEx.record(this);
                _patternBuffer.Clear();

                foreach (var pattern in patterns)
                {
                    if (pattern != defaultPattern && containsPattern(pattern))
                    {
                        _patterns.Remove(pattern);
                        _patternBuffer.Add(pattern);
                    }
                }

                foreach (var pattern in _patternBuffer)
                    UndoEx.destroyObjectImmediate(pattern);
            }
        }

        public bool containsPattern(IntPattern pattern)
        {
            return _patterns.Contains(pattern);
        }

        public int indexOf(IntPattern pattern)
        {
            if (pattern == null) return -1;
            return _patterns.IndexOf(pattern);
        }

        public void getPatternNames(List<string> names, string ignoredName)
        {
            names.Clear();
            foreach (var pattern in _patterns)
            {
                if (pattern.patternName != ignoredName)
                    names.Add(pattern.patternName);
            }
        }

        public void getPatterns(List<IntPattern> patterns)
        {
            patterns.Clear();
            patterns.AddRange(_patterns);
        }

        public IntPattern findPattern(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            foreach (var pattern in _patterns)
            {
                if (pattern.patternName == name) return pattern;
            }

            return null;
        }

        private void createDefaultPattern()
        {
            if (_defaultPattern == null)
            {
                _defaultPattern = createPattern(defaultPatternName);
                _defaultPattern.compile("add(1);");
            }
        }

        private void deleteAllPatterns()
        {
            if (_patterns.Count != 0)
            {
                UndoEx.record(this);
                _patternBuffer.Clear();
                _patternBuffer.AddRange(_patterns);

                _patterns.Clear();
                _defaultPattern = null;

                foreach (var pattern in _patternBuffer)
                    UndoEx.destroyObjectImmediate(pattern);
            }
        }

        private void OnDestroy()
        {
            deleteAllPatterns();
            ScriptableObjectEx.destroyImmediate(_ui);
        }
    }
}
#endif
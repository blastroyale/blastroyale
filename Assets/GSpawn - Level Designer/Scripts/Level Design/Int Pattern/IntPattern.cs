#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class IntPattern : ScriptableObject, IUIItemStateProvider
    {
        [SerializeField]
        private PluginGuid      _guid               = new PluginGuid(Guid.NewGuid());
        [SerializeField]
        private string          _text               = string.Empty;
        [SerializeField]
        private List<int>       _values             = new List<int>();
        [SerializeField]
        private string          _patternName        = string.Empty;

        [SerializeField]
        private bool            _uiSelected         = false;
        [NonSerialized]
        private CopyPasteMode   _uiCopyPasteMode    = CopyPasteMode.None;

        public PluginGuid       guid                { get { return _guid; } }
        public string           text                { get { return _text; } }
        public int              numValues           { get { return _values.Count; } }
        public string           patternName         { get { return _patternName; } set { if (!string.IsNullOrEmpty(value)) { UndoEx.record(this); _patternName = value; } } }
        public bool             uiSelected          { get { return _uiSelected; } set { UndoEx.record(this); _uiSelected = value; } }
        public CopyPasteMode    uiCopyPasteMode     { get { return _uiCopyPasteMode; } set { _uiCopyPasteMode = value; } }

        public void getValues(List<int> values)
        {
            values.Clear();
            values.AddRange(_values);
        }

        public bool compile(string text)
        {
            _text = text;
            _values.Clear();
            EditorUtility.SetDirty(this);

            var lexer = new IntPatternLexer();
            if (!lexer.lex(text)) return false;
            var parser = new IntPatternParser();
            if (!parser.parse(lexer)) return false;

            var visitor = new IntPatternTreeVisitor();
            visitor.visitRoot(parser.rootStatement);
            visitor.getPatternValues(_values);

            EditorUtility.SetDirty(this);
            return true;
        }
    }
}
#endif